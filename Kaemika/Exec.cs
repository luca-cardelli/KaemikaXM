using System;
using System.Threading;
using System.Collections.Generic;
using QuickGraph;


namespace Kaemika {

    public enum ExportAs : int { None, ChemicalTrace, ComputationalTrace, ReactionGraph, ComplexGraph, MSRC_LBS, MSRC_CRN, ODE, Protocol, ProtocolGraph, PDMPGraph, GraphViz, PDMP, PDMP_Sequential }

    public class ExecutionInstance { // collect the results of running an Eval
        private static int id = 0;
        public int Id { get; }
        public SampleValue vessel;
        public Netlist netlist;
        public Style style;
        public Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>> graphCache;
        public Dictionary<string, object> layoutCache; // Dictionary<string, GraphSharp.GraphLayout>
        public ExecutionInstance (SampleValue vessel, Netlist netlist, Style style) {
            this.Id = id; id++;
            this.vessel = vessel;
            this.netlist = netlist;
            this.style = style;
            this.graphCache = new Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>>();
            this.layoutCache = new Dictionary<string, object>();
        }
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedPDMPGraph = null;
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedProtocolGraph = null;
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedReactionGraph = null;
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedComplexGraph = null;
    }

    public class Exec { // what runs when we press the "Start" button

        private static int UID = 0;
        public static int NewUID() {
            UID = UID + 1;
            return UID;
        }

        public static string defaultVarchar = "•";  // "•" "≈" "▪"
        public static ExecutionInstance lastExecution = null;

        public static string lastReport = ""; // the last report of the last simulation, in string form
        public static string lastState = ""; // the last state of the last simulation, in string form, including covariance matrix if LNA

        public static void Execute_Starter(bool forkWorker, bool doParse = false, bool doAST = false, bool doScope = false) {
            if (Gui.gui.StopEnabled()) return; // we are already running an executor; this may or may not be interlocked in the Gui, so better be safe
            Gui.gui.StopEnable(true);
            ProtocolActuator.continueExecution = true;
            if (forkWorker) {
                Thread thread = new Thread(() => Execute_Worker(doParse, doAST, doScope));
                thread.SetApartmentState(ApartmentState.STA); // required to use the clipboard
                thread.Start();
            } else {
                Execute_Worker(doParse, doAST, doScope);
            }
        }

        private static void Execute_Worker(bool doParse, bool doAST, bool doScope) {
            Gui.gui.SaveInput();
            Gui.gui.OutputClear("");
            Gui.gui.ChartClear("");
            lastExecution = null;
            if (TheParser.parser.Parse(Gui.gui.InputGetText(), out IReduction root)) {
                if (doParse) root.DrawReductionTree(Gui.gui);
                else try {
                        Statements statements = Parser.ParseTop(root);
                        if (doAST) Gui.gui.OutputAppendText(statements.Format());
                        else {
                            SampleValue vessel = Vessel();
                            Scope scope = statements.Scope(new NullScope().BuiltIn(vessel));
                            if (doScope) Gui.gui.OutputAppendText(scope.Format());
                            else {
                                Netlist netlist = new Netlist();
                                netlist.Emit(new SampleEntry(vessel));
                                Style style = new Style(varchar: Gui.gui.ScopeVariants() ? defaultVarchar : null, new SwapMap(),
                                                        map: Gui.gui.RemapVariants() ? new AlphaMap() : null, numberFormat: "G4", dataFormat: "full",  // we want it full for samples, but maybe only headers for functions/networks?
                                                        exportTarget: ExportTarget.Standard, traceComputational: false);
                                Env ignoreEnv = statements.Eval(new NullEnv().BuiltIn(vessel), netlist, style);
                                lastExecution = new ExecutionInstance(vessel, netlist, style);
                                Gui.gui.ProcessOutput();
                            }
                        }
                    } catch (Error ex) { Gui.gui.InputSetErrorSelection(-1, -1, 0, ex.Message); }
            } else {
                Gui.gui.InputSetErrorSelection(TheParser.parser.FailLineNumber(), TheParser.parser.FailColumnNumber(), TheParser.parser.FailLength(), TheParser.parser.FailMessage());
            }
            Gui.gui.StopEnable(false);
        }

        public static SampleValue Vessel() {
            return new SampleValue(new Symbol("vessel"), new NumberValue(1.0), new NumberValue(293.15), produced: false);
        }

