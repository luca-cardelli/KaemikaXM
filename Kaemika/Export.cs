using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using SkiaSharp;

namespace Kaemika {

    public abstract class Vertex {
    }
    public class Vertex_Label : Vertex {
        public string Label { get; }
        public Vertex_Label(string label) {
            this.Label = (label == null) ? "" : label;
        }
    }
    public class Vertex_Rectangle: Vertex {
        public SKSize Size { get; }
        public bool Filled { get; }
        public Vertex_Rectangle(SKSize size, bool filled) { // size proportional to label text height
            this.Size = size;
            this.Filled = filled;
        }
    }
    public class Vertex_Routing: Vertex {
        public Edge<Vertex> fromEdge;
        public Edge<Vertex> toEdge;
        public Vertex_Routing(Edge<Vertex> fromEdge, Edge<Vertex> toEdge) {
            this.fromEdge = fromEdge;
            this.toEdge = toEdge;
        }
    }

    public enum Directed { No, Solid, Pointy, Ball }
    public class Edge<Vertex> : IEdge<Vertex> {
        public Vertex Source { get; }
        public Vertex Target { get; }
        public string Label { get; }
        public Directed Directed  { get; }
        public Edge(Vertex source, Vertex target, string label = null, Directed directed = Directed.Solid) {
            this.Source = source;
            this.Target = target;
            this.Label = label;
            this.Directed = directed;
        }
    }

    public class Graph<V, E> where V : Vertex where E : Edge<Vertex> {
        public IEnumerable<V> Vertexes { get; }
        public IEnumerable<E> Edges { get; }
        public Graph(IEnumerable<V> vertexes, IEnumerable<E> edges) {
            this.Vertexes = vertexes;
            this.Edges = edges;
        }
        public string ToGraphviz() {
            Dictionary<Vertex, string> names = new Dictionary<Vertex, string>();
            string vertexes = VertexesToGraphviz(names);
            string edges = EdgesToGraphviz(names);
            return
                  "// Copy and paste into GraphViz " + Environment.NewLine
                + "// e.g. at https://dreampuf.github.io/GraphvizOnline" + Environment.NewLine
                + Environment.NewLine
                + "digraph G {" + Environment.NewLine + vertexes + edges + "}" + Environment.NewLine;
        }
        private string VertexesToGraphviz(Dictionary<Vertex, string> names) {
            string s = "";
            int i = 1;
            foreach (V vertex in Vertexes) {
                string name = "N" + i; i++;
                names[vertex] = name;
                if (vertex is Vertex_Label)
                    s += name + "[shape=oval, label=" + Parser.FormatString((vertex as Vertex_Label).Label) + "];" + Environment.NewLine;
                else if (vertex is Vertex_Rectangle)
                    s += name + "[shape=square, label=\"\"];" + Environment.NewLine;
                else if (vertex is Vertex_Routing)
                    s += name + "[shape=point];" + Environment.NewLine;
            }
            return s;
        }
        private string EdgesToGraphviz(Dictionary<Vertex, string> names) {
            string s = "";
            foreach (E edge in Edges)
                s += names[edge.Source] + " -> " + names[edge.Target]
                    + ((edge.Label == null ) ? "" : "[label=" + Parser.FormatString(edge.Label) + "]; ") + Environment.NewLine;               
            return s;
        }
    }

    public static class Export {
 
        // Export for LBS Tool

