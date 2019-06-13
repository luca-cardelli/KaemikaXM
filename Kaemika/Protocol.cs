using System;
using System.Collections.Generic;
using Microsoft.Research.Oslo;
using System.Drawing; //###
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;

namespace Kaemika
{
    public abstract class Protocol {
        // just a container for static methods

        public static bool continueExecution = true;

        private static Color[] palette = { Color.Red, Color.Green, Color.Blue, Color.Gold, Color.Cyan, Color.GreenYellow, Color.Violet, Color.Purple };
        private static int paletteNo = 0;

        public static double NormalizeVolume(double volume, string unit) {
            if (unit == "L") { return volume; } // ok
            else if (unit == "mL") { return volume * 1e-3; }
            else if (unit == "uL" || unit == "μL") { return volume * 1e-6; }
            else if (unit == "nL") { return volume * 1e-9; }
            else throw new Error("Invalid volume unit '" + unit + "'");
        }

        public static double NormalizeTemperature(double temperature, string unit) {
            if (unit == "K" || unit == "Kelvin") { return temperature; } // ok
            else if (unit == "C" || unit == "Celsius") { return temperature + 273.15; }
            else throw new Error("Invalid temperature unit '" + unit + "'");
        }

        public static double NormalizeWeight(double weight, string dimension) {
            if (dimension == "kg") return weight * 1e3;
            else if (dimension == "g") return weight;
            else if (dimension == "mg") return weight * 1e-3;
            else if (dimension == "ug" || dimension == "μg") return weight * 1e-6;
            else if (dimension == "ng") return weight * 1e-9;
            else return -1;
        }

        public static double NormalizeMole(double mole, string dimension) {
            if (dimension == "kmol") return mole * 1e3;
            else if (dimension == "mol") return mole;
            else if (dimension == "mmol") return mole * 1e-3;
            else if (dimension == "umol" || dimension == "μmol") return mole * 1e-6;
            else if (dimension == "nmol") return mole * 1e-9;
            else return -1;
        }

        public static double NormalizeMolarity(double molarity, string dimension) {
            if (dimension == "kM") return molarity * 1e3;
            else if (dimension == "M") return molarity;
            else if (dimension == "mM") return molarity * 1e-3;
            else if (dimension == "uM" || dimension == "μM") return molarity * 1e-6;
            else if (dimension == "nM") return molarity * 1e-9;
            else return -1;
        }

        public static SampleValue Mix(Symbol symbol, SampleValue mixFst, SampleValue mixSnd, Style style) {
            mixFst.Consume(new List<ReactionValue> (), style);
            mixSnd.Consume(new List<ReactionValue> (), style);
            double fstVolume = mixFst.Volume();
            double sndVolume = mixSnd.Volume();
            NumberValue volume = new NumberValue(fstVolume + sndVolume);
            NumberValue temperature = new NumberValue((fstVolume * mixFst.Temperature() + sndVolume * mixSnd.Temperature()) / (fstVolume + sndVolume));
            SampleValue result = new SampleValue(symbol, volume, temperature, produced: true);
            result.AddSpecies(mixFst, volume.value, fstVolume);
            result.AddSpecies(mixSnd, volume.value, sndVolume);
            return result;
        }

        public static (SampleValue, SampleValue) Split(Symbol symbol1, Symbol symbol2, SampleValue sample, double proportion, Style style) {
            sample.Consume(new List<ReactionValue>(), style);
            double sampleVolume = sample.Volume();

            NumberValue volume1 = new NumberValue(sampleVolume * proportion);
            NumberValue temperature1 = new NumberValue(sample.Temperature());
            SampleValue result1 = new SampleValue(symbol1, volume1, temperature1, produced: true);
            result1.AddSpecies(sample, sampleVolume, sampleVolume); // add species from other sample without changing their concentations

            NumberValue volume2 = new NumberValue(sampleVolume * (1 - proportion));
            NumberValue temperature2 = new NumberValue(sample.Temperature());
            SampleValue result2 = new SampleValue(symbol2, volume2, temperature2, produced: true);
            result2.AddSpecies(sample, sampleVolume, sampleVolume); // add species from other sample without changing their concentations

            return (result1, result2);
        }

