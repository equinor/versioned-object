using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using static VersionedObject.EntityGraphComparer;
using static VersionedObject.JsonLdHelper;

namespace VersionedObject.Tests
{
    public class VersionedObjectTests
    {

            public static readonly JObject different_jsonld = new JObject()
            {
                ["@graph"] = new JArray()
                {
                    new JObject()
                    {
                        ["@id"] = "sor:Row1",
                        ["@type"] = "MelRow",
                        ["rdfs:label"] = "A different MEL Row"
                    }
                },
                ["@context"] = new JObject()
                {
                    ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                    ["@vocab"] = "http://rdf.equinor.com/ontology/mel#",
                    ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                    ["@version"] = "1.1"
                }
            };

            public static readonly JObject row2_jsonld = new JObject()
            {
                ["@graph"] = new JArray()
                {
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
                    ["@version"] = "1.1"
                }
            };

            public static readonly JObject simple_jsonld = new JObject()
            {
                ["@graph"] = new JArray()
                {
                    new JObject()
                    {
                        ["@id"] = "sor:Row1",
                        ["@type"] = "MelRow",
                        ["rdfs:label"] = "An empty MEL Row"
                    }
                },
                ["@context"] = new JObject()
                {
                    ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                    ["@vocab"] = "http://rdf.equinor.com/ontology/mel#",
                    ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                    ["@version"] = "1.1"
                }
            };

