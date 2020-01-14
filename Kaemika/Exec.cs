using System;
using System.Threading;
using System.Collections.Generic;
using QuickGraph;


namespace Kaemika {

    public enum ExportAs : int { None, CRN,
        ChartSnapToClipboard, ChartSnapToSvg, ChartData, OutputCopy,
        ChemicalTrace, ComputationalTrace,
        ReactionGraph, ComplexGraph,
        MSRC_LBS, MSRC_CRN, ODE, SteadyState,
        Protocol, ProtocolGraph,
        PDMPreactions, PDMPequations, PDMPstoichiometry, // or PDMP_Parallel,
        PDMPGraph, // or PDMPGraph_Parallel,
        PDMP_GraphViz, // or PDMP_Parallel_GraphViz, 
    }

    public class ExportAction {
        public string name;
        public ExportAs export;
        public System.Action action;  // this may be invoked from work thread
        public ExportAction(string name, ExportAs export, System.Action action) {
            this.name = name;
            this.export = export;
            this.action = action;
        }
    }

    public class ExecutionInstance { // collect the results of running an Eval
        private static int id = 0;
        public int Id { get; }
        public SampleValue vessel;
        public Netlist netlist;
        public Style style;
        public CRN lastCRN;
        public DateTime startTime;
        public DateTime evalTime;
        public DateTime endTime;
        public Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>> graphCache;
        public Dictionary<string, object> layoutCache; // Dictionary<string, GraphSharp.GraphLayout> from KaemikaXM.GraphLayout.cs
        public ExecutionInstance(SampleValue vessel, Netlist netlist, Style style, DateTime startTime, DateTime evalTime) {
            this.Id = id; id++;
            this.vessel = vessel;
            this.netlist = netlist;
            this.style = style;
            this.lastCRN = null;
            this.startTime = startTime;
            this.evalTime = evalTime;
            this.endTime = DateTime.MinValue;
            this.graphCache = new Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>>();
            this.layoutCache = new Dictionary<string, object>();
        }
        public void EndTime() { this.endTime = DateTime.Now; }
        public string ElapsedTime() {
            return "Elapsed Time: " + endTime.Subtract(startTime).TotalSeconds + "s" 
                //+ "(total), " + endTime.Subtract(evalTime).TotalSeconds + "s (eval)" 
                + Environment.NewLine;
        }
        public string PartialElapsedTime(string msg) {
            return "Partial Elapsed Time: " + DateTime.Now.Subtract(startTime).TotalSeconds + "s (" + msg + ")" 
                //+ "(total), " + endTime.Subtract(evalTime).TotalSeconds + "s (eval)" 
                + Environment.NewLine;
        }
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
            
        public static ExportAction currentOutputAction = 
            new ExportAction("Show initial CRN", ExportAs.CRN, () => {  Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.CRN); });

