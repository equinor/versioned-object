[To index](/README.md)
## EdgeReifier
Edge reification: Any reference to a different object is removed and replaced with an object that represents that relation. The relation-object gets the persistent IRI : <property-IRI>/subject-hash/object-hash
The class PersistentEdge represents such reified edges, while the class EdgeReifier contains the methods for reifying edges. The easiest way to use edge reification is to call the method PersistentObjectData.ReifyNodeEdges

Note that edge reification must happen before versioning if the history of edges is to be kept. 

It is technically no problemto use the versioning of objects without edge reification. Depending on the graph structure this might lead to changes in objects propagating along the edges in the graph, such that large parts of a graph must be updated whenever some or any objects are changed.