        public static SampleValue Transfer(Symbol symbol, double volume, double temperature, SampleValue inSample, Style style) {
            inSample.Consume(new List<ReactionValue>(), style);
            SampleValue result = new SampleValue(symbol, new NumberValue(volume), new NumberValue(temperature), produced: true);
            result.AddSpecies(inSample, volume, inSample.Volume());
            return result;
        }

        public static void Dispose(SampleValue sample, Style style) {
            sample.Consume(new List<ReactionValue>(), style);
        }

        public static void PauseEquilibrate(Netlist netlist, Style style) {
            if (!netlist.autoContinue) {
                while ((!continueExecution) && Exec.IsExecuting()) {
                    // if (!Gui.gui.ContinueEnabled()) Gui.gui.OutputAppendText(netlist.Format(style));
                    Gui.gui.ContinueEnable(true);
                    Thread.Sleep(100);
                }
                Gui.gui.ContinueEnable(false); continueExecution = false;
                Gui.gui.OutputSetText(""); // clear last results in preparation for the next, only if not autoContinue
            }
        }

        public static (string[] series, string[] seriesLNA) GenerateSeries(List<ReportEntry> reports, Noise noise, Style style) {

            string[] seriesLNA = new string[reports.Count]; // can contain nulls if series are duplicates
            paletteNo = (reports.Count - 1) % palette.Length; // because we scan palette backwards
            for (int i = reports.Count - 1; i >= 0; i--) {    // add series backwards so that Red is in front
                // generate LNA-dependent series
                ReportEntry entry = reports[i];
                if ((noise != Noise.None) && entry.flow.HasStochasticVariance() && !entry.flow.HasNullVariance()) {
                    string reportName = (entry.asLabel != null) ? entry.asLabel : entry.flow.TopFormat(style.RestyleAsNumberFormat("G4"));
                    string seriesName = reportName + Gui.StringOfNoise(noise);
                    seriesLNA[i] = Gui.gui.ChartAddSeries(seriesName, palette[paletteNo % palette.Length], noise); // could be null
                }
                paletteNo--; if (paletteNo < 0) paletteNo += palette.Length; // decrement out here to keep colors coordinated
            }

            string[] series = new string[reports.Count]; // can contain nulls if series are duplicates
            paletteNo = (reports.Count - 1) % palette.Length; // because we scan palette backwards
            for (int i = reports.Count - 1; i >= 0; i--) {    // add series backwards so that Red is in front
                // generate deterministic series
                ReportEntry entry = reports[i];
                if ((noise == Noise.None && entry.flow.HasDeterministicValue()) ||
                    ((noise != Noise.None) && entry.flow.HasStochasticMean())) {
                    string reportName = (entry.asLabel != null) ? entry.asLabel : entry.flow.TopFormat(style.RestyleAsNumberFormat("G4"));
                    string seriesName = reportName + ((noise == Noise.None) ? "" : Gui.StringOfNoise(Noise.None));
                    series[i] = Gui.gui.ChartAddSeries(seriesName, palette[paletteNo % palette.Length], Noise.None); // could be null
                }
                paletteNo--; if (paletteNo < 0) paletteNo += palette.Length; // decrement out here to keep colors coordinated
            }

            for (int i = 0; i < reports.Count; i++) {
                if (series[i] != null) { // if a series was actually generated from this report
                    ReportEntry entry = reports[i];
                    string name = (entry.asLabel != null) ? entry.asLabel : entry.flow.TopFormat(style.RestyleAsNumberFormat("G4"));
                    Gui.gui.ChartListboxAddSeries(name);
                }
            }

            return (series, seriesLNA);
        }