        public static List<ExportAction> outputActionsList() {
            return new List<ExportAction>() {
                new ExportAction("Show initial CRN", ExportAs.CRN, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.CRN); }),
                new ExportAction("Show chemical trace", ExportAs.ChemicalTrace, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ChemicalTrace); }),
                new ExportAction("Show computational trace", ExportAs.ComputationalTrace, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ComputationalTrace); }),
                new ExportAction("Show reactions", ExportAs.PDMPreactions, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPreactions); }),
                new ExportAction("Show equations", ExportAs.PDMPequations, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPequations); }),
                new ExportAction("Show stoichiometry", ExportAs.PDMPstoichiometry, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPstoichiometry); }),
                new ExportAction("Show protocol", ExportAs.Protocol, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.Protocol); }),
                new ExportAction("Show last simulation state", ExportAs.None, () => {
                    Gui.gui.OutputSetText("");
                    string s = Exec.lastReport + Environment.NewLine + Exec.lastState + Environment.NewLine;
                    Gui.gui.OutputAppendText(s);
                    // try { System.Windows.Forms.Clipboard.SetText(s); } catch (ArgumentException) { };
                }),
            };
        }

        public static List<ExportAction> exportActionsList() {
            return new List<ExportAction>() {
                new ExportAction("Write chart image to clipboard", ExportAs.ChartSnapToClipboard, () => { Gui.gui.ChartSnap(); }),
                new ExportAction("Write chart image as SVG", ExportAs.ChartSnapToSvg, () => { Gui.gui.ChartSnapToSvg(); }),
                new ExportAction("Write visible chart data to disk", ExportAs.ChartData, () => { Gui.gui.ChartData(); }),
                new ExportAction("Write output text to clipboard", ExportAs.OutputCopy, () => { Gui.gui.OutputCopy(); }),

                new ExportAction("Export reaction graph", ExportAs.ReactionGraph, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ReactionGraph); }),
                new ExportAction("Export reaction complex graph", ExportAs.ComplexGraph, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ComplexGraph); }),
                new ExportAction("Export protocol step graph", ExportAs.ProtocolGraph, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ProtocolGraph); }),
                new ExportAction("Export protocol state graph", ExportAs.PDMPGraph, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPGraph); }),

                new ExportAction("Export CRN (LBS silverlight)", ExportAs.MSRC_LBS, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.MSRC_LBS); }),
                new ExportAction("Export CRN (LBS html5)", ExportAs.MSRC_CRN, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.MSRC_CRN); }),
                new ExportAction("Export ODE (Oscill8)", ExportAs.ODE, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ODE); }),
                new ExportAction("Export equilibrium (Wolfram)", ExportAs.SteadyState, () => { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.SteadyState); }),

                //new ExportAction("PDMP GraphViz", ExportAs.PDMP_GraphViz, () => { Exec.Execute_Exporter(false, ExportAs.PDMP_GraphViz); }),
                //new ExportAction("PDMP Parallel", ExportAs.PDMP_Parallel, () => { Exec.Execute_Exporter(false, ExportAs.PDMP_Parallel); }),
                //new ExportAction("PDMP Parallel GraphViz", ExportAs.PDMP_Parallel_GraphViz, () => { Exec.Execute_Exporter(false, ExportAs.PDMP_Parallel_GraphViz); }),
            }; 
        }

        public static void Execute_Starter(bool forkWorker, bool doParse = false, bool doAST = false, bool doScope = false, bool autoContinue = false) {
            lock (executionMutex) {
                if (executionMutex.IsExecuting()) return; // we are already running an executor worker thread
                else executionMutex.BeginningExecution();
            }
            Protocol.continueExecution = true;
            if (forkWorker) {
                Thread thread = new Thread(() => Execute_Worker(doParse, doAST, doScope, autoContinue));
                thread.SetApartmentState(ApartmentState.STA); // required to use the clipboard
                thread.Start();
            } else {
                Execute_Worker(doParse, doAST, doScope, autoContinue);
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

        const bool scopeVariants = true;
        const bool remapVariants = true;

        private static void Execute_Worker(bool doParse, bool doAST, bool doScope, bool autoContinue) {
            Gui.gui.BeginningExecution();
            Gui.gui.SaveInput();
            Gui.gui.OutputClear("");
            KChartHandler.ChartClear("");
            Gui.gui.ParametersClear();
            ProtocolDevice.Clear();
            lastExecution = null;
            DateTime startTime = DateTime.Now;
            if (TheParser.Parser().Parse(Gui.gui.InputGetText(), out IReduction root)) {
                if (doParse) root.DrawReductionTree(Gui.gui);
                else try {
                        Statements statements = Parser.ParseTop(root);
                        if (doAST) Gui.gui.OutputAppendText(statements.Format());
                        else {
                            SampleValue vessel = Vessel();
                            Scope scope = statements.Scope(new NullScope().BuiltIn(vessel));
                            if (doScope) Gui.gui.OutputAppendText(scope.Format());
                            else {
                                Style style = new Style(varchar: scopeVariants ? defaultVarchar : null, new SwapMap(),
                                                        map: remapVariants ? new AlphaMap() : null, numberFormat: "G4", dataFormat: "full",  // we want it full for samples, but maybe only headers for functions/networks?
                                                        exportTarget: ExportTarget.Standard, traceComputational: false);
                                Netlist netlist = new Netlist(autoContinue);
                                KChartHandler.SetStyle(style);
                                ProtocolDevice.SetStyle(style);
                                ProtocolDevice.Sample(vessel, style);
                                netlist.Emit(new SampleEntry(vessel));
                                DateTime evalTime = DateTime.Now;
                                lastExecution = new ExecutionInstance(vessel, netlist, style, startTime, evalTime);
                                Env ignoreEnv = statements.Eval(new NullEnv().BuiltIn(vessel), netlist, style);
                                lastExecution.EndTime();
                                foreach (DistributionValue parameter in netlist.Parameters())
                                    Gui.gui.AddParameter(parameter.parameter.Format(style), parameter.drawn, parameter.distribution, parameter.arguments);
                                Gui.gui.ParametersUpdate();
                                Gui.gui.ProcessOutput();
                            }
                        }
                    }
                    catch (ExecutionEnded) { }
                    catch (Error ex) { Gui.gui.InputSetErrorSelection(-1, -1, 0, ex.Message); }
            } else {
                Gui.gui.InputSetErrorSelection(TheParser.Parser().FailLineNumber(), TheParser.Parser().FailColumnNumber(), TheParser.Parser().FailLength(), TheParser.Parser().FailMessage());
            }
            EndingExecution();
        }

        public static SampleValue Vessel() {
            Symbol vessel = new Symbol("vessel");
            return new SampleValue(vessel, new StateMap(vessel, new List<SpeciesValue> { }, new State(0, lna:false)), new NumberValue(1.0), new NumberValue(293.15), produced: false);
        }
        public static bool IsVesselVariant(SampleValue sample) {
            return sample.symbol.IsVesselVariant();
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
            try {
                var execution = lastExecution; // atomically copy it
                if (execution == null) {
                } else if (exportAs == ExportAs.None) {

                } else if (exportAs == ExportAs.CRN) {
                    Gui.gui.OutputAppendText((execution.lastCRN != null ? execution.lastCRN.FormatNice(execution.style) : "") + execution.ElapsedTime());
                } else if (exportAs == ExportAs.ChemicalTrace) {
                    Gui.gui.OutputAppendText(execution.netlist.Format(execution.style.RestyleAsTraceComputational(false)) + execution.ElapsedTime());
                } else if (exportAs == ExportAs.ComputationalTrace) {
                    Gui.gui.OutputAppendText(execution.netlist.Format(execution.style.RestyleAsTraceComputational(true)) + execution.ElapsedTime());
                } else if (exportAs == ExportAs.PDMPreactions) {
                    Gui.gui.OutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: true).HybridSystem(Export.Presentation.Reactions, execution.style));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                } else if (exportAs == ExportAs.PDMPequations) {
                    Gui.gui.OutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: true).HybridSystem(Export.Presentation.ODEs, execution.style));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                } else if (exportAs == ExportAs.PDMPstoichiometry) {
                    Gui.gui.OutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: true).HybridSystem(Export.Presentation.Stoichiometry, execution.style));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };

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
                    if (execution.graphCache.ContainsKey("PDMPGraphSequential")) { }
                    else execution.graphCache["PDMPGraphSequential"] = Export.PDMPGraph(execution.netlist, execution.style, sequential: true);
                    Gui.gui.ProcessGraph("PDMPGraphSequential"); 

                // Windows only
                } else if (exportAs == ExportAs.MSRC_LBS) { // export only the vessel
                    Gui.gui.OutputAppendText(Export.MSRC_LBS(execution.netlist.Reports(execution.vessel.stateMap.species), execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.LBS, traceComputational: false)));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                } else if (exportAs == ExportAs.MSRC_CRN) { // export only the vessel
                    Gui.gui.OutputAppendText(Export.MSRC_CRN(execution.netlist.Reports(execution.vessel.stateMap.species), execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.CRN, traceComputational: false)));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                } else if (exportAs == ExportAs.ODE) { // export only the vessel
                    Gui.gui.OutputAppendText(Export.ODE(execution.vessel, new CRN(execution.vessel, execution.vessel.ReactionsAsConsumed(execution.style), precomputeLNA: false),
                        new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.Standard, traceComputational: false)));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                } else if (exportAs == ExportAs.SteadyState) { // export only the vessel
                    Gui.gui.OutputAppendText(Export.SteadyState(new CRN(execution.vessel, execution.vessel.ReactionsAsConsumed(execution.style), precomputeLNA: false),
                        new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.WolframNotebook, traceComputational: false)));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                } else if (exportAs == ExportAs.Protocol) {
                    Gui.gui.OutputAppendText(Export.Protocol(execution.netlist, new Style(varchar: defaultVarchar, new SwapMap(), map: new AlphaMap(), numberFormat: "G4", dataFormat: "symbol", exportTarget: ExportTarget.Standard, traceComputational: false)));
                    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };

                //} else if (exportAs == ExportAs.PDMP_GraphViz) {
                //    Gui.gui.OutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: true).GraphViz(execution.style));
                //    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                //} else if (exportAs == ExportAs.PDMP_Parallel) {
                //    Gui.gui.OutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: false).Format(execution.style));
                //    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                //} else if (exportAs == ExportAs.PDMPGraph_Parallel) {
                //    if (execution.graphCache.ContainsKey("PDMPGraphParallel")) { }
                //    else execution.graphCache["PDMPGraphParallel"] = Export.PDMPGraph(execution.netlist, execution.style, sequential: false);
                //    Gui.gui.ProcessGraph("PDMPGraphParallel"); 
                //} else if (exportAs == ExportAs.PDMP_Parallel_GraphViz) {
                //    Gui.gui.OutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: false).GraphViz(execution.style));
                //    //try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                } else { };

                lock (exporterMutex) { exporterMutex[exportAs] = false; }
            } catch (Error ex) { Gui.gui.InputSetErrorSelection(-1, -1, 0, ex.Message); }
        }

    }

}
