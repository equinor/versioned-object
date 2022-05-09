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
    public class AspectEntity
    {
        public IRIReference PersistentIRI { get; }
        public IEnumerable<JProperty> Content { get; }

        public string[] filter = { "@id", "http://www.w3.org/ns/prov#wasDerivedFrom", "asa:hasVersion" };

        public AspectEntity(JObject jsonLdJObject) : this(jsonLdJObject.SelectToken("@id"), jsonLdJObject)
        { }
        public AspectEntity(IRIReference persistentIRI, JObject content)
        {
            PersistentIRI = persistentIRI;
            Content = content.Children<JProperty>().Where(
                child => !filter.Contains(child.Name));
        }

        public AspectEntity(JToken persistentIRI, JObject content) : this(new IRIReference(persistentIRI.ToString()), content)
        {
            ;
        }

        public bool SamePersistentIRI(AspectEntity other) =>
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
                Content.Append(new JProperty("@id", PersistentIRI))
            };




        public override bool Equals(object obj)
        {
            if (obj is VersionedEntity versioned)
                obj = versioned.Entity;
            if (obj is AspectEntity other)
            {
                return ToJsonldGraph().AspectEquals(other.ToJsonldGraph(), JsonLdHelper.RdfEqualsTriples);
            }
            return false;
        }

    }

    public class VersionedEntity
    {
        public string Version { get; }
        public AspectEntity Entity { get; }
        public IEnumerable<JProperty> GetContent() =>
            Entity.Content.Append(new JProperty("@id", GetVersionedIRI()));

        public VersionedEntity(AspectEntity persistent)
        {
            this.Entity = persistent;
            this.Version = persistent.GetNewVersion();
        }

        public VersionedEntity(IRIReference _VersionedIri, JObject content, IEnumerable<IRIReference> persistentIris)
        {
            this.Entity = new AspectEntity(_VersionedIri.GetPersistentUri(), content.RemoveVersionFromUris(persistentIris));
            this.Version = _VersionedIri.GetUriVersion();
        }

        public VersionedEntity(JToken _VersionedIri, JObject content, IEnumerable<IRIReference> persistentIris) : this(new IRIReference(_VersionedIri.ToString()), content, persistentIris)
        { }
        public IRIReference GetVersionedIRI() =>
            new($"{this.Entity.PersistentIRI}/{this.Version}");

        public IRIReference GetPersistentIRI() =>
            Entity.PersistentIRI;

        public JObject ToJObject() =>
            new(GetContent());
    }

    public class ProvenanceEntity : VersionedEntity
    {
        public VersionedEntity WasDerivedFrom { get; }
        public new IEnumerable<JProperty> GetContent() =>
            base.GetContent().Append(new JProperty("http://www.w3.org/ns/prov#wasDerivedFrom", WasDerivedFrom));

        public ProvenanceEntity(AspectEntity persistent, VersionedEntity _WasDerivedFrom) : base(persistent)
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