        public static NumberValue Equilibrate(SampleValue resultSample, SampleValue sample, Noise noise, double fortime, Flow mintimeFlow, Flow mintimeFlowDiff, Netlist netlist, Style style) {
            double initialTime = 0.0;
            double finalTime = fortime;

            Gui.gui.ChartClear((resultSample.symbol.Raw() == "vessel") ? "" : "Sample " + resultSample.symbol.Format(style));

            List<SpeciesValue> species = sample.Species(out double[] speciesState);
            State initialState = new State(species.Count, noise != Noise.None).InitMeans(speciesState);
            List<ReactionValue> reactions = sample.RelevantReactions(netlist, style);
            CRN crn = new CRN(sample, reactions, precomputeLNA: (noise != Noise.None) && Gui.gui.PrecomputeLNA());
            List<ReportEntry> reports = netlist.Reports(species);
            sample.Consume(reactions, style);

            Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver;
            if (Gui.gui.Solver() == "GearBDF") Solver = Ode.GearBDF; else if (Gui.gui.Solver() == "RK547M") Solver = Ode.RK547M; else throw new Error("No solver");

            Func<double, Vector, Vector> Flux;
            if (noise != Noise.None) Flux = (t, x) => crn.LNAFlux(t, x, style);
            else Flux = (t, x) => crn.Flux(t, x, style);

            (string[] series, string[] seriesLNA) = GenerateSeries(reports, noise, style);

            bool nonTrivialSolution =
                (species.Count > 0)        // we don't want to run on the empty species list: Oslo crashes
                && (!crn.Trivial(style))   // we don't want to run trivial ODEs: some Oslo solvers hang on very small stepping
                && finalTime > 0;            // we don't want to run when fortime==0

            (double lastTime, State lastState, int pointsCounter, int renderedCounter) =
                Integrate(mintimeFlow, mintimeFlowDiff, Solver, initialState, initialTime, finalTime, Flux, sample, reports, noise, series, seriesLNA, nonTrivialSolution, style);

            if (lastState == null) lastState = initialState;
            for (int i = 0; i < species.Count; i++) {
                double molarity = lastState.Mean(i);
                if (molarity < 0) molarity = 0; // the ODE solver screwed up
                resultSample.SetMolarity(species[i], new NumberValue(molarity), style);
            }

            Exec.lastReport = "======= Last report: time=" + lastTime.ToString() + ", " + lastState.FormatReports(reports, sample, Flux, lastTime, noise, series, seriesLNA, style);
            Exec.lastState = "======= Last state: total points=" + pointsCounter + ", drawn points=" + renderedCounter + ", time=" + lastTime.ToString() + ", " + lastState.FormatSpecies(species, style);
            return new NumberValue(lastTime);
        }

        private static IEnumerable<SolPoint> SolutionGererator (
                Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver,
                State initialState,
                double initialTime, double finalTime,
                Func<double, Vector, Vector> Flux,
                bool nonTrivialSolution) {
            IEnumerable<SolPoint> solution;
            if (nonTrivialSolution) {
                try {
                    IEnumerable<SolPoint> solver = Solver(initialTime, finalTime, initialState.ToArray(), Flux);
                    solution = OdeHelpers.SolveTo(solver, finalTime);
                }
                catch (Error e) { throw new Error(e.Message); }
                catch (Exception e) { throw new Error("ODE Solver FAILED: " + e.Message); }
            } else { // build a dummy point series, in case we want to report and plot just some numerical expressions
                List<SolPoint> list = new List<SolPoint> { }; // SolPoint constructor was changed to public from internal
                if (finalTime <= initialTime) list.Add(new SolPoint(initialTime, initialState.ToArray()));
                else for (double t = initialTime; t <= finalTime; t += ((finalTime - initialTime) / 1000.0)) list.Add(new SolPoint(t, initialState.ToArray()));
                solution = list;
            }
            return solution;
        }

        private static (double lastTime, State lastState, int pointsCounter, int renderedCounter)
            Integrate(Flow mintimeFlow, Flow mintimeFlowDiff, Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver,
                State initialState, double initialTime, double finalTime, Func<double, Vector, Vector> Flux,
                SampleValue sample, List<ReportEntry> reports, Noise noise, string[] series, string[] seriesLNA, bool nonTrivialSolution, Style style) {
        
            if (mintimeFlow == null) {
                (double lastTime, State lastState, int pointsCounter, int renderedCounter) =
                     NormalIntegrate( Solver, initialState, initialTime, finalTime, Flux, sample, reports, noise, series, seriesLNA, nonTrivialSolution, style);
                return (lastTime, lastState, pointsCounter, renderedCounter);
            } else { 
                Gui.gui.OutputAppendText("Cost = " + mintimeFlow.Format(style) + Environment.NewLine + "∂Cost = " + mintimeFlowDiff.Format(style) + Environment.NewLine);
                (double lastTime, State lastState, int pointsCounter, int renderedCounter) =
                     OptimalIntegrate(mintimeFlow, mintimeFlowDiff, Solver, initialState, initialTime, finalTime, Flux, sample, reports, noise, series, seriesLNA, nonTrivialSolution, style);
                return (lastTime, lastState, pointsCounter, renderedCounter);
            }
        }

