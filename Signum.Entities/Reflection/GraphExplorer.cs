using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using System.Linq;

namespace Signum.Entities.Reflection
{
    public static class GraphExplorer
    {
        public static void PropagateModifications(DirectedGraph<Modifiable> inverseGraph)
        {
            if (inverseGraph == null)
                throw new ArgumentNullException("inverseGraph");

            foreach (Modifiable item in inverseGraph)
                if (item.SelfModified && !(item is IdentifiableEntity))
                    Propagate(item, inverseGraph); 
        }

        private static void Propagate(Modifiable item, DirectedGraph<Modifiable> inverseGraph)
        {
            item.Modified = true;
            if (!(item is IdentifiableEntity))
                foreach (var other in inverseGraph.RelatedTo(item))
                    Propagate(other, inverseGraph);
        }

        //public static string GraphIntegrityCheck(this Modifiable modifiable, Func<Modifiable, IEnumerable<Modifiable>> explorer)
        //{
        //    DirectedGraph<Modifiable> eg = DirectedGraph<Modifiable>.Generate(modifiable, explorer);

        //    string result = eg.Select(m => new { m, Error = m.IntegrityCheck() })
        //        .Where(p => p.Error.HasText())
        //        .ToString(p => "{0}\r\n{1}".Formato(eg.ShortestPath(modifiable, p.m).PreAnd(modifiable).ToString(e=>e.ToString().DefaultText("[...]"), "->"), p.Error.Indent(3)), "\r\n");

        //    return result;
        //}



