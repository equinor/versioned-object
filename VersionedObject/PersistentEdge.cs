using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionedObject
{
    /// <summary>
    /// Represents a reified property which has become its own edge. 
    /// </summary>
    public class PersistentEdge : PersistentObjectData
    {
        public PersistentEdge(IRIReference s, IRIReference p, IRIReference o) : base(CreateEdgeIRI(s,p, o), CreateEdgeObject(s,p,o))
        { }

        private static IRIReference CreateEdgeIRI(IRIReference s, IRIReference p, IRIReference o)
            => new($"{s.ToString()}/{p.GetHashCode()}/{o.GetHashCode()}");

        private static JObject CreateEdgeObject(IRIReference s, IRIReference p, IRIReference o) =>
            new()
            {
                ["rdf:subject"] = s,
                ["rdf:predicate"] = p,
                ["rdf:object"] = o
            };
    }
    }
}