        public static Dictionary<ExportAs, bool> exporterMutex = new Dictionary<ExportAs, bool>(); // only run one of each kind of export at once

        public static void Execute_Exporter(bool forkWorker, ExportAs exportAs) {
            if (forkWorker) {
                lock (exporterMutex) {
                    if (exporterMutex.ContainsKey(exportAs) && exporterMutex[exportAs] == true) return;
                    else exporterMutex[exportAs] = true;
                }
                Thread thread = new Thread(() => Execute_Exporter_Worker(exportAs));
                thread.SetApartmentState(ApartmentState.STA); // required to use the clipboard
                thread.Start();
            } else {
                Execute_Exporter_Worker(exportAs);
            }
        }

        private static void Execute_Exporter_Worker(ExportAs exportAs) {
            var execution = lastExecution; // atomically copy it
            if (execution == null) {
            } else if (exportAs == ExportAs.None) {
            } else if (exportAs == ExportAs.ChemicalTrace) {
                Gui.gui.OutputSetText(execution.netlist.Format(execution.style.RestyleAsTraceComputational(false)));
            } else if (exportAs == ExportAs.ComputationalTrace) {
                Gui.gui.OutputSetText(execution.netlist.Format(execution.style.RestyleAsTraceComputational(true)));
            } else if (exportAs == ExportAs.MSRC_LBS) {
                Gui.gui.OutputSetText(Export.MSRC_LBS(execution.netlist, execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.LBS, traceComputational: false)));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else if (exportAs == ExportAs.MSRC_CRN) {
                Gui.gui.OutputSetText(Export.MSRC_CRN(execution.netlist, execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.CRN, traceComputational: false)));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else if (exportAs == ExportAs.ODE) {
                Gui.gui.OutputSetText(Export.ODE(execution.vessel, new CRN(execution.vessel, execution.netlist.RelevantReactions(execution.vessel, execution.vessel.species, execution.style), precomputeLNA: false),
                    new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.Standard, traceComputational: false)));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else if (exportAs == ExportAs.Protocol) {
                Gui.gui.OutputSetText(Export.Protocol(execution.netlist, new Style(varchar: defaultVarchar, new SwapMap(), map: new AlphaMap(), numberFormat: "G4", dataFormat: "symbol", exportTarget: ExportTarget.Standard, traceComputational: false)));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else if (exportAs == ExportAs.ReactionGraph) {
                if (execution.graphCache.ContainsKey("ReactionGraph")) { }
                else execution.graphCache["ReactionGraph"] = Export.ReactionGraph(execution.netlist.AllSpecies(), execution.netlist.AllReactions(), execution.style);
                Gui.gui.ProcessGraph("ReactionGraph");
            } else if (exportAs == ExportAs.ComplexGraph) {
                if (execution.graphCache.ContainsKey("ComplexGraph")) { }
                else execution.graphCache["ComplexGraph"] = Export.ComplexGraph(execution.netlist.AllSpecies(), execution.netlist.AllReactions(), execution.style);
                Gui.gui.ProcessGraph("ComplexGraph");
            } else if (exportAs == ExportAs.ProtocolGraph) {
                if (execution.graphCache.ContainsKey("ProtocolGraph")) { }
                else execution.graphCache["ProtocolGraph"] = Export.ProtocolGraph(execution.netlist, execution.style);
                Gui.gui.ProcessGraph("ProtocolGraph");
            } else if (exportAs == ExportAs.PDMPGraph) {
                if (execution.graphCache.ContainsKey("PDMPGraph")) { }
                else execution.graphCache["PDMPGraph"] = Export.PDMPGraph(execution.netlist, execution.style, sequential: false);
                Gui.gui.ProcessGraph("PDMPGraph"); 
            } else if (exportAs == ExportAs.GraphViz) {
                Gui.gui.OutputSetText(Export.GraphViz(execution.netlist));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else if (exportAs == ExportAs.PDMP) {
                Gui.gui.OutputSetText(Export.PDMPGraphViz(execution.netlist, execution.style, sequential: false));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else if (exportAs == ExportAs.PDMP_Sequential) {
                Gui.gui.OutputSetText(Export.PDMPGraphViz(execution.netlist, execution.style, sequential: true));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else { };

            lock (exporterMutex) { exporterMutex[exportAs] = false; }
        }

    }

}