        public static string MSRC_LBS(List<ReportEntry> reportList, SampleValue sample, Style style) {
            return MSRC_LBShead(reportList, sample, style) + MSRC_LBSbody(sample, style);
        }
        private static string MSRC_LBShead(List<ReportEntry> reportList, SampleValue sample, Style style) {
            string final = null;
            string plots = null;
            foreach (ReportEntry report in reportList) {
                string series = report.Format(style);
                plots = (plots == null) ? series : plots + "; " + series;
            }
            string head = "directive simulation deterministic" + Environment.NewLine;
            // string head = "directive simulation lna" + Environment.NewLine;
            head = head + "directive sample " + ((final != null) ? final : "1.0") + " 1000" + Environment.NewLine;
            head = (plots == null) ? head : head + "directive plot " + plots + Environment.NewLine;
            return head;
        }
        private static string MSRC_LBSbody(SampleValue sample, Style style) {
            string body = "";
            string tail = "";
            List<SpeciesValue> speciesList = sample.stateMap.species;
            List<ReactionValue> reactionList = sample.ReactionsAsConsumed(style);
            foreach (SpeciesValue species in speciesList) {
                body = body + tail + "init " + species.Format(style) + " " + style.FormatDouble(sample.stateMap.Molarity(species.symbol, style));
                tail = " |" + Environment.NewLine;
            }
            foreach (ReactionValue reaction in reactionList) {
                List<Symbol> reactants = reaction.reactants;
                List<Symbol> products = reaction.products;
                if (!(reaction.rate is MassActionRateValue)) throw new Error("Export LBS/CNR: only mass action reactions are supported");
                double rate = ((MassActionRateValue)reaction.rate).Rate(0.0); // ignore activation energy of reaction
                body = body + tail
                    + Style.FormatSequence(reactants, " + ", x => x.Format(style))
                    + " ->" + "{" + rate.ToString() + "} "
                    + Style.FormatSequence(products, " + ", x => x.Format(style));
                tail = " |" + Environment.NewLine;
            }
            return body;
        }

        // Export for CRN Tool

        public static string MSRC_CRN(List<ReportEntry> reportList, SampleValue sample, Style style) {
            return Export_CRNhead(reportList, sample, style) + MSRC_LBSbody(sample, style);
        }
        private static string Export_CRNhead(List<ReportEntry> reportList, SampleValue sample, Style style) {
            string final = null;
            string plots = null;
            foreach (ReportEntry report in reportList) {
                string series = report.flow.Format(style); // ignore .asList
                plots = (plots == null) ? series : plots + "; " + series;
            }
            string head = "directive simulator deterministic" + Environment.NewLine;
            // string head = "directive simulator lna" + Environment.NewLine;
            string finalDirect = (final != null) ? "; final=" + final : "; final=1.0";
            string plotsDirect = (plots != null) ? "; plots=[" + plots + "]" : "";
            head = head + "directive simulation {points=1000" + finalDirect + plotsDirect + "}" + Environment.NewLine;
            return head;
        }

        // Export Protocol steps as TEXT

        public static string Protocol(Netlist netlist, Style style) {
            string protocol = "";
            foreach (ProtocolEntry entry in netlist.Protocols())
                protocol += ((ProtocolEntry)entry).Format(style.RestyleAsDataFormat("header")) + Environment.NewLine;
            return protocol;
        }

        // Export ODEs and LNA ODEs for Wolfram Notebook

        public static string SteadyState(CRN crn, Style style) {
            return crn.FormatAsODE(style)
                + Environment.NewLine;
        }

        // Export ODEs for OSCILL8

        public static string ODE(SampleValue sample, CRN crn, Style style) {
            return ODE_Header(sample, style) 
                + crn.FormatAsODE(style, prefixDiff: "", suffixDiff: "'")
                + Environment.NewLine
                + ODE_Initializations(sample, style);
        }
        private static string ODE_Header(SampleValue sample, Style style) {
            //string plots = "";
            //foreach (Entry entry in this.entries) {
            //    if (entry.type == "report" && entry.value is ReportValue") {
            //        string species = ((ReportValue)entry.value).Format(style);
            //        plots = (plots == null) ? species : plots + "; " + species;
            //    }
            //}
            //return "directive plot " + plots + Environment.NewLine;
            return ""; // Oscill8 does not accept plot directives?
        }
        private static string ODE_Initializations(SampleValue sample, Style style) {
            string inits = "";
            List<SpeciesValue> speciesList = sample.stateMap.species;
            foreach (SpeciesValue species in speciesList) {
                inits = inits + "init " + species.Format(style) + " = " 
                    + sample.stateMap.Molarity(species.symbol, style).ToString() + Environment.NewLine;
            }
            return inits;
        }

        // Export AdjacencyGraph as GraphViz (withot producing layout for AdjacencyGraph)

        public static string GraphViz(AdjacencyGraph<Vertex, Edge<Vertex>> graph) {
            return new Graph<Vertex, Edge<Vertex>>(graph.Vertices, graph.Edges).ToGraphviz();
        }

