using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AngleSharp.Dom;
using VDS.RDF.Query.Algebra;
using Xunit;
using static VersionedObject.EntityGraphComparer;
using static VersionedObject.JsonLdHelper;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602

namespace VersionedObject.Tests
{
    public class EdgeReificationTests
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

        public static readonly int test_size = 1;

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
                            ["@type"] = new JArray() {"http://rdf.equinor.com/ontology/mel#MelRow"},
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

        [Fact]
        public void TestEdgeReifier()
        {
            var simple_list = VersionedObjectTests.SimpleJsonLd.GetInputGraphAsEntities();
            var refs = simple_list.First().ReifyNodeEdges(new List<IRIReference>());
            Assert.Single(refs);
            var single_refs = simple_list.ReifyAllEdges(new List<IRIReference>());
            Assert.Single(single_refs);
            var edged_list = InputEdgeJsonLd.GetInputGraphAsEntities();
            Assert.Equal(2, edged_list.Count());
            var persistentEntities = GetAllPersistentIris(InputEdgeJsonLd, VersionedObjectTests.aspect_jsonld);
            var refs2 = edged_list.ReifyAllEdges(persistentEntities);
            var reified_json = from j in refs2 select j.ToJsonldJObject();
            Assert.Equal(3, refs2.Count());
            var refs3 = edged_list.Skip(1).First().ReifyNodeEdges(persistentEntities);
            Assert.Equal(3, refs2.Count());
        }

        [Fact]
        public void TestVersionedEdgeReifier()
        {
            var edged_list = InputEdgeJsonLd.GetInputGraphAsEntities();

            var persistentEntities = GetAllPersistentIris(InputEdgeJsonLd, VersionedObjectTests.aspect_jsonld);
            var existingJObject = VersionedObjectTests.aspect_jsonld.ToString();
            var existing_list = VersionedObjectTests.aspect_jsonld.GetExistingGraphAsEntities(persistentEntities);
            var refs2 = edged_list.ReifyAllEdges(persistentEntities);
            var reified_json = from j in refs2 select j.ToJsonldJObject();
            var updateList = refs2.MakeUpdateList(existing_list);
            var reified_update = from j in updateList select j.ToJObject();
            Assert.Equal(2, updateList.Count());
            var map = updateList.Union(existing_list).MakePersistentIriMap();
            var versionedUpdate = updateList.UpdateEdgeIris(map);
            var versioned_update_json = from j in versionedUpdate select j.ToJObject();
            var deleteList = EntityGraphComparer.MakeDeleteList(refs2, existing_list);
            Assert.Empty(deleteList);
        }

        [Fact]
        public void TestFullEdgeReifierPerformance()
        {
            LargeInputJsonLd.HandleGraphCompleteUpdate(LargeAspectJsonLd);
        }

        [Fact]
        public void TestGetPersistentIrirs()
        {
            var persistent = GetAllPersistentIris(LargeInputJsonLd, LargeAspectJsonLd);
            Assert.NotNull(persistent);
            Assert.Equal(test_size, persistent.Count());
        }

        [Fact]
        public void TestEdgeReifierPerformance()
        {
            var edged_list = LargeInputJsonLd.GetInputGraphAsEntities().ToImmutableList();

            var persistentEntities = LargeAspectGraph.Select(o => o.PersistentIRI).ToImmutableList();
            var existing_list = LargeAspectVersionedGraph.ToImmutableList();
            var refs2 = edged_list.ReifyAllEdges(persistentEntities).ToImmutableList();
            var reified_json = from j in refs2 select j.ToJsonldJObject();
            var updateList = refs2.MakeUpdateList(existing_list).ToImmutableList();
            var reified_update = from j in updateList select j.ToJObject();
            Assert.Equal(test_size, updateList.Count());
            //var map = updateList.Union(existing_list).MakePersistentIriMap();
            var map = ImmutableDictionary.CreateRange(
                    from obj in existing_list
                    select new KeyValuePair<IRIReference, VersionedIRIReference>(obj.GetPersistentIRI(),
                        obj.VersionedIri)
                )
                .RemoveRange(updateList.Select(o => o.GetPersistentIRI()))
                .AddRange(
                    from obj in updateList
                    select new KeyValuePair<IRIReference, VersionedIRIReference>(obj.GetPersistentIRI(),
                        obj.VersionedIri)
                );
            var versionedUpdate = updateList.UpdateEdgeIris(map).ToImmutableList();
            var versioned_update_json = from j in versionedUpdate select j.ToJObject();
            var deleteList = EntityGraphComparer.MakeDeleteList(refs2, existing_list).ToImmutableList();
            Assert.Empty(deleteList);
        }

    }
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602
}