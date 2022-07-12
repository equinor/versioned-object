using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static VersionedObject.EntityGraphComparer;
using static VersionedObject.JsonLdHelper;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602

namespace VersionedObject.Tests
{
    public class EdgeReificationTests
    {

        public static readonly JObject EdgeJsonLd = new()
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

        [Fact]
        public void TestEdgeReifier()
        {
            var simple_list = VersionedObjectTests.SimpleJsonLd.GetInputGraphAsEntities();
            var refs = simple_list.First().ReifyNodeEdges(new List<IRIReference>());
            Assert.Single(refs);
            var single_refs = simple_list.ReifyAllEdges(new List<IRIReference>());
            Assert.Single(single_refs);
            var edged_list = EdgeJsonLd.GetInputGraphAsEntities();
            Assert.Equal(2, edged_list.Count());
            var persistentEntities = GetAllPersistentIris(EdgeJsonLd,  VersionedObjectTests.aspect_jsonld);
            var refs2 = edged_list.ReifyAllEdges(persistentEntities);
            var reified_json = from j in refs2 select j.ToJsonldJObject();
            Assert.Equal(3, refs2.Count());
            var refs3 = edged_list.Skip(1).First().ReifyNodeEdges(persistentEntities);
            Assert.Equal(3, refs2.Count());
        }

    }
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602
}