        public static string ProcessGraph(string graphFamily) { // for WinForms version: generate graph and just get graphviz in text form
            var execution = Exec.lastExecution; // atomically copy it
            if (execution == null) return ""; // something's wrong
            if (execution.graphCache.ContainsKey(graphFamily)) {
                var graph = execution.graphCache[graphFamily];
                if (graph.VertexCount == 0 || graph.EdgeCount == 0) return "";
                else return Export.GraphViz(graph);
            } else return "";
        }

        // Export Reactions as COMPLEX GRAPH

        public static AdjacencyGraph<Vertex, Edge<Vertex>> ComplexGraph(List<Symbol> species, List<ReactionValue> reactions, Style style) {
            AdjacencyGraph<Vertex, Edge<Vertex>> graph = new AdjacencyGraph<Vertex, Edge<Vertex>>();
            Dictionary<String, Vertex> names = new Dictionary<String, Vertex>();
            ComplexVertexes(graph, names, reactions, style);
            ComplexEdges(graph, names, reactions, style);
            return graph;
        }
        public static void ComplexVertexes(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<String, Vertex> names, List<ReactionValue> reactions, Style style) {
            foreach (ReactionValue r in reactions) {
                string reactants = CanonicalComplex(r.reactants, style);
                if (!names.ContainsKey(reactants)) names[reactants] = new Vertex_Label(reactants);
                graph.AddVertex(names[reactants]);
                string products = CanonicalComplex(r.products, style);
                if (!names.ContainsKey(products)) names[products] = new Vertex_Label(products);
                graph.AddVertex(names[products]);
            }
        }
        public static void ComplexEdges(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<String, Vertex> names, List<ReactionValue> reactions, Style style) {
            foreach (ReactionValue r in reactions) {
                GraphAddEdge(graph, names[CanonicalComplex(r.reactants, style)], names[CanonicalComplex(r.products, style)]);
            }
        }
        public static string CanonicalComplex(List<Symbol> complex, Style style) {
            SortedList<string, int> l = new SortedList<string, int>();
            foreach (Symbol symbol in complex) {
                string species = symbol.Format(style);
                if (!l.ContainsKey(species)) l[species] = 0;
                l[species]++;
            }
            return Style.FormatSequence(l, "+", kvp => ((kvp.Value > 1) ? kvp.Value.ToString() : "") + kvp.Key);
        }
       
        public static void GraphAddEdge(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Vertex source, Vertex target, string label = null, Directed directed = Directed.Solid) {
            // Sugiyama crashes on self loops?
            if (source == target) {
                Vertex_Routing v1 = new Vertex_Routing(null, null);
                Vertex_Routing v2 = new Vertex_Routing(null, null);
                Edge<Vertex> e1 = new Edge<Vertex>(source, v1, label, Directed.No);
                Edge<Vertex> e2 = new Edge<Vertex>(v1, v2, null, Directed.No);
                Edge<Vertex> e3 = new Edge<Vertex>(v2, target, null, directed);
                v1.fromEdge = e1;
                v1.toEdge = e2;
                v2.fromEdge = e2;
                v1.toEdge = e3;
                graph.AddVertex(v1);
                graph.AddVertex(v2);
                graph.AddEdge(e1);
                graph.AddEdge(e2);
                graph.AddEdge(e3);
            } else graph.AddEdge(new Edge<Vertex>(source, target, label, directed));
        }

        public static void GraphAddRoutedEdge(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Vertex source, Vertex target, string label = null, Directed directed = Directed.Solid) {
            Vertex_Routing v = new Vertex_Routing(null, null);
            Edge<Vertex> fromEdge = new Edge<Vertex>(source, v, label, Directed.No);
            Edge<Vertex> toEdge = new Edge<Vertex>(v, target, null, directed);
            v.fromEdge = fromEdge;
            v.toEdge = toEdge;
            graph.AddVertex(v);
            graph.AddEdge(fromEdge);
            graph.AddEdge(toEdge);
        }

        // Export Reactions as REACTION GRAPH

