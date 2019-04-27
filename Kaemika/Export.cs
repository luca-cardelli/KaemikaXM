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
            Style style = new Style("•", new SwapMap(subsup: true), new AlphaMap(), "G3", "symbol", ExportTarget.Standard);
            string edges = GraphViz_Edges(netlist, style);
            string nodes = GraphViz_Nodes(netlist, style);
            return "digraph G {" + Environment.NewLine + edges + nodes + "}" + Environment.NewLine;
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
                if (label.Length > 0 && label[label.Length - 1] == '\n') label = label.Substring(0, label.Length - 2);
                nodes += sample.symbol.Format(style) + "[shape=box, label=" + Parser.FormatString(label) + "];" + Environment.NewLine;
            }   
            return nodes + "XXX [shape=box, label=\"(dispose)\"]" + Environment.NewLine; // Dispose node
        }

    }
}
