[To index](/README.md)
# IRIReference
Drop-in replacement for System.Uri for use with RDF. This is needed because URIs per definition and implementation do not take into account the fragment, especially for equality comparison, while in RDF the fragment is important.
* Code in [/VersionedObject/IRIReference.cs]
* Tests in [/TestVersionedObject/IRIReferenceTests.cs]

## Background
In the specifications for URIs, the "anchor", is any part after an #. This is not considered part of the Uri, and is specifically not considered when comparing Uris, f.ex. in C#s library. 
However, when using IRIs in semantic technology, the # is often used and the part after # must be included in equality comparisons. 
Therefore any correct built-in Uri implementation, like System.Uri in C# cannot be used indiscriminately for handling IRIs in semantic technology.

## VersionedIRIReference
This is used for IRIs that refer to immutable versioned objects. It is useful to separate this in a subclass of IRIReference since parsing of the IRI itself is not always enough. 
These IRIs are of the following form: 
{persistent base IRI}/version/{bytes of hash of object}/{arbitrary version numbering system}

The method for calculating the hash is in JsonLdHelper.GetHash. This hashing must be done in the same way everyhwere. The algorithm is this:
- Translate to n-triples (if ambiguous, in the same way as the default in dotnetrdf)
- Split the non-empty lines of n-triples into an array of lines. Each entry is a triple
- Run CRC64 on each such line, resulting in a byte array for each line
- Run xor of all these byte arrays
- Put all elements in the array after each other as string (String.Join in C#)

The constructors for VersionedIRIReference check for the pattern using the string "version" above and throw ArgumentException if invalid. 

THe helper method VersionedIRIReference.TryGetVersionedIRIReference will only instantiate VersionedIRIReference if syntax is valid, but it is usually safer to keep these types of object separate.