        public static AdjacencyGraph<Vertex, Edge<Vertex>> ReactionGraph(List<Symbol> species, List<ReactionValue> reactions, Style style) {
            AdjacencyGraph<Vertex, Edge<Vertex>> graph = new AdjacencyGraph<Vertex, Edge<Vertex>>();
            Dictionary<String, Vertex> names = new Dictionary<String, Vertex>();
            ReactionVertexes(graph, names, species, reactions, style);
            ReactionEdges(graph, names, reactions, style);
            return graph;
        }
        public static void ReactionVertexes(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<String, Vertex> names, List<Symbol> species, List<ReactionValue> reactions, Style style) {
            Dictionary<string, bool> used = new Dictionary<string, bool>();
            foreach (ReactionValue r in reactions) {
                foreach (Symbol reactant in r.reactants)
                    used[reactant.Format(style)] = true;
                foreach (Symbol product in r.products)
                    used[product.Format(style)] = true;
            }
            foreach (var kvp in used) {
                Vertex v = new Vertex_Label(kvp.Key);
                names[kvp.Key] = v;
                graph.AddVertex(v);
            }
        }
        public static void ReactionEdges(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<String, Vertex> names, List<ReactionValue> reactions, Style style) {
            foreach (ReactionValue r in reactions) {
                Vertex reaction = new Vertex_Rectangle(new SKSize(0.5f, 0.5f), true); // size proportional to label text height
                graph.AddVertex(reaction);

                //foreach (Symbol reactant in r.ReactantsSet()) {
                //    int n = r.Stoichiometry(reactant, r.reactants);
                //    GraphAddEdge(graph, names[reactant.Format(style)], reaction, null, Directed.Pointy);
                //    for (int i = 1; i < n; i++) { // all edges except the first
                //        GraphAddRoutedEdge(graph, names[reactant.Format(style)], reaction, null, Directed.Pointy);
                //    }
                //}
                //foreach (Symbol product in r.ProductsSet()) {
                //    int n = r.Stoichiometry(product, r.products);
                //    GraphAddEdge(graph, reaction, names[product.Format(style)], null, Directed.Solid);
                //    for (int i = 1; i < n; i++) { // all edges except the first
                //        GraphAddRoutedEdge(graph, reaction, names[product.Format(style)], null, Directed.Solid);
                //    }
                //}

                (SymbolMultiset catalystsMset, SymbolMultiset reactantsMset, SymbolMultiset productsMset) = r.CatalistForm();
                foreach (Symbol catalyst in catalystsMset.ToSet()) {
                    int n = catalystsMset.Count(catalyst);
                    GraphAddEdge(graph, names[catalyst.Format(style)], reaction, null, Directed.Ball);
                    for (int i = 1; i < n; i++) { // all edges except the first
                        GraphAddRoutedEdge(graph, names[catalyst.Format(style)], reaction, null, Directed.Ball);
                    }
                }
                foreach (Symbol reactant in reactantsMset.ToSet()) {
                    int n = reactantsMset.Count(reactant);
                    GraphAddEdge(graph, names[reactant.Format(style)], reaction, null, Directed.Pointy);
                    for (int i = 1; i < n; i++) { // all edges except the first
                        GraphAddRoutedEdge(graph, names[reactant.Format(style)], reaction, null, Directed.Pointy);
                    }
                }
                foreach (Symbol product in productsMset.ToSet()) {
                    int n = productsMset.Count(product);
                    GraphAddEdge(graph, reaction, names[product.Format(style)], null, Directed.Solid);
                    for (int i = 1; i < n; i++) { // all edges except the first
                        GraphAddRoutedEdge(graph, reaction, names[product.Format(style)], null, Directed.Solid);
                    }
                }

            }
        }

        // Build a PROTOCOL GRAPH from a Netlist

        public static string disposeLabel = " ";
        public static Vertex disposeVertex = new Vertex_Label(disposeLabel);
        private static bool disposeAdded = false;

