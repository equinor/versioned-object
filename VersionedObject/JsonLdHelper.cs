/*
Copyright 2022 Equinor ASA

This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using System.Data.HashFunction.CRC;
using System.Diagnostics;
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
            return RdfComparer(oldGraph, inputGraph);
        }


        /// <summary>
        /// Generic functions for applying a function to all JValues in json
        /// </summary>
        private static readonly Func<Func<JValue, JValue>, Func<JToken, JToken>> ChangeValuesInToken =
            valueChanger => token =>
                token switch
                {
                    JArray array => new JArray(array.Select(ChangeValuesInToken(valueChanger))),
                    JObject obj => ChangeValuesInObject(valueChanger)(obj),
                    JValue val => valueChanger(val),
                    _ => throw new InvalidJsonLdException($"Unknown json token {token}")
                };

        private static readonly Func<Func<JValue, JValue>, Func<JProperty, JProperty>> ChangeValuesInProperty =
            valueChanger => prop =>
                new JProperty(prop.Name, ChangeValuesInToken(valueChanger)(prop.Value));

        public static readonly Func<Func<JValue, JValue>, Func<JObject, JObject>> ChangeValuesInObject =
            valueChanger => versionedEntity =>
                new JObject(versionedEntity
                    .Properties()
                    .Select(ChangeValuesInProperty(valueChanger)));


        /// <summary>
        /// Removes the version suffix from all persistent URIs in the JObject
        /// </summary>
        public static JObject RemoveVersionsFromIris(this JObject versionedEntity, ImmutableHashSet<IRIReference> dict) =>
            ChangeValuesInObject(RemoveVersionFromValue(dict))(versionedEntity);

        /// <summary>
        /// Helper functions for removeing versions from objects
        /// </summary>
        private static readonly Func<ImmutableHashSet<IRIReference>, Func<JValue, JValue>> RemoveVersionFromValue =
            persistentUris => val =>
            {
                try
                {
                    var iri_object = new VersionedIRIReference(val.ToString());
                    return (persistentUris.Contains(iri_object.PersistentIRI)) ? iri_object.PersistentIRI : val;
                }
                catch (UriFormatException)
                {
                    return val;
                }
                catch (ArgumentException)
                {
                    return val;
                }
            };

        /// <summary>
        /// Helper functions for adding versions to objects
        /// </summary>
        private static readonly Func<ImmutableDictionary<IRIReference, VersionedIRIReference>, Func<JValue, JValue>>
            AddVersionToValue =
                uriMap => val =>
                {
                    try
                    {
                        return uriMap.ContainsKey(val.ToString()) ? uriMap[val.ToString()] : val;
                    }
                    catch (UriFormatException)
                    {
                        return val;
                    }
                };

        /// <summary>
        /// Adds the version suffix to all persistent URIs in the JObject
        /// </summary>
        public static JObject AddVersionsToUris(this JObject orig,
            ImmutableDictionary<IRIReference, VersionedIRIReference> map) =>
            ChangeValuesInObject(AddVersionToValue(map))(orig);

        public static byte[] GetHash(this IGraph g)
        {
            var writer = new NTriplesWriter();
            var graphString = VDS.RDF.Writing.StringWriter.Write(g, writer);
            var hasher = SHA1.Create();
            var triplesHash = graphString
                .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (IEnumerable<byte>)hasher.ComputeHash(Encoding.UTF8.GetBytes(x)))
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

        public static IRIReference GetIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));

        public static VersionedIRIReference GetVersionedIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));
    }
}

