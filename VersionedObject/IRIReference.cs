using System.Runtime.CompilerServices;
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

    public JValue ToJValue() => new(uri);
    public JValue ToJToken() => ToJValue();

    /// <summary>
    /// Adds version suffix to IRI to create an identifier for an immutable version object
    /// The inverse operation is GetPersistentUri below
    /// </summary>
    public VersionedIRIReference AddVersionToUri(string versionHash, DateTime time) =>
        new($"{this}/version/{versionHash}/{time}");


    public VersionedIRIReference AddDatedVersionToUri(string versionHash) =>
        AddVersionToUri(versionHash, DateTime.Now);

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
/// Theses IRIs consist of a first part, that is a normal IRIReference, followed by "/version/" then a hash of the object and finally "/" and an arbitrary version string (f.ex. a date)
/// </summary>
public class VersionedIRIReference : IRIReference
{
    public static implicit operator VersionedIRIReference(Uri uri) => new(uri);
    public static implicit operator VersionedIRIReference(string uriString) => new(uriString);

    public string VersionDate { get; }
    public string VersionHash { get;  }

    public VersionedIRIReference(Uri uri) : this(uri.ToString())
    { }

    public VersionedIRIReference(string uriString) : base(uriString)
    {
        var segments = uriString.Split("/").Reverse();
        if(segments.Count() < 4 || !segments.ElementAt(2).Equals("version"))
            throw new ArgumentException($"Invalid syntax for versioned IRI: {uriString}");
        VersionDate = segments.ElementAt(0);
        VersionHash = segments.ElementAt(1);
    }

    
    /// <summary>
    /// Gets the persistent URI part of the versioned URI. This is the inverse of AddVersionToUri above
    /// </summary>
    public IRIReference GetPersistentUri() =>
        new(ToString().Split("/").SkipLast(3).Aggregate((x, y) => $"{x}/{y}"));


}
