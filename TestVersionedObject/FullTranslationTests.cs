using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.JsonLd;
using Xunit;
using static VersionedObject.EntityGraphComparer;
using static VersionedObject.JsonLdHelper;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602

namespace VersionedObject.Tests
{
    public class FullTranslationTests
    {
        [Fact]
        public void TestFullTranslation()
        {
            using var reader = new StreamReader("Data/data.ttl");
            var mel_text = reader.ReadToEnd();
            var mel = new Graph();

            mel.LoadFromString(mel_text);
            var opts = new JsonLdProcessorOptions();
            opts.OmitDefault = true;
            opts.ProcessingMode = VDS.RDF.JsonLd.Syntax.JsonLdProcessingMode.JsonLd11;
            var MelFrame = new JObject()
            {
                ["@context"] = new JObject()
                {
                    ["@vocab"] = "http://example.com/ontology#",
                    ["sor"] = "http://rdf.equinor.com/ontology/sor#",
                    ["@version"] = "1.1"
                },
                ["@type"] = new JArray() { "ontRow", "sor:File" }
            };

            var config = (Frame: MelFrame, Opts: opts);
            var store = new TripleStore();
            store.Add(mel);
            JArray aasjson = (new VDS.RDF.Writing.JsonLdWriter()).SerializeStore(store);

            var jsonLd = JsonLdProcessor.Frame(aasjson, config.Frame, config.Opts);

            var existingGraph = new JObject()
            {
                ["@graph"] = new JArray()
            };
            var updateBody = jsonLd.HandleGraphCompleteUpdate(existingGraph);
            Assert.Contains("update", updateBody);
            Assert.True(updateBody["update"].Value<JObject>().ContainsKey("@graph"));
            Assert.NotNull(updateBody["update"]["@graph"]);

            Assert.True(updateBody.ContainsKey("delete"));
            Assert.Empty(updateBody["delete"]);
            var updateGraph = updateBody["update"]["@graph"].Value<JArray>();
            Assert.Equal(4, updateGraph.Count());
        }
    }
}
#pragma warning restore
