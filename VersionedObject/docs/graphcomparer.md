[To index](/README.md)

## GraphComparer
Library for comparing unversioned (persistent) input graph with versioned (existing) graph and creates update command for the aspect api.
The versioned objects that are created have only persistent IRIs inside them.

The methods HandleGraphCompleteUpdate can be called on the new/incoming data (in JSON-LD representation) with the single parameter being the existing data (in JSON-LD representation)

The assumption is that the existing data will usually be taken from some local storage or API, while the new data will be coming in from a different, not local storage.

Example usage is in TestVersionedObject/VersionedObjectTests/TestFullTranslation
