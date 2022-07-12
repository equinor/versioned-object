# versioned-object
Library for handling and creating versioned objects and IRIs for use with the Aspect API

The library consists of three parts that can to some extent be used separately:

# IRIReference
Drop-in replacement for System.Uri for use with RDF. This is needed because URIs per definition and implementation do not take into account the fragment, especially for equality comparison

#VersionedIRIReference
This is used for IRIs that refer to immutable versioned objects. It is useful to separate this in a subclass of IRIReference since parsing of the IRI itself is not always enough. 
These IRIs are of the following form: 
{persistent base IRI}/version/{bytes of hash of object}/{arbitrary version numbering system}
The method for calculating the hash is in JsonLdHelper.GetHash. This hashing must be done in the same way everyhwere. The algorithm is this:
- Translate to n-triples (if ambiguous, in the same way as the default in dotnetrdf)
- Split the non-empty lines of n-triples into an array of lines. Each entry is a triple
- Run CRC64 on each such line, resulting in a byte array for each line
- Run xor of all these byte arrays
- Put all elements in the array after each other as string (String.Join in C#)


#EntityGraphComparer
Library for comparing unversioned (persistent) input graph with versioned (existing) graph and creates update command for the aspect api.
The versioned objects that are created have only persistent IRIs inside them.

The methods HandleGraphCompleteUpdate can be called on the new/incoming data (in JSON-LD representation) with the single parameter being the existing data (in JSON-LD representation)

The assumption is that the existing data will usually be taken from some local storage or API, while the new data will be coming in from a different, not local storage.

Example usage is in TestVersionedObject/VersionedObjectTests/TestFullTranslation

#EdgeReifier
Edge reification: Any reference to a different object is removed and replaced with an object that represents that relation. The relation-object gets the persistent IRI : <property-IRI>/subject-hash/object-hash
The class PersistentEdge represents such reified edges, while the class EdgeReifier contains the methods for reifying edges. The easiest way to use edge reification is to call the method PersistentObjectData.ReifyNodeEdges

Note that edge reification must happen before versioning if the history of edges is to be kept. 

It is technically no problemto use the versioning of objects without edge reification. Depending on the graph structure this might lead to changes in objects propagating along the edges in the graph, such that large parts of a graph must be updated whenever some or any objects are changed.