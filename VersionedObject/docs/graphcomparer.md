[To index](/README.md)

# GraphComparer
Library for comparing unversioned (persistent) input graph with versioned (existing) graph and creates update command for the aspect api.
The versioned objects that are created have only persistent IRIs inside them.

The methods HandleGraphCompleteUpdate and HandleGraphUpdate in [EntityGraphComparer](/VersionedObject/EntityGraphComparer.cs) can be called on the new/incoming data (in JSON-LD representation) with the single parameter being the existing data (in JSON-LD representation)

The assumption is that the existing data will usually be taken from some local storage or API, while the new data will be coming in from a different, not local storage.

Example usage is in [TestVersionedObject/FullTranslationTests.cs](TestVersionedObject/FullTranslationTests.cs), example data shown below:

This input json

```json
{
  "@graph": [
    {
      "@id": "http://data.example.com/project/Example/1234",
      "@type": "ontRow",
      "disciplineCode": "K",
      "functionArea": "Somewhere",
      "systemNumber": "12",
      "topTag": "YES"
    },
    {
      "@id": "http://data.example.com/project/Example/12345",
      "@type": "ontRow",
      "equipmentTypeDescription": "Thing",
      "functionArea": "Somwhere Else",
      "lengthOrTanTan_in_millimetre": "314",
      "tagNumber": "B-1234"
    },
    {
      "@id": "http://data.example.com/project/Example/123456",
      "@type": "ontRow",
      "functionArea": "In Space",
      "lengthOrTanTan_in_millimetre": "1",
      "netDryWeight_in_kilogram": "100",
      "subAreaCode": "XXYYZZ"
    },
    {
      "@id": "http://data.aibel.com/project/Example2/EQ_5690",
      "@type": "ontRow",
      "footprint_in_square_metre": "2",
      "functionArea": "Nowhere",
      "lengthOrTanTan_in_millimetre": "2"
    }
  ],
  "@context": {
    "@vocab": "http://example.com/ontology#",
    "sor": "http://rdf.equinor.com/ontology/sor#",
    "@version": "1.1"
  }
}
```

is handled by 
```c#
var updateBody = jsonLd.HandleGraphCompleteUpdate(existingGraph);
```
where existingGraph in this case is empty:

```json
{
  "@graph": []
}
```


And in return you get this, which is both versioned and packaged in and update and delete syntax:

```json
{
  "update": {
    "@graph": [
      {
        "@type": "http://example.com/ontology#ontRow",
        "http://example.com/ontology#disciplineCode": "K",
        "http://example.com/ontology#functionArea": "Somewhere",
        "http://example.com/ontology#systemNumber": "12",
        "http://example.com/ontology#topTag": "YES",
        "@id": "http://data.example.com/project/Example/1234/version/182149653431496461/1657618156",
        "http://www.w3.org/ns/prov#wasDerivedFrom": "http://www.w3.org/1999/02/22-rdf-syntax-ns#nil"
      },
      {
        "@type": "http://example.com/ontology#ontRow",
        "http://example.com/ontology#equipmentTypeDescription": "Thing",
        "http://example.com/ontology#functionArea": "Somwhere Else",
        "http://example.com/ontology#lengthOrTanTan_in_millimetre": "314",
        "http://example.com/ontology#tagNumber": "B-1234",
        "@id": "http://data.example.com/project/Example/12345/version/14089251491563981210/1657618156",
        "http://www.w3.org/ns/prov#wasDerivedFrom": "http://www.w3.org/1999/02/22-rdf-syntax-ns#nil"
      },
      {
        "@type": "http://example.com/ontology#ontRow",
        "http://example.com/ontology#functionArea": "In Space",
        "http://example.com/ontology#lengthOrTanTan_in_millimetre": "1",
        "http://example.com/ontology#netDryWeight_in_kilogram": "100",
        "http://example.com/ontology#subAreaCode": "XXYYZZ",
        "@id": "http://data.example.com/project/Example/123456/version/832241417010910130240/1657618156",
        "http://www.w3.org/ns/prov#wasDerivedFrom": "http://www.w3.org/1999/02/22-rdf-syntax-ns#nil"
      },
      {
        "@type": "http://example.com/ontology#ontRow",
        "http://example.com/ontology#footprint_in_square_metre": "2",
        "http://example.com/ontology#functionArea": "Nowhere",
        "http://example.com/ontology#lengthOrTanTan_in_millimetre": "2",
        "@id": "http://data.aibel.com/project/Example2/EQ_5690/version/20116124611320116621636/1657618156",
        "http://www.w3.org/ns/prov#wasDerivedFrom": "http://www.w3.org/1999/02/22-rdf-syntax-ns#nil"
      }
    ],
    "@context": {
      "@version": "1.1"
    }
  },
  "delete": []
}
```

The delete list is only populated by HandleGraphCompleteUpdate. If you dont want to delete elements, call HandleGraphUpdate
