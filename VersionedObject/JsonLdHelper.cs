using Newtonsoft.Json.Linq;
using System.Data.HashFunction.CRC;
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
            var oldGraph = LoadGraph(old.ToString());
            var inputGraph = LoadGraph(input.ToString());
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
                            .Replace(ent, versioned.VersionedIRI.ToString())
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

        public static IGraph LoadGraph(string valueAsString)
        {
            var parser = new VDS.RDF.Parsing.JsonLdParser();
            using var store = new TripleStore();

            using (TextReader reader = new StringReader(valueAsString))
                parser.Load(store, reader);

            if (store.Graphs.Count != 1)
                throw new InvalidDataException("Input JSON contained more than one graph, this is an error");

            return store.Graphs.First();
        }

        /**
        * Creates a new version string usable for this aspect object
        */
        public static byte[] GetHash(this AspectObject @object)
        {
            var graph = LoadGraph(@object.ToJsonldGraph().ToString());
            return graph.GetHash();
        }

        public static Uri GetJsonLdIRI(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new GraphEntityComparerException($"No @id field in object {jsonld}"));

    }
}

