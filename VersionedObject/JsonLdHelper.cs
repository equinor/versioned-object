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
using System.Text;
using System.Text.RegularExpressions;
using VDS.RDF;
using VDS.RDF.Writing;
using System;
using System.Xml.Linq;
using VDS.RDF.Query.Algebra;

namespace VersionedObject
{
    public static class JsonLdHelper
    {
        /// <summary>
        /// Checks equality of two JSON-LD objects by calculating and comparing hashes
        /// Adds the type and version information to the input object from the old object, 
        /// simulating the version control that should be in the clients
        /// </summary>
        public static bool RdfEqualsHash(JObject old, JObject input) =>
            input.GetHash()
                .SequenceEqual(old.GetHash());

        /// <summary>
        /// Checks Equality of two JSON-LD Objects (Not graphs)
        /// </summary>
        /// <param name="old"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool RdfEqualsTriples(JObject old, JObject input) =>
            RdfEqualsTriples(ParseJsonLdObject(old), ParseJsonLdObject(input));

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
        public static bool AspectEquals(this JObject old, JObject input, System.Func<JObject, JObject, bool> RdfComparer) =>
            RdfComparer(old, input);

        /// <summary>
        /// Generic functions for applying a function to all JValues in json
        /// </summary>
        private static readonly Func<Func<JObject, JObject>, Func<JValue, JValue>, Func<JToken, JToken>> ChangeValuesInToken =
            (objectChanger, valueChanger) => token =>
                token switch
                {
                    JArray array => new JArray(array.Select(ChangeValuesInToken(objectChanger, valueChanger))),
                    JObject obj => ChangeValuesInObject(objectChanger, valueChanger)(obj),
                    JValue val => valueChanger(val),
                    _ => throw new InvalidJsonLdException($"Unknown json token {token}")
                };

        private static readonly Func<Func<JObject, JObject>, Func<JValue, JValue>, Func<JProperty, JProperty>> ChangeValuesInProperty =
            (objectChanger, valueChanger) => prop =>
                new JProperty(prop.Name, ChangeValuesInToken(objectChanger, valueChanger)(prop.Value));

        public static readonly Func<Func<JObject, JObject>, Func<JValue, JValue>, Func<JObject, JObject>> ChangeValuesInObject =
            (objectChanger, valueChanger) => versionedEntity =>
                objectChanger(new JObject(versionedEntity
                    .Properties()
                    .Select(ChangeValuesInProperty(objectChanger, valueChanger))));

        //select: Func<Func<I,O>, Func<IEnumerable<I>,IEnumerable<O>> 
        /// <summary>
        /// Removes the version suffix from all persistent URIs in the JObject
        /// </summary>
        public static JObject RemoveVersionsFromIris(this JObject versionedEntity, ImmutableHashSet<IRIReference> dict) =>
            ChangeValuesInObject(x=>x, RemoveVersionFromValue(dict))(versionedEntity);

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
            ChangeValuesInObject(x => x, AddVersionToValue(map))(orig);

        public static IRIReference BlankNodeMarker =
            new("https://github.com/equinor/versioned-object/blob/develop/VersionedObject/docs/blanknode.md");

        public static JObject AddBlankNodeId(JObject orig) =>
            orig.IsBlankNode() ? orig.ReplaceIdValue(new IRIReference($"{BlankNodeMarker}#{orig.HashWithoutId()}")) : orig;
        
        public static bool IsBlankNode(this JObject orig) =>
            orig.SelectToken("@id") switch
            {
                JValue s => s.ToString().StartsWith("_:"),
                null => true,
                _ => throw new InvalidJsonLdException("Only JValue is allowed at \"@id\".")
            };

        /// <summary>
        /// Creates hashes to put in the "@id" position of all blank nodes of a top-level JSON-LD object
        /// </summary>
        /// <param name="orig">A JSON-Ld object</param>
        public static JObject HashBlankNodes(this JObject orig) =>
            ChangeValuesInObject(AddBlankNodeId, x => x)(orig);

        /// <summary>
        /// Creates hashes to put in the "@id" position of all blank nodes in a json-ld graph
        /// </summary>
        /// <param name="orig">A JSON-Ld graph</param>
        //public static JObject HashBlankNodesInGraph(this JObject orig) =>
        //    orig["@graph"] switch
        //    {
        //        null => ChangeValuesInObject(AddBlankNodeId, x => x)(orig),
        //        var graph => new JObject()
        //            {["@graph"] = ChangeValuesInToken(AddBlankNodeId, x => x)(graph)}
        //    };


        /// <summary>
        /// replaces "@id" with the newId in the object
        /// </summary>
        public static JObject ReplaceIdValue(this JObject orig, IRIReference newId) =>
            new(orig.Properties()
                .Where(p => !p.Name.Equals("@id"))
                .Append(new JProperty("@id", newId.ToJValue()))
            );

        public static string HashWithoutId(this JObject obj) =>
            string.Join(
                "", 
                obj.ReplaceIdValue(BlankNodeMarker).GetHash()
                );

        /// <summary>
        /// Gets the hash on a JSON-LD Top-level object
        /// </summary>
        public static byte[] GetHash(this JObject orig)
        {
            var hashed = orig.HashBlankNodes();
            var g = ParseJsonLdObject(hashed);

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

        /// <summary>
        /// Parses a JSON-LD Object (not graph) into a dotnet IGraph
        /// </summary>
        /// <param name="jsonLdObject"></param>
        /// <returns></returns>
        public static IGraph ParseJsonLdObject(JObject jsonLdObject) =>
            ParseJsonLdGraph(new()
            {
                ["@graph"] = new JArray()
                    {jsonLdObject}
            }
            );

        /// <summary>
        /// Parses a string describing a json-ld graph into dotnet IGraph
        /// </summary>
         public static IGraph ParseJsonLdGraph(JObject jsonLdGraph)
        {
            var parser = new VDS.RDF.Parsing.JsonLdParser();
            using var store = new TripleStore();
            try
            {
                using (TextReader reader = new StringReader(jsonLdGraph.ToString()))
                    parser.Load(store, reader);
            }
            catch (NullReferenceException e)
            {
                throw new InvalidJsonLdException($"Invalid JSON-LD in {jsonLdGraph.ToString()}");
            }

            if (store.Graphs.Count != 1)
                throw new InvalidDataException("Input JSON contained more than one graph, this is an error");

            return store.Graphs.First();
        }

        /**
        * Creates a new version string usable for this aspect object
        */
        public static byte[] GetHash(this PersistentObjectData @object)
        {
            return @object.ToJsonldJObject().GetHash();
        }

        public static IRIReference GetIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));

        public static VersionedIRIReference GetVersionedIRIReference(this JToken jsonld) =>
            new(jsonld.SelectToken("@id")?.ToString() ?? throw new InvalidJsonLdException($"No @id field in object {jsonld}"));
    }
}

