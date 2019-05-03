using System;
using System.Collections.Generic;
using System.Linq;

namespace Kaemika {

    public static class Export {

        // Export LBS
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

        // Export CRN
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

        // Export Protocol
        public static string Protocol(Netlist netlist, Style style) {
            string protocol = "";
            foreach (ProtocolEntry entry in netlist.Protocols())
                protocol += ((ProtocolEntry)entry).Format(style.RestyleAsDataFormat("header")) + Environment.NewLine;
            return protocol;
        }

        // Export ODE
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

        public static string GraphViz(Netlist netlist) {
            Style style = new Style("•", new SwapMap(subsup: true), new AlphaMap(), "G3", "symbol", ExportTarget.Standard, false);
            string edges = GraphViz_Edges(netlist, style);
            string nodes = GraphViz_Nodes(netlist, style);
            return
                  "// Copy and paste into GraphViz " + Environment.NewLine
                + "// e.g. at https://dreampuf.github.io/GraphvizOnline" + Environment.NewLine
                + Environment.NewLine
                + "digraph G {" + Environment.NewLine + edges + nodes + "}" + Environment.NewLine;
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
                    + new CRN(sample, netlist.RelevantReactions(sample, sample.species, style)).FormatAsODE(style, prefixDiff: "", suffixDiff: "'")
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
                return state1.GraphVizNodeName() + " -> " + state2.GraphVizNodeName() + " [label=" + Parser.FormatString(label) + "];" + Environment.NewLine;
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
            public string GraphVizNodeName() { return "N" + id; }
            public string GraphVizNode(Style style) {
                string s = "";
                foreach (SampleValue sample in samples) {
                    if (sample.IsProduced() || sample.IsConsumed()) // ignore the extraneous samples
                        s += sample.symbol.Format(style) + ", ";
                }
                if (s.Length > 0) s = s.Substring(0, s.Length - 2);
                return "N" + id + "[shape=box, label=" + Parser.FormatString("{"+s+"}") + "];" + Environment.NewLine;
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

        public static string PDMP(Netlist netlist, Style style, bool sequential) {
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

            return closure.GraphViz(style);
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
                            "mix " + outSample.FormatSymbol(style) + " := " + inSample1.FormatSymbol(style) + " with " + inSample2.FormatSymbol(style)));
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
                            "split " + outSample1.FormatSymbol(style) + ", " + outSample2.FormatSymbol(style) + " := " + inSample.FormatSymbol(style) + " by " + (entry as SplitEntry).proportion.value));
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
                            "equilibrate " + outSample.FormatSymbol(style) + " := " + inSample.FormatSymbol(style) + " for " + (entry as EquilibrateEntry).time.value));
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
                            "dispose " + inSample.FormatSymbol(style)));
                        nextStates.AddUnique(newState, style);
                        if (sequential) return nextStates; // otherwise keep accumulating
                    }
                } else { }
            }
            return nextStates;
        }

        ////###MSAGL
        //public static Microsoft.Msagl.Drawing.Graph MSAGL(Netlist netlist) {
        //    Style style = new Style("•", new SwapMap(subsup: true), new AlphaMap(), "G3", "symbol", ExportTarget.Standard);
        //    Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph");
        //    MSAGL_Edges(graph, netlist, style);
        //    // MSAGL_GraphViz_Nodes(graph, netlist, style);
        //    return graph;
        //}

        //public static void MSAGL_Edges(Microsoft.Msagl.Drawing.Graph graph, Netlist netlist, Style style) {
        //    foreach (ProtocolEntry entry in netlist.AllOperations())
        //        if (entry is MixEntry) {
        //            var node = entry as MixEntry;
        //            graph.AddEdge(node.inSample1.symbol.Format(style), "mix", node.outSample.symbol.Format(style));
        //            graph.AddEdge(node.inSample2.symbol.Format(style), "mix", node.outSample.symbol.Format(style));
        //        } else if (entry is SplitEntry) {
        //            var node = entry as SplitEntry;
        //            graph.AddEdge(node.inSample.symbol.Format(style), "split", node.outSample1.symbol.Format(style));
        //            graph.AddEdge(node.inSample.symbol.Format(style), "split", node.outSample2.symbol.Format(style));
        //        } else if (entry is EquilibrateEntry) {
        //            var node = entry as EquilibrateEntry;
        //            graph.AddEdge(node.inSample.symbol.Format(style), "equilibrate for " + node.time.Format(style), node.outSample.symbol.Format(style));
        //        } else if (entry is TransferEntry) {
        //            var node = entry as TransferEntry;
        //            graph.AddEdge(node.inSample.symbol.Format(style), "transfer", node.outSample.symbol.Format(style));
        //        } else if (entry is DisposeEntry) {
        //            var node = entry as DisposeEntry;
        //            graph.AddEdge(node.inSample.symbol.Format(style), "dispose", "XXX");
        //        }
        //}

    }
    }
