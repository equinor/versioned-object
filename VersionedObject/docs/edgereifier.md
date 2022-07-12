[To index](/README.md)
# EdgeReifier
Edge reification: Any reference to a different object is removed and replaced with an object that represents that relation. The relation-object gets the persistent IRI : <property-IRI>/subject-hash/object-hash
The class PersistentEdge represents such reified edges, while the class EdgeReifier contains the methods for reifying edges. The easiest way to use edge reification is to call the method PersistentObjectData.ReifyNodeEdges

Note that edge reification must happen before versioning if the history of edges is to be kept. 

It is technically no problemto use the versioning of objects without edge reification. 
Depending on the graph structure this might lead to changes in objects propagating along the edges in the graph, such that large parts of a graph must be updated whenever some or any objects are changed.

## Example
The method TestEdgeReifier in [VersionedObjectTests](/TestVersionedObject/VersionedObjectTests.cs) includes examples of using the edge reifier:

This json as input:
```json
{
  "@graph": [
    {
      "@id": "sor:Row1",
      "@type": "MelRow",
      "rdfs:label": "An empty MEL Row",
      "imf:hasChild": "http://rdf.equinor.com/ontology/sor#Row2"
    },
    {
      "@id": "sor:Row2",
      "@type": "MelRow",
      "rdfs:label": "The second MEL Row"
    }
  ],
  "@context": {
    "rdfs": "http://www.w3.org/2000/01/rdf-schema#",
    "@vocab": "http://rdf.equinor.com/ontology/mel#",
    "sor": "http://rdf.equinor.com/ontology/sor#",
    "imf": "http://imf.imfid.org/ontology/imf#",
    "@version": "1.1"
  }
}
```

Is processed by parsing it into the JObject EdgeJsonLd and then:

```c#
var edged_list = EdgeJsonLd.GetInputGraphAsEntities();
var persistentEntities = GetAllPersistentIris(EdgeJsonLd, ExistingJsonLd);
var refs2 = edged_list.ReifyAllEdges(persistentEntities);
```

* GetInputGraphAsEntities extracts a list of all top-level objects in the input json-ld
* GetAllPersistentIris extracts all the persistent IRIs in the two graphs (EdgeJsonLd + an existing ExistingJsonLd assumed extracated from some local storage)
* ReifyAllEdges actually reifies the edges ,and results in these three objects:

```json
{
  "rdf:subject": "http://rdf.equinor.com/ontology/sor#Row1",
  "rdf:predicate": "http://imf.imfid.org/ontology/imf#hasChild",
  "rdf:object": "http://rdf.equinor.com/ontology/sor#Row2",
  "@id": "http://rdf.equinor.com/ontology/sor#Row1/1765549990/-970849053"
}
```
```json
{
  "@type": "http://rdf.equinor.com/ontology/mel#MelRow",
  "http://www.w3.org/2000/01/rdf-schema#label": "An empty MEL Row",
  "@id": "http://rdf.equinor.com/ontology/sor#Row1"
}
```
```json
{
  "@type": "http://rdf.equinor.com/ontology/mel#MelRow",
  "http://www.w3.org/2000/01/rdf-schema#label": "The second MEL Row",
  "@id": "http://rdf.equinor.com/ontology/sor#Row2"
}
```
