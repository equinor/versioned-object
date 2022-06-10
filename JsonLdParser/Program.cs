// See https://aka.ms/new-console-template for more information

using System.Text.Json.Nodes;
using System;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace VersionedObject
{
    public class JsonLdParser
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
                throw new InvalidDataException("Usage: dotnet run JsonLdParser infile.jsonld out.ttl");

            var parser = new VDS.RDF.Parsing.JsonLdParser();
            var store = new TripleStore();
            parser.Load(store, args[0]);
            if (store.Graphs.Count < 1)
                throw new InvalidDataException("Input JSON contained no graphs, this is an error");
            var w = new CompressingTurtleWriter();
            w.Save(store.Graphs.First(), args[1]);

        }
    }
}