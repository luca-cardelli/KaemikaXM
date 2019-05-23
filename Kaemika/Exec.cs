using System;
using System.Threading;
using System.Collections.Generic;
using QuickGraph;


namespace Kaemika {

    public enum ExportAs : int { None,
        ChemicalTrace, ComputationalTrace,
        ReactionGraph, ComplexGraph,
        MSRC_LBS, MSRC_CRN, ODE,
        Protocol, ProtocolGraph,
        PDMP, //PDMP_Parallel,
        PDMPGraph, //PDMPGraph_Parallel,
        PDMP_GraphViz, //PDMP_Parallel_GraphViz, 
    }

    public class ExecutionInstance { // collect the results of running an Eval
        private static int id = 0;
        public int Id { get; }
        public SampleValue vessel;
        public Netlist netlist;
        public Style style;
        public DateTime startTime;
        public DateTime evalTime;
        public DateTime endTime;
        public Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>> graphCache;
        public Dictionary<string, object> layoutCache; // Dictionary<string, GraphSharp.GraphLayout>
        public ExecutionInstance(SampleValue vessel, Netlist netlist, Style style, DateTime startTime, DateTime evalTime) {
            this.Id = id; id++;
            this.vessel = vessel;
            this.netlist = netlist;
            this.style = style;
            this.startTime = startTime;
            this.evalTime = evalTime;
            this.endTime = DateTime.Now;
            this.graphCache = new Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>>();
            this.layoutCache = new Dictionary<string, object>();
        }
        public string ElapsedTime() {
            return "Elapsed Time: " + endTime.Subtract(startTime).TotalSeconds + "s" 
                //+ "(total), " + endTime.Subtract(evalTime).TotalSeconds + "s (eval)" 
                + Environment.NewLine;
        }
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedPDMPGraph = null;
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedProtocolGraph = null;
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedReactionGraph = null;
        public AdjacencyGraph<Vertex, Edge<Vertex>> cachedComplexGraph = null;
    }

    public class ExecutionMutex {
        private bool isExecuting;
        public ExecutionMutex() {
            this.isExecuting = false;
        }
        public bool IsExecuting() {
            return this.isExecuting;
        }
        public void BeginningExecution() {
            this.isExecuting = true;
        }
        public void EndingExecution() {
            this.isExecuting = false;
        }
    }

    public class Exec { // what runs when we press the "Start" button

        private static ExecutionMutex executionMutex = new ExecutionMutex();

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
            lock (executionMutex) {
                if (executionMutex.IsExecuting()) return; // we are already running an executor worker thread
                else executionMutex.BeginningExecution();
            }
            ProtocolActuator.continueExecution = true;
            if (forkWorker) {
                Thread thread = new Thread(() => Execute_Worker(doParse, doAST, doScope));
                thread.SetApartmentState(ApartmentState.STA); // required to use the clipboard
                thread.Start();
            } else {
                Execute_Worker(doParse, doAST, doScope);
            }
        }

        public static bool IsExecuting() {
            return executionMutex.IsExecuting();
            // do not lock the mutex, this is just an indication, even if say we are not executing, and then we are,
            // double execution is still prevented by the test-and-set lock(executionMutex) at the beginning of Execute_Starter
        }

        public static void EndingExecution() {
            executionMutex.EndingExecution();
            Gui.gui.EndingExecution();
        }

        private static void Execute_Worker(bool doParse, bool doAST, bool doScope) {
            Gui.gui.BeginningExecution();
            Gui.gui.SaveInput();
            Gui.gui.OutputClear("");
            Gui.gui.ChartClear("");
            lastExecution = null;
            DateTime startTime = DateTime.Now;
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
                                DateTime evalTime = DateTime.Now;
                                Env ignoreEnv = statements.Eval(new NullEnv().BuiltIn(vessel), netlist, style);
                                lastExecution = new ExecutionInstance(vessel, netlist, style, startTime, evalTime);
                                foreach (DistributionValue parameter in netlist.Parameters())
                                    Gui.gui.ChartAddParameter(parameter.parameter.Format(style), parameter.drawn, parameter.distribution, parameter.arguments);
                                Gui.gui.ProcessOutput();
                            }
                        }
                    } catch (Error ex) { Gui.gui.InputSetErrorSelection(-1, -1, 0, ex.Message); }
            } else {
                Gui.gui.InputSetErrorSelection(TheParser.parser.FailLineNumber(), TheParser.parser.FailColumnNumber(), TheParser.parser.FailLength(), TheParser.parser.FailMessage());
            }
            EndingExecution();
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
                Gui.gui.OutputSetText(execution.netlist.Format(execution.style.RestyleAsTraceComputational(false)) + execution.ElapsedTime());
            } else if (exportAs == ExportAs.ComputationalTrace) {
                Gui.gui.OutputSetText(execution.netlist.Format(execution.style.RestyleAsTraceComputational(true)) + execution.ElapsedTime());
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
            } else if (exportAs == ExportAs.PDMP) {
                Gui.gui.OutputSetText(Export.PDMP(execution.netlist, execution.style, sequential: true).Format(execution.style));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else if (exportAs == ExportAs.PDMPGraph) {
                if (execution.graphCache.ContainsKey("PDMPGraphSequential")) { }
                else execution.graphCache["PDMPGraphSequential"] = Export.PDMPGraph(execution.netlist, execution.style, sequential: true);
                Gui.gui.ProcessGraph("PDMPGraphSequential"); 
            } else if (exportAs == ExportAs.PDMP_GraphViz) {
                Gui.gui.OutputSetText(Export.PDMP(execution.netlist, execution.style, sequential: true).GraphViz(execution.style));
                try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            //} else if (exportAs == ExportAs.PDMP_Parallel) {
            //    Gui.gui.OutputSetText(Export.PDMP(execution.netlist, execution.style, sequential: false).Format(execution.style));
            //    try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            //} else if (exportAs == ExportAs.PDMPGraph_Parallel) {
            //    if (execution.graphCache.ContainsKey("PDMPGraphParallel")) { }
            //    else execution.graphCache["PDMPGraphParallel"] = Export.PDMPGraph(execution.netlist, execution.style, sequential: false);
            //    Gui.gui.ProcessGraph("PDMPGraphParallel"); 
            //} else if (exportAs == ExportAs.PDMP_Parallel_GraphViz) {
            //    Gui.gui.OutputSetText(Export.PDMP(execution.netlist, execution.style, sequential: false).GraphViz(execution.style));
            //    try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
            } else { };

            lock (exporterMutex) { exporterMutex[exportAs] = false; }
        }

    }

}
