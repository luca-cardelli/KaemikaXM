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
            else if (unit == "mL" || unit == "ml") { return volume * 1e-3; }
            else if (unit == "uL" || unit == "μL" || unit == "ul" || unit == "μl") { return volume * 1e-6; }
            else if (unit == "nL" || unit == "nl") { return volume * 1e-9; }
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

        public static SampleValue Mix(Symbol symbol, SampleValue mixFst, SampleValue mixSnd, Netlist netlist, Style style) {
            mixFst.Consume(null, 0, null, netlist, style);
            mixSnd.Consume(null, 0, null, netlist, style);
            double fstVolume = mixFst.Volume();
            double sndVolume = mixSnd.Volume();
            NumberValue volume = new NumberValue(fstVolume + sndVolume);
            NumberValue temperature = new NumberValue((fstVolume * mixFst.Temperature() + sndVolume * mixSnd.Temperature()) / (fstVolume + sndVolume));
            SampleValue result = new SampleValue(symbol, volume, temperature, produced: true);
            result.AddSpecies(mixFst, volume.value, fstVolume);
            result.AddSpecies(mixSnd, volume.value, sndVolume);
            return result;
        }

        public static (SampleValue, SampleValue) Split(Symbol symbol1, Symbol symbol2, SampleValue sample, double proportion, Netlist netlist, Style style) {
            sample.Consume(null, 0, null, netlist, style);
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

        public static SampleValue Transfer(Symbol symbol, double volume, double temperature, SampleValue inSample, Netlist netlist, Style style) {
            inSample.Consume(null, 0, null, netlist, style);
            SampleValue result = new SampleValue(symbol, new NumberValue(volume), new NumberValue(temperature), produced: true);
            result.AddSpecies(inSample, volume, inSample.Volume());
            return result;
        }

        public static void Dispose(SampleValue sample, Netlist netlist, Style style) {
            sample.Consume(null, 0, null, netlist, style);
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

        public static SampleValue Equilibrate(Symbol outSymbol, SampleValue inSample, Noise noise, double fortime, Netlist netlist, Style style) {
            double initialTime = 0.0;
            double finalTime = fortime;

            Gui.gui.ChartClear((outSymbol.Raw() == "vessel") ? "" : "Sample " + inSample.FormatSymbol(style));

            List<SpeciesValue> species = inSample.Species(out double[] speciesState);
            State initialState = new State(species.Count, noise != Noise.None).InitMeans(speciesState);
            List<ReactionValue> reactions = inSample.RelevantReactions(netlist, style);
            CRN crn = new CRN(inSample, reactions, precomputeLNA: (noise != Noise.None) && Gui.gui.PrecomputeLNA());
            List<ReportEntry> reports = netlist.Reports(species);

            SampleValue outSample = new SampleValue(outSymbol, new NumberValue(inSample.Volume()), new NumberValue(inSample.Temperature()), produced: true);

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
                Integrate(Solver, initialState, initialTime, finalTime, Flux, inSample, reports, noise, series, seriesLNA, nonTrivialSolution, style);

            if (lastState == null) lastState = initialState;
            for (int i = 0; i < species.Count; i++) {
                double molarity = lastState.Mean(i);
                if (molarity < 0) molarity = 0; // the ODE solver screwed up
                outSample.SetMolarity(species[i], new NumberValue(molarity), style);
            }

            inSample.Consume(reactions, lastTime, lastState, netlist, style);

            Exec.lastReport = "======= Last report: time=" + lastTime.ToString() + ", " + lastState.FormatReports(reports, inSample, Flux, lastTime, noise, series, seriesLNA, style);
            Exec.lastState = "======= Last state: total points=" + pointsCounter + ", drawn points=" + renderedCounter + ", time=" + lastTime.ToString() + ", " + lastState.FormatSpecies(species, style);
            return outSample;
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
            Integrate(Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver,
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
                                double mean = reports[i].flow.ObserveMean(sample, solPoint.T, state, Flux, style);
                                Gui.gui.ChartAddPoint(series[i], solPoint.T, mean, 0.0, Noise.None);
                            }
                            // generate LNA-dependent series
                            if (noise != Noise.None && reports[i].flow.HasStochasticVariance() && !reports[i].flow.HasNullVariance()) {
                                double mean = reports[i].flow.ObserveMean(sample, solPoint.T, state, Flux, style);
                                double variance = reports[i].flow.ObserveVariance(sample, solPoint.T, state, style);
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

        // BFGF Minimizer
        public static Value Argmin(Value function, Value initial, Netlist netlist, Style style) {
            if (!(initial is NumberValue)) throw new Error("argmin: expecting a number for second argument");
            Vector<double> initialGuess = CreateVector.Dense(new double[1] { (initial as NumberValue).value });
            if (!(function is FunctionValue)) throw new Error("argmin: expecting a function as first argument");
            FunctionValue closure = function as FunctionValue;
            if (closure.parameters.ids.Count != 1) throw new Error("argmin: initial values and function parameters have different lengths");

            IObjectiveFunction objectiveFunction = ObjectiveFunction.Gradient(
                (Vector<double> parameters) => {
                    List<Value> arguments = new List<Value>(); arguments.Add(new NumberValue(parameters[0]));
                    bool autoContinue = netlist.autoContinue; netlist.autoContinue = true;
                    Value result = closure.Apply(arguments, netlist, style);
                    netlist.autoContinue = autoContinue;
                    if (!(result is ListValue)) throw new Error("argmin: objective function should return a list or two numbers");
                    List<Value> list = (result as ListValue).elements;
                    if (list.Count != 2 || !(list[0] is NumberValue) || !(list[1] is NumberValue)) throw new Error("argmin: objective function should return a list or two numbers");
                    double cost = (list[0] as NumberValue).value;
                    double gradient = (list[1] as NumberValue).value;
                    Gui.gui.OutputAppendText("argmin: parameter=" + style.FormatDouble(parameters[0]) + " => cost=" + style.FormatDouble(cost) + ", gradient=" + style.FormatDouble(gradient) + Environment.NewLine);
                    return new Tuple<double, Vector<double>>(cost, CreateVector.Dense(1, gradient));
                });

            try {
                BfgsMinimizer minimizer = new BfgsMinimizer(1e-3, 1e-3, 1e-3); // tolerances????
                MinimizationResult result = minimizer.FindMinimum(objectiveFunction, initialGuess);
                if (result.ReasonForExit == ExitCondition.Converged || result.ReasonForExit == ExitCondition.AbsoluteGradient || result.ReasonForExit == ExitCondition.RelativeGradient) {
                    Gui.gui.OutputAppendText("argmin: converged with parameter=" + result.MinimizingPoint[0] + " and reason '" + result.ReasonForExit + "'" + Environment.NewLine);
                    return new NumberValue(result.MinimizingPoint[0]);
                 } else throw new Error("reason '" + result.ReasonForExit.ToString() + "'");
            } catch (Exception e) { throw new Error("argmin ended: " + ((e.InnerException == null) ? e.Message : e.InnerException.Message)); } // somehow we need to recatch the inner exception coming from CostAndGradient
        }

        // try this for multiparameter optimization: https://numerics.mathdotnet.com/api/MathNet.Numerics.Optimization.TrustRegion/index.htm

        // Golden Section Minimizer
        public static Value Argmin(Value function, Value lowerBound, Value upperBound, Netlist netlist, Style style) {
            if (!(lowerBound is NumberValue) || !(upperBound is NumberValue)) throw new Error("argmin: expecting numbers for lower and upper bounds");
            double lower = (lowerBound as NumberValue).value;
            double upper = (upperBound as NumberValue).value;
            if (lower > upper) throw new Error("argmin: lower bound greater than upper bound");
            if (!(function is FunctionValue)) throw new Error("argmin: expecting a function as first argument");
            FunctionValue closure = function as FunctionValue;
            if (closure.parameters.ids.Count != 1) throw new Error("argmin: initial values and function parameters have different lengths");

            IScalarObjectiveFunction objectiveFunction = ObjectiveFunction.ScalarValue(
                (double parameter) => {
                    List<Value> arguments = new List<Value>(); arguments.Add(new NumberValue(parameter));
                    bool autoContinue = netlist.autoContinue; netlist.autoContinue = true;
                    Value result = closure.Apply(arguments, netlist, style);
                    netlist.autoContinue = autoContinue;
                    if (!(result is NumberValue)) throw new Error("Objective function must return a number, not: " + result.Format(style));
                    Gui.gui.OutputAppendText("argmin: parameter=" + Expressions.FormatValues(arguments, style) + " => cost=" + result.Format(style) + Environment.NewLine);
                    return (result as NumberValue).value;
                });

            try {
                ScalarMinimizationResult result = GoldenSectionMinimizer.Minimum(objectiveFunction, lower, upper);
                if (result.ReasonForExit == ExitCondition.Converged || result.ReasonForExit == ExitCondition.BoundTolerance) {
                    Gui.gui.OutputAppendText("argmin: converged with parameter=" + result.MinimizingPoint + " and reason '" + result.ReasonForExit + "'" + Environment.NewLine);
                    return new NumberValue(result.MinimizingPoint);
                 } else throw new Error("reason '" + result.ReasonForExit.ToString() + "'");
            } catch (Exception e) { throw new Error("argmin ended: " + ((e.InnerException == null) ? e.Message : e.InnerException.Message)); } // somehow we need to recatch the inner exception coming from CostAndGradient
        }

    }

}
