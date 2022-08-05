/*
Copyright 2022 Equinor ASA

This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using AngleSharp.Common;
using VDS.RDF.JsonLd;
using static VersionedObject.JsonLdHelper;

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
            var inputList = input.GetInputGraphAsEntities().ToImmutableList();
            var persistentEntities = GetAllPersistentIris(input, existing).ToImmutableHashSet();
            var reifiedInput = inputList.ReifyAllEdges(persistentEntities).ToImmutableList();
            var existingList = existing.GetExistingGraphAsEntities(persistentEntities).ToImmutableList();
            var updateList = reifiedInput.MakeUpdateList(existingList).ToImmutableList();
            var versionedIriMap = existingList.MakeUpdatedPersistentIriMap(updateList);
            var versionedUpdateList = updateList.UpdateEdgeIris(versionedIriMap).ToImmutableList();
            var deleteList = MakeDeleteList(reifiedInput, existingList).ToImmutableList();
            return CreateUpdateJObject(versionedUpdateList, deleteList);
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
        public static IEnumerable<VersionedObject> GetExistingGraphAsEntities(this JObject jsonld, ImmutableHashSet<IRIReference> persistentUris) =>
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
        /// <summary>
        /// Called on input (and existing) data before comparison to get all persistent IRIs
        /// </summary>
        /// <param name="input"></param>
        /// <param name="existing"></param>
        /// <returns></returns>
        public static IEnumerable<IRIReference> GetAllPersistentIris(JObject input, JObject existing) =>
            (from x in input.GetAllEntityIds()
             select new IRIReference(x))
                .Union(
                    from s in existing
                        .GetAllEntityIds()
                    select new VersionedIRIReference(s).PersistentIRI
                );

        /// <summary>
        /// Helper function for getting persistent IRIs from all objects
        /// It needs to use Uri because we dont know if its a versioned IRI
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="InvalidJsonLdException"></exception>
        public static IEnumerable<Uri> GetAllEntityIds(this JObject input) =>
            input
                .RemoveContext()
                .GetJsonLdGraph().Values<JObject>()
                .Select(s => (obj: s, id: s?.SelectToken("@id")?.Value<string>()))
                .Select(s => s.id ?? throw new InvalidJsonLdException($"No @id element found in JObject {s.obj}"))
                .Select(s => new Uri(s));

        public static ImmutableDictionary<IRIReference, VersionedIRIReference> MakeUpdatedPersistentIriMap(
            this IEnumerable<VersionedObject> existingObjects, IEnumerable<VersionedObject> updateObjects) =>
            existingObjects.MakePersistentIriMap()
                .UpdateIriDictionary(updateObjects.MakePersistentIriMap());


        ///<summary>
        /// Makes an updated list of versioned objects, removing those that are being updated
        /// </summary>
        internal static ImmutableDictionary<IRIReference, VersionedIRIReference> UpdateIriDictionary(
            this ImmutableDictionary<IRIReference, VersionedIRIReference> existingDict,
            ImmutableDictionary<IRIReference, VersionedIRIReference> updateDict) =>
            updateDict.Aggregate(existingDict, (dict, u) => dict.SetItem(u.Key, u.Value))
                .ToImmutableDictionary();

        /// <summary>
        /// Called on list of versioned objects to get a mapping from persistent to versioned IRIs
        /// Similar to a HEAD mapping in aspect api
        /// </summary>
        /// <returns></returns>
        public static ImmutableDictionary<IRIReference, VersionedIRIReference> MakePersistentIriMap(
            this IEnumerable<VersionedObject> objectList) =>
            ImmutableDictionary.CreateRange(
                from obj in objectList
                select new KeyValuePair<IRIReference, VersionedIRIReference>(obj.GetPersistentIRI(), obj.VersionedIri)
                );

        public static VersionedObject CreateVersionedIRIs(this VersionedObject orig, ImmutableDictionary<IRIReference, VersionedIRIReference> map) =>
            new(new PersistentObjectData(orig.Object, map), orig.WasDerivedFrom);

        public static IEnumerable<VersionedObject> UpdateEdgeIris(this IEnumerable<VersionedObject> updateList,
            ImmutableDictionary<IRIReference, VersionedIRIReference> map) =>
            from obj in updateList
            select obj.CreateVersionedIRIs(map);


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
            var newObjects =
                from i in oldNewMap
                where !i.existing.Any()
                select new VersionedObject(i.input);

            var updatedObjects =
                from i in oldNewMap
                where i.existing.Any() && !i.input.Equals(i.existing.First().Object)
                select new VersionedObject(i.input, i.existing.First().VersionedIri);

            return newObjects.Union(updatedObjects);
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