        public static AdjacencyGraph<Vertex, Edge<Vertex>> ProtocolGraph(Netlist netlist, Style style) {
            AdjacencyGraph<Vertex, Edge<Vertex>> graph = new AdjacencyGraph<Vertex, Edge<Vertex>>();
            disposeAdded = false;
            Dictionary<String, Vertex> verticesDict = ProtocolVertices(netlist, graph, style);
            ProtocolEdges(netlist, graph, verticesDict, style);
            return graph;
        }
        public static Dictionary<String,Vertex> ProtocolVertices(Netlist netlist, AdjacencyGraph<Vertex, Edge<Vertex>> graph, Style style) {
            Dictionary<String, Vertex> veticesDict = new Dictionary<String, Vertex>();
            List<SampleValue> samples = netlist.AllSamples();
            foreach (SampleValue sample in samples) {
                if (sample.IsProduced() || sample.IsConsumed()) { // ignore the extraneous samples
                    string label = sample.FormatSymbol(style);
                    Vertex vertex = new Vertex_Label(label);
                    veticesDict[label] = vertex;
                    graph.AddVertex(vertex);
                }
            }
            return veticesDict;
        }
        public static void ProtocolEdges(Netlist netlist, AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<String, Vertex> veticesDict, Style style) {
            foreach (ProtocolEntry entry in netlist.AllOperations())
                if (entry is MixEntry) {
                    var node = entry as MixEntry;
                    var outS = node.outSample.FormatSymbol(style);
                    foreach (SampleValue inSample in node.inSamples) {
                        var inS = inSample.FormatSymbol(style);
                        GraphAddEdge(graph, veticesDict[inS], veticesDict[outS], "mix");
                    }
                } else if (entry is SplitEntry) {
                    var node = entry as SplitEntry; 
                    var inS = node.inSample.FormatSymbol(style);
                    for (int i = 0; i < node.outSamples.Count; i++) {
                        var outS = node.outSamples[i].FormatSymbol(style);
                        GraphAddEdge(graph, veticesDict[inS], veticesDict[outS], "split " + node.proportions[i].value.ToString("G3"));
                    }
                } else if (entry is EquilibrateEntry) {
                    var node = entry as EquilibrateEntry;
                    for (int i = 0; i < node.outSamples.Count; i++) {
                        var inS = node.inSamples[i].FormatSymbol(style);
                        var outS = node.outSamples[i].FormatSymbol(style);
                        GraphAddEdge(graph, veticesDict[inS], veticesDict[outS], "equilibrate for " + node.fortime.ToString("G3"));
                    }
                } else if (entry is RegulateEntry) {
                    var node = entry as RegulateEntry; 
                    for (int i = 0; i < node.outSamples.Count; i++) {
                        var inS = node.inSamples[i].FormatSymbol(style);
                        var outS = node.outSamples[i].FormatSymbol(style);
                        GraphAddEdge(graph, veticesDict[inS], veticesDict[outS], "regulate to " + node.temperature.ToString("G3"));
                    }
                } else if (entry is ConcentrateEntry) {
                    var node = entry as ConcentrateEntry; 
                    for (int i = 0; i < node.outSamples.Count; i++) {
                        var inS = node.inSamples[i].FormatSymbol(style);
                        var outS = node.outSamples[i].FormatSymbol(style);
                        GraphAddEdge(graph, veticesDict[inS], veticesDict[outS], "concentrate to " + node.volume.ToString("G3"));
                    }
                } else if (entry is DisposeEntry) {
                    var node = entry as DisposeEntry;
                    if (!disposeAdded) { veticesDict[disposeLabel] = disposeVertex; graph.AddVertex(disposeVertex); }
                    foreach (SampleValue inSample in node.inSamples) {
                        GraphAddEdge(graph, veticesDict[inSample.symbol.Format(style)], disposeVertex, "dispose");
                    }
                }
        }


        // ===============
        // Export Protocol
        // ===============

        // Building a PDMP GRAPH from a Netlist, via a Closure

        public enum Presentation { Reactions, ODEs, Stoichiometry };

        public static AdjacencyGraph<Vertex, Edge<Vertex>> PDMPGraph(Netlist netlist, Style style, bool sequential) {
            Closure closure = PDMP(netlist, style, sequential);
            return closure.PDMPGraph(style);
        }

        // The data structures needed to build a PDMP GRAPH: Closure, StateSet, State, Transition
        // Also, how to export a Closure (PDMP Graph) as a GraphViz graph