        private static (double lastTime, State lastState, int pointsCounter, int renderedCounter)
            NormalIntegrate(Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver,
                State initialState, double initialTime, double finalTime, Func<double, Vector, Vector> Flux,
                SampleValue sample, List<ReportEntry> reports, Noise noise, string[] series, string[] seriesLNA, bool nonTrivialSolution, Style style) {
            double redrawTick = initialTime; double redrawStep = (finalTime - initialTime) / 50;
            double densityTick = initialTime; double densityStep = (finalTime - initialTime) / 1000;
            int pointsCounter = 0;
            int renderedCounter = 0;
            double lastTime = finalTime;
            State lastState = null;

            Gui.gui.ChartClearData();
            Gui.gui.LegendUpdate();

            IEnumerable<SolPoint> solution = SolutionGererator(Solver, initialState, initialTime, finalTime, Flux, nonTrivialSolution);

            // BEGIN foreach (SolPoint solPoint in solution)  -- done by hand to catch exceptions in MoveNext()
            SolPoint solPoint = new SolPoint(0, new Vector());
            bool hasSolPoint = false;
            var enumerator = solution.GetEnumerator();
            do {
                try {
                    if (!enumerator.MoveNext()) break;
                    solPoint = enumerator.Current;       // get next step of integration from solver
                    hasSolPoint = true;
                }
                catch (Error e) { throw new Error(e.Message); }
                catch (Exception e) { throw new Error("ODE Solver FAILED: " + e.Message); }
                pointsCounter++;

                // LOOP BODY of foreach (SolPoint solPoint in solution):
                if (!Exec.IsExecuting()) break;
                if (solPoint.T >= densityTick) { // avoid drawing too many points
                    State state = new State(sample.species.Count, noise != Noise.None).InitAll(solPoint.X);
                    for (int i = 0; i < reports.Count; i++) {
                        if (series[i] != null) { // if a series was actually generated from this report
                            // generate deterministic series
                            if ((noise == Noise.None && reports[i].flow.HasDeterministicValue()) ||
                                (noise != Noise.None && reports[i].flow.HasStochasticMean())) {
                                double mean = reports[i].flow.ReportMean(sample, solPoint.T, state, Flux, style);
                                Gui.gui.ChartAddPoint(series[i], solPoint.T, mean, 0.0, Noise.None);
                            }
                            // generate LNA-dependent series
                            if (noise != Noise.None && reports[i].flow.HasStochasticVariance() && !reports[i].flow.HasNullVariance()) {
                                double mean = reports[i].flow.ReportMean(sample, solPoint.T, state, Flux, style);
                                double variance = reports[i].flow.ReportVariance(sample, solPoint.T, state, style);
                                Gui.gui.ChartAddPoint(seriesLNA[i], solPoint.T, mean, variance, noise);
                            }
                        }
                    }
                    renderedCounter++;
                    densityTick += densityStep;
                }
                if (solPoint.T >= redrawTick) { // avoid redrawing the plot too often
                    Gui.gui.ChartUpdate();
                    redrawTick += redrawStep;
                }
                lastTime = solPoint.T;

                // END foreach (SolPoint solPoint in solution)
            } while (true);

            if (hasSolPoint) lastState = new State(sample.species.Count, noise != Noise.None).InitAll(solPoint.X);
            Gui.gui.ChartUpdate();

            return (lastTime, lastState, pointsCounter, renderedCounter);
        }

        // https://numerics.mathdotnet.com/api/MathNet.Numerics.Optimization/ObjectiveFunction.htm

