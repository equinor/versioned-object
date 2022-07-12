# <p align="center">versioned-object</p>
Library for handling and creating versioned objects and IRIs for use with the Aspect API

The library consists of three parts that can to some extent be used separately:
[IRIReference /docs/irireference.md](docs/irireference.md).
[Edge Reifier /docs/edgereifier.md](docs/edgereifier.md).




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


## EntityGraphComparer
Library for comparing unversioned (persistent) input graph with versioned (existing) graph and creates update command for the aspect api.
The versioned objects that are created have only persistent IRIs inside them.

The methods HandleGraphCompleteUpdate can be called on the new/incoming data (in JSON-LD representation) with the single parameter being the existing data (in JSON-LD representation)

The assumption is that the existing data will usually be taken from some local storage or API, while the new data will be coming in from a different, not local storage.

Example usage is in TestVersionedObject/VersionedObjectTests/TestFullTranslation
