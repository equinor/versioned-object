using Newtonsoft.Json.Linq;
using System.Data.HashFunction.CRC;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
            return RdfComparer(oldGraph.AddAspectApiTriples(inputGraph), inputGraph.AddAspectApiTriples(oldGraph));
        }

        /// <summary>
        /// Removes the version suffix from all persistent URIs in the JObject
        /// </summary>
        public static JObject RemoveVersionFromUris(this JObject versionedEntity, IEnumerable<IRIReference> persistentUris) =>
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

        public static IGraph AddAspectApiTriples(this IGraph inputGraph, IGraph oldGraph)
        {
            //oldGraph.NamespaceMap.AddNamespace("rdf", new IRIReference("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            //oldGraph.NamespaceMap.AddNamespace("asa", new IRIReference("https://rdf.equinor.com/ontology/aspect-api#"));
            inputGraph.Assert(oldGraph.GetTriplesWithObject(oldGraph.CreateUriNode(new Uri("https://rdf.equinor.com/ontology/aspect-api#Object"))));
            return inputGraph;
        }
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

        /// <summary>
        /// Removes any references to the persistentIris in the props argument, and adds all these to the list of edges
        /// </summary>
        /// <param name="props"></param>
        /// <param name="persistentIris"></param>
        /// <returns></returns>
        public static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyEdges(this IEnumerable<JProperty> props,
            IEnumerable<IRIReference> persistentIris) =>
                props.Aggregate(
                    (new List<JProperty>(), new List<IRIReference>()),
                    ((IEnumerable<JProperty> props, IEnumerable<IRIReference> edges) acc, JProperty prop) =>
                        @prop.Value switch
                       {
                           JObject obj =>
                               ReifyObjectChild(acc, prop, obj.Properties().ReifyEdges(persistentIris)),
                           JValue val =>
                               ReifyPropertyChild(acc, prop, val, persistentIris),
                           JArray vals =>
                               ReifyPropertyArray(acc, prop, vals, persistentIris),
                            _  => throw new Exception("Expected JObject, JValue or JArray")                  
                       },
                       
                    acc => (acc.Item1, acc.Item2)
                );

        private static (IEnumerable<JProperty> props, IEnumerable<PersistendEdge> edges) ReifyPropertyArray((IEnumerable<JProperty> props, IEnumerable<IRIReference> edges) acc, JProperty prop, JArray vals, IEnumerable<IRIReference> persistentIris)
        {
            var externalEdges = vals
                .Select(v => v.Value<string>())
                .Where(v => v != null)
                .Cast<string>()
                .Where(v => persistentIris.Any(p => p.ToString().Equals(v)))
                .Select(v => new PersistentEdge(prop.Name, v));
            if (externalEdges.Any())
            {
                var internalEdges = vals
                    .Select(v => v.Value<string>())
                    .Where(v => v != null)
                    .Cast<string>()
                    .Where(v => !persistentIris.Any(p => p.ToString().Equals(v)))
                    .Select(v => new JValue(v));
                return (acc.props.Append(new JProperty(prop.Name, new JArray(internalEdges))), acc.edges.Union(externalEdges));
            }
            else
                return (acc.props.Append(prop), acc.edges);
        }

        private static (IEnumerable<JProperty> props, IEnumerable<IRIReference> edges) ReifyPropertyChild((IEnumerable<JProperty> props, IEnumerable<IRIReference> edges) acc, JProperty prop, JValue val, IEnumerable<IRIReference> persistentIris)
        {
            if (persistentIris.Any(i => i.ToString().Equals(val.ToString())))
                return (acc.props, acc.edges.Append(new IRIReference(val.ToString())));
            return (acc.props.Append(prop), acc.edges);
        }
        /// <summary>
        /// Any child objects are always just treated, even if the IRI is a persistent object.
        /// I am not sure how to handle this situation
        /// </summary>
        /// <param name="acc"></param>
        /// <param name="prop"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        static (IEnumerable<JProperty> props, IEnumerable<IRIReference> edges) ReifyObjectChild((IEnumerable<JProperty> props, IEnumerable<IRIReference> edges) acc, JProperty prop, (IEnumerable<JProperty> props, IEnumerable<IRIReference> edges) children) =>
            (acc.props.Append(new JProperty(prop.Name, new JObject(children.props))),acc.edges.Union(children.edges));
        
        public static IRIReference GetIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));

        public static VersionedIRIReference GetVersionedIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));
    }
}

