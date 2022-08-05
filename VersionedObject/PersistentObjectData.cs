/*
Copyright 2022 Equinor ASA
This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using VDS.RDF;
using VDS.RDF.Writing;
using VersionedObject;

namespace VersionedObject
{
    public class PersistentObjectData
    {
        public IRIReference PersistentIRI { get; }
        public IEnumerable<JProperty> Content { get; }

        public string[] filter = { "@id", "http://www.w3.org/ns/prov#wasDerivedFrom" };

        public PersistentObjectData(IRIReference persistentIRI, JObject content)
        {
            PersistentIRI = persistentIRI;
            Content = content.Children<JProperty>()
                    .Where(child => !filter.Contains(child.Name));
        }

        public PersistentObjectData(JToken persistentIRI, JObject content) : this(new IRIReference(persistentIRI.ToString()), content)
        { }

        /// <summary>
        /// Adds versions to all references to persistent IRIs in the map argument
        /// </summary>
        public PersistentObjectData(PersistentObjectData orig,
            ImmutableDictionary<IRIReference, VersionedIRIReference> map) : this(orig.PersistentIRI, new JObject(orig.Content).AddVersionsToUris(map))
        { }

        public bool SamePersistentIRI(PersistentObjectData other) =>
            PersistentIRI.ToString().Equals(other.PersistentIRI.ToString());

        public JObject ToJsonldGraph() =>
            new()
            {
                ["@graph"] = new JArray()
                {
                ToJsonldJObject()
                }
            };

        public JObject ToJsonldJObject() =>
            new()
            {
                Content
                    .Append(new JProperty("@id", PersistentIRI.ToJValue()))
            };
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is VersionedObject versioned)
                obj = versioned.Object;
            if (obj is PersistentObjectData other)
            {
                return ToJsonldGraph().AspectEquals(other.ToJsonldGraph(), JsonLdHelper.RdfEqualsTriples);
            }
            return false;
        }

        public IEnumerable<PersistentObjectData> ReifyNodeEdges(IEnumerable<IRIReference> persistentIris)
        {
            var (props, edges) = Content.ReifyEdges(PersistentEdge.MakePersistentEdge(PersistentIRI), persistentIris);
            return edges
                .Append(new PersistentObjectData(PersistentIRI, new JObject(props)));
        }
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