        private static (double endTime, State endState, int pointsCounter, int renderedCounter)
            OptimalIntegrate( Flow minimize, Flow mintimeDiff, Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver,
                State initialState, double initialTime, double finalTimeGuess, Func<double, Vector, Vector> Flux,
                SampleValue sample, List<ReportEntry> reports, Noise noise, string[] series, string[] seriesLNA, bool nonTrivialSolution, Style style) {

            double lastEndTime = finalTimeGuess; State lastEndState = null; int lastPointsCounter = 0; int lastRenderereCounter = 0;

            // --- Cost function minimizing simulation endtime, with time-derivative gradient function
            (double cost, double gradient) CostAndGradient(double endTimeGuess) {

                if (Double.IsNaN(endTimeGuess)) throw new Error("time = NaN");
                if (endTimeGuess <= 0) return (cost: double.MaxValue, gradient: -1.0);
                (double endTime, State endState, int pointsCounter, int renderedCounter) =
                    NormalIntegrate(Solver, initialState, initialTime, endTimeGuess, Flux, sample, reports, noise, series, seriesLNA, nonTrivialSolution, style);
                lastEndTime = endTime; lastEndState = endState; lastPointsCounter = pointsCounter; lastRenderereCounter = renderedCounter;

                double cost = minimize.ReportMean(sample, endTime, endState, null, style); // compute cost on basis of endTimeGuess or true endTime?
                double gradient = mintimeDiff.ReportMean(sample, endTime, endState, Flux, style);
                double gradientNum = minimize.ReportDiff(sample, endTime, endState, Flux, style);

                Gui.gui.OutputAppendText("BFGS: endTimeGuess = " + endTimeGuess.ToString("G4") + ", cost = " + cost.ToString("G4") 
                    + ", symbolic gradient = " + gradient.ToString("G4") + ", numeric gradient = " + gradientNum.ToString("G4") + Environment.NewLine);
                return (cost, gradient);
            }

            // Set objectiveFunction = ObjectiveFunction.Gradient(CostAndGradient)
            // but need to adapt the type of CostAndGradient to the type that ObjectiveFunction.Gradient wants:
            IObjectiveFunction objectiveFunction = ObjectiveFunction.Gradient(                                                                    
                (Vector<double> parameters) => {
                    (double cost, double gradient) = CostAndGradient(parameters[0]);
                    return new Tuple<double, Vector<double>>(cost, CreateVector.Dense(1, gradient));
                });
            // Set initialGuess = endTimeGuess
            Vector<double> initialGuess = CreateVector.Dense(1, finalTimeGuess);

            // --- Cost function minimizing a vector of simulation parameters, with no gradient function
            double Cost(Vector<double> parameters) { //presumably same size as initialGuess_Value
                // assign parameters values so that Distribution.distrEval will find them 
                // for (int i = 0; i < parameters.Count; i++) optimalIntegrateMinimizationParameters[i].drawn = parameters[i];

                (double endTime, State endState, int pointsCounter, int renderedCounter) =
                    NormalIntegrate(Solver, initialState, initialTime, finalTimeGuess, Flux, sample, reports, noise, series, seriesLNA, nonTrivialSolution, style);
                lastEndTime = endTime; lastEndState = endState; lastPointsCounter = pointsCounter; lastRenderereCounter = renderedCounter;

                double cost = minimize.ReportMean(sample, endTime, endState, null, style); 
                Gui.gui.OutputAppendText("BFGS: cost = " + cost.ToString("G4") + Environment.NewLine);
                return cost;
            }
            IObjectiveFunction objectiveFunction_Value = ObjectiveFunction.Value(
                (Vector<double> parameters) => { return Cost(parameters); } );
            // optimalIntegrateMinimizationParameters = minimizationParameters; //### this won't work because we do not get the list of random distribution parameters until after the first simulation
            // Vector<double> initialGuess_Value = a Vector built from random List<InitialValue> minimizationParameters;

            // --- FindMinimum(objectiveFunction, initialGuess)
            try {
                BfgsMinimizer minimizer = new BfgsMinimizer(1e-2, 1e-2, 1e-2); //### tolerances????
                MinimizationResult result = minimizer.FindMinimum(objectiveFunction, initialGuess);
                if (result.ReasonForExit == ExitCondition.Converged || result.ReasonForExit == ExitCondition.AbsoluteGradient)
                    return (endTime: result.MinimizingPoint[0], endState: lastEndState, pointsCounter: lastPointsCounter, renderedCounter: lastRenderereCounter);
                else throw new Error("reason '" + result.ReasonForExit.ToString() + "' at time " + result.MinimizingPoint[0].ToString());
            } catch (Exception e) {
                throw new Error("Minimization ended: " + ((e.InnerException == null) ? e.Message : e.InnerException.Message)); } // somehow we need to recatch the inner exception coming from CostAndGradient
        }

