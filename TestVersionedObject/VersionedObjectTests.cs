using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using static VersionedObject.EntityGraphComparer;
using static VersionedObject.JsonLdHelper;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602

namespace VersionedObject.Tests
{
    public class VersionedObjectTests
    {
        public static readonly JObject DifferentJsonLd = new()
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

        public static readonly JObject Row2JsonLd = new()
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

        public static readonly JObject SimpleJsonLd = new()
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

        public static readonly JObject BlankNodeJsonLd = new()
        {
            ["@graph"] = new JArray()
            {
                new JObject()
                {
                    ["@id"] = "sor:Row1",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "An empty MEL Row",
                    ["http://rds.posccaesar.org/ontology/lis14/rdl/hasPhysicalQuantity"] = new JObject()
                    {
                        ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
                        ["rdfs:label"] = "Weight of object",
                        ["http://rds.posccaesar.org/ontology/lis14/rdl/qualityQuantifiedAs"] = new JArray(){
                            new JObject()
                        {
                            ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                            ["rdfs:label"] = "Weight specified",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "12345",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                        },
                            new JObject(){
                                ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                                ["rdfs:label"] = "Weight specified",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "23456",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                                }
                            }
                    }
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

        public static readonly JObject BlankNodeJsonLd2 = new()
        {
            ["@graph"] = new JArray()
            {
                new JObject()
                {
                    ["@id"] = "sor:Row1",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "An empty MEL Row",
                    ["http://rds.posccaesar.org/ontology/lis14/rdl/hasPhysicalQuantity"] = new JObject()
                    {
                        ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
                        ["@id"] = "_:1234",
                        ["rdfs:label"] = "Weight of object",
                        ["http://rds.posccaesar.org/ontology/lis14/rdl/qualityQuantifiedAs"] = new JArray(){ new JObject()
                        {
                            ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                            ["@id"] = "_:212",
                            ["rdfs:label"] = "Weight specified",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "12345",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                        },
                            new JObject(){
                                ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                                ["rdfs:label"] = "Weight specified",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "23456",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                            }
                        }
                    }
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

        public static readonly JObject BlankNodeJsonLd2a = new()
        {
            ["@graph"] = new JArray()
            {
                new JObject()
                {
                    ["@id"] = "sor:Row1",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "An empty MEL Row",
                    ["http://rds.posccaesar.org/ontology/lis14/rdl/hasPhysicalQuantity"] = new JObject()
                    {
                        ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
                        ["@id"] = "_:1234",
                        ["rdfs:label"] = "Weight of object",
                        ["http://rds.posccaesar.org/ontology/lis14/rdl/qualityQuantifiedAs"] = new JArray(){
                            new JObject(){
                                ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                                ["rdfs:label"] = "Weight specified",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "1",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                            },
                            new JObject()
                        {
                            ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                            ["rdfs:label"] = "Weight specified",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "22",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                        },
                            
                            new JObject()
                            {
                                ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                                ["rdfs:label"] = "Weight specified",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "333",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                            }
                        }
                    }
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

        public static readonly JObject BlankNodeJsonLd2b = new()
        {
            ["@graph"] = new JArray()
            {
                new JObject()
                {
                    ["@id"] = "sor:Row1",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "An empty MEL Row",
                    ["http://rds.posccaesar.org/ontology/lis14/rdl/hasPhysicalQuantity"] = new JObject()
                    {
                        ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
                        ["@id"] = "_:1234",
                        ["rdfs:label"] = "Weight of object",
                        ["http://rds.posccaesar.org/ontology/lis14/rdl/qualityQuantifiedAs"] = new JArray(){
                            new JObject(){
                                ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                                ["rdfs:label"] = "Weight specified",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "333",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                            },
                            new JObject()
                        {
                            ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                            ["rdfs:label"] = "Weight specified",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "1",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                        },

                            new JObject()
                            {
                                ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                                ["rdfs:label"] = "Weight specified",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "22",
                                ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                            }
                        }
                    }
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

        public static readonly JObject BlankNodeJsonLd3 = new()
        {
            ["@graph"] = new JArray()
            {
                new JObject()
                {
                    ["@id"] = "sor:Row1",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "An empty MEL Row",
                    ["http://rds.posccaesar.org/ontology/lis14/rdl/hasPhysicalQuantity"] = new JObject()
                    {
                        ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
                        ["@id"] = "_:1",
                        ["rdfs:label"] = "Weight of object",
                        ["http://rds.posccaesar.org/ontology/lis14/rdl/qualityQuantifiedAs"] = new JObject()
                        {
                            ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                            ["@id"] = "_:2",
                            ["rdfs:label"] = "Weight specified",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "12346",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                        }
                    }
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

        public static readonly JObject BlankNodeJsonLd4 = new()
        {
            ["@graph"] = new JArray()
            {
                new JObject()
                {
                    ["@id"] = "sor:Row1",
                    ["@type"] = "MelRow",
                    ["rdfs:label"] = "An empty MEL Row",
                    ["http://rds.posccaesar.org/ontology/lis14/rdl/hasPhysicalQuantity"] = new JObject()
                    {
                        ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
                        ["@id"] = "_:strangelongblankid",
                        ["rdfs:label"] = "Weight of object",
                        ["http://rds.posccaesar.org/ontology/lis14/rdl/qualityQuantifiedAs"] = new JObject()
                        {
                            ["@type"] = new JArray("http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003620", "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100004048"),
                            ["@id"] = "_:evenweirderlongblankid",
                            ["rdfs:label"] = "Weight specified",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumValue"] = "12346",
                            ["http://rds.posccaesar.org/ontology/lis14/rdl/datumUOM"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003684"
                        }
                    }
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

        public static readonly JObject aspect_jsonld = new()
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
        public static readonly JObject aspect_persistent_jsonld = new()
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
        public static readonly JObject expanded_jsonld = new()
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
                SimpleJsonLd.GetInputGraphAsEntities().First().Equals(
                    aspect_jsonld.GetExistingGraphAsEntities(ImmutableHashSet.Create<IRIReference>()).First().Object),
                "Equality test on input and aspect jsonld failed");
            Assert.False(
                SimpleJsonLd.GetInputGraphAsEntities().First()
                .Equals(DifferentJsonLd.GetInputGraphAsEntities().First()), "Equality test on input and aspect jsonld failed");
        }

        [Fact()]
        public void RdfEqualsTest()
        {
            var aspectFirst = aspect_jsonld
                .GetExistingGraphAsEntities(ImmutableHashSet<IRIReference>.Empty.Add(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1")))
                .First().Object;
            var simpleFirst = SimpleJsonLd.GetInputGraphAsEntities().First();
            var aspectHashCode = aspectFirst.GetHash();
            var simpleHashCode = simpleFirst.GetHash();
            Assert.Equal(aspectHashCode, simpleHashCode);
            Assert.True(aspectFirst.Equals(simpleFirst),
                "Equality test on input and aspect jsonld failed");
            Assert.False(
                aspect_jsonld.GetExistingGraphAsEntities(ImmutableHashSet<IRIReference>.Empty.Add(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1"))).First().Object
                .Equals(DifferentJsonLd.GetInputGraphAsEntities().First()), "Equality test on input and aspect jsonld failed");
        }

        [Fact()]
        public void RdfHashTest()
        {
            var aspectFirst = aspect_jsonld
                .GetExistingGraphAsEntities(ImmutableHashSet<IRIReference>.Empty.Add(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1")))
                .First();
            var aspectHashCode = string.Join("", aspectFirst.Object.GetHash());
            Assert.Equal(aspectHashCode, aspectFirst.VersionedIri.VersionHash);
        }

        [Fact]
        public void TestIsBlankNode()
        {
            var bnode = new JObject()
            {
                ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
                ["@id"] = "_:1234",
                ["rdfs:label"] = "Weight of object"
            };
        Assert.True(bnode.IsBlankNode());

        var bnode2 = new JObject()
        {
            ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
            ["rdfs:label"] = "Weight of object"
        };
        Assert.True(bnode.IsBlankNode());

        var normal_node = new JObject()
        {
            ["@type"] = "http://rds.posccaesar.org/ontology/plm/rdl/PCA_100003589",
            ["@id"] = "http://example.com/id/_1",
            ["rdfs:label"] = "Weight of object"
        };
        Assert.False(normal_node.IsBlankNode());
        }

        [Fact]
        public void BlankNodeTest()
        {
            var obj1 = BlankNodeJsonLd.GetInputGraphAsEntities().First();
            var hash1 = obj1.GetHash();
            var obj2 = BlankNodeJsonLd2.GetInputGraphAsEntities().First();
            var hash2 = obj2.GetHash();
            var obj2a = BlankNodeJsonLd2a.GetInputGraphAsEntities().First();
            var hash2a = obj2a.GetHash();
            var obj2b = BlankNodeJsonLd2b.GetInputGraphAsEntities().First();
            var hash2b = obj2b.GetHash();
            var obj3 = BlankNodeJsonLd3.GetInputGraphAsEntities().First();
            var hash3 = obj3.GetHash();
            var obj4 = BlankNodeJsonLd4.GetInputGraphAsEntities().First();
            var hash4 = obj4.GetHash();

            Assert.Equal(obj2.GetHash(), obj1.GetHash());
            Assert.NotEqual(obj2.GetHash(), obj2a.GetHash());
             Assert.Equal(obj2b.GetHash(), obj2a.GetHash());
            Assert.Equal(obj3.GetHash(), obj4.GetHash());
            Assert.NotEqual(obj1.GetHash(), obj3.GetHash());
            Assert.NotEqual(obj2.GetHash(), obj4.GetHash());
        }

        [Fact()]
        public void LoadGraphTest()
        {
            var graph = ParseJsonLdString(SimpleJsonLd.ToString());
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
            var updatelist = DifferentJsonLd.GetInputGraphAsEntities().MakeUpdateList(aspect_jsonld.GetExistingGraphAsEntities(ImmutableHashSet<IRIReference>.Empty.Add(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1"))));
            Assert.True(updatelist.Any());
            Assert.Single(updatelist);
            Assert.Equal(DifferentJsonLd.GetInputGraphAsEntities().First().PersistentIRI, updatelist.First().GetPersistentIRI());
        }

        [Fact()]
        public void TestHashTriples()
        {
            var simple_graph = SimpleJsonLd;
            var aspect_persistent_graph = aspect_persistent_jsonld;
            var simple_aspect_graph = SimpleJsonLd;
            var aspect_graph = aspect_jsonld;
            var simple_expanded = SimpleJsonLd.RemoveContext();
            var aspet_persistent_expanded = aspect_persistent_jsonld.RemoveContext();
            Assert.True(simple_expanded.AspectEquals(aspet_persistent_expanded, RdfEqualsHash));

            var simple_hash = simple_graph.GetHash();
            var simple_aspect_hash = simple_aspect_graph.GetHash();
            var different_hash = DifferentJsonLd.GetHash();
            var aspect_hash = aspect_graph.GetHash();
            var aspect_persistent_hash = aspect_persistent_graph.GetHash();
            var row2_hash = Row2JsonLd.GetHash();
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
            var input_entities = SimpleJsonLd.GetInputGraphAsEntities();
            var existing_entities = aspect_jsonld.GetExistingGraphAsEntities(ImmutableHashSet<IRIReference>.Empty
                .Add(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1")));
            var updatelist = input_entities.MakeUpdateList(existing_entities);
            Assert.NotNull(updatelist);
            Assert.False(updatelist.Any());
        }

        [Fact()]
        public void AspectEntityEqualsTest()
        {
            var simple_object = SimpleJsonLd.RemoveContext();
            var simple_entity = SimpleJsonLd.GetInputGraphAsEntities().First();
            var aspect_list = aspect_jsonld.GetExistingGraphAsEntities(
                ImmutableHashSet<IRIReference>.Empty.Add(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1"))).ToImmutableList();
            var aspect_entity = aspect_list.First();
            Assert.Equal(simple_entity, aspect_entity.Object);
        }

        [Fact()]
        public void TestMelDocumentAdded()
        {
            var finishedGraph = SimpleJsonLd.GetInputGraphAsEntities();
            Assert.NotNull(finishedGraph);

        }

        [Fact()]
        public void MakeNoDeleteListTest()
        {
            var deletelist = SimpleJsonLd.GetInputGraphAsEntities().MakeDeleteList(aspect_jsonld.GetExistingGraphAsEntities(ImmutableHashSet<IRIReference>.Empty.Add(new IRIReference("http://rdf.equinor.com/ontology/sor#Row1"))));
            Assert.NotNull(deletelist);
            Assert.False(deletelist.Any());
        }

        [Fact()]
        public void MakeInputGrahEntitiesTest()
        {
            var input = Row2JsonLd.GetInputGraphAsEntities();
            Assert.NotNull(input);
            Assert.Single(input);
            var iri = input.First().PersistentIRI;
            Assert.Equal("http://rdf.equinor.com/ontology/sor#Row2", iri.ToString());
            Assert.NotEqual("http://rdf.equinor.com/ontology/sor#Row1", iri.ToString());
        }

        [Fact()]
        public void MakeExistingGrahEntitiesTest()
        {
            var persistentIris = GetAllPersistentIris(Row2JsonLd, expanded_jsonld).ToImmutableHashSet();
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
            var input = Row2JsonLd.GetInputGraphAsEntities();
            var persistentIris = EntityGraphComparer.GetAllPersistentIris(Row2JsonLd, expanded_jsonld).ToImmutableHashSet();
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
            var expanded = Row2JsonLd.RemoveContext();
            Assert.NotNull(expanded);
            var second = expanded.RemoveContext();
            Assert.Equal(new IRIReference("http://rdf.equinor.com/ontology/sor#Row2"), new IRIReference(second.GetJsonLdGraph().Values<JObject>().First().GetIRIReference()));

            var vobject = JObject.Parse(@"{
                ""@graph"": [
                {
                    ""@type"": [
                    ""http://rdf.equinor.com/ontology/mel#MelRow""
                        ],
                    ""rdfs:label"": ""An empty MEL Row"",
                    ""@id"": ""http://rdf.equinor.com/ontology/sor#Row1/version/22310441759822761165/1659612640"",
                    ""http://www.w3.org/ns/prov#wasDerivedFrom"": ""http://www.w3.org/1999/02/22-rdf-syntax-ns#nil""
                }
                ],
                ""@context"": {
                    ""rdfs"": ""http://www.w3.org/2000/01/rdf-schema#"",
                    ""sor"": ""http://rdf.equinor.com/ontology/sor#"",
                    ""imf"": ""http://imf.imfid.org/ontology/imf#"",
                    ""@version"": ""1.1""
                }
            }");
            var flat = vobject.RemoveContext();
            Assert.NotNull(flat);

        }

        [Fact()]
        public void GetAllIdsTest()
        {
            var persistentIris = Row2JsonLd.GetAllEntityIds();
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
            var persistentIris = GetAllPersistentIris(Row2JsonLd, expanded_jsonld);
            Assert.NotNull(persistentIris);
            Assert.Equal(2, persistentIris.Count());
            Assert.Contains("http://rdf.equinor.com/ontology/sor#Row1", persistentIris.Select(x => x.ToString()));
            Assert.Contains("http://rdf.equinor.com/ontology/sor#Row2", persistentIris.Select(x => x.ToString()));
        }

        [Fact()]
        public void MakeGraphUpdateTest()
        {
            var diff_object = Row2JsonLd.HandleGraphCompleteUpdate(expanded_jsonld);
            Assert.NotNull(diff_object);
#pragma warning disable CS8604
            var row2_iri = new IRIReference(Row2JsonLd.RemoveContext().GetJsonLdGraph().Values<JObject>().First().SelectToken("@id").Value<string>());
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
            var simple_list = SimpleJsonLd.GetInputGraphAsEntities();
            Assert.True(simple_list.Any());
            var num_items = SimpleJsonLd.SelectToken("@graph").Count();
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
            var urilist = ImmutableHashSet<IRIReference>.Empty.Add(new("http://rdf.equinor.com/ontology/sor#Row1"));
            var removed_versions = aspect_persistent_jsonld.RemoveContext().RemoveVersionsFromIris(urilist);
            Assert.Equal("http://rdf.equinor.com/ontology/sor#Row1", removed_versions["@id"]);
        }


        [Fact]
        public void TestRemoveContext()
        {
            var edged_graph = EdgeReificationTests.InputEdgeJsonLd.RemoveContext();
            Assert.Equal(2, edged_graph.SelectToken("@graph").Value<JArray>().Count());

            var simple_graph = SimpleJsonLd.RemoveContext();
            Assert.Single(new JArray(simple_graph));

            var edged_expanded = EdgeReificationTests.InputEdgeJsonLd.GetInputGraphAsEntities();

            var childList = from edge in (from node in edged_expanded
                                          where node.PersistentIRI.ToString().Equals("http://rdf.equinor.com/ontology/sor#Row1")
                                          select node.Content).First()
                            where edge.Name.ToString().Equals("http://imf.imfid.org/ontology/imf#hasChild")
                            select edge.Value;
            var child = childList.First();
            Assert.NotNull(child);
            Assert.Equal("http://rdf.equinor.com/ontology/sor#Row2", child.ToString());
        }

        [Fact]
        public void TestGetGraph()
        {
            var edged_graph = EdgeReificationTests.InputEdgeJsonLd.RemoveContext().GetJsonLdGraph();
            Assert.Equal(2, edged_graph.Count());

            var simple_graph = SimpleJsonLd.RemoveContext().GetJsonLdGraph();
            Assert.Single(simple_graph);
        }
    }
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602
}