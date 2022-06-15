/*
Copyright 2022 Equinor ASA

This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
    public class AspectObject
    {
        public IRIReference PersistentIRI { get; }
        public IEnumerable<JProperty> Content { get; }

        public string[] filter = { "@id", "http://www.w3.org/ns/prov#wasDerivedFrom", "asa:hasVersion", "@type" };

        public AspectObject(JObject jsonLdJObject) : this(jsonLdJObject.SelectToken("@id"), jsonLdJObject)
        { }
        public AspectObject(IRIReference persistentIRI, JObject content)
        {
            PersistentIRI = persistentIRI;
            var tmp_content = content.Children<JProperty>()
                    .Where(child => !filter.Contains(child.Name));
            if (content.ContainsKey("@type") && content.SelectToken("@type") != null)
            {
                JArray types_array;

                if (content.SelectToken("@type").Type == JTokenType.Array)
                    types_array = content.SelectToken("@type").Value<JArray>();
                else
                    types_array = new JArray() { content.SelectToken("@type") };

                Content = tmp_content.Append(new JProperty("@type", types_array.Append(new JValue("https://rdf.equinor.com/ontology/aspect-api#Object"))));
            }
            else
                Content = tmp_content;

        }

        public AspectObject(JToken persistentIRI, JObject content) : this(new IRIReference(persistentIRI.ToString()), content)
        {
            ;
        }

        public bool SamePersistentIRI(AspectObject other) =>
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




        public override bool Equals(object obj)
        {
            if (obj is VersionedObject versioned)
                obj = versioned.Object;
            if (obj is AspectObject other)
            {
                return ToJsonldGraph().AspectEquals(other.ToJsonldGraph(), JsonLdHelper.RdfEqualsTriples);
            }
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    public class VersionedObject
    {
        public VersionedIRIReference VersionedIRI { get; }
        public AspectObject Object { get; }
        public IEnumerable<JProperty> GetContent() =>
            Object.Content.Append(new JProperty("@id", VersionedIRI.ToJValue()));

        public VersionedObject(AspectObject persistent)
        {
            Object = persistent;
            var versionHash = persistent.GetHash();
            VersionedIRI = new(persistent.PersistentIRI, versionHash);
        }

        public VersionedObject(VersionedIRIReference _VersionedIri, JObject content, IEnumerable<IRIReference> persistentIris)
        {
            this.Object = new AspectObject(_VersionedIri.PersistentIRI, content.RemoveVersionFromUris(persistentIris));
            VersionedIRI = _VersionedIri;
        }

        public VersionedObject(JToken _VersionedIri, JObject content, IEnumerable<IRIReference> persistentIris) : this(new VersionedIRIReference(_VersionedIri.ToString()), content, persistentIris)
        { }


        public IRIReference GetPersistentIRI() =>
            Object.PersistentIRI;

        public JObject ToJObject() =>
            new(GetContent());
    }

    public class ProvenanceObject : VersionedObject
    {
        public VersionedIRIReference WasDerivedFrom { get; }
        public new IEnumerable<JProperty> GetContent() =>
            base.GetContent().Append(new JProperty("http://www.w3.org/ns/prov#wasDerivedFrom", WasDerivedFrom));

        public ProvenanceObject(AspectObject persistent, VersionedObject _WasDerivedFrom) : base(persistent)
        {
            WasDerivedFrom = _WasDerivedFrom.VersionedIRI;
        }

        /// <summary>
        /// I dont know if this is unnecessary
        /// </summary>
        /// <returns></returns>
        public new JObject ToJObject() =>
            new(GetContent());


    }
}
