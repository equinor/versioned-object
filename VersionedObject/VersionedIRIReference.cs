/*
Copyright 2022 Equinor ASA
This program is free software: you can redistribute it and/or modify it under the terms of version 3 of the GNU Lesser General Public License as published by the Free Software Foundation.
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

namespace VersionedObject;

/// <summary>
/// Represents IRIs that reference versioned, immutable objects
/// Theses IRIs consist of a first part, that is a normal IRIReference, followed by "/version/" then a hash of the object and finally "/" and an arbitrary version string (f.ex. a date)
/// </summary>
public class VersionedIRIReference : IRIReference
{
    public static explicit operator VersionedIRIReference(Uri uri) => new(uri);
    public static explicit operator VersionedIRIReference(string uriString) => new(uriString);

    public string VersionInfo { get; }
    public string VersionHash { get; }
    public IRIReference PersistentIRI { get; }

    public VersionedIRIReference(Uri uri) : this(uri.ToString())
    {
    }

    public VersionedIRIReference(string uriString) : base(uriString)
    {
        var segments = uriString.Split("/").Reverse();
        if (segments.Count() < 4 || !segments.ElementAt(2).Equals("version"))
            throw new ArgumentException($"Invalid syntax for versioned IRI: {uriString}");
        VersionInfo = segments.ElementAt(0);
        VersionHash = segments.ElementAt(1);
        PersistentIRI = new(ToString().Split("/").SkipLast(3).Aggregate((x, y) => $"{x}/{y}"));
    }

    public VersionedIRIReference(IRIReference uri, byte[] versionHash, long versionInfo) : base(
        $"{uri}/version/{string.Join("", versionHash)}/{versionInfo}")
    {
        VersionHash = string.Join("", versionHash);
        VersionInfo = versionInfo.ToString();
        PersistentIRI = uri;
    }

    public VersionedIRIReference(IRIReference uri, byte[] versionHash) : this(uri, versionHash,
        DateTimeOffset.Now.ToUnixTimeSeconds())
    {
    }

    public void Deconstruct(out IRIReference persistentIri, out string versionHash, out string versionInfo)
    {
        persistentIri = PersistentIRI;
        versionInfo = VersionInfo;
        versionHash = VersionHash;
    }
}

public static class IRIReferenceHelper {

    /// <summary>
    /// Helper method for making versioned IRI reference if possible
    /// Use of this method should be avoided whenever possible
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static IRIReference TryGetVersionedIriReference(string uri)
    {
        try
        {
            return new VersionedIRIReference(uri);
        }
        catch (ArgumentException _)
        {
            return new IRIReference(uri);
        }
    }
}