        public static DirectedGraph<Modifiable> FromRootIdentifiable(Modifiable root)
        {
            return DirectedGraph<Modifiable>.Generate(root, ModifyInspector.IdentifiableExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static DirectedGraph<Modifiable> FromRoot(Modifiable root)
        {
            return DirectedGraph<Modifiable>.Generate(root, ModifyInspector.FullExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static DirectedGraph<Modifiable> FromRoots<T>(IEnumerable<T> roots)
            where T : Modifiable
        {
            return DirectedGraph<Modifiable>.Generate(roots.Cast<Modifiable>(), ModifyInspector.FullExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static DirectedGraph<Modifiable> FromRootsIdentifiable<T>(IEnumerable<T> roots)
            where T : Modifiable
        {
            return DirectedGraph<Modifiable>.Generate(roots.Cast<Modifiable>(), ModifyInspector.IdentifiableExplore, ReferenceEqualityComparer<Modifiable>.Default);
        }

        public static Dictionary<ModifiableEntity, string> IntegrityDictionary(DirectedGraph<Modifiable> graph)
        {
            var result = graph.OfType<ModifiableEntity>().Select(m => new { m, Error = m.IntegrityCheck() })
                .Where(p => p.Error.HasText()).ToDictionary(a => a.m, a => a.Error);

            return result;
        }

        public static string IdentifiableIntegrityCheck(DirectedGraph<Modifiable> graph)
        {
            return graph.OfType<ModifiableEntity>().Select(m => m.IntegrityCheck()).Where(e => e.HasText()).ToString("\r\n");
        }

        public static string Integrity(DirectedGraph<Modifiable> graph)
        {
            var problems = (from m in graph.OfType<IdentifiableEntity>()
                            group m by new { Type = m.GetType(), Id = (m as IdentifiableEntity).TryCS(ident => (long?)ident.IdOrNull) ?? -m.temporalId } into g
                            where g.Count() > 1 && g.Count(m => m.SelfModified) > 0
                            select g).ToList();

            if (problems.Count > 0)
                return "CLONE ATACK!\r\n\r\n" + problems.ToString(p => Properties.Resources.DifferentInstancesOfTheSameEntity1HaveBeenFound2.Formato(
                    p.Count(),
                    p.Key,
                    p.ToString(m => "  {0}{1}".Formato(m.SelfModified ? "[SelfModified] " : "", m), "\r\n")), "\r\n\r\n");

            return (from ident in graph.OfType<IdentifiableEntity>()
                    let error = ident.IdentifiableIntegrityCheck()
                    where error.HasText()
                    select new { ident, error }).ToString(p => "{0}:\r\n{1}".Formato(p.ident.BaseToString(), p.error.Indent(2)), "\r\n");
        }

        public static DirectedGraph<Modifiable> PreSaving(Func<DirectedGraph<Modifiable>> recreate)
        {
            return ModifyGraph(recreate(), (Modifiable m, ref bool graphModified) => m.PreSaving(ref graphModified), recreate);
        }

        public delegate void ModifyEntityEventHandler(Modifiable m, ref bool graphModified);

        public static DirectedGraph<Modifiable> ModifyGraph(DirectedGraph<Modifiable> graph, ModifyEntityEventHandler modifier, Func<DirectedGraph<Modifiable>> recreate)
        {
            bool graphModified = false; 
            foreach (var m in graph)
            {
                modifier(m, ref graphModified);
            }

            if(!graphModified)
                return graph; //common case

            do
            {
                var newGraph = recreate();
                graphModified = false;
                foreach (var m in newGraph.Except(graph))
                {
                    modifier(m, ref graphModified);
                }

                graph = newGraph;
            } while (graphModified);

            return graph; 
        }


        static string[] colors = 
        {
             "aquamarine1",  "aquamarine4", "blue", "blueviolet",
             "brown4", "burlywood", "cadetblue1", "cadetblue",
             "chartreuse", "chocolate", "cornflowerblue",
             "darkgoldenrod", "darkolivegreen3", "darkorchid", "darkseagreen",
             "darkturquoise", "darkviolet", "deeppink", "deepskyblue", "forestgreen"
        };


        public static string SuperGraphviz(this DirectedGraph<Modifiable> modifiables)
        {
            Func<Type, string> color = t => colors[Math.Abs(t.FullName.GetHashCode()) % colors.Length];

            var listNodes = modifiables.Nodes.Select(n => new
            {
                Node = n,

                Fillcolor =  n is Lite? "white": color(n.GetType()),
                Color = 
                    n is Lite ? color(Reflector.ExtractLite(n.GetType())):
                    (n.SelfModified ? "red" : n.Modified ? "red4" :"black"),

                Shape = n is Lite ? "ellipse" :
                        n is IdentifiableEntity ? "ellipse" :
                        n is EmbeddedEntity ? "box" :
                        Reflector.IsMList(n.GetType()) ? "hexagon" : "plaintext",
                Style = n is Entity ? ", style=\"diagonals,filled,bold\"" : 
                        n is Lite? "style=\"solid,bold\"": "",

                Label = n.ToString().Etc(30, "..").RemoveDiacritics()

            }).ToList();

            string nodes = listNodes.ToString(t => "    {0} [color={1}, fillcolor={2} shape={3}{4}, label=\"{5}\"]".Formato(modifiables.Comparer.GetHashCode(t.Node), t.Color, t.Fillcolor, t.Shape, t.Style, t.Label), "\r\n");

            string arrows = modifiables.Edges.ToString(e => "    {0} -> {1}".Formato(modifiables.Comparer.GetHashCode(e.From), modifiables.Comparer.GetHashCode(e.To)), "\r\n");

            return "digraph \"Grafo\"\r\n{{\r\n    node [ style = \"filled,bold\"]\r\n\r\n{0}\r\n\r\n{1}\r\n}}".Formato(nodes, arrows);
        }

        public static DirectedGraph<IdentifiableEntity> ColapseIdentifiables(DirectedGraph<Modifiable> modifiables)
        {
            DirectedGraph<IdentifiableEntity> result = new DirectedGraph<IdentifiableEntity>();
            foreach (var item in modifiables.OfType<IdentifiableEntity>())
            {
                var toColapse = modifiables.IndirectlyRelatedTo(item, i => !(i is IdentifiableEntity));
                var toColapseFriends = toColapse.SelectMany(i => modifiables.RelatedTo(i).OfType<IdentifiableEntity>());
                result.Add(item, toColapseFriends);
                result.Add(item, modifiables.RelatedTo(item).OfType<IdentifiableEntity>());
            }
            return result;
        }
    }
}
