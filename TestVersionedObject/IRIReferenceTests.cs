using System;
using System.Text;
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
            var uri = new VersionedIRIReference("http://rdf.equinor.com/data/objectx/version/12345/2022-05-01");
            var uri2 = new Uri("http://rdf.equinor.com/data/objectx/version/12345/2022-05-01");
            var grafuri = new Uri("asa:Scope");
            var grafiri = new IRIReference("asa:Scope");
            var uri_in_iri = new IRIReference(new Uri("asa:Scope"));
            Assert.Equal(grafuri, grafiri.uri);
            Assert.Equal(uri_in_iri, grafiri);
            var versionHash = "12345";
            var versionDate = "2022-05-01";
            Assert.Equal(uri, (IRIReference)uri2);
            Assert.Equal(versionHash, uri.VersionHash);
            Assert.Equal(versionDate, uri.VersionInfo);
            Assert.Equal(new IRIReference("http://rdf.equinor.com/data/objectx"), uri.PersistentIRI);
        }

        [Fact]
        public void TestFragmentVersionedUri()
        {
            var uri = new VersionedIRIReference("http://rdf.equinor.com/data#objectx/version/12345/2022-05-01");
            var version = "12345";
            Assert.Equal(version, uri.VersionHash);
            Assert.Equal(new IRIReference("http://rdf.equinor.com/data#objectx"), uri.PersistentIRI);
        }

        [Fact]
        public void TestGeneratedVersionedUri()
        {
            var persistentIri = new IRIReference("http://rdf.equinor.com/data/objectx");
            var versionedIri = new VersionedIRIReference(persistentIri, Encoding.UTF8.GetBytes("12345"), DateTimeOffset.Now.ToUnixTimeSeconds());
            var versionDate = versionedIri.VersionInfo;
            var versionHash = versionedIri.VersionHash;
            var composedIri = new Uri($"{persistentIri}/version/{versionHash}/{versionDate}");
            Assert.Equal((IRIReference)composedIri, versionedIri);
            Assert.Equal(persistentIri, versionedIri.PersistentIRI);
        }

        [Fact]
        public void TestHandmadeSerialization()
        {
            var uri = new IRIReference("http://rdf.equinor.com/data/objectx/version/12345/2022-06-08");
            var uri2 = new Uri("http://rdf.equinor.com/data/objectx/version/12345/2022-06-08");
            var uri3 = (IRIReference)"http://rdf.equinor.com/data/objectx/version/12345/2022-06-08";
            var uriJson = JsonConvert.SerializeObject(uri);
            var uri2Json = JsonConvert.SerializeObject(uri2);

            Assert.NotEqual(uriJson, uri2Json);
            Assert.Equal(uri, uri3);
            var generatedUri = JsonConvert.DeserializeObject<IRIReference>(uriJson);
            Assert.Equal(uri, generatedUri);
        }

        [Fact]
        public void TestDeconstructor()
        {
            var persistentIri = new IRIReference("http://rdf.equinor.com/data/objectx");
            var dateNow = DateTimeOffset.Now.ToUnixTimeSeconds();
            var hashFake = Encoding.UTF8.GetBytes("12345");
            var versionedIri = new VersionedIRIReference(persistentIri, hashFake, dateNow);
            var (persComponent, versionHash, versionDate) = versionedIri;
            Assert.Equal(persistentIri, persComponent);
            Assert.Equal(string.Join("", hashFake), versionHash);
            Assert.Equal(versionDate, dateNow.ToString());
        }
    }
}