# versioned-object
Library for handling and creating versioned objects and IRIs for use with the Aspect API

# IRIReference
Drop-in replacement for System.Uri for use with RDF. This is needed because URIs per definition and implementation do not take into account the fragment, especially for equality comparison

#VersionedIRIReference
This is used for IRIs that refer to immutable versioned objects. It is useful to separate this in a subclass of IRIReference since parsing of the IRI itself is not always enough. 

#EntityGraphComparer
Library for comparing unversioned (persistent) input graph with versioned (existing) graph and creates update command for the aspect api
