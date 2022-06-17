using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;

namespace VersionedObject
{
    /// <summary>
    /// Represents IRIs that can be the provenance of another object
    /// </summary>
    public interface ProvenanceIRI
    {
    }

    public record NoProvenance : ProvenanceIRI
    {
        private readonly IRIReference Iri = new("http://www.w3.org/1999/02/22-rdf-syntax-ns#nil");
    };

    public record Provenance(VersionedIRIReference provenance) : ProvenanceIRI
    {
        public static implicit operator Provenance(VersionedIRIReference prov) => new(prov);
    }

}
