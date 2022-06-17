using Newtonsoft.Json.Linq;

namespace VersionedObject;

public class VersionedObject
{
    public VersionedIRIReference VersionedIri { get; }
    public PersistentObjectData Object { get; }
    public IRIReference WasDerivedFrom { get; }
    
    public static IRIReference NoProvenance = new("http://www.w3.org/1999/02/22-rdf-syntax-ns#nil");
    public static string ProvWasDerivedFrom = "http://www.w3.org/ns/prov#wasDerivedFrom";
    public IEnumerable<JProperty> GetContent() =>
        Object.Content.Append(new JProperty("@id", VersionedIri.ToJValue()))
            .Append(new JProperty(ProvWasDerivedFrom, WasDerivedFrom.ToJValue()));

    public VersionedObject(PersistentObjectData persistent) : this(persistent, NoProvenance)
    { }


    public VersionedObject(PersistentObjectData persistent, IRIReference provenance)
    {
        Object = persistent;
        var versionHash = persistent.GetHash();
        VersionedIri = new VersionedIRIReference(persistent.PersistentIRI, versionHash);
        WasDerivedFrom = provenance;
    }

    public VersionedObject(VersionedIRIReference versionedIri, JObject content, IEnumerable<IRIReference> persistentIris, VersionedIRIReference wasDerivedFrom)
    {
        Object = new PersistentObjectData(versionedIri.PersistentIRI, content.RemoveVersionFromUris(persistentIris));
        VersionedIri = versionedIri;
        WasDerivedFrom = wasDerivedFrom;
    }

    public VersionedObject(VersionedIRIReference versionedIri, JObject content, IEnumerable<IRIReference> persistentIris)
    {
        Object = new PersistentObjectData(versionedIri.PersistentIRI, content.RemoveVersionFromUris(persistentIris));
        VersionedIri = versionedIri;
        var props = content.Properties();
        var first = content.SelectToken("http://www.w3.org/ns/prov#wasDerivedFrom");
        var second = props.FirstOrDefault(child => child.Name.Equals("http://www.w3.org/ns/prov#wasDerivedFrom"));
        var WasDerivedFromString = second?.Value.Value<string>()
                                   ?? throw new InvalidJsonLdException(
                                       "Could not find required prov:wasDerivedFrom property in Json-LD object");

        WasDerivedFrom = new IRIReference(WasDerivedFromString);

    }

    public VersionedObject(JToken versionedIri, JObject content, IEnumerable<IRIReference> persistentIris, VersionedIRIReference wasDerivedFrom) : this(new VersionedIRIReference(versionedIri.ToString()), content, persistentIris, wasDerivedFrom)
    { }


    public IRIReference GetPersistentIRI() =>
        Object.PersistentIRI;

    public JObject ToJObject() =>
        new(GetContent());
}