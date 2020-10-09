using System;
using System.Threading;
using System.Collections.Generic;
using QuickGraph;
using SkiaSharp;


namespace Kaemika {

    public enum ExportAs : int { None, ReactionScore, CRN,
        ChartSnapToClipboard, ChartSnapToSvg, ChartData, OutputCopy,
        ChemicalTrace, FullTrace, Evaluation,
        ReactionGraph, ComplexGraph,
        MSRC_LBS, MSRC_CRN, ODE, SteadyState,
        Protocol, ProtocolGraph,
        PDMPreactions, PDMPequations, PDMPstoichiometry, // or PDMP_Parallel,
        PDMPGraph, // or PDMPGraph_Parallel,
        PDMP_GraphViz, // or PDMP_Parallel_GraphViz, 
        SVG,
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
        public Env environment;
        public Netlist netlist;
        public Style style;
        public CRN lastCRN;
        public DateTime startTime;
        public DateTime evalTime;
        public DateTime endTime;
        public Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>> graphCache;
        public Dictionary<string, object> layoutCache; // Dictionary<string, GraphSharp.GraphLayout> from KaemikaXM.GraphLayout.cs
        public int rejected;
        public ExecutionInstance(SampleValue vessel, Netlist netlist, Style style, DateTime startTime, DateTime evalTime) {
            this.Id = id; id++;
            this.vessel = vessel;
            this.environment = new NullEnv();
            this.netlist = netlist;
            this.style = style;
            this.lastCRN = null;
            this.startTime = startTime;
            this.evalTime = evalTime;
            this.endTime = DateTime.MinValue;
            this.graphCache = new Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>>();
            this.layoutCache = new Dictionary<string, object>();
            this.rejected = 0;
        }
        public void EndTime() { this.endTime = DateTime.Now; }
        public string ElapsedTime() {
            return 
                (rejected > 0 ? "Rejected " + rejected.ToString() + Environment.NewLine : "" ) +
                "Elapsed Time: " + endTime.Subtract(startTime).TotalSeconds + "s" 
                //+ "(total), " + endTime.Subtract(evalTime).TotalSeconds + "s (eval)" 
                + Environment.NewLine;
        }
        public string PartialElapsedTime(string msg) {
            return "Partial Elapsed Time: " + DateTime.Now.Subtract(startTime).TotalSeconds + "s (" + msg + ")" 
                //+ "(total), " + endTime.Subtract(evalTime).TotalSeconds + "s (eval)" 
                + Environment.NewLine;
        }
        public void ResetGraphCache() {
            this.graphCache = new Dictionary<string, AdjacencyGraph<Vertex, Edge<Vertex>>>();
            this.layoutCache = new Dictionary<string, object>();
        }
        public static void Reject() {
            if (Exec.lastExecution != null) Exec.lastExecution.rejected++;
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

        public static string defaultVarchar = "»"; // "ˍ" "•" "≈" "▪" "ˬ"
        public static ExecutionInstance lastExecution = null;

        //public static string lastReport = ""; // the last report of the last simulation, in string form
        //public static string lastState = ""; // the last state of the last simulation, in string form, including covariance matrix if LNA

        public static ExportAction showReactionScoreOutputAction =
            new ExportAction("Show reaction score", ExportAs.ReactionScore, () => { KScoreHandler.ScoreShow(); KGui.gui.GuiOutputTextHide(); Exec.Execute_Exporter(false, ExportAs.ReactionScore); });
        public static ExportAction showInitialCRNOutputAction =
            new ExportAction("Show initial CRN", ExportAs.CRN, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.CRN); });
        public static ExportAction showEvaluationOutputAction =
            new ExportAction("Show evaluation", ExportAs.Evaluation, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.Evaluation); });

        public static ExportAction currentOutputAction = showReactionScoreOutputAction; // initialized by RestorePreferences

        public static List<ExportAction> outputActionsList() {
            return new List<ExportAction>() {
                showReactionScoreOutputAction,
                showInitialCRNOutputAction,
                showEvaluationOutputAction,
                //new ExportAction("Show full trace", ExportAs.FullTrace, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.FullTrace); }),
                new ExportAction("Show chemical trace", ExportAs.ChemicalTrace, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ChemicalTrace); }),
                new ExportAction("Show reactions", ExportAs.PDMPreactions, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide();  KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPreactions); }),
                new ExportAction("Show equations", ExportAs.PDMPequations, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide();  KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPequations); }),
                new ExportAction("Show stoichiometry", ExportAs.PDMPstoichiometry, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide();  KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPstoichiometry); }),
                new ExportAction("Show protocol", ExportAs.Protocol, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide();  KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.Protocol); }),
                //new ExportAction("Show last simulation state", ExportAs.None, () => {
                //    KGui.gui.GuiOutputSetText("");
                //    string s = Exec.lastReport + Environment.NewLine + Exec.lastState + Environment.NewLine;
                //    KGui.gui.GuiOutputAppendText(s);
                //    // try { System.Windows.Forms.Clipboard.SetText(s); } catch (ArgumentException) { };
                //}),
            };
        }

        public static ExportAction OutputActionNamed(string name) {
            foreach (ExportAction item in outputActionsList())
                if (item.name == name) return item;
            return showReactionScoreOutputAction; // this may happen if we obsolete or rename an output action, but then we restore its menu selection from disk-stored preferences. So we reset it to default.
        }

        public static List<ExportAction> exportActionsList() {
            return new List<ExportAction>() {
                new ExportAction("Write images to clipboard", ExportAs.ChartSnapToClipboard, () => { KGui.gui.GuiChartSnap(); }),
                new ExportAction("Write images to file (SVG)", ExportAs.ChartSnapToSvg, () => { KGui.gui.GuiChartSnapToSvg(); }),
                new ExportAction("Write visible chart data to file", ExportAs.ChartData, () => { KGui.gui.GuiChartData(); }),
                new ExportAction("Write output text to clipboard", ExportAs.OutputCopy, () => { KGui.gui.GuiOutputCopy(); }),

                //new ExportAction("Export reaction graph", ExportAs.ReactionGraph, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ReactionGraph); }),
                //new ExportAction("Export reaction complex graph", ExportAs.ComplexGraph, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ComplexGraph); }),
                new ExportAction("Export protocol step graph", ExportAs.ProtocolGraph, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ProtocolGraph); }),
                new ExportAction("Export protocol state graph", ExportAs.PDMPGraph, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPGraph); }),

                new ExportAction("Export CRN (LBS silverlight)", ExportAs.MSRC_LBS, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.MSRC_LBS); }),
                new ExportAction("Export CRN (LBS html5)", ExportAs.MSRC_CRN, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.MSRC_CRN); }),
                new ExportAction("Export ODE (Oscill8)", ExportAs.ODE, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ODE); }),
                new ExportAction("Export equilibrium (Wolfram)", ExportAs.SteadyState, () => { KGui.gui.GuiOutputTextShow(); KScoreHandler.ScoreHide(); KGui.gui.GuiOutputSetText(""); Exec.Execute_Exporter(false, ExportAs.SteadyState); }),

                //new ExportAction("PDMP GraphViz", ExportAs.PDMP_GraphViz, () => { Exec.Execute_Exporter(false, ExportAs.PDMP_GraphViz); }),
                //new ExportAction("PDMP Parallel", ExportAs.PDMP_Parallel, () => { Exec.Execute_Exporter(false, ExportAs.PDMP_Parallel); }),
                //new ExportAction("PDMP Parallel GraphViz", ExportAs.PDMP_Parallel_GraphViz, () => { Exec.Execute_Exporter(false, ExportAs.PDMP_Parallel_GraphViz); }),
            }; 
        }

        public static void Execute_Starter(bool forkWorker, bool doParse = false, bool doAST = false, bool doScope = false, bool autoContinue = false, bool chartOutput = true) {
            lock (executionMutex) {
                if (executionMutex.IsExecuting()) return; // we are already running an executor worker thread
                else executionMutex.BeginningExecution();
            }
            Protocol.continueExecution = true;
            if (forkWorker) {
                Thread thread = new Thread(() => Execute_Worker(doParse, doAST, doScope, autoContinue, chartOutput));
                thread.SetApartmentState(ApartmentState.STA); // required to use the clipboard
                thread.Start();
            } else {
                Execute_Worker(doParse, doAST, doScope, autoContinue, chartOutput);
            }
        }

        public static bool IsExecuting() {
            return executionMutex.IsExecuting();
            // do not lock the mutex, this is just an indication, even if say we are not executing, and then we are,
            // double execution is still prevented by the test-and-set lock(executionMutex) at the beginning of Execute_Starter
        }

        public static void EndingExecution() {
            executionMutex.EndingExecution();
            KGui.gui.GuiEndingExecution();
        }

        const bool scopeVariants = true;
        const bool remapVariants = true;

        private static void Execute_Worker(bool doParse, bool doAST, bool doScope, bool autoContinue, bool chartOutput) {
            KGui.gui.GuiBeginningExecution();
            lastExecution = null;
            KGui.gui.GuiSaveInput();
            KGui.gui.GuiOutputClear();
            DateTime startTime = DateTime.Now;
            if (TheParser.Parser().Parse(KGui.gui.GuiInputGetText(), out IReduction root)) {
               if (doParse) root.DrawReductionTree();
                else {
                    Netlist netlist = new Netlist(autoContinue);
                    try {
                        Statements statements = Parser.ParseTop(root);
                        if (doAST) KGui.gui.GuiOutputAppendText(statements.Format());
                        else {
                            SampleValue vessel = Vessel();
                            Env initialEnv = new ValueEnv("vessel", Type.Sample, vessel, new BuiltinEnv(new NullEnv()));
                            Scope initialScope = initialEnv.ToScope();
                            Scope scope = statements.Scope(initialScope);
                            if (doScope) KGui.gui.GuiOutputAppendText(scope.Format());
                            else {
                                Style style = new Style(varchar: scopeVariants ? defaultVarchar : null, new SwapMap(),
                                                        map: remapVariants ? new AlphaMap() : null, numberFormat: "G4", dataFormat: "full",  // we want it full for samples, but maybe only headers for functions/networks?
                                                        exportTarget: ExportTarget.Standard, traceFull: false, chartOutput: chartOutput);
                                KChartHandler.ChartClear("", "s", "M", style);
                                KChartHandler.LegendUpdate(style);
                                KScoreHandler.ScoreClear();
                                KControls.ParametersClear(style);
                                KDeviceHandler.Clear(style);
                                KDeviceHandler.Sample(vessel, style);

                                netlist.Emit(new SampleEntry(vessel));
                                DateTime evalTime = DateTime.Now;
                                lastExecution = new ExecutionInstance(vessel, netlist, style, startTime, evalTime);
                                lastExecution.environment = statements.EvalReject(initialEnv, netlist, style, 0);
                                if (lastExecution.environment == null) throw new Error("Top level reject");
                                lastExecution.EndTime();

                                if (style.chartOutput) {
                                    foreach (ParameterEntry parameter in netlist.Parameters())
                                        KControls.AddParameter(parameter.symbol.Format(style), (parameter.value as NumberValue).value, parameter.distribution, style);
                                    KGui.gui.GuiParametersUpdate(); // calls back KControls.ParametersUpdate, but only on Win/Mac
                                }

                                KGui.gui.GuiProcessOutput();
                            }
                        }
                    }
                    catch (ExecutionEnded) { lastExecution.EndTime(); KGui.gui.GuiOutputAppendText(lastExecution.ElapsedTime()); }
                    catch (ConstantEvaluation ex) { string cat = "Does not have a value: "; netlist.Emit(new CommentEntry(cat + ": " + ex.Message)); KGui.gui.GuiInputSetErrorSelection(-1, -1, 0, cat, ex.Message); }
                    catch (Error ex) { netlist.Emit(new CommentEntry(ex.Message)); KGui.gui.GuiInputSetErrorSelection(-1, -1, 0, "Error", ex.Message); try { KGui.gui.GuiProcessOutput(); } catch { }; }
                    catch (StackOverflowException ex) { netlist.Emit(new CommentEntry(ex.Message)); KGui.gui.GuiInputSetErrorSelection(-1, -1, 0, "Stack Overflow", ex.Message); } 
                    catch (Exception ex) { string cat = "Something happened"; netlist.Emit(new CommentEntry(cat + ": " + ex.Message)); KGui.gui.GuiInputSetErrorSelection(-1, -1, 0, cat, ex.Message); } 
                }
            } else {
                KGui.gui.GuiInputSetErrorSelection(TheParser.Parser().FailLineNumber(), TheParser.Parser().FailColumnNumber(), TheParser.Parser().FailLength(), TheParser.Parser().FailCategory(), TheParser.Parser().FailMessage());
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

                } else if (exportAs == ExportAs.ReactionScore) {

                } else if (exportAs == ExportAs.CRN) {
                    string hungarization = "";
                    //####Poliynomize
                    //Hungarize.HungarizeCRN(execution.lastCRN, execution.style);

                    KGui.gui.GuiOutputAppendText(execution.netlist.AllComments() + (execution.lastCRN != null ? execution.lastCRN.FormatNice(execution.style) : "") + hungarization + execution.ElapsedTime());
                } else if (exportAs == ExportAs.Evaluation) {
                    KGui.gui.GuiOutputAppendText(execution.netlist.AllComments() + Env.FormatTopLevel(execution.environment, execution.style) + execution.ElapsedTime());
                } else if (exportAs == ExportAs.ChemicalTrace) {
                    KGui.gui.GuiOutputAppendText(execution.netlist.Format(execution.style.RestyleAsTraceFull(false)) + execution.ElapsedTime());
                //} else if (exportAs == ExportAs.FullTrace) {
                //    KGui.gui.GuiOutputAppendText(execution.netlist.Format(execution.style.RestyleAsTraceFull(true)) + execution.ElapsedTime());
                } else if (exportAs == ExportAs.PDMPreactions) {
                    KGui.gui.GuiOutputAppendText(execution.netlist.AllComments() + Export.PDMP(execution.netlist, execution.style, sequential: true).HybridSystem(Export.Presentation.Reactions, execution.style));
                } else if (exportAs == ExportAs.PDMPequations) {
                    KGui.gui.GuiOutputAppendText(execution.netlist.AllComments() + Export.PDMP(execution.netlist, execution.style, sequential: true).HybridSystem(Export.Presentation.ODEs, execution.style));
                }
                else if (exportAs == ExportAs.PDMPstoichiometry) {
                    KGui.gui.GuiOutputAppendText(execution.netlist.AllComments() + Export.PDMP(execution.netlist, execution.style, sequential: true).HybridSystem(Export.Presentation.Stoichiometry, execution.style));

                    //} else if (exportAs == ExportAs.ReactionGraph) {
                    //    if (execution.graphCache.ContainsKey("ReactionGraph")) { }
                    //    else execution.graphCache["ReactionGraph"] = Export.ReactionGraph(execution.netlist.AllSpecies(), execution.netlist.AllReactions(), execution.style);
                    //    KGui.gui.GuiProcessGraph("ReactionGraph");
                    //} else if (exportAs == ExportAs.ComplexGraph) {
                    //    if (execution.graphCache.ContainsKey("ComplexGraph")) { }
                    //    else execution.graphCache["ComplexGraph"] = Export.ComplexGraph(execution.netlist.AllSpecies(), execution.netlist.AllReactions(), execution.style);
                    //    KGui.gui.GuiProcessGraph("ComplexGraph");
                }
                else if (exportAs == ExportAs.ProtocolGraph) {
                    if (execution.graphCache.ContainsKey("ProtocolGraph")) { }
                    else execution.graphCache["ProtocolGraph"] = Export.ProtocolGraph(execution.netlist, execution.style);
                    KGui.gui.GuiProcessGraph("ProtocolGraph");
                } else if (exportAs == ExportAs.PDMPGraph) {
                    if (execution.graphCache.ContainsKey("PDMPGraphSequential")) { }
                    else execution.graphCache["PDMPGraphSequential"] = Export.PDMPGraph(execution.netlist, execution.style, sequential: true);
                    KGui.gui.GuiProcessGraph("PDMPGraphSequential");

                // Android/iOS only
                } else if (exportAs == ExportAs.SVG) {
                    KGui.gui.GuiOutputAppendText(Export.SnapToSVG(KGui.gui.GuiChartSize()));

                // Windows/Mac only
                }
                else if (exportAs == ExportAs.MSRC_LBS) { // export only the vessel
                    //KGui.gui.GuiOutputAppendText(Export.MSRC_LBS(execution.netlist.Reports(execution.vessel.stateMap.species), execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.LBS, traceFull: false, chartOutput: false)));
                    KGui.gui.GuiOutputAppendText(Export.MSRC_LBS(execution.vessel.RelevantReports(execution.style), execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.LBS, traceFull: false, chartOutput: false)));
                }
                else if (exportAs == ExportAs.MSRC_CRN) { // export only the vessel
                    //KGui.gui.GuiOutputAppendText(Export.MSRC_CRN(execution.netlist.Reports(execution.vessel.stateMap.species), execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.CRN, traceFull: false, chartOutput: false)));
                    KGui.gui.GuiOutputAppendText(Export.MSRC_CRN(execution.vessel.RelevantReports(execution.style), execution.vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.CRN, traceFull: false, chartOutput: false)));
                }
                else if (exportAs == ExportAs.ODE) { // export only the vessel
                    KGui.gui.GuiOutputAppendText(Export.ODE(execution.vessel, new CRN(execution.vessel, execution.vessel.ReactionsAsConsumed(execution.style), precomputeLNA: false),
                        new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.Standard, traceFull: false, chartOutput: false)));
                }
                else if (exportAs == ExportAs.SteadyState) { // export only the vessel
                    KGui.gui.GuiOutputAppendText(Export.SteadyState(new CRN(execution.vessel, execution.vessel.ReactionsAsConsumed(execution.style), precomputeLNA: false),
                        new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.WolframNotebook, traceFull: false, chartOutput: false)));
                }
                else if (exportAs == ExportAs.Protocol) {
                    KGui.gui.GuiOutputAppendText(Export.Protocol(execution.netlist, new Style(varchar: defaultVarchar, new SwapMap(), map: new AlphaMap(), numberFormat: "G4", dataFormat: "symbol", exportTarget: ExportTarget.Standard, traceFull: false, chartOutput: false)));

                    //} else if (exportAs == ExportAs.PDMP_GraphViz) {
                    //    KGui.gui.GuiOutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: true).GraphViz(execution.style));
                    //} else if (exportAs == ExportAs.PDMP_Parallel) {
                    //    KGui.gui.GuiOutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: false).Format(execution.style));
                    //} else if (exportAs == ExportAs.PDMPGraph_Parallel) {
                    //    if (execution.graphCache.ContainsKey("PDMPGraphParallel")) { }
                    //    else execution.graphCache["PDMPGraphParallel"] = Export.PDMPGraph(execution.netlist, execution.style, sequential: false);
                    //    Gui.gui.ProcessGraph("PDMPGraphParallel"); 
                    //} else if (exportAs == ExportAs.PDMP_Parallel_GraphViz) {
                    //    KGui.gui.GuiOutputAppendText(Export.PDMP(execution.netlist, execution.style, sequential: false).GraphViz(execution.style));
                }
                else { };

                lock (exporterMutex) { exporterMutex[exportAs] = false; }
            } catch (Error ex) { KGui.gui.GuiInputSetErrorSelection(-1, -1, 0, "Error", ex.Message); }
        }

    }

}
