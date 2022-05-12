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

        public string[] filter = { "@id", "http://www.w3.org/ns/prov#wasDerivedFrom", "asa:hasVersion" };

        public AspectObject(JObject jsonLdJObject) : this(jsonLdJObject.SelectToken("@id"), jsonLdJObject)
        { }
        public AspectObject(IRIReference persistentIRI, JObject content)
        {
            PersistentIRI = persistentIRI;
            Content = content.Children<JProperty>().Where(
                child => !filter.Contains(child.Name));
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
                Content.Append(new JProperty("@id", PersistentIRI.ToJValue()))
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

    }

    public class VersionedObject
    {
        public string Version { get; }
        public AspectObject Object { get; }
        public IEnumerable<JProperty> GetContent() =>
            Object.Content.Append(new JProperty("@id", GetVersionedIRI().ToJValue()));

        public VersionedObject(AspectObject persistent)
        {
            this.Object = persistent;
            this.Version = persistent.GetNewVersion();
        }

        public VersionedObject(IRIReference _VersionedIri, JObject content, IEnumerable<IRIReference> persistentIris)
        {
            this.Object = new AspectObject(_VersionedIri.GetPersistentUri(), content.RemoveVersionFromUris(persistentIris));
            this.Version = _VersionedIri.GetUriVersion();
        }

        public VersionedObject(JToken _VersionedIri, JObject content, IEnumerable<IRIReference> persistentIris) : this(new IRIReference(_VersionedIri.ToString()), content, persistentIris)
        { }
        public IRIReference GetVersionedIRI() =>
            new($"{this.Object.PersistentIRI}/{this.Version}");

        public IRIReference GetPersistentIRI() =>
            Object.PersistentIRI;

        public JObject ToJObject() =>
            new(GetContent());
    }

    public class ProvenanceObject : VersionedObject
    {
        public VersionedObject WasDerivedFrom { get; }
        public new IEnumerable<JProperty> GetContent() =>
            base.GetContent().Append(new JProperty("http://www.w3.org/ns/prov#wasDerivedFrom", WasDerivedFrom));

        public ProvenanceObject(AspectObject persistent, VersionedObject _WasDerivedFrom) : base(persistent)
        {
            this.WasDerivedFrom = _WasDerivedFrom;
        }

        /// <summary>
        /// I dont know if this is unnecessary
        /// </summary>
        /// <returns></returns>
        public new JObject ToJObject() =>
            new(GetContent());


    }
}