        public class Closure {
            private StateSet states;
            private List<Transition> transitions;
            public Netlist netlist;
            public Closure(Netlist netlist) {
                this.netlist = netlist;
                this.states = new StateSet();
                this.transitions = new List<Transition>();
            }
            public State AddUniqueState(State state, Style style) {
                return this.states.AddUnique(state, style);
            }
            public void AddTransition(Transition transition) {
                this.transitions.Add(transition);
            }
            public string Format(Style style) {
                string s = "";
                foreach (State state in states.states) {
                    s += "STATE_" + state.id.ToString() + Environment.NewLine + state.Format(style) + Environment.NewLine;
                }
                foreach (Transition transition in transitions) {
                    s += "TRANSITION " + transition.Format(style) + Environment.NewLine;
                }
                return s;
            }
            public string HybridSystem(Presentation rep, Style style) {
                string s = "";
                s += Environment.NewLine;
                foreach (State state in states.states) {
                    s += "STATE_" + state.id.ToString() + Environment.NewLine +
                        state.Format(style) + Environment.NewLine + Environment.NewLine;
                    bool found = false;
                    foreach (Transition transition in state.transitionsOut) {
                        if (transition.entry is EquilibrateEntry) {
                            if (found) throw new Error("More than one equilibrate transitions out of one state.");
                            foreach (SampleValue inSample in (transition.entry as EquilibrateEntry).inSamples) {
                                List<ReactionValue> reactions = inSample.ReactionsAsConsumed(style);
                                // List<ReactionValue> reactions = inSample.RelevantReactions(netlist, style); // this would pick up reactions that were added after the sample was consumed
                                s += "KINETICS for STATE_" + transition.source.id.ToString() + " (sample " + inSample.FormatSymbol(style) + ") for " + style.FormatDouble((transition.entry as EquilibrateEntry).fortime) + " time units:" + Environment.NewLine;
                                if (rep == Presentation.Reactions) {
                                    foreach (ReactionValue reaction in reactions) s += reaction.Format(style) + Environment.NewLine;
                                } else if (rep == Presentation.ODEs) {
                                    s += (new CRN(inSample, reactions)).FormatAsODE(style);
                                } else if (rep == Presentation.Stoichiometry) {
                                    s += (new CRN(inSample, reactions)).FormatStoichiometry(style);
                                }
                                s += Environment.NewLine;
                                found = true;
                            }
                        }
                    }
                    foreach (Transition transition in state.transitionsOut) {
                        s += "TRANSITION" + Environment.NewLine;
                        s += transition.Format(style) + Environment.NewLine + Environment.NewLine;
                    }
                }
                return s;
            }
            public string GraphViz(Style style) {
                string s = "";
                foreach (State state in states.states) s += state.GraphVizNode(style);
                string t = "";
                foreach (Transition transition in transitions) t += transition.GraphVizEdge(style);
                return "// Copy and paste into GraphViz " + Environment.NewLine
                    + "// e.g. at https://dreampuf.github.io/GraphvizOnline" + Environment.NewLine
                    + Environment.NewLine
                    + "digraph G {" + Environment.NewLine + t + s + "}" + Environment.NewLine;
            }
            public AdjacencyGraph<Vertex, Edge<Vertex>> PDMPGraph(Style style) {
                AdjacencyGraph<Vertex, Edge<Vertex>> graph = new AdjacencyGraph<Vertex, Edge<Vertex>>();
                Dictionary<int, Vertex> ids = new Dictionary<int, Vertex>();
                foreach (State state in states.states) state.AddToGraph(graph, ids, style);
                foreach (Transition transition in transitions) transition.AddToGraph(graph, ids, style);
                return graph;
            }
        }

        public class Transition { // a state transition is induced by a sample transition from some samples in the source state to some samples in the target state
            public State source; // the source state of the state transition
            public State target; // the target state of the state transition
            public string label; // transition label
            public OperationEntry entry; // contains the source and target samples of this transition
            public Transition (State source, State target, OperationEntry entry, string label) {
                this.source = source;
                this.target = target;
                this.label = label;
                this.entry = entry;
                source.transitionsOut.Add(this);
                target.transitionsIn.Add(this);
            }
            public string Format(Style style) {
//                return "[" + source.Format(style) + " ->{" + label + "} " + target.Format(style) + "]";
                return "[STATE_" + source.id.ToString() + "   (" + label + ")=>   STATE_" + target.id.ToString() + "]";
            }
            public string GraphVizEdge(Style style) {
                return source.UniqueNodeName() + " -> " + target.UniqueNodeName() + " [label=" + Parser.FormatString(label) + "];" + Environment.NewLine;
            }
            public void AddToGraph(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<int, Vertex> ids, Style style) {
                GraphAddEdge(graph, ids[source.id], ids[target.id], label);
            }
        }

        public class StringComparer : IComparer<string> {
            public int Compare(string s1, string s2) {
                return string.Compare(s1,s2); }
        }

