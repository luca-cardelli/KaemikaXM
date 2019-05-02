using System;
using System.IO;
using System.Threading;
using System.Text;

namespace Kaemika
{
    public enum ExportAs : int { None, MSRC_LBS, MSRC_CRN, ODE, Protocol, GraphViz, PDMP, PDMP_Sequential }

    public class Exec { // what runs when we press the "Start" button

        private static int UID = 0;
        public static int NewUID() {
            UID = UID + 1;
            return UID;
        }


        public static string lastReport = ""; // the last report of the last simulation, in string form
        public static string lastState = ""; // the last state of the last simulation, in string form, including covariance matrix if LNA

        public static void Execute_Starter(bool forkWorker, bool doParse = false, bool doAST = false, bool doScope = false, bool doEval = false, ExportAs doExport = ExportAs.None) {
            if (Gui.gui.StopEnabled()) return; // we are already running an executor; this may or may not be interlocked in the Gui, so better be safe
            Gui.gui.StopEnable(true);
            ProtocolActuator.continueExecution = true;
            if (forkWorker) {
                Thread thread = new Thread(() => Execute_Worker(doParse, doAST, doScope, doEval, doExport));
                thread.SetApartmentState(ApartmentState.STA); // required to use the clipboard
                thread.Start();
            } else {
                Execute_Worker(doParse, doAST, doScope, doEval, doExport);
            }
        }

        public static void Execute_Worker(bool doParse, bool doAST, bool doScope, bool doEval, ExportAs doExport = ExportAs.None) {
            Gui.gui.SaveInput();
            Gui.gui.OutputSetText("");
            Gui.gui.ChartClear("");
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
                            string defaultVarchar = "•";  // "•" "≈" "▪"
                            Netlist netlist = new Netlist();
                            netlist.Emit(new SampleEntry(vessel));
                            Style style = new Style(varchar: Gui.gui.ScopeVariants() ? defaultVarchar : null, new SwapMap(),               
                                                    map: Gui.gui.RemapVariants() ? new AlphaMap() : null, numberFormat: "G4", dataFormat: "full",  // we want it full for samples, but maybe only headers for functions/networks?
                                                    exportTarget: ExportTarget.Standard, traceComputational: false);
                                Env ignoreEnv = statements.Eval(new NullEnv().BuiltIn(vessel), netlist, style);
                            if (doEval) {
                                    Gui.gui.TextOutput();
                               Gui.gui.OutputAppendComputation(netlist.Format(style), netlist.Format(style.RestyleAsTraceComputational(true)), Export.GraphViz(netlist));
//###MSAGL                               Gui.gui.DrawGraph(Export.MSAGL(netlist));
                            } else { // export and copy to clipboard
                               if (doExport == ExportAs.MSRC_LBS) Gui.gui.OutputAppendText(Export.MSRC_LBS(netlist, vessel, new Style(varchar: "_", new SwapMap(subsup:true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.LBS, traceComputational: false)));
                               else if (doExport == ExportAs.MSRC_CRN) Gui.gui.OutputAppendText(Export.MSRC_CRN(netlist, vessel, new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.CRN, traceComputational: false)));
                               else if (doExport == ExportAs.ODE) Gui.gui.OutputAppendText(Export.ODE(vessel, 
                                   new CRN(vessel, netlist.RelevantReactions(vessel, vessel.species, style)), 
                                   new Style(varchar: "_", new SwapMap(subsup: true), map: new AlphaMap(), numberFormat: null, dataFormat: "full", exportTarget: ExportTarget.Standard, traceComputational: false)));
                               else if (doExport == ExportAs.Protocol) Gui.gui.OutputAppendText(Export.Protocol(netlist, new Style(varchar: defaultVarchar, new SwapMap(), map: new AlphaMap(), numberFormat: "G4", dataFormat: "symbol", exportTarget: ExportTarget.Standard, traceComputational: false)));
                               else if (doExport == ExportAs.GraphViz) Gui.gui.OutputAppendText(Export.GraphViz(netlist));
                               else if (doExport == ExportAs.PDMP) Gui.gui.OutputAppendText(Export.PDMP(netlist, style, sequential:false));
                               else if (doExport == ExportAs.PDMP_Sequential) Gui.gui.OutputAppendText(Export.PDMP(netlist, style, sequential:true));
                                    else { }
                               try { Gui.gui.ClipboardSetText(Gui.gui.OutputGetText()); } catch (ArgumentException) { };
                            }
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

    }

}
