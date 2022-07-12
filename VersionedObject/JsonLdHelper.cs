/*
Copyright 2022 Equinor ASA

This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
using Newtonsoft.Json.Linq;
using System.Data.HashFunction.CRC;
using System.Text;
using System.Text.RegularExpressions;
using VDS.RDF;
using VDS.RDF.Writing;

namespace VersionedObject
{
    public static class JsonLdHelper
    {
        /// <summary>
        /// Checks equality of two JSON-LD objects by calculating and comparing hashes
        /// Adds the type and version information to the input object from the old object, 
        /// simulating the version control that should be in the clients
        /// </summary>
        public static bool RdfEqualsHash(IGraph old, IGraph input) =>
            input.GetHash()
                .SequenceEqual(old.GetHash());

        /// <summary>
        /// Compares two json-ld objects by checking the triples are the same
        /// </summary>
        public static bool RdfEqualsTriples(IGraph old, IGraph input) =>
            old.Triples.Count() != input.Triples.Count() ||
            !old.Triples.Any(triple => !input.ContainsTriple(triple));

        /// <summary>
        /// Checks equality of two JSON-LD objects 
        /// Adds the type and version information to the input object from the old object, 
        /// simulating the version control that should be in the clients
        /// </summary>
        public static bool AspectEquals(this JObject old, JObject input, System.Func<IGraph, IGraph, bool> RdfComparer)
        {
            var oldGraph = ParseJsonLdString(old.ToString());
            var inputGraph = ParseJsonLdString(input.ToString());
            return RdfComparer(oldGraph, inputGraph);
        }

        /// <summary>
        /// Removes the version suffix from all persistent URIs in the JObject
        /// </summary>
        public static JObject RemoveVersionFromUris(this JObject versionedEntity,
            IEnumerable<IRIReference> persistentUris) =>
            JObject.Parse(persistentUris
                .Aggregate(versionedEntity.ToString(),
                    (ent, persistent) =>
                        new Regex($"{persistent.ToString().Replace(".", "\\.")}/\\w+")
                            .Replace(ent, persistent.ToString())
                )
            );

        /// <summary>
        /// Adds the version suffix to all persistent URIs in the JObject
        /// </summary>
        public static JObject AddVersionToUris(this JObject persistentEntity, IEnumerable<VersionedObject> entities) =>
            JObject.Parse(entities
                .Aggregate(persistentEntity.ToString(),
                    (ent, versioned) =>
                        new Regex($"{versioned.GetPersistentIRI().ToString().Replace(".", "\\.")}/\\w+")
                            .Replace(ent, versioned.VersionedIri.ToString())
                )
            );

        public static byte[] GetHash(this IGraph g)
        {
            var writer = new NTriplesWriter();
            var graphString = VDS.RDF.Writing.StringWriter.Write(g, writer);
            var crcFactory = CRCFactory.Instance;
            var hasher = crcFactory.Create(CRCConfig.CRC64);
            var triplesHash = graphString
                .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (IEnumerable<byte>)hasher.ComputeHash(Encoding.UTF8.GetBytes(x)).Hash)
                .Aggregate((x, y) => x.Zip(y, (l1, l2) => (byte)(l1 ^ l2)));
            return triplesHash.ToArray();

        }

        public static IGraph ParseJsonLdString(string jsonLdString)
        {
            var parser = new VDS.RDF.Parsing.JsonLdParser();
            using var store = new TripleStore();

            using (TextReader reader = new StringReader(jsonLdString))
                parser.Load(store, reader);

            if (store.Graphs.Count != 1)
                throw new InvalidDataException("Input JSON contained more than one graph, this is an error");

            return store.Graphs.First();
        }

        /**
        * Creates a new version string usable for this aspect object
        */
        public static byte[] GetHash(this PersistentObjectData @object)
        {
            var graph = ParseJsonLdString(@object.ToJsonldGraph().ToString());
            return graph.GetHash();
        }

        private static readonly Func<IEnumerable<IRIReference>, Func<IRIReference, Func<IRIReference, PersistentEdge>>,
                Func<(IEnumerable<JProperty>, IEnumerable<PersistentEdge>), JProperty,
                    (IEnumerable<JProperty>, IEnumerable<PersistentEdge>)>>
            reifyJToken =
                (persistentIris, MakeEdge) => (acc, prop) =>
                    prop.Name switch
                    {
                        "@id" or "@type" => (acc.Item1.Append(prop), acc.Item2),
                        _ => @prop.Value switch
                        {
                            JObject obj =>
                                @obj.SelectToken("@id") switch
                                {
                                    JValue id => persistentIris.Any(i => i.ToString().Equals(id.ToString())) switch
                                    {
                                        true => (acc.Item1, acc.Item2.Append(MakeEdge(prop.Name)(id.ToString()))),
                                        false => ReifyObjectChild(acc, prop, obj.Properties().ReifyEdges(MakeEdge, persistentIris))
                                    },
                                    _ => ReifyObjectChild(acc, prop, obj.Properties().ReifyEdges(MakeEdge, persistentIris))
                                },
                            JValue val =>
                                ReifyPropertyChild(acc, MakeEdge(prop.Name), prop, val, persistentIris),
                            JArray vals =>
                                ReifyPropertyArray(acc, MakeEdge(prop.Name), MakeEdge, prop, vals, persistentIris),
                            _ => throw new Exception("Expected JObject, JValue or JArray")
                        }
                    };

        /// <summary>
        /// Removes any references to the persistentIris in the props argument, and adds all these to the list of edges
        /// </summary>
        /// <param name="props"></param>
        /// <param name="persistentIris"></param>
        /// <returns></returns>
        public static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyEdges(this IEnumerable<JProperty> props,
                Func<IRIReference, Func<IRIReference, PersistentEdge>> MakeEdge,
                IEnumerable<IRIReference> persistentIris) =>
                    props.Aggregate(
                        (new List<JProperty>(), new List<PersistentEdge>()),
                        reifyJToken(persistentIris, MakeEdge),
                        acc => (acc.Item1, acc.Item2)
                    );


        private static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyPropertyArray((IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) acc, Func<IRIReference, PersistentEdge> MakeEdgeFromObject, Func<IRIReference, Func<IRIReference, PersistentEdge>> MakeEdge, JProperty prop, JArray vals, IEnumerable<IRIReference> persistentIris)
        {
            var edges =
                from v in (
                    from v in vals
                    where v != null
                    select v
                )
                select (Value: v, External:
                        from p in persistentIris
                        where p.ToString().Equals(v.ToString())
                        select p
                    );

            var externalEdges =
                from edge in edges
                where edge.External.Any()
                select MakeEdgeFromObject(edge.Value.ToString());

            var internalEdges =
                from edge in edges
                where !edge.External.Any()
                select reifyJToken(persistentIris, MakeEdge)(acc, prop);

            return (acc.props.Append(new JProperty(prop.Name, new JArray(internalEdges))), acc.edges.Union(externalEdges));
        }

        private static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyPropertyChild((IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) acc, Func<IRIReference, PersistentEdge> MakeEdge, JProperty prop, JValue val, IEnumerable<IRIReference> persistentIris)
        {
            if (persistentIris.Any(i => i.ToString().Equals(val.ToString())))
                return (acc.props, acc.edges.Append(MakeEdge(val.ToString())));
            return (acc.props.Append(prop), acc.edges);
        }

        static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyObjectChild((IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) acc, JProperty prop, (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) children) =>
            (acc.props.Append(new JProperty(prop.Name, new JObject(children.props))), acc.edges.Union(children.edges));

        public static IRIReference GetIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));

        public static VersionedIRIReference GetVersionedIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));
    }
}