        //### HMMM BFGS requires a gradient
        //public static Value Argmin(Value function, Value initial, Style style) {
        //    if (!(initial is ListValue)) throw new Error("");
        //    Value[] elements = (initial as ListValue).elements;
        //    double[] v = new double[elements.Length];
        //    for (int i = 0; i < elements.Length; i++) { if (elements[i] is NumberValue) v[i] = (elements[i] as NumberValue).value; else throw new Error(""); }
        //    Vector<double> initialGuess = CreateVector.Dense(v);
        //    if (!(function is FunctionValue)) throw new Error("");
        //    FunctionValue closure = function as FunctionValue;
        //    if (closure.parameters.parameters.Count != elements.Length) throw new Error("");

        //    IObjectiveFunction objectiveFunction = ObjectiveFunction.Value(
        //        (Vector<double> parameters) => {
        //            List<Value> arguments = new List<Value>();
        //            for (int i = 0; i < parameters.Count; i++) arguments.Add(new NumberValue(parameters[i]));
        //            Value result = closure.ApplyFlow(arguments, style);
        //            if (!(result is NumberValue)) throw new Error("");
        //            Gui.gui.OutputAppendText("argmin: " + Expressions.FormatValues(arguments, style) + " => " + result.Format(style));
        //            return (result as NumberValue).value;
        //        });

        //    try {
        //        BfgsMinimizer minimizer = new BfgsMinimizer(1e-2, 1e-2, 1e-2); //### tolerances????
        //        MinimizationResult result = minimizer.FindMinimum(objectiveFunction, initialGuess);
        //        if (result.ReasonForExit == ExitCondition.Converged || result.ReasonForExit == ExitCondition.AbsoluteGradient) {
        //            Vector<double> minimizingPoint = result.MinimizingPoint;
        //            Value[] minim = new Value[minimizingPoint.Count];
        //            for (int i = 0; i < minimizingPoint.Count; i++) minim[i] = new NumberValue(minimizingPoint[i]);
        //            return new ListValue(minim);
        //         } else throw new Error("reason '" + result.ReasonForExit.ToString());
        //    } catch (Exception e) { throw new Error("Minimization ended: " + ((e.InnerException == null) ? e.Message : e.InnerException.Message)); } // somehow we need to recatch the inner exception coming from CostAndGradient
        //}

        // try this for multiparameter optimization: https://numerics.mathdotnet.com/api/MathNet.Numerics.Optimization.TrustRegion/index.htm

        public static Value Argmin(Value function, Value lowerBound, Value upperBound, Netlist netlist, Style style) {
            if (!(lowerBound is NumberValue) || !(upperBound is NumberValue)) throw new Error("");
            double lower = (lowerBound as NumberValue).value;
            double upper = (upperBound as NumberValue).value;
            if (lower > upper) throw new Error("");
            if (!(function is FunctionValue)) throw new Error("");
            FunctionValue closure = function as FunctionValue;
            if (closure.parameters.ids.Count != 1) throw new Error("");

            IScalarObjectiveFunction objectiveFunction = ObjectiveFunction.ScalarValue(
                (double parameter) => {
                    List<Value> arguments = new List<Value>();
                    arguments.Add(new NumberValue(parameter));
                    bool autoContinue = netlist.autoContinue; netlist.autoContinue = true;
                    Value result = closure.Apply(arguments, netlist, style);
                    netlist.autoContinue = autoContinue;
                    if (!(result is NumberValue)) throw new Error("Objective function must return a number, not: " + result.Format(style));
                    Gui.gui.OutputAppendText("argmin: " + Expressions.FormatValues(arguments, style) + " => " + result.Format(style) + Environment.NewLine);
                    return (result as NumberValue).value;
                });

            try {
                ScalarMinimizationResult result = GoldenSectionMinimizer.Minimum(objectiveFunction, lower, upper);
                if (result.ReasonForExit == ExitCondition.Converged || result.ReasonForExit == ExitCondition.BoundTolerance) {
                    double minimizingPoint = result.MinimizingPoint;
                    return new NumberValue(result.MinimizingPoint);
                 } else throw new Error("reason '" + result.ReasonForExit.ToString());
            } catch (Exception e) {
                throw new Error("Minimization ended: " + ((e.InnerException == null) ? e.Message : e.InnerException.Message)); } // somehow we need to recatch the inner exception coming from CostAndGradient
        }

    }

}
