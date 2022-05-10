using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace VersionedObject;

/// <summary>
/// Handles "versioned IRIs". These identify immutable sets of data about an object.
/// The versioned IRIs have a slash followd by a unique version ID suffixed to the persistent IRI of the object
/// If for example: "http://rdf.equinor.com/data/objectx" is an object, then "http://rdf.equinor.com/data/objectx/12345" is a versioned IRI for version "12345"
///
/// Also useful for other IRIs because a fragment is not part of a URI, and URI.Equals ignores the fragment.
/// A URI Reference includes the fragment
/// </summary>
[Serializable]
public class IRIReference : IEquatable<IRIReference>
{
    public Uri uri { get; set; }

    public static implicit operator IRIReference(Uri uri) => new(uri);
    public static implicit operator IRIReference(string uri) => new(uri);
    public static implicit operator Uri(IRIReference r) => r.uri;

    public static implicit operator JValue(IRIReference r) => r.ToJValue();

    bool IEquatable<IRIReference>.Equals(IRIReference? other) =>
        (other != null) && ToString().Equals(other.ToString());
    
    public override string ToString() => uri.ToString();

    public JValue ToJValue() => new (uri);
    public JValue ToJToken() => ToJValue();

    /// <summary>
    /// Adds version suffix to IRI to create an identifier for an immutable version object
    /// The inverse operation is GetPersistentUri below
    /// </summary>
    public VersionedIRIReference AddVersionToUri(string version) =>
        new($"{this}/{version}");

    /// <summary>
    /// Cannot use Uri.getHashCode since that ignores the fragment
    /// </summary>
    public override int GetHashCode() => ToString().GetHashCode();

    [Newtonsoft.Json.JsonConstructor]
    public IRIReference(Uri uri)
    {
        this.uri = uri;
    }
    public IRIReference(string uriString)
    {
        uri = new Uri(uriString);
    }
}
/// <summary>
/// Represents IRIs that reference versioned, immutable objects
/// </summary>
public class VersionedIRIReference : IRIReference
{
    public VersionedIRIReference(Uri uri) : base(uri)
    { }
    public VersionedIRIReference(string UriString) : base(UriString)
    { }

    /// <summary>
    /// Gets the version part of the versioned URI (See also AddVersionToUri above)
    /// </summary>
    public string GetUriVersion() => ToString().Split("/").Last();

    /// <summary>
    /// Gets the persistent URI part of the versioned URI. This is the inverse of AddVersionToUri above
    /// </summary>
    public IRIReference GetPersistentUri() =>
        new(ToString().Split("/").SkipLast(1).Aggregate((x, y) => $"{x}/{y}"));
}