        public class State {
            private static int unique = 0;
            public int id;
            private List<SampleValue> samples;
            public List<Transition> transitionsIn;
            public List<Transition> transitionsOut;
            public static void Reset() { unique = 0; }
            public State() {
                this.id = unique; unique++;
                this.samples = new List<SampleValue>();
                this.transitionsIn = new List<Transition>();
                this.transitionsOut = new List<Transition>();
            }
            public bool Contains(SampleValue sample) {
                return this.samples.Contains(sample);
            }
            public bool Contains(List<SampleValue> samples) {
                foreach (SampleValue sample in samples) if (!Contains(sample)) return false;
                return true;
            }
            public void Add(SampleValue sample) {
                this.samples.Add(sample);
            }
            public void Remove(SampleValue sample) {
                this.samples.Remove(sample);
            }
            public State Copy() {
                State newState = new State();
                foreach (SampleValue s in this.samples) newState.Add(s);
                return newState;
            }
            public SortedSet<string> Membership(Style style) {
                SortedSet<string> members = new SortedSet<string>(new StringComparer());
                foreach (SampleValue sample in samples) members.Add(sample.FormatSymbol(style));
                return members;
            }
            public bool Same(State other, Style style) {
                return this.Membership(style).SetEquals(other.Membership(style));
            }
            public string Format(Style style) {
                return Style.FormatSequence(samples, ", " + Environment.NewLine,
                    x => (x.Count() > 0) ? x.Format(style) : "" ); // returning "" here will skip the separator for that iteration
                //string s = "";
                //foreach (SampleValue sample in samples) {
                //    if (sample.Count() > 0)
                //        s += sample.Format(style) + ", " + Environment.NewLine;
                //}
                //if (s.Length > 0) s = s.Substring(0, s.Length - 3);
                //return s;
            }
            public string Label(Style style) {
                return Style.FormatSequence(samples, ", ",
                    x => (x.IsProduced() || x.IsConsumed()) ? x.FormatSymbol(style) : ""); // ignore the extraneous samples: returning "" here will skip the separator for that iteration
                //string s = "";
                //foreach (SampleValue sample in samples) {
                //    if (sample.IsProduced() || sample.IsConsumed()) // ignore the extraneous samples
                //        s += sample.FormatSymbol(style) + ", ";
                //}
                //if (s.Length > 0) s = s.Substring(0, s.Length - 2);
                //return s;
            }
            public string UniqueNodeName() { return "N" + id; }
            public string GraphVizNode(Style style) {
                return UniqueNodeName() + "[shape=box, label=" + Parser.FormatString("{" + Label(style) + "}") + "];" + Environment.NewLine;
            }
            public void AddToGraph(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<int, Vertex> ids, Style style) {
                Vertex vertex = new Vertex_Label(Label(style));
                ids[this.id] = vertex;
                graph.AddVertex(vertex);
            }
        }

        public class StateSet {
            public List<State> states;
            public StateSet() {
                this.states = new List<State>();
            }
            public int Count() {
                return states.Count;
            }
            public State AddUnique(State other, Style style) {
                foreach (State state in states) if (other.Same(state, style)) return state;
                states.Add(other);
                return other;
            }
            public void AddUnique(StateSet otherStates, Style style) {
                foreach (State other in otherStates.states) AddUnique(other, style);
            }
            public string Format(Style style) {
                return Style.FormatSequence(states, ", ", x => x.Format(style));
            }
        }

        //  Building a PDPM Closure from a Netlist

        public static Closure PDMP(Netlist netlist, Style style, bool sequential) {
            State.Reset();
            Closure closure = new Closure(netlist);
            State current = PDMP_InitialState(closure, style);

            StateSet currentStates = new StateSet();
            currentStates.AddUnique(current, style);
            while (currentStates.Count() > 0) {
                StateSet nextStates = new StateSet();
                foreach (State state in currentStates.states)
                    nextStates.AddUnique(PDMP_MultiTransition(closure, state, style, sequential), style);
                currentStates = nextStates;
            }
            return closure;
        }

        public static State PDMP_InitialState(Closure closure, Style style) {
            State state = new State();
            foreach (SampleValue sample in closure.netlist.SourceSamples()) {
                state.Add(sample);
            }
            closure.AddUniqueState(state, style);
            return state;
        }

