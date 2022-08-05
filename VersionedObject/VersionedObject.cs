/*
Copyright 2022 Equinor ASA
This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace VersionedObject;

public class VersionedObject
{
    public VersionedIRIReference VersionedIri { get; }
    public PersistentObjectData Object { get; }
    public IRIReference WasDerivedFrom { get; }

    public static readonly IRIReference NoProvenance = new("http://www.w3.org/1999/02/22-rdf-syntax-ns#nil");
    public const string ProvWasDerivedFrom = "http://www.w3.org/ns/prov#wasDerivedFrom";
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

    public VersionedObject(VersionedIRIReference versionedIri, JObject content, ImmutableHashSet<IRIReference> persistentIris, VersionedIRIReference wasDerivedFrom)
    {
        Object = new PersistentObjectData(versionedIri.PersistentIRI, JsonLdHelper.RemoveVersionFromObject(persistentIris)(content));
        VersionedIri = versionedIri;
        WasDerivedFrom = wasDerivedFrom;
    }

    public VersionedObject(VersionedIRIReference versionedIri, JObject content, ImmutableHashSet<IRIReference> persistentIris)
    {
        Object = new PersistentObjectData(versionedIri.PersistentIRI, JsonLdHelper.RemoveVersionFromObject(persistentIris)(content));
        VersionedIri = versionedIri;
        var props = content.Properties();
        var first = content.SelectToken("http://www.w3.org/ns/prov#wasDerivedFrom");
        var second = props.FirstOrDefault(child => child.Name.Equals("http://www.w3.org/ns/prov#wasDerivedFrom"));
        var WasDerivedFromString = second?.Value.Value<string>()
                                   ?? throw new InvalidJsonLdException(
                                       "Could not find required prov:wasDerivedFrom property in Json-LD object");

        WasDerivedFrom = new IRIReference(WasDerivedFromString);

    }

    public VersionedObject(JToken versionedIri, JObject content, ImmutableHashSet<IRIReference> persistentIris, VersionedIRIReference wasDerivedFrom) : this(new VersionedIRIReference(versionedIri.ToString()), content, persistentIris, wasDerivedFrom)
    { }


    public IRIReference GetPersistentIRI() =>
        Object.PersistentIRI;

    public JObject ToJObject() =>
        new(GetContent());
}