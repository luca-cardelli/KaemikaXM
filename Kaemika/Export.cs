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
        public Vertex_Routing() {
        }
    }

    public enum Directed { No, Solid, Pointy }
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

        public static string MSRC_LBS(Netlist netlist, SampleValue sample, Style style) {
            return MSRC_LBShead(netlist, sample, style) + MSRC_LBSbody(netlist, sample, style);
        }
        private static string MSRC_LBShead(Netlist netlist, SampleValue sample, Style style) {
            string final = null;
            string plots = null;
            List<ReportEntry> reportList = netlist.Reports(sample.species);
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
        private static string MSRC_LBSbody(Netlist netlist, SampleValue sample, Style style) {
            string body = "";
            string tail = "";
            List<SpeciesValue> speciesList = sample.species;
            List<ReactionValue> reactionList = netlist.RelevantReactions(sample, speciesList, style);
            foreach (SpeciesValue species in speciesList) {
                body = body + tail + "init " + species.Format(style) + " " + sample.Molarity(species.symbol, style).Format(style);
                tail = " |" + Environment.NewLine;
            }
            foreach (ReactionValue reaction in reactionList) {
                List<Symbol> reactants = reaction.reactants;
                List<Symbol> products = reaction.products;
                if (!(reaction.rate is MassActionRateValue)) throw new Error("Export LBS/CNR: only mass action reactions are supported");
                double rate = ((MassActionRateValue)reaction.rate).Rate(0.0); // ignore activation energy of reaction
                body = body + tail
                    + reactants.Aggregate("", (a, b) => (a == "") ? b.Format(style) : a + " + " + b.Format(style))
                    + " ->" + "{" + rate.ToString() + "} "
                    + products.Aggregate("", (a, b) => (a == "") ? b.Format(style) : a + " + " + b.Format(style));
                tail = " |" + Environment.NewLine;
            }
            return body;
        }

        // Export for CRN Tool

        public static string MSRC_CRN(Netlist netlist, SampleValue sample, Style style) {
            return Export_CRNhead(netlist, sample, style) + MSRC_LBSbody(netlist, sample, style);
        }
        private static string Export_CRNhead(Netlist netlist, SampleValue sample, Style style) {
            string final = null;
            string plots = null;
            List<ReportEntry> reportList = netlist.Reports(sample.species);
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

        // Export ODEs for OSCILL8

        public static string ODE(SampleValue sample, CRN crn, Style style) {
            return ODE_Header(sample, style) 
                // + Export_ODE_Equations(vessel, style) 
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
            List<SpeciesValue> speciesList = sample.species;
            foreach (SpeciesValue species in speciesList) {
                inits = inits + "init " + species.Format(style) + " = " 
                    + sample.Molarity(species.symbol, style).value.ToString() + Environment.NewLine;
            }
            return inits;
        }

        // Export Reactions as COMPLEX GRAPH

        public static void GraphAddEdge(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Vertex source, Vertex target, string label = null, Directed directed = Directed.Solid) {
            // Sugiyama crashes on self loops?
            if (source == target) {
                Vertex self = new Vertex_Rectangle(new SKSize(0.5f, 0.5f), false);
                graph.AddVertex(self);
                graph.AddEdge(new Edge<Vertex>(source, self, label));
                graph.AddEdge(new Edge<Vertex>(self, target));
            } else graph.AddEdge(new Edge<Vertex>(source, target, label, directed));
        }

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
            string s = "";
            //foreach (var kvp in l) for(int i = 0; i < kvp.Value; i++) s += kvp.Key + "+";
            foreach (var kvp in l) s += ((kvp.Value > 1) ? kvp.Value.ToString() : "") + kvp.Key + "+";
            if (s != "") s = s.Substring(0, s.Length - 1);
            return s;
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

                //foreach (Symbol reactant in r.reactants) {
                //    int n = r.Stoichiometry(reactant, r.reactants);
                //    GraphAddEdge(graph, names[reactant.Format(style)], reaction, (n==1) ? null : n.ToString(), Directed.Pointy);
                //}
                //foreach (Symbol product in r.products) {
                //    int n = r.Stoichiometry(product, r.products);
                //    GraphAddEdge(graph, reaction, names[product.Format(style)], (n == 1) ? null : n.ToString(), Directed.Solid);
                //}

                foreach (Symbol reactant in r.reactants) {
                    Vertex incoming = new Vertex_Routing();
                    graph.AddVertex(incoming);
                    GraphAddEdge(graph, names[reactant.Format(style)], incoming, null, Directed.No);
                    GraphAddEdge(graph, incoming, reaction, null, Directed.Pointy);
                }
                foreach (Symbol product in r.products) {
                    Vertex outgoing = new Vertex_Routing();
                    graph.AddVertex(outgoing);
                    GraphAddEdge(graph, reaction, outgoing, null, Directed.No);
                    GraphAddEdge(graph, outgoing, names[product.Format(style)], null, Directed.Solid);
                }

                //Vertex reactionIn = new Vertex_Routing();
                //Vertex reactionOut = new Vertex_Routing();
                //graph.AddVertex(reactionIn);
                //graph.AddVertex(reactionOut);
                //foreach (Symbol reactant in r.reactants)
                //    GraphAddEdge(graph, names[reactant.Format(style)], reactionIn);
                //GraphAddEdge(graph, reactionIn, reactionOut);
                //foreach (Symbol product in r.products)
                //    GraphAddEdge(graph, reactionOut, names[product.Format(style)]);
            }
        }

        // Export Protocol as GRAPH

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
                    var node = entry as MixEntry; var inS1 = node.inSample1.FormatSymbol(style); var inS2 = node.inSample2.FormatSymbol(style); var outS = node.outSample.FormatSymbol(style);
                    GraphAddEdge(graph, veticesDict[inS1], veticesDict[outS], "mix");
                    GraphAddEdge(graph, veticesDict[inS2], veticesDict[outS], "mix");
                } else if (entry is SplitEntry) {
                    var node = entry as SplitEntry; var inS = node.inSample.FormatSymbol(style); var outS1 = node.outSample1.FormatSymbol(style); var outS2 = node.outSample2.FormatSymbol(style);
                    GraphAddEdge(graph, veticesDict[inS], veticesDict[outS1], "split " + node.proportion.value.ToString("G3"));
                    GraphAddEdge(graph, veticesDict[inS], veticesDict[outS2], "split " + (1 - node.proportion.value).ToString("G3"));
                } else if (entry is EquilibrateEntry) {
                    var node = entry as EquilibrateEntry; var inS = node.inSample.FormatSymbol(style); var outS = node.outSample.FormatSymbol(style);
                    GraphAddEdge(graph, veticesDict[inS], veticesDict[outS], "eq for " + node.time.value.ToString("G3"));
                } else if (entry is TransferEntry) {
                    var node = entry as TransferEntry; var inS = node.inSample.FormatSymbol(style); var outS = node.outSample.FormatSymbol(style);
                    GraphAddEdge(graph, veticesDict[inS], veticesDict[outS], "transfer");
                } else if (entry is DisposeEntry) {
                    var node = entry as DisposeEntry;
                    if (!disposeAdded) { veticesDict[disposeLabel] = disposeVertex; graph.AddVertex(disposeVertex); }
                    GraphAddEdge(graph, veticesDict[node.inSample.symbol.Format(style)], disposeVertex, "dispose");
                }
        }

        // Export Protocol as GRAPHVIZ text (obsolete)

        public static string GraphViz(Netlist netlist) {
            Style style = new Style("•", new SwapMap(subsup: true), new AlphaMap(), "G3", "symbol", ExportTarget.Standard, false);
            string edges = GraphViz_Edges(netlist, style);
            string nodes = GraphViz_Nodes(netlist, style);
            return
                  "// Copy and paste into GraphViz " + Environment.NewLine
                + "// e.g. at https://dreampuf.github.io/GraphvizOnline" + Environment.NewLine
                + Environment.NewLine
                + "digraph G {" + Environment.NewLine + nodes + edges + "}" + Environment.NewLine;
        }

        public static string GraphViz_Edges(Netlist netlist, Style style) {
            string edges = "";
            foreach (ProtocolEntry entry in netlist.AllOperations())
                if (entry is MixEntry) {
                    var node = entry as MixEntry;
                    edges += node.inSample1.symbol.Format(style) + " -> " + node.outSample.symbol.Format(style) + "[label=\"mix\"];" + Environment.NewLine;
                    edges += node.inSample2.symbol.Format(style) + " -> " + node.outSample.symbol.Format(style) + "[label=\"mix\"];" + Environment.NewLine;
                } else if (entry is SplitEntry) {
                    var node = entry as SplitEntry;
                    edges += node.inSample.symbol.Format(style) + " -> " + node.outSample1.symbol.Format(style) + "[label=\"split\"];" + Environment.NewLine;
                    edges += node.inSample.symbol.Format(style) + " -> " + node.outSample2.symbol.Format(style) + "[label=\"split\"];" + Environment.NewLine;
                } else if (entry is EquilibrateEntry) {
                    var node = entry as EquilibrateEntry;
                    edges += node.inSample.symbol.Format(style) + " -> " + node.outSample.symbol.Format(style) + "[label=\"equilibrate for " + node.time.Format(style) + "\"];" + Environment.NewLine;
                } else if (entry is TransferEntry) {
                    var node = entry as TransferEntry;
                    edges += node.inSample.symbol.Format(style) + " -> " + node.outSample.symbol.Format(style) + "[label=\"transfer\"];" + Environment.NewLine;
                } else if (entry is DisposeEntry) {
                    var node = entry as DisposeEntry;
                    edges += node.inSample.symbol.Format(style) + " -> " + "XXX" + "[label=\"dispose\"];" + Environment.NewLine;
                }
            return edges;
        }

        public static string GraphViz_Nodes(Netlist netlist, Style style) {
            string nodes = "";
            //List<SampleValue> sources = netlist.SourceSamples();
            //foreach (SampleValue sample in sources) { // report the inital conditions of source samples at the time they were consumed (i.e. when they where fully initialized)
            //    SampleValue s = (sample.asConsumed == null) ? sample : sample.asConsumed;
            //    string node_proper = s.symbol.Format(style);
            //    string node_init = node_proper + "_INITIAL";
            //    string label = "(" + node_proper + ")" + Environment.NewLine + s.FormatContent(style);
            //    if (label.Length > 0 && label[label.Length - 1] == '\n') label = label.Substring(0, label.Length - 2);
            //    nodes += node_init + "[label=" + Parser.FormatString(label) + "];" + Environment.NewLine;
            //    nodes += node_init + " -> " + node_proper + "[label=\"init\"];";
            //}
            List<SampleValue> samples = netlist.AllSamples();
            foreach (SampleValue sample in samples) {
                string label =
                    sample.FormatHeader(style) + Environment.NewLine
                    + new CRN(sample, netlist.RelevantReactions(sample, sample.species, style), precomputeLNA: false).FormatAsODE(style, prefixDiff: "", suffixDiff: "'")
                    + ((sample.asConsumed == null) ? sample : sample.asConsumed).FormatContent(style, breaks:true);
                if (label.Length > 0 && label[label.Length - 1] == '\n') label = label.Substring(0, label.Length - 1);
                nodes += sample.symbol.Format(style) + "[shape=box, label=" + Parser.FormatString(label) + "];" + Environment.NewLine;
            }   
            return nodes + "XXX [shape=box, label=\"(dispose)\"]" // Dispose node
               // + Environment.NewLine    // www.webgraphviz.com complains if there is an empty line before the last '}'
               ;  
        }

        //class ID<T> {

        //    public ID() { }

        //    public bool Same(ID<T> other) {
        //        return this == other;
        //    }

        //    private static Dictionary<ID<T>, Organization<T>> organizationRegistry;

        //    public static ID<T> OrganizationID(HashSet<ID<T>> membership) {
        //        foreach (var keyPair in organizationRegistry) {
        //            IEnumerable<ID<T>> i = membership;
        //            if (membership.SetEquals(keyPair.Value.Membership())) return keyPair.Key;
        //        }
        //        return null;
        //    }

        //    public static Organization<T> OrganizationOfMembership(HashSet<ID<T>> membership) {
        //        ID<T> id = OrganizationID(membership);
        //        if (id != null) return organizationRegistry[id];
        //        else {
        //            ID<T> newId = new ID<T>();
        //            Organization<T> newOrganization = new Organization<T>(newId, membership);
        //            organizationRegistry[newId] = newOrganization;
        //            return newOrganization;
        //        }
        //    }

        //    public static Organization<T> EmptyOrganization() {
        //        return OrganizationOfMembership(new HashSet<ID<T>>());
        //    }
        //}

        //class Individual<T> {
        //    private T element;
        //    private ID<T> id;
        //    public Individual(T element) {
        //        this.element = element;
        //        this.id = new ID<T>();
        //    }
        //    public ID<T> Id() {
        //        return this.id;
        //    }
        //    public bool Same(Individual<T> other) { return id.Same(other.id);  }
        //}

        //class Organization<T> {
        //    private ID<T> id;
        //    private HashSet<ID<T>> membership;
        //    public Organization(ID<T> id, HashSet<ID<T>> membership) {
        //        this.id = id;
        //        this.membership = membership;
        //    }
        //    public HashSet<ID<T>> Membership() { return this.membership; }
        //    public Organization<T> Plus(Individual<T> individual) {
        //        if (membership.Contains(individual.Id())) return this;
        //        else {
        //            HashSet<ID<T>> newMembership = membership.Copy().Add(individual.Id());
        //            return ID<T>.OrganizationOfMembership(newMembership);
        //        }
        //    }
        //    public Organization<T> Minus(Individual<T> individual) {
        //        if (!membership.Contains(individual.Id())) return this;
        //        else {
        //            HashSet<ID<T>> newMembership = membership.Copy().Remove(individual.Id());
        //            return ID<T>.OrganizationOfMembership(newMembership);
        //        }
        //    }
        //    public bool Same(Organization<T> other) {
        //        return membership.SetEquals(other.membership);
        //    }

        //}

        // Export Protocol as PDMP

        public static AdjacencyGraph<Vertex, Edge<Vertex>> PDMPGraph(Netlist netlist, Style style, bool sequential) {
            return PDMP(netlist, style, sequential).PDMPGraph(style);
        }

        public class Closure {
            private StateSet states;
            private List<Transition> transitions;
            public Netlist netlist;
            public Closure(Netlist netlist) {
                this.netlist = netlist;
                this.states = new StateSet();
                this.transitions = new List<Transition>();
            }
            public State AddUnique(State state, Style style) {
                return this.states.AddUnique(state, style);
            }
            public void AddTransition(Transition transition) {
                this.transitions.Add(transition);
            }
            public string Format(Style style) {
                string s = "";
                foreach (State state in states.states) {
                    s += "{" + state.Format(style) + "}, ";
                }
                if (s.Length > 0) s = s.Substring(0, s.Length - 2);
                string t = "";
                foreach (Transition transition in transitions) {
                    t += transition.Format(style) + ", ";
                }
                if (t.Length > 0) t = t.Substring(0, t.Length - 2);
                return s + Environment.NewLine + t + Environment.NewLine;
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

        public class Transition {
            public State state1;
            public State state2;
            public string label;
            public Transition (State state1, State state2, string label) {
                this.state1 = state1;
                this.state2 = state2;
                this.label = label;
            }
            public string Format(Style style) {
                return "[" + state1.Format(style) + " ->{" + label + "} " + state2.Format(style) + "]";
            }
            public string GraphVizEdge(Style style) {
                return state1.UniqueNodeName() + " -> " + state2.UniqueNodeName() + " [label=" + Parser.FormatString(label) + "];" + Environment.NewLine;
            }
            public void AddToGraph(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Dictionary<int, Vertex> ids, Style style) {
                GraphAddEdge(graph, ids[state1.id], ids[state2.id], label);
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
            public State() {
                this.id = unique; unique++;
                this.samples = new List<SampleValue>();
            }
            public bool Contains(SampleValue sample) {
                return this.samples.Contains(sample);
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
                string s = "";
                foreach (SampleValue sample in samples) {
                    s += sample.symbol.Format(style) + ", ";
                }
                if (s.Length > 0) s = s.Substring(0, s.Length - 2);
                return s;
            }
            public string Label(Style style) {
                string s = "";
                foreach (SampleValue sample in samples) {
                    if (sample.IsProduced() || sample.IsConsumed()) // ignore the extraneous samples
                        s += sample.FormatSymbol(style) + ", ";
                }
                if (s.Length > 0) s = s.Substring(0, s.Length - 2);
                return s;
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
                string s = "";
                foreach (State state in states) s += state.Format(style) + ", ";
                if (s.Length > 0) s = s.Substring(0, s.Length - 2);
                return s;
            }
        }

        //   To extract the ODEs: closure.netlist.RelevantReactions(sample, sample.species, style)));

        public static Closure PDMP(Netlist netlist, Style style, bool sequential) {
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

        public static string PDMPGraphViz(Netlist netlist, Style style, bool sequential) {
            return PDMP(netlist, style, sequential).GraphViz(style);
        }

        public static State PDMP_InitialState(Closure closure, Style style) {
            State state = new State();
            foreach (SampleValue sample in closure.netlist.SourceSamples()) {
                state.Add(sample);
            }
            closure.AddUnique(state, style);
            return state;
        }

        public static StateSet PDMP_MultiTransition(Closure closure, State state, Style style, bool sequential) {
            StateSet nextStates = new StateSet();
            foreach (OperationEntry entry in closure.netlist.AllOperations()) {
                if (entry is MixEntry) {
                    if (state.Contains((entry as MixEntry).inSample1) && state.Contains((entry as MixEntry).inSample2)) {
                        SampleValue inSample1 = (entry as MixEntry).inSample1;
                        SampleValue inSample2 = (entry as MixEntry).inSample2;
                        SampleValue outSample = (entry as MixEntry).outSample;
                        State newState = state.Copy();
                        newState.Remove(inSample1);
                        newState.Remove(inSample2);
                        newState.Add(outSample);
                        newState = closure.AddUnique(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState,
                            "mix " + outSample.FormatSymbol(style) + " := " + inSample1.FormatSymbol(style) + ", " + inSample2.FormatSymbol(style)));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                }  else if (entry is SplitEntry)  {
                    if (state.Contains((entry as SplitEntry).inSample)) {
                        SampleValue inSample = (entry as SplitEntry).inSample;
                        SampleValue outSample2 = (entry as SplitEntry).outSample2;
                        SampleValue outSample1 = (entry as SplitEntry).outSample1;
                        State newState = state.Copy();
                        newState.Remove(inSample);
                        newState.Add(outSample1);
                        newState.Add(outSample2);
                        newState = closure.AddUnique(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState,
                            "split " + outSample1.FormatSymbol(style) + ", " + outSample2.FormatSymbol(style) + " := " + inSample.FormatSymbol(style) + " by " + (entry as SplitEntry).proportion.value.ToString("G3")));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else if (entry is EquilibrateEntry) {
                    if (state.Contains((entry as EquilibrateEntry).inSample)) {
                        SampleValue inSample = (entry as EquilibrateEntry).inSample;
                        SampleValue outSample = (entry as EquilibrateEntry).outSample;
                        State newState = state.Copy();
                        newState.Remove(inSample);
                        newState.Add(outSample);
                        newState = closure.AddUnique(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState,
                            "eq " + outSample.FormatSymbol(style) + " := " + inSample.FormatSymbol(style) + " for " + (entry as EquilibrateEntry).time.value.ToString("G3")));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else if (entry is TransferEntry) {
                    if (state.Contains((entry as TransferEntry).inSample)) {
                        SampleValue inSample = (entry as TransferEntry).inSample;
                        SampleValue outSample = (entry as TransferEntry).outSample;
                        State newState = state.Copy();
                        newState.Remove(inSample);
                        newState.Add(outSample);
                        newState = closure.AddUnique(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState,
                            "transfer " + outSample.FormatSymbol(style) + " := " + inSample.FormatSymbol(style)));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else if (entry is DisposeEntry) {
                    if (state.Contains((entry as DisposeEntry).inSample)) {
                        SampleValue inSample = (entry as DisposeEntry).inSample;
                        State newState = state.Copy();
                        newState.Remove(inSample);
                        newState = closure.AddUnique(newState, style); // may replace it with an existing state
                        closure.AddTransition(new Transition(state, newState, 
                            "disp " + inSample.FormatSymbol(style)));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else { }
            }
            return nextStates;
        }

    }
}
