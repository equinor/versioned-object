﻿using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF.JsonLd;

namespace VersionedObject
{
    public static class EntityGraphComparer
    {


        public static JObject? MakeGraphUpdate(this JObject input, JObject existing) =>
            input.MakeGraphUpdate(existing, x => x);
        /// <summary>
        /// Creates update object for use with the put method on the Aspect API graph endpoint
        /// Takes in full json-ld objects of input and existing snapshot
        /// </summary>
        public static JObject? MakeGraphUpdate(this JObject input, JObject existing, Func<IEnumerable<AspectObject>?, IEnumerable<AspectObject>?> inputModifier)
        {
            var inputList = inputModifier(input.GetInputGraphAsEntities());
            var persistentEntities = GetAllPersistentIris(input, existing);
            //var reifiedInput = inputList.reifyEdges(input, existing);
            var existingList = existing.GetExistingGraphAsEntities(persistentEntities);
            var updateList = inputList.MakeUpdateList(existingList);
            var allEntities = existingList.Union(updateList.Values);

            var deleteList = inputList.MakeDeleteList(existingList);
            //return CreateUpdateJObject(updateList, deleteList, x => x.AddVersionToUris(allEntities));
            return CreateUpdateJObject(updateList.Values, deleteList, x => x);
        }

        /// <summary>
        /// Returns all IRIs to objects not inside this entity. These should be reified edges
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IEnumerable<VersionedIRIReference> GetExternalIriReferences(this IDictionary<IRIReference, VersionedObject> updateList)
        {
            return updateList.Values.Select(v => v.GetA)
        }

        /// <summary>
        /// Translates JSON-LD coming from Aspect-API (so using versioned IRIs)
        /// into list of VersionedObject objects
        /// The list of persistent iris is used to translate the versioned IRIs inside the json so it can be matched with the incoming json
        /// </summary>
        /// <param name="jsonld"></param>
        /// <param name="persistentUris"></param>
        public static IEnumerable<VersionedObject>? GetExistingGraphAsEntities(this JObject jsonld, IEnumerable<IRIReference> persistentUris) =>
            jsonld.RemoveContext()
                .GetJsonLdGraph()?
                .Values<JObject>()
                .Select(x =>
                    new VersionedObject(
                        x.GetJsonLdIRI(),
                        x,
                        persistentUris)
                    );

        public static IEnumerable<AspectObject>? GetInputGraphAsEntities(this JObject jsonld) =>
            jsonld.RemoveContext()
                .GetJsonLdGraph()?
                .Values<JObject>()
                .Select(x =>
                    new AspectObject(
                        x.GetJsonLdIRI(),
                        x)
                    );

        public static JArray GetJsonLdGraph(this JObject jsonld)
        {
            if (jsonld.ContainsKey("@graph") && jsonld.SelectToken("@graph") != null)
            {
                var graphArray = jsonld.SelectToken("@graph").Value<JArray>();
                if (graphArray != null)
                    return graphArray;
            }
            return new JArray() { jsonld };
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
            return JsonLdProcessor.Compact(
                    JsonLdProcessor.Expand(jsonld, expanderOptions).First,
                    compacterContext,
                    compacterOptions
                );
        }
        public static IEnumerable<IRIReference>? GetAllPersistentIris(JObject input, JObject existing) =>
            input
                .GetAllEntityIds()?
                .Select(x => new IRIReference(x))
                .Union(
                    existing
                        .GetAllEntityIds()?
                        .Select(s => new VersionedIRIReference(s).PersistentIRI)
                );
        public static IEnumerable<Uri>? GetAllEntityIds(this JObject input) =>
            input
                .RemoveContext()
                .GetJsonLdGraph()?.Values<JObject>()
                .Select(s => new Uri(s.SelectToken("@id").Value<string>()));

        public static JObject CreateUpdateJObject(IEnumerable<VersionedObject> updateList,
            IEnumerable<IRIReference> deleteList, Func<JObject, JObject> outputModifier) =>
            new()
            {
                ["update"] = new JObject()
                {
                    ["@graph"] = updateList.MakeUpdateGraph(outputModifier),
                    ["@context"] = new JObject() { ["@version"] = "1.1" }
                },
                ["delete"] = deleteList.MakeDeleteGraph(),
            };

        public static JArray MakeUpdateGraph(this IEnumerable<VersionedObject> updateList, Func<JObject, JObject> outputModifier) =>
            new(updateList.Select(o => outputModifier(o.ToJObject())));

        public static IDictionary<IRIReference, VersionedObject> MakeUpdateList(this IEnumerable<AspectObject> inputList,
            IEnumerable<VersionedObject> existingList)
        {
            var oldNewMap = inputList.Select(
                i => (
                    input: i,
                    old: existingList.Where(x => i.SamePersistentIRI(x.Object))
                )
            );
            return new Dictionary<IRIReference, VersionedObject>(
                oldNewMap
                    .Where(i => !i.old.Any())
                    .Select(i => new KeyValuePair<IRIReference, VersionedObject>(i.input.PersistentIRI, new VersionedObject(i.input)))
                    .Union(
                        oldNewMap
                            .Where(i => i.old.Any()
                                        && !i.input.Equals(i.old.First().Object))
                            .Select(i => new KeyValuePair<IRIReference, VersionedObject>(i.input.PersistentIRI, new ProvenanceObject(i.input, i.old.First()))
                            )
                        )
                );
        }

        public static JArray MakeDeleteGraph(this IEnumerable<IRIReference> deleteList) =>
            new(deleteList.Select(x => x.ToJValue()));

        /// <summary>
        /// Creates a list of objects that should  be deleted from the aspect api, based on an assumed complete list of "new objects"
        /// </summary>
        public static IEnumerable<IRIReference> MakeDeleteList(this IEnumerable<AspectObject> input,
            IEnumerable<VersionedObject> existing) =>
            existing
                .Where(x => !input.Any(i => x.Object.SamePersistentIRI(i)))
                .Select(x => x.VersionedIRI);
    }
}