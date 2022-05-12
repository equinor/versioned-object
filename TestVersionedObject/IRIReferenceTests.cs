using System;
using Xunit;
using Newtonsoft.Json;
using VersionedObject;

namespace VersionedObject.Tests
{
    public class IRIReferenceTests
    {
        [Fact]
        public void TestHandmadeVersionedUri()
        {
            var uri = new VersionedIRIReference("http://rdf.equinor.com/data/objectx/12345");
            var uri2 = new Uri("http://rdf.equinor.com/data/objectx/12345");
            var grafuri = new Uri("asa:Scope");
            var grafiri = new IRIReference("asa:Scope");
            var uri_in_iri = new IRIReference(new Uri("asa:Scope"));
            Assert.Equal(grafuri, grafiri.uri);
            Assert.Equal(uri_in_iri, grafiri);
            var version = "12345";
            Assert.Equal(uri, (IRIReference)uri2);
            Assert.Equal(version, uri.GetUriVersion());
            Assert.Equal(new IRIReference("http://rdf.equinor.com/data/objectx"), uri.GetPersistentUri());
        }

        [Fact]
        public void TestFragmentVersionedUri()
        {
            var uri = new VersionedIRIReference("http://rdf.equinor.com/data#objectx/12345");
            var version = "12345";
            Assert.Equal(version, uri.GetUriVersion());
            Assert.Equal(new IRIReference("http://rdf.equinor.com/data#objectx"), uri.GetPersistentUri());
        }

        [Fact]
        public void TestGeneratedVersionedUri()
        {
            var persistentIri = new IRIReference("http://rdf.equinor.com/data/objectx");
            var versionedIri = persistentIri.AddVersionToUri("12345");
            var version = versionedIri.GetUriVersion();
            var composedIri = new Uri($"{persistentIri}/{version}");
            Assert.Equal((IRIReference)composedIri, versionedIri);
            Assert.Equal(persistentIri, versionedIri.GetPersistentUri());
        }

        [Fact]
        public void TestHandmadeSerialization()
        {
            var uri = new IRIReference("http://rdf.equinor.com/data/objectx/12345");
            var uri2 = new Uri("http://rdf.equinor.com/data/objectx/12345");
            var uri3 = (IRIReference)"http://rdf.equinor.com/data/objectx/12345";
            var uriJson = JsonConvert.SerializeObject(uri);
            var uri2Json = JsonConvert.SerializeObject(uri2);

            Assert.NotEqual(uriJson, uri2Json);
            Assert.Equal(uri, uri3);
            var generatedUri = JsonConvert.DeserializeObject<IRIReference>(uriJson);
            Assert.Equal(uri, generatedUri);
        }
    }
}