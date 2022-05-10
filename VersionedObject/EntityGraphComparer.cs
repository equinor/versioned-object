﻿using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF.JsonLd;

namespace VersionedObject
{
    public static class EntityGraphComparer
    {


        public static JObject MakeGraphUpdate(this JObject input, JObject existing) =>
            input.MakeGraphUpdate(existing, x => x);
        /// <summary>
        /// Creates update object for use with the put method on the Aspect API graph endpoint
        /// Takes in full json-ld objects of input and existing snapshot
        /// </summary>
        public static JObject MakeGraphUpdate(this JObject input, JObject existing, Func<IEnumerable<AspectEntity>, IEnumerable<AspectEntity>> inputModifier)
        {
            var inputList = inputModifier(input.GetInputGraphAsEntities());
            var existingList = existing.GetExistingGraphAsEntities(GetAllPersistentIris(input, existing));
            var updateList = inputList.MakeUpdateList(existingList);
            var allEntities = existingList.Union(updateList);

            var deleteList = inputList.MakeDeleteList(existingList);
            return CreateUpdateJObject(updateList, deleteList, x => x.AddVersionToUris(allEntities));
        }

        /// <summary>
        /// Translates JSON-LD coming from Aspect-API (so using versioned IRIs)
        /// into list of VersionedEntity objects
        /// The list of persistent iris is used to translate the versioned IRIs inside the json so it can be matched with the incoming json
        /// </summary>
        /// <param name="jsonld"></param>
        /// <param name="persistentUris"></param>
        public static IEnumerable<VersionedEntity> GetExistingGraphAsEntities(this JObject jsonld, IEnumerable<IRIReference> persistentUris) =>
            jsonld.RemoveContext()
                .GetJsonLdGraph()
                .Values<JObject>()
                .Select(x =>
                    new VersionedEntity(
                        x.GetJsonLdIRI(),
                        x,
                        persistentUris)
                    );

        public static IEnumerable<AspectEntity> GetInputGraphAsEntities(this JObject jsonld) =>
            jsonld.RemoveContext()
                .GetJsonLdGraph()
                .Values<JObject>()
                .Select(x =>
                    new AspectEntity(
                        x.GetJsonLdIRI(),
                        x)
                    );

        public static JArray GetJsonLdGraph(this JObject jsonld)
        {
            if (jsonld.ContainsKey("@graph"))
                return jsonld.SelectToken("@graph").Value<JArray>();
            if (jsonld.Type == JTokenType.Array)
                return jsonld.Value<JArray>();
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
        public static IEnumerable<IRIReference> GetAllPersistentIris(JObject input, JObject existing) =>
            input
                .GetAllEntityIds()
                .Union(
                    existing
                        .GetAllEntityIds()
                        .Select(s => s.GetPersistentUri())
                );
        public static IEnumerable<IRIReference> GetAllEntityIds(this JObject input) =>
            input
                .RemoveContext()
                .GetJsonLdGraph().Values<JObject>()
                .Select(s => new IRIReference(s.SelectToken("@id").Value<string>()));

        public static JObject CreateUpdateJObject(IEnumerable<VersionedEntity> updateList,
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

        public static JArray MakeUpdateGraph(this IEnumerable<VersionedEntity> updateList, Func<JObject, JObject> outputModifier) =>
            new(updateList.Select(o => outputModifier(o.ToJObject())));

        public static IEnumerable<VersionedEntity> MakeUpdateList(this IEnumerable<AspectEntity> inputList,
            IEnumerable<VersionedEntity> existingList)
        {
            var oldNewMap = inputList.Select(
                i => (
                    input: i,
                    old: existingList.Where(x => i.SamePersistentIRI(x.Entity))
                )
            );
            return oldNewMap
                .Where(i => !i.old.Any())
                .Select(i => new VersionedEntity(i.input))
                .Union(
                    oldNewMap
                        .Where(i => i.old.Any()
                                    && !i.input.Equals(i.old.First().Entity))
                        .Select(i => new ProvenanceEntity(i.input, i.old.First()))
                );
        }

        public static JArray MakeDeleteGraph(this IEnumerable<IRIReference> deleteList) =>
            new(deleteList.Select(x => x.ToJValue()));

        /// <summary>
        /// Creates a list of objects that should  be deleted from the aspect api, based on an assumed complete list of "new objects"
        /// </summary>
        public static IEnumerable<IRIReference> MakeDeleteList(this IEnumerable<AspectEntity> input,
            IEnumerable<VersionedEntity> existing) =>
            existing
                .Where(x => !input.Any(i => x.Entity.SamePersistentIRI(i)))
                .Select(x => x.GetVersionedIRI());
    }
}
