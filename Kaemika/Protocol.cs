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
            else return -1; // don't give error because we'll try another conversion
        }

        public static double NormalizeMole(double mole, string dimension) {
            if (dimension == "kmol") return mole * 1e3;
            else if (dimension == "mol") return mole;
            else if (dimension == "mmol") return mole * 1e-3;
            else if (dimension == "umol" || dimension == "μmol") return mole * 1e-6;
            else if (dimension == "nmol") return mole * 1e-9;
            else return -1; // don't give error because we'll try another conversion
        }

        public static double NormalizeMolarity(double molarity, string dimension) {
            if (dimension == "kM") return molarity * 1e3;
            else if (dimension == "M") return molarity;
            else if (dimension == "mM") return molarity * 1e-3;
            else if (dimension == "uM" || dimension == "μM") return molarity * 1e-6;
            else if (dimension == "nM") return molarity * 1e-9;
            else return -1; // don't give error because we'll try another conversion
        }

        public static (string[] series, string[] seriesLNA) GenerateSeries(List<ReportEntry> reports, Noise noise, Style style) {

            string[] seriesLNA = new string[reports.Count]; // can contain nulls if series are duplicates
            paletteNo = (reports.Count - 1) % palette.Length; // because we scan palette backwards
            for (int i = reports.Count - 1; i >= 0; i--) {    // add series backwards so that Red is in front
                // generate LNA-dependent series
                ReportEntry entry = reports[i];
                bool noisePlottable = (noise != Noise.None) && entry.flow.HasStochasticVariance() && !entry.flow.HasNullVariance();
                if (noisePlottable) {
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
                bool meanPlottable = (noise == Noise.None && entry.flow.HasDeterministicValue()) || ((noise != Noise.None) && entry.flow.HasStochasticMean());
                bool noisePlottable = (noise != Noise.None) && entry.flow.HasStochasticVariance() && !entry.flow.HasNullVariance();
                if (meanPlottable) {
                    string reportName = (entry.asLabel != null) ? entry.asLabel : entry.flow.TopFormat(style.RestyleAsNumberFormat("G4"));
                    string seriesName = reportName + (noisePlottable ? Gui.StringOfNoise(Noise.None) : ""); // do postfix mu if there is no sigma plot for it
                    //string seriesName = reportName + ((noise == Noise.None) ? "" : Gui.StringOfNoise(Noise.None)); // previous version
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

        public static SampleValue Sample(Symbol symbol, double volumeValue, double temperatureValue) {
            return new SampleValue(symbol,
                new StateMap(symbol, new List<SpeciesValue> { }, new State(0, false)),
                new NumberValue(volumeValue), new NumberValue(temperatureValue), produced: false);
        }

        public static void Amount(SampleValue sample, SpeciesValue species, NumberValue initial, string dimension, Style style) {
            sample.stateMap.AddDimensionedSpecies((SpeciesValue)species, ((NumberValue)initial).value, dimension, sample.Volume(), style);
        }

        public static SampleValue Mix(Symbol symbol, List<SampleValue> samples, Netlist netlist, Style style) {
            double sumVolume = 0.0;
            double sumTemperature = 0.0;
            bool needLna = false;
            foreach (SampleValue sample in samples) {
                sample.Consume(null, 0, null, netlist, style);
                sumVolume += sample.Volume();
                sumTemperature += sample.Volume() * sample.Temperature();
                needLna = needLna || sample.stateMap.state.lna;
            }
            NumberValue volume = new NumberValue(sumVolume);
            NumberValue temperature = new NumberValue(sumTemperature / sumVolume);
            SampleValue result = new SampleValue(symbol, new StateMap(symbol, new List<SpeciesValue> { }, new State(0, lna: needLna)), volume, temperature, produced: true);
            foreach (SampleValue sample in samples) {
                result.stateMap.Mix(sample.stateMap, volume.value, sample.Volume(), style); // mix adding the means and covariances, scaled by volume
            }
            return result;
        }

        public static List<SampleValue> Split(List<Symbol> symbols, SampleValue sample, List<NumberValue> proportions, Netlist netlist, Style style) {
            sample.Consume(null, 0, null, netlist, style);
            List<SampleValue> result = new List<SampleValue> { };
            for (int i = 0; i < symbols.Count; i ++) {
                NumberValue iVolume = new NumberValue(sample.Volume() * proportions[i].value);
                NumberValue iTemperature = new NumberValue(sample.Temperature());
                SampleValue iResult = new SampleValue(symbols[i], new StateMap(symbols[i], new List<SpeciesValue> { }, new State(0, lna: sample.stateMap.state.lna)), iVolume, iTemperature, produced: true);
                iResult.stateMap.Split(sample.stateMap, style);
                result.Add(iResult);
            }
            return result;
        }

        public static List<SampleValue> Regulate(List<Symbol> symbols, double temperature, List<SampleValue> inSamples, Netlist netlist, Style style) {
            List<SampleValue> outSamples = new List<SampleValue> { };
            for (int i = 0; i < symbols.Count; i++) {
                inSamples[i].Consume(null, 0, null, netlist, style);
                double volume = inSamples[i].Volume();
                SampleValue outSample = new SampleValue(symbols[i], new StateMap(symbols[i], new List<SpeciesValue> { }, new State(0, lna: inSamples[i].stateMap.state.lna)), new NumberValue(volume), new NumberValue(temperature), produced: true);
                outSample.stateMap.Mix(inSamples[i].stateMap, volume, volume, style);
                outSamples.Add(outSample);
            }
            return outSamples;
        }

        public static List<SampleValue> Concentrate(List<Symbol> symbols, double volume, List<SampleValue> inSamples, Netlist netlist, Style style) {
            List<SampleValue> outSamples = new List<SampleValue> { };
            for (int i = 0; i < symbols.Count; i++) {
                inSamples[i].Consume(null, 0, null, netlist, style);
                double temperature = inSamples[i].Temperature();
                SampleValue outSample = new SampleValue(symbols[i], new StateMap(symbols[i], new List<SpeciesValue> { }, new State(0, lna: inSamples[i].stateMap.state.lna)), new NumberValue(volume), new NumberValue(temperature), produced: true);
                outSample.stateMap.Mix(inSamples[i].stateMap, volume, inSamples[i].Volume(), style); // same as Mix, but in this case we can also have volume < inSamples[i].Volume()
                outSamples.Add(outSample);
            }
            return outSamples;
        }

        public static void Dispose(List<SampleValue> samples, Netlist netlist, Style style) {
            foreach (SampleValue sample in samples)
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
                //Gui.gui.OutputSetText(""); // clear last results in preparation for the next, only if not autoContinue
            }
        }

        public static List<SampleValue> EquilibrateList(List<Symbol> outSymbols, List<SampleValue> inSamples, Noise noise, double fortime, Netlist netlist, Style style) {
            List<SampleValue> result = new List<SampleValue> { };
            for (int i = 0; i < outSymbols.Count; i++)
                result.Add(Equilibrate(outSymbols[i], inSamples[i], noise, fortime, netlist, style));
            return result;
        }

        public static SampleValue Equilibrate(Symbol outSymbol, SampleValue inSample, Noise noise, double fortime, Netlist netlist, Style style) {
            inSample.CheckConsumed(style); // we will consume it later, but we need to check now
            double initialTime = 0.0;
            double finalTime = fortime;

            Gui.gui.ChartClear((outSymbol.Raw() == "vessel") ? "" : "Sample " + inSample.FormatSymbol(style));

            List<SpeciesValue> inSpecies = inSample.stateMap.species;
            State initialState = inSample.stateMap.state;
            if ((noise == Noise.None) && initialState.lna) initialState = new State(initialState.size, lna: false).InitMeans(initialState.MeanVector());
            if ((noise != Noise.None) && !initialState.lna) initialState = new State(initialState.size, lna: true).InitMeans(initialState.MeanVector());
            List<ReactionValue> reactions = inSample.RelevantReactions(netlist, style);
            CRN crn = new CRN(inSample, reactions, precomputeLNA: (noise != Noise.None) && Gui.gui.PrecomputeLNA());
            List<ReportEntry> reports = netlist.Reports(inSpecies);

            Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver;
            if (Gui.gui.Solver() == "GearBDF") Solver = Ode.GearBDF; else if (Gui.gui.Solver() == "RK547M") Solver = Ode.RK547M; else throw new Error("No solver");

            Func<double, Vector, Vector> Flux;
            if (noise != Noise.None) Flux = (t, x) => crn.LNAFlux(t, x, style);
            else Flux = (t, x) => crn.Flux(t, x, style);

            (string[] series, string[] seriesLNA) = GenerateSeries(reports, noise, style);

            bool nonTrivialSolution =
                (inSpecies.Count > 0)        // we don't want to run on the empty species list: Oslo crashes
                && (!crn.Trivial(style))     // we don't want to run trivial ODEs: some Oslo solvers hang on very small stepping
                && finalTime > 0;            // we don't want to run when fortime==0

            (double lastTime, State lastState, int pointsCounter, int renderedCounter) =
                Integrate(Solver, initialState, initialTime, finalTime, Flux, inSample, reports, noise, series, seriesLNA, nonTrivialSolution, style);

            if (lastState == null) lastState = initialState.Clone();
            lastState = lastState.Positive();
            List<SpeciesValue> outSpecies = new List<SpeciesValue> { }; foreach (SpeciesValue sp in inSpecies) outSpecies.Add(sp); // the species list may be destructively modified (added to) later in the new sample
            SampleValue outSample = new SampleValue(outSymbol, new StateMap(outSymbol, outSpecies, lastState), new NumberValue(inSample.Volume()), new NumberValue(inSample.Temperature()), produced: true);

            inSample.Consume(reactions, lastTime, lastState, netlist, style);

            Exec.lastReport = "======= Last report: time=" + lastTime.ToString() + ", " + lastState.FormatReports(reports, inSample, Flux, lastTime, noise, series, seriesLNA, style);
            Exec.lastState = "======= Last state: total points=" + pointsCounter + ", drawn points=" + renderedCounter + ", time=" + lastTime.ToString() + ", " + lastState.FormatSpecies(inSpecies, style);
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
            if (initialState.NaN()) {
                Gui.Log("Initial state contains NaN.");
                return (lastTime, lastState, pointsCounter, renderedCounter);
            }

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
                catch (ConstantEvaluation e) { // stop simulation but allow execution to proceed
                    Gui.Log("Simulation stopped and ignored: cannot evaluate constant '" + e.Message + "'");
                    return (lastTime, lastState, pointsCounter, renderedCounter); 
                } 
                catch (Error e) { throw new Error(e.Message); }
                catch (Exception e) { throw new Error("ODE Solver FAILED: " + e.Message); }
                pointsCounter++;

                // LOOP BODY of foreach (SolPoint solPoint in solution):
                if (!Exec.IsExecuting()) break;
                if (solPoint.T >= densityTick) { // avoid drawing too many points
                    State state = new State(sample.Count(), lna: noise != Noise.None).InitAll(solPoint.X);
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

            if (hasSolPoint) lastState = new State(sample.Count(), lna: noise != Noise.None).InitAll(solPoint.X);
            Gui.gui.ChartUpdate();

            return (lastTime, lastState, pointsCounter, renderedCounter);
        }

        // BFGF Minimizer
        public static Value Argmin(Value function, Value initial, Value tolerance, Netlist netlist, Style style) {
            if (!(initial is ListValue<Value>)) throw new Error("argmin: expecting a list for second argument");
            Vector<double> initialGuess = CreateVector.Dense((initial as ListValue<Value>).ToDoubleArray("argmin: expecting a list of numbers for second argument"));
            if (!(tolerance is NumberValue)) throw new Error("argmin: expecting a number for third argument");
            double toler = (tolerance as NumberValue).value;
            if (!(function is FunctionValue)) throw new Error("argmin: expecting a function as first argument");
            FunctionValue closure = function as FunctionValue;
            if (closure.parameters.parameters.Count != 1) throw new Error("argmin: initial values and function parameters have different lengths");

            IObjectiveFunction objectiveFunction = ObjectiveFunction.Gradient(
                (Vector<double> objParameters) => {
                const string badResult = "argmin: objective function should return a list with a number (cost) and a list of numbers (partial derivatives of cost)";
                List<Value> parameters = new List<Value>(); foreach (double parameter in objParameters) parameters.Add(new NumberValue(parameter));
                ListValue<Value> arg1 = new ListValue<Value>(parameters);
                List<Value> arguments = new List<Value>(); arguments.Add(arg1);
                bool autoContinue = netlist.autoContinue; netlist.autoContinue = true;
                Value result = closure.Apply(arguments, netlist, style);
                netlist.autoContinue = autoContinue;
                if (!(result is ListValue<Value>)) throw new Error(badResult);
                List<Value> results = (result as ListValue<Value>).elements;
                if (results.Count != 2 || !(results[0] is NumberValue) || !(results[1] is ListValue<Value>)) throw new Error(badResult);
                double cost = (results[0] as NumberValue).value;
                ListValue<Value> gradients = results[1] as ListValue<Value>;
                Gui.gui.OutputAppendText("argmin: parameters=" + arg1.Format(style) + " => cost=" + style.FormatDouble(cost) + ", gradients=" + results[1].Format(style) + Environment.NewLine);
                return new Tuple<double, Vector<double>>(cost, CreateVector.Dense(gradients.ToDoubleArray(badResult)));
            });

            try {
                BfgsMinimizer minimizer = new BfgsMinimizer(toler, toler, toler);
                MinimizationResult result = minimizer.FindMinimum(objectiveFunction, initialGuess);
                if (result.ReasonForExit == ExitCondition.Converged || result.ReasonForExit == ExitCondition.AbsoluteGradient || result.ReasonForExit == ExitCondition.RelativeGradient) {
                    List<Value> elements = new List<Value>();
                    for (int i = 0; i < result.MinimizingPoint.Count; i++) elements.Add(new NumberValue(result.MinimizingPoint[i]));
                    ListValue<Value> list = new ListValue<Value>(elements);
                    Gui.gui.OutputAppendText("argmin: converged with parameters " + list.Format(style) + " and reason '" + result.ReasonForExit + "'" + Environment.NewLine);
                    return list;
                 } else throw new Error("reason '" + result.ReasonForExit.ToString() + "'");
            } catch (Exception e) { throw new Error("argmin ended: " + ((e.InnerException == null) ? e.Message : e.InnerException.Message)); } // somehow we need to recatch the inner exception coming from CostAndGradient
        }

        // try this for multiparameter optimization: https://numerics.mathdotnet.com/api/MathNet.Numerics.Optimization.TrustRegion/index.htm

        // Golden Section Minimizer
        public static Value Argmin(Value function, Value lowerBound, Value upperBound, Value tolerance, Netlist netlist, Style style) {
            if (!(lowerBound is NumberValue) || !(upperBound is NumberValue)) throw new Error("argmin: expecting numbers for lower and upper bounds");
            double lower = (lowerBound as NumberValue).value;
            double upper = (upperBound as NumberValue).value;
            if (lower > upper) throw new Error("argmin: lower bound greater than upper bound");
            if (!(function is FunctionValue)) throw new Error("argmin: expecting a function as first argument");
            FunctionValue closure = function as FunctionValue;
            if (closure.parameters.parameters.Count != 1) throw new Error("argmin: initial values and function parameters have different lengths");

            IScalarObjectiveFunction objectiveFunction = ObjectiveFunction.ScalarValue(
                (double parameter) => {
                    List<Value> arguments = new List<Value>(); arguments.Add(new NumberValue(parameter));
                    bool autoContinue = netlist.autoContinue; netlist.autoContinue = true;
                    Value result = closure.Apply(arguments, netlist, style);
                    netlist.autoContinue = autoContinue;
                    if (!(result is NumberValue)) throw new Error("Objective function must return a number, not: " + result.Format(style));
                    Gui.gui.OutputAppendText("argmin: parameter=" + Style.FormatSequence(arguments, ", ", x => x.Format(style)) + " => cost=" + result.Format(style) + Environment.NewLine);
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
