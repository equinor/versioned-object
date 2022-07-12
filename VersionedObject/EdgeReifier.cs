using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VersionedObject
{
    public static class EdgeReifier
    {
        /// <summary>
        /// Reifies a single JObject. That is, for the token and any child elements recursively removes any properties referring to the list of persistentIris and
        /// creates new PersistentEdge objects of those.
        /// </summary>
        private static readonly Func<(IEnumerable<JProperty>, IEnumerable<PersistentEdge>),
                Func<IRIReference, Func<IRIReference, PersistentEdge>>,
                JProperty, JObject, IEnumerable<IRIReference>,
                (IEnumerable<JProperty>, IEnumerable<PersistentEdge>)>
            ReifyJObject = (acc, MakeEdge, prop, obj, persistentIris) =>
                @obj.SelectToken("@id") switch
                {
                    JValue id => persistentIris.Any(i => i.ToString().Equals(id.ToString())) switch
                    {
                        true => (acc.Item1, acc.Item2.Append(MakeEdge(prop.Name)(id.ToString()))),
                        false => ReifyObjectChild(acc, prop, obj.Properties().ReifyEdges(MakeEdge, persistentIris))
                    },
                    _ => ReifyObjectChild(acc, prop, obj.Properties().ReifyEdges(MakeEdge, persistentIris))
                };


        /// <summary>
        /// Reifies a single JToken. That is, for the token and any child elements recursively removes any properties referring to the list of persistentIris and
        /// creates new PersistentEdge objects of those.
        /// </summary>
        private static readonly Func<IEnumerable<IRIReference>, Func<IRIReference, Func<IRIReference, PersistentEdge>>,
                Func<(IEnumerable<JProperty>, IEnumerable<PersistentEdge>), JProperty,
                    (IEnumerable<JProperty>, IEnumerable<PersistentEdge>)>>
            ReifyJToken =
                (persistentIris, MakeEdge) => (acc, prop) =>
                    prop.Name switch
                    {
                        "@id" or "@type" or VersionedObject.ProvWasDerivedFrom => (acc.Item1.Append(prop), acc.Item2),
                        _ => @prop.Value switch
                        {
                            JObject val =>
                                ReifyJObject(acc, MakeEdge, prop, val, persistentIris),
                            JValue val =>
                                ReifyPropertyChild(acc, MakeEdge(prop.Name), prop, val, persistentIris),
                            JArray vals =>
                                ReifyPropertyArray(acc, MakeEdge(prop.Name), MakeEdge, prop, vals, persistentIris),
                            _ => throw new Exception("Expected JObject, JValue or JArray")
                        }
                    };

        /// <summary>
        /// Removes any references to the persistentIris in the props argument, and adds all these to the list of edges
        /// </summary>
        /// <param name="props"></param>
        /// <param name="persistentIris"></param>
        /// <returns></returns>
        public static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyEdges(this IEnumerable<JProperty> props,
                Func<IRIReference, Func<IRIReference, PersistentEdge>> MakeEdge,
                IEnumerable<IRIReference> persistentIris) =>
                    props.Aggregate(
                        (new List<JProperty>(), new List<PersistentEdge>()),
                        ReifyJToken(persistentIris, MakeEdge),
                        acc => (acc.Item1, acc.Item2)
                    );


        private static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyPropertyArray((IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) acc, Func<IRIReference, PersistentEdge> MakeEdgeFromObject, Func<IRIReference, Func<IRIReference, PersistentEdge>> MakeEdge, JProperty prop, JArray vals, IEnumerable<IRIReference> persistentIris)
        {
            var edges =
                from v in (
                    from v in vals
                    where v != null
                    select v
                )
                select (Value: v, External:
                        from p in persistentIris
                        where p.ToString().Equals(v.ToString())
                        select p
                    );

            var externalEdges =
                from edge in edges
                where edge.External.Any()
                select MakeEdgeFromObject(edge.Value.ToString());

            var internalEdges =
                from edge in edges
                where !edge.External.Any()
                select ReifyJToken(persistentIris, MakeEdge)(acc, prop);

            return (acc.props.Append(new JProperty(prop.Name, new JArray(internalEdges))), acc.edges.Union(externalEdges));
        }

        private static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyPropertyChild((IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) acc, Func<IRIReference, PersistentEdge> MakeEdge, JProperty prop, JValue val, IEnumerable<IRIReference> persistentIris)
        {
            if (persistentIris.Any(i => i.ToString().Equals(val.ToString())))
                return (acc.props, acc.edges.Append(MakeEdge(val.ToString())));
            return (acc.props.Append(prop), acc.edges);
        }

        static (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) ReifyObjectChild((IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) acc, JProperty prop, (IEnumerable<JProperty> props, IEnumerable<PersistentEdge> edges) children) =>
            (acc.props.Append(new JProperty(prop.Name, new JObject(children.props))), acc.edges.Union(children.edges));

    }
}
