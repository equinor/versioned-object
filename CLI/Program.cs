using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AngleSharp.Dom;
using VDS.RDF.Query.Algebra;
using static VersionedObject.EntityGraphComparer;
using static VersionedObject.JsonLdHelper;


namespace VersionedObject.CLI
{
    public class Data
    {

        public static readonly JObject InputEdgeJsonLd = new()
        {
            ["@graph"] = new JArray()
            {
                new JObject()
                {
                    ["@id"] = "sor:Row1",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "An empty MEL Row",
                    ["imf:hasChild"] = "http://rdf.equinor.com/ontology/sor#Row2"
                },
                new JObject()
                {
                    ["@id"] = "sor:Row2",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "The second MEL Row"
                }
            },
            ["@context"] = new JObject()
            {
                ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                ["@vocab"] = "http://rdf.equinor.com/ontology/mel#",
                ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                ["imf"] = "http://imf.imfid.org/ontology/imf#",
                ["@version"] = "1.1"
            }
        };

        public static readonly int test_size = 4000;

        public static readonly JObject LargeInputJsonLd = new()
        {
            ["@graph"] = new JArray()
            {
                Enumerable.Range(1, test_size)
                    .Select(i => new JObject()
                        {
                            ["@id"] = new JValue($"sor:Row{i}"),
                            ["@type"] = "MelRow",
                            ["rdfs:label"] = $"Empty MEL Row {i}"
                        }
                    )
            },
            ["@context"] = new JObject()
            {
                ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                ["@vocab"] = "http://rdf.equinor.com/ontology/mel#",
                ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                ["imf"] = "http://imf.imfid.org/ontology/imf#",
                ["@version"] = "1.1"
            }
        };

        public static readonly IEnumerable<PersistentObjectData> LargeAspectGraph =
            Enumerable.Range(1, test_size)
                .Select(i =>
                    new PersistentObjectData(
                        new IRIReference(new string($"http://rdf.equinor.com/ontology/sor#Row{i}")),
                        new JObject()
                        {
                            ["@type"] = new JArray() { "http://rdf.equinor.com/ontology/mel#MelRow" },
                            ["rdfs:label"] = "An empty MEL Row",
                        }
                    )
                );

        public static readonly IEnumerable<VersionedObject> LargeAspectVersionedGraph =
            LargeAspectGraph.Select(o => new VersionedObject(o));

        public static readonly JObject LargeAspectJsonLd = new()
        {
            ["@graph"] = new JArray(LargeAspectVersionedGraph.Select(o => o.ToJObject())),

            ["@context"] = new JObject()
            {
                ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                ["imf"] = "http://imf.imfid.org/ontology/imf#",
                ["@version"] = "1.1"
            }
        };

        public static void Main()
        {
            var start_time = DateTime.Now;
            var update = Data.LargeInputJsonLd.HandleGraphCompleteUpdate(Data.LargeAspectJsonLd);
            var used_time = DateTime.Now - start_time;
            System.Console.WriteLine($"Objects: {test_size}. Time: {used_time}");
        }

    }

}
