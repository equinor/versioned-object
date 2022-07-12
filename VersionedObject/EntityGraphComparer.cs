/*
Copyright 2022 Equinor ASA

This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using VDS.RDF.JsonLd;

namespace VersionedObject
{
    public static class EntityGraphComparer
    {
        /// <summary>
        /// Creates update object for use with the put method on the Aspect API graph endpoint
        /// Takes in full json-ld objects of input and existing snapshot
        /// Assumes input is a complete version of the new graph, so lacking entries in input are assumed
        /// not relevant anymore
        /// </summary>
        public static JObject HandleGraphCompleteUpdate(this JObject input, JObject existing) =>
            HandleGraphUpdate(input, existing, MakeDeleteList);


        /// <summary>
        /// Used for handling new entries but not a complete version of the graph
        /// </summary>
        /// <param name="input"></param>
        /// <param name="existing"></param>
        /// <returns></returns>
        public static JObject HandleGraphEntries(this JObject input, JObject existing) =>
            HandleGraphUpdate(input, existing, (_, _) => new List<VersionedIRIReference>());


        private static JObject HandleGraphUpdate(this JObject input, JObject existing, Func<IEnumerable<PersistentObjectData>, IEnumerable<VersionedObject>, IEnumerable<VersionedIRIReference>> MakeDeleteList)
        {
            var inputList = input.GetInputGraphAsEntities();
            var persistentEntities = GetAllPersistentIris(input, existing);
            var reifiedInput = inputList.ReifyAllEdges(persistentEntities);
            var existingList = existing.GetExistingGraphAsEntities(persistentEntities);
            var updateList = reifiedInput.MakeUpdateList(existingList);
            var deleteList = MakeDeleteList(reifiedInput, existingList);
            return CreateUpdateJObject(updateList, deleteList);
        }
        /// <summary>
        /// Returns all IRIs to objects not inside this entity. These should be reified edges
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IEnumerable<PersistentObjectData> ReifyAllEdges(this IEnumerable<PersistentObjectData> updateList, IEnumerable<IRIReference> persistentIris)
        => updateList.SelectMany(obj => obj.ReifyNodeEdges(persistentIris));

        /// <summary>
        /// Translates JSON-LD coming from Aspect-API (so using versioned IRIs)
        /// into list of VersionedObject objects
        /// The list of persistent iris is used to translate the versioned IRIs inside the json so it can be matched with the incoming json
        /// </summary>
        /// <param name="jsonld"></param>
        /// <param name="persistentUris"></param>
        public static IEnumerable<VersionedObject> GetExistingGraphAsEntities(this JObject jsonld, IEnumerable<IRIReference> persistentUris) =>
            jsonld.RemoveContext()
                .GetJsonLdGraph()
                .Values<JObject>()
                .Select(s => s ?? throw new InvalidJsonLdException("Null value found when expected existing versioned graph entity"))
                .Select(x =>
                    new VersionedObject(
                        x.GetVersionedIRIReference(),
                        x,
                        persistentUris)
                    );

        public static IEnumerable<PersistentObjectData> GetInputGraphAsEntities(this JObject jsonld) =>
            jsonld.RemoveContext()
                .GetJsonLdGraph()
                .Values<JObject>()
                .Select(s => s ?? throw new InvalidJsonLdException("Null value found when expected input graph entity"))
                .Select(x =>
                    new PersistentObjectData(
                        x.GetIRIReference(),
                        x)
                    );

        public static JArray GetJsonLdGraph(this JObject jsonld)
        {
            if (jsonld.ContainsKey("@graph") && jsonld.SelectToken("@graph") != null)
            {
                var graphArray = jsonld.SelectToken("@graph")?.Value<JArray>();
                return graphArray ??
                       throw new InvalidJsonLdException(
                           "No value found in the @graph element of the JSON-LD graph");
            }
            else if (jsonld.HasValues)
            {
                return new JArray() { jsonld };
            }
            else
            {
                return new JArray();
            }
        }

        public static JObject RemoveContext(this JObject jsonld)
        {
            var expanderOptions = new JsonLdProcessorOptions()
            {
                ProcessingMode = VDS.RDF.JsonLd.Syntax.JsonLdProcessingMode.JsonLd11

            };
            var expanderContext = jsonld.SelectToken("@context");

            if (expanderContext is not null && expanderContext.HasValues)
                expanderOptions.ExpandContext = expanderContext;

            var compacterContext = new JObject();
            var compacterOptions = new JsonLdProcessorOptions();

            var expandedList = JsonLdProcessor.Expand(jsonld, expanderOptions);
            return JsonLdProcessor.Compact(
                expandedList,
                compacterContext,
                compacterOptions
            );
        }
        public static IEnumerable<IRIReference> GetAllPersistentIris(JObject input, JObject existing) =>
            input
                .GetAllEntityIds()
                .Select(x => new IRIReference(x))
                .Union(
                    existing
                        .GetAllEntityIds()
                        .Select(s => new VersionedIRIReference(s).PersistentIRI)
                );
        public static IEnumerable<Uri> GetAllEntityIds(this JObject input) =>
            input
                .RemoveContext()
                .GetJsonLdGraph().Values<JObject>()
                .Select(s => (obj: s, id: s?.SelectToken("@id")?.Value<string>()))
                .Select(s => s.id ?? throw new InvalidJsonLdException($"No @id element found in JObject {s.obj}"))
                .Select(s => new Uri(s));

        public static JObject CreateUpdateJObject(IEnumerable<VersionedObject> updateList,
            IEnumerable<VersionedIRIReference> deleteList) =>
            new()
            {
                ["update"] = new JObject()
                {
                    ["@graph"] = updateList.MakeUpdateGraph(),
                    ["@context"] = new JObject() { ["@version"] = "1.1" }
                },
                ["delete"] = deleteList.MakeDeleteGraph(),
            };

        public static JArray MakeUpdateGraph(this IEnumerable<VersionedObject> updateList) =>
            new(updateList.Select(o => o.ToJObject()));

        public static IEnumerable<VersionedObject> MakeUpdateList(this IEnumerable<PersistentObjectData> inputList,
            IEnumerable<VersionedObject> existingList)
        {
            var oldNewMap = inputList.Select(
                i => (
                    input: i,
                    existing: existingList.Where(x => i.SamePersistentIRI(x.Object))
                )
            );
            return oldNewMap
                .Where(i => !i.existing.Any())
                .Select(i => new VersionedObject(i.input))
                .Union(
                    oldNewMap
                        .Where(i => i.existing.Any()
                                    && !i.input.Equals(i.existing.First().Object))
                        .Select(i => new VersionedObject(i.input, i.existing.First().VersionedIri))
                );
        }

        public static JArray MakeDeleteGraph(this IEnumerable<VersionedIRIReference> deleteList) =>
            new(deleteList.Select(x => x.ToJValue()));

        /// <summary>
        /// Creates a list of objects that should  be deleted from the aspect api, based on an assumed complete list of "new objects"
        /// </summary>
        public static IEnumerable<VersionedIRIReference> MakeDeleteList(this IEnumerable<PersistentObjectData> input,
            IEnumerable<VersionedObject> existing) =>
            existing
                .Where(x => !input.Any(i => x.Object.SamePersistentIRI(i)))
                .Select(x => x.VersionedIri);
    }
}