        public static StateSet PDMP_MultiTransition(Closure closure, State state, Style style, bool sequential) {
            StateSet nextStates = new StateSet();
            foreach (OperationEntry entry in closure.netlist.AllOperations()) {
                if (entry is MixEntry) {
                    MixEntry mixEntry = entry as MixEntry;
                    if (state.Contains(mixEntry.inSamples)) {
                        State newState = state.Copy();
                        foreach (SampleValue inSample in mixEntry.inSamples) newState.Remove(inSample);
                        newState.Add(mixEntry.outSample);
                        newState = closure.AddUniqueState(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState, entry,
                                "mix " + mixEntry.outSample.FormatSymbol(style) + " = " + Style.FormatSequence(mixEntry.inSamples, ", ", x => x.FormatSymbol(style))));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                }  else if (entry is SplitEntry)  {
                    SplitEntry splitEntry = entry as SplitEntry;
                    if (state.Contains(splitEntry.inSample)) {
                        SampleValue inSample = splitEntry.inSample;
                        State newState = state.Copy();
                        newState.Remove(inSample);
                        foreach (SampleValue outSample in splitEntry.outSamples) newState.Add(outSample);
                        newState = closure.AddUniqueState(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState, entry,
                            "split " + Style.FormatSequence(splitEntry.outSamples, ", ", x => x.FormatSymbol(style)) + " = " + inSample.FormatSymbol(style) + " by " + Style.FormatSequence(splitEntry.proportions, ", ", x => x.value.ToString("G3"))));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else if (entry is EquilibrateEntry) {
                    EquilibrateEntry eqEntry = entry as EquilibrateEntry;
                    if (state.Contains(eqEntry.inSamples)) {
                        State newState = state.Copy();
                        foreach (SampleValue inSample in eqEntry.inSamples) newState.Remove(inSample);
                        foreach (SampleValue outSample in eqEntry.outSamples) newState.Add(outSample);
                        newState = closure.AddUniqueState(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState, entry,
                            "equilibrate " + Style.FormatSequence(eqEntry.outSamples, ", ", x => x.FormatSymbol(style)) + " = " + Style.FormatSequence(eqEntry.inSamples, ", ", x => x.FormatSymbol(style)) + " for " + eqEntry.fortime.ToString("G3")));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else if (entry is RegulateEntry) {
                    RegulateEntry regulateEntry = entry as RegulateEntry;
                    if (state.Contains(regulateEntry.inSamples)) {
                        State newState = state.Copy();
                        foreach (SampleValue inSample in regulateEntry.inSamples) newState.Remove(inSample);
                        foreach (SampleValue outSample in regulateEntry.outSamples) newState.Add(outSample);
                        newState = closure.AddUniqueState(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState, entry,
                            "regulate " + Style.FormatSequence(regulateEntry.outSamples, ", ", x => x.FormatSymbol(style)) + " = " + Style.FormatSequence(regulateEntry.inSamples, ", ", x => x.FormatSymbol(style)) + " to " + regulateEntry.temperature.ToString("G3")));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else if (entry is ConcentrateEntry) {
                    ConcentrateEntry concentrateEntry = entry as ConcentrateEntry;
                    if (state.Contains(concentrateEntry.inSamples)) {
                        State newState = state.Copy();
                        foreach (SampleValue inSample in concentrateEntry.inSamples) newState.Remove(inSample);
                        foreach (SampleValue outSample in concentrateEntry.outSamples) newState.Add(outSample);
                        newState = closure.AddUniqueState(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState, entry,
                            "concentrate " + Style.FormatSequence(concentrateEntry.outSamples, ", ", x => x.FormatSymbol(style)) + " = " + Style.FormatSequence(concentrateEntry.inSamples, ", ", x => x.FormatSymbol(style)) + " to " + concentrateEntry.volume.ToString("G3")));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else if (entry is DisposeEntry) {
                    DisposeEntry disEntry = entry as DisposeEntry;
                    if (state.Contains(disEntry.inSamples)) {
                        State newState = state.Copy();
                        foreach (SampleValue inSample in disEntry.inSamples) newState.Remove(inSample);
                        newState = closure.AddUniqueState(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState, entry,
                            "dispose " + Style.FormatSequence(disEntry.inSamples, ", ", x => x.FormatSymbol(style))));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else { }
            }
            return nextStates;
        }

    }
}