            public static readonly JObject edge_jsonld = new JObject()
            {
                ["@graph"] = new JArray()
                {
                    new JObject()
                    {
                        ["@id"] = "sor:Row1",
                        ["@type"] = "MelRow",
                        ["rdfs:label"] = "An empty MEL Row",
                        ["imf:hasChild"] = "sor:Row2"
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

            public static readonly JObject aspect_jsonld = new JObject()
            {
                ["@graph"] = new JArray()
                {
                    new JObject()
                    {
                        ["@id"] = "sor:Row1/version/29110145432144214285/2022-05-01",
                        ["@type"] = new JArray(){ "http://rdf.equinor.com/ontology/mel#MelRow" },
                        ["rdfs:label"] = "An empty MEL Row",
                        [VersionedObject.ProvWasDerivedFrom] = VersionedObject.NoProvenance.ToString()
                    }
                },
                ["@context"] = new JObject()
                {
                    ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                    ["@vocab"] = "http://rdf.equinor.com/ontology/mel#",
                    ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                    ["@version"] = "1.1"
                }
            };
            public static readonly JObject aspect_persistent_jsonld = new JObject()
            {
                ["@graph"] = new JArray()
                {
                    new JObject()
                    {
                        ["@id"] = "sor:Row1",
                        ["@type"] = new JArray(){ "http://rdf.equinor.com/ontology/mel#MelRow" },
                        ["rdfs:label"] = "An empty MEL Row"
                    }
                },
                ["@context"] = new JObject()
                {
                    ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                    ["@vocab"] = "http://rdf.equinor.com/ontology/mel#",
                    ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                    ["@version"] = "1.1"
                }
            };
            public static readonly JObject expanded_jsonld = new JObject()
            {
                ["@graph"] = new JArray()
                {
                    new JObject()
                    {
                        ["@id"] = "http://rdf.equinor.com/ontology/sor#Row1/version/29110145432144214285/2022-05-01",
                        ["@type"] = new JArray(){ "http://rdf.equinor.com/ontology/mel#MelRow" },
                        ["rdfs:label"] = "An empty MEL Row",
                        ["http://www.w3.org/ns/prov#wasDerivedFrom"] = VersionedObject.NoProvenance.ToString()
                    }
                },
                ["@context"] = new JObject()
                {
                    ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                    ["@vocab"] = "http://rdf.equinor.com/ontology/mel#",
                    ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                    ["@version"] = "1.1"
                }
            };
        
        [Fact()]
        public void RdfEqualsHashTest()
        {
            Assert.True(
                simple_jsonld.GetInputGraphAsEntities().First().Equals(
                    simple_jsonld.GetInputGraphAsEntities().First()),
                "Equality test on input and aspect jsonld failed");
            Assert.False(
                simple_jsonld.GetInputGraphAsEntities().First()
                .Equals(different_jsonld.GetInputGraphAsEntities().First()), "Equality test on input and aspect jsonld failed");
        }

        [Fact()]
        public void RdfEqualsTest()
        {
            var aspectFirst = aspect_jsonld
                .GetExistingGraphAsEntities(new[] { new IRIReference("http://rdf.equinor.com/ontology/sor#Row1") })
                .First().Object;
            var simpleFirst = simple_jsonld.GetInputGraphAsEntities().First();
            var aspectHashCode = aspectFirst.GetHash();
            var simpleHashCode = simpleFirst.GetHash();
            Assert.Equal(aspectHashCode, simpleHashCode);
            Assert.True(aspectFirst.Equals(simpleFirst),
                "Equality test on input and aspect jsonld failed");
            Assert.False(
                aspect_jsonld.GetExistingGraphAsEntities(new[] { new IRIReference("http://rdf.equinor.com/ontology/sor#Row1") }).First().Object
                .Equals(different_jsonld.GetInputGraphAsEntities().First()), "Equality test on input and aspect jsonld failed");
        }

        [Fact()]
        public void RdfHashTest()
        {
            var aspectFirst = aspect_jsonld
                .GetExistingGraphAsEntities(new[] { new IRIReference("http://rdf.equinor.com/ontology/sor#Row1") })
                .First();
            var aspectHashCode = string.Join("", aspectFirst.Object.GetHash());
            Assert.Equal(aspectHashCode, aspectFirst.VersionedIri.VersionHash);
        }


        [Fact()]
        public void LoadGraphTest()
        {
            var graph = ParseJsonLdString(simple_jsonld.ToString());
            Assert.NotNull(graph);
            Assert.False(graph.IsEmpty);
        }

        [Fact()]
        public void LoadStringGraphTest()
        {
            var graph = ParseJsonLdString(@"{
                ""@graph"": [
                    {
                        ""@id"": ""sor:Row1"",
                        ""@type"": ""MelRow"",
                        ""rdfs:label"": ""An empty MEL Row""
                    }
                ],
                ""@context"": { 
                    ""rdfs"": ""http://www.w3.org/2000/01/rdf-schema#"",
                    ""@vocab"": ""http://rdf.equinor.com/ontology/mel#"",
                    ""sor"": ""http://rdf.equinor.com/ontology/sor#"",
                    ""@version"": ""1.1""
                }
            }");
            Assert.NotNull(graph);
            Assert.False(graph.IsEmpty);
        }

        [Fact()]
        public void LoadMarkusGraphTest()
        {
            var graph = ParseJsonLdString(@"{
                ""@graph"":[
                    {
                        ""@id"": ""https://example.com/yo"",
                        ""https://example.com/prop/hasName"": ""Yo"",
                        ""@type"": ""https://example.com/class/cool""
                    }
                ],
                ""@context"": {
                    ""@version"": ""1.1""
                }
            }");
            Assert.NotNull(graph);
            Assert.False(graph.IsEmpty);
        }

        [Fact()]
        public void MakeUpdateListTest()
        {
            var updatelist = different_jsonld.GetInputGraphAsEntities().MakeUpdateList(aspect_jsonld.GetExistingGraphAsEntities(new[] { new IRIReference("http://rdf.equinor.com/ontology/sor#Row1") }));
            Assert.True(updatelist.Any());
            Assert.Single(updatelist);
            Assert.Equal(different_jsonld.GetInputGraphAsEntities().First().PersistentIRI, updatelist.First().GetPersistentIRI());
        }

        [Fact()]
        public void TestHashTriples()
        {
            var simple_graph = ParseJsonLdString(simple_jsonld.ToString());
            var aspect_persistent_graph = ParseJsonLdString(aspect_persistent_jsonld.ToString());
            var simple_aspect_graph = ParseJsonLdString(simple_jsonld.ToString()).AddAspectApiTriples(aspect_persistent_graph);
            var aspect_graph = ParseJsonLdString(aspect_jsonld.ToString());
            var simple_expanded = simple_jsonld.RemoveContext();
            var aspet_persistent_expanded = aspect_persistent_jsonld.RemoveContext();
            Assert.True(simple_expanded.AspectEquals(aspet_persistent_expanded, RdfEqualsHash));

            var simple_hash = simple_graph.GetHash();
            var simple_aspect_hash = simple_aspect_graph.GetHash();
            var different_hash = ParseJsonLdString(different_jsonld.ToString()).GetHash();
            var aspect_hash = aspect_graph.GetHash();
            var aspect_persistent_hash = aspect_persistent_graph.GetHash();
            var row2_hash = ParseJsonLdString(row2_jsonld.ToString()).GetHash();
            Assert.NotEqual(simple_hash, different_hash);
            Assert.NotEqual(aspect_hash, different_hash);
            Assert.NotEqual(row2_hash, different_hash);
            Assert.NotEqual(simple_hash, aspect_hash);
            Assert.NotEqual(simple_hash, row2_hash);
            Assert.Equal(aspect_persistent_hash, simple_aspect_hash);
        }

        [Fact()]
        public void MakeNoUpdateListTest()
        {
            var input_entities = simple_jsonld.GetInputGraphAsEntities();
            var existing_entities = aspect_jsonld.GetExistingGraphAsEntities(new[]
                {new IRIReference("http://rdf.equinor.com/ontology/sor#Row1")});
            var updatelist = input_entities.MakeUpdateList(existing_entities);
            Assert.NotNull(updatelist);
            Assert.False(updatelist.Any());
        }

        [Fact()]
        public void AspectEntityEqualsTest()
        {
            var simple_object = simple_jsonld.RemoveContext();
            var simple_entity = simple_jsonld.GetInputGraphAsEntities().First();
            var aspect_entity = aspect_jsonld.GetExistingGraphAsEntities(new[] { new IRIReference("http://rdf.equinor.com/ontology/sor#Row1") }).First();
            Assert.Equal(simple_entity, aspect_entity.Object);
        }

        [Fact()]
        public void TestMelDocumentAdded()
        {
            var finishedGraph = simple_jsonld.GetInputGraphAsEntities();
            Assert.NotNull(finishedGraph);

        }

        [Fact()]
        public void MakeNoDeleteListTest()
        {
            var deletelist = simple_jsonld.GetInputGraphAsEntities().MakeDeleteList(aspect_jsonld.GetExistingGraphAsEntities(new[] { new IRIReference("http://rdf.equinor.com/ontology/sor#Row1") }));
            Assert.NotNull(deletelist);
            Assert.False(deletelist.Any());
        }

        [Fact()]
        public void MakeInputGrahEntitiesTest()
        {
            var input = row2_jsonld.GetInputGraphAsEntities();
            Assert.NotNull(input);
            Assert.Single(input);
            var iri = input.First().PersistentIRI;
            Assert.Equal("http://rdf.equinor.com/ontology/sor#Row2", iri.ToString());
            Assert.NotEqual("http://rdf.equinor.com/ontology/sor#Row1", iri.ToString());
        }

        [Fact()]
        public void MakeExistingGrahEntitiesTest()
        {
            var persistentIris = GetAllPersistentIris(row2_jsonld, expanded_jsonld);
            var existing = expanded_jsonld.GetExistingGraphAsEntities(persistentIris);
            Assert.NotNull(existing);
            Assert.Single(existing);
            var iri = existing.First().GetPersistentIRI();
            Assert.Equal("http://rdf.equinor.com/ontology/sor#Row1", iri.ToString());
            Assert.NotEqual("http://rdf.equinor.com/ontology/sor#Row2", iri.ToString());
        }

        [Fact()]
        public void MakeDeleteListTest()
        {
            var input = row2_jsonld.GetInputGraphAsEntities();
            var persistentIris = EntityGraphComparer.GetAllPersistentIris(row2_jsonld, expanded_jsonld);
            var existing = expanded_jsonld.GetExistingGraphAsEntities(persistentIris);

            var deletelist = input.MakeDeleteList(existing);
            Assert.NotNull(deletelist);
            Assert.True(deletelist.Any());
            Assert.Single(deletelist);
            Assert.Equal(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1/version/29110145432144214285/2022-05-01").ToString(), deletelist.First().ToString());
        }

        [Fact()]
        public void RemoveContextTest()
        {
            var expanded = row2_jsonld.RemoveContext();
            Assert.NotNull(expanded);
            var second = expanded.RemoveContext();
            Assert.Equal(new IRIReference("http://rdf.equinor.com/ontology/sor#Row2"), new IRIReference(second.GetJsonLdGraph().Values<JObject>().First().GetIRIReference()));
        }


        [Fact()]
        public void GetAllIdsTest()
        {
            var persistentIris = row2_jsonld.GetAllEntityIds();
            Assert.NotNull(persistentIris);
            Assert.Single(persistentIris);
            Assert.Contains("http://rdf.equinor.com/ontology/sor#Row2", persistentIris.Select(x => x.ToString()));

            persistentIris = expanded_jsonld.GetAllEntityIds();
            Assert.NotNull(persistentIris);
            Assert.Single(persistentIris);
            Assert.Contains("http://rdf.equinor.com/ontology/sor#Row1/version/29110145432144214285/2022-05-01", persistentIris.Select(x => x.ToString()));

        }

        [Fact()]
        public void TestUriEquals()
        {
            var iri1 = new IRIReference("http://rdf.equinor.com/ontology/sor#Row1");
            var iri2 = new IRIReference("http://rdf.equinor.com/ontology/sor#Row2");
            Assert.NotEqual(iri1, iri2);
            IRIReference[] irilist1 = { iri1 };
            IRIReference[] irilist2 = { iri2 };
            Assert.NotEqual(irilist1, irilist2);
            Assert.Contains(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1"), irilist1);
            Assert.DoesNotContain(new("http://rdf.equinor.com/ontology/sor#Row2"), irilist1);
            Assert.Equal(2, irilist1.Union(irilist2).Count());
        }

        [Fact()]
        public void GetPersistentIRIsTest()
        {
            var persistentIris = GetAllPersistentIris(row2_jsonld, expanded_jsonld);
            Assert.NotNull(persistentIris);
            Assert.Equal(2, persistentIris.Count());
            Assert.Contains("http://rdf.equinor.com/ontology/sor#Row1", persistentIris.Select(x => x.ToString()));
            Assert.Contains("http://rdf.equinor.com/ontology/sor#Row2", persistentIris.Select(x => x.ToString()));
        }

        [Fact()]
        public void MakeGraphUpdateTest()
        {
            var diff_object = row2_jsonld.HandleGraphCompleteUpdate(expanded_jsonld);
            Assert.NotNull(diff_object);
            var row2_iri = new IRIReference(row2_jsonld.RemoveContext().GetJsonLdGraph().Values<JObject>().First().SelectToken("@id").Value<string>());
            var diff_iri = diff_object
                .SelectToken("update")
                .Value<JObject>()
                .GetJsonLdGraph()
                .Values<JObject>()
                .First()
                .GetVersionedIRIReference();

            var persistent_diff_iri = diff_iri.PersistentIRI;
            Assert.Equal(row2_iri, persistent_diff_iri);
            Assert.Single(diff_object.SelectToken("update").Value<JObject>().GetJsonLdGraph());
        }


        [Fact()]
        public void GetGraphAsEnumerableTest()
        {
            var simple_list = simple_jsonld.GetInputGraphAsEntities();
            Assert.True(simple_list.Any());
#pragma warning disable CS8604 // Possible null reference argument.
            var num_items = simple_jsonld.SelectToken("@graph").Count();
#pragma warning restore CS8604 // Possible null reference argument.
            Assert.Equal(num_items, simple_list.Count());
        }

        [Fact]
        public void TestVersionedUri()
        {
            var uri = new VersionedIRIReference("http://rdf.equinor.com/data/objectx/version/12345/2022-05-01");
            Assert.Equal("12345", uri.VersionHash);
            Assert.Equal(new IRIReference("http://rdf.equinor.com/data/objectx"), uri.PersistentIRI);
        }
        [Fact]
        public void TestRemoveVersionFromUris()
        {
            var urilist = new List<IRIReference>() { new("http://rdf.equinor.com/ontology/sor#Row1") };
            var removed_versions = aspect_persistent_jsonld.RemoveContext().RemoveVersionFromUris(urilist);
            Assert.Equal("http://rdf.equinor.com/ontology/sor#Row1", removed_versions["@id"]);
        }

      
        [Fact]
        public void TestRemoveContext()
        {
            var edged_graph = edge_jsonld.RemoveContext();
            Assert.Equal(2, edged_graph.SelectToken("@graph").Value<JArray>().Count());

            var simple_graph = simple_jsonld.RemoveContext();
            Assert.Equal(1, new JArray(simple_graph).Count());
        }

        [Fact]
        public void TestGetGraph()
        {
            var edged_graph = edge_jsonld.RemoveContext().GetJsonLdGraph();
            Assert.Equal(2, edged_graph.Count());

            var simple_graph = simple_jsonld.RemoveContext().GetJsonLdGraph();
            Assert.Equal(1, simple_graph.Count());
        }

        [Fact]
        public void TestGetExternalIRIs()
        {
            var simple_list = simple_jsonld.GetInputGraphAsEntities();
            var refs = simple_list.First().ReifyNodeEdges(new List<IRIReference>());
            Assert.Single(refs);
            var single_refs = simple_list.ReifyAllEdges(new List<IRIReference>());
            Assert.Single(single_refs);
            var edged_list = edge_jsonld.GetInputGraphAsEntities();
            Assert.Equal(2, edged_list.Count());
            var persistentEntities = GetAllPersistentIris(edge_jsonld, aspect_jsonld);
            var refs2 = edged_list.ReifyAllEdges(persistentEntities);
            Assert.Equal(3, refs2.Count());
            var refs3 = edged_list.Skip(1).First().ReifyNodeEdges(persistentEntities);
            Assert.Equal(3, refs2.Count());

        }
    }
}