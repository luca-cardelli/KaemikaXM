using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Microsoft.Research.Oslo;

namespace Kaemika {

    public enum Type { NONE, Bool, Number, Omega, Random, String, List, Flow, Function, Network, Species, Sample, Value };

    public abstract class Types {
        public static string Format (Type type) {
            if (type == Type.Bool) return "bool";
            else if (type == Type.Number) return "number";
            else if (type == Type.Omega) return "omega";
            else if (type == Type.Random) return "random";
            else if (type == Type.String) return "string";
            else if (type == Type.List) return "list";
            else if (type == Type.Flow) return "flow";
            else if (type == Type.Function) return "function";
            else if (type == Type.Network) return "network";
            else if (type == Type.Species) return "species";
            else if (type == Type.Sample) return "sample";
            else if (type == Type.Value) return "value";
            else return "NONE";
        }
        public static Type Parse (string s) {
            if (s == "bool") return Type.Bool;
            else if (s == "number") return Type.Number;
            else if (s == "omega") return Type.Omega;
            else if (s == "random") return Type.Random;
            else if (s == "string") return Type.String;
            else if (s == "list") return Type.List;
            else if (s == "flow") return Type.Flow;
            else if (s == "function") return Type.Function;
            else if (s == "network") return Type.Network;
            else if (s == "species") return Type.Species;
            else if (s == "sample") return Type.Sample;
            else if (s == "value") return Type.Value;
            else return Type.NONE;
        }
        public static bool Matches(Type type, Value value) {
          return
            ((type == Type.Bool) && (value is BoolValue)) ||
            ((type == Type.Number) && (value is NumberValue)) ||
            ((type == Type.Omega) && (value is OmegaValue)) ||
            ((type == Type.Random) && (value is DistributionValue)) ||
            ((type == Type.String) && (value is StringValue)) ||
            ((type == Type.List) && (value is ListValue<Value> || value is ListValue<Flow>)) ||
            ((type == Type.Flow) && (value is Flow)) ||
            ((type == Type.Function) && (value is FunctionValue || value is FunctionOperatorValue)) ||
            ((type == Type.Network) && (value is NetworkValue || value is NetworkOperatorValue)) ||
            ((type == Type.Species) && (value is SpeciesValue)) ||
            ((type == Type.Sample) && (value is SampleValue)) ||
            (type == Type.Value)
            ;
        }
    }

    public abstract class Value {
        public const Value REJECT = null; // return REJECT=null from Expression evaluation to mean "sample is rejected" in random variable sampling (instead of an exception, trapping which is way too expensive)
        public Type type;
        public bool EqualValue(Value other, Style style, out bool hasFlows) { // gives the straight equality answer including for flows, but also says if it encountered flows. But it does not check inside lists since those cannot be turned into flows anyway.
            hasFlows = false;
            if (this is Flow thisFlow && other is Flow otherFlow) { hasFlows = true; return thisFlow.EqualFlow(otherFlow); }
            else if (this is BoolValue && other is BoolValue) return (this as BoolValue).value == (other as BoolValue).value;
            else if (this is NumberValue && other is NumberValue) return NumberValue.EqualDouble((this as NumberValue).value, (other as NumberValue).value);
            else if (this is StringValue && other is StringValue) return (this as StringValue).value == (other as StringValue).value;
            else if (this is SpeciesValue && other is SpeciesValue) return (this as SpeciesValue).symbol.SameSymbol((other as SpeciesValue).symbol);
            else if (this is ListValue<Value> && other is ListValue<Value>) return (this as ListValue<Value>).EqualList(other as ListValue<Value>, style);
            else if (this is ListValue<Flow> thisFlows && other is ListValue<Flow> otherFlows) { hasFlows = true; return thisFlows.EqualList(otherFlows, style); } 
            else if (this is Flow || other is Flow || this is SpeciesValue || other is SpeciesValue) { hasFlows = true; return false; } // consider species as flows, we do not want 'cond(a=3,b,c)' to give an error. Still, 'cond(a=a,b,c)' reduces to b, but that's ok
            else throw new Error("Different types of arguments for '=' or '<>'");
        }
        public abstract string Format(Style style);
        public string TopFormat(Style style) {
            string s = this.Format(style);
            if (s.Length > 0 && s.Substring(0, 1) == "(" && s.Substring(s.Length - 1, 1) == ")") s = s.Substring(1, s.Length - 2);
            return s;
        }
        public Flow ToFlow() {
            if (this is Flow) return (Flow)this;
            else if (this is BoolValue) return new BoolFlow(((BoolValue)this).value);
            else if (this is NumberValue) return new NumberFlow(((NumberValue)this).value);
            else if (this is SpeciesValue) return new SpeciesFlow(((SpeciesValue)this).symbol);
            else if (this is SampleValue) return new SampleFlow((SampleValue)this);
            else if (this is OperatorValue) { // handle the nullary operators from the built-in environment
                if (((OperatorValue)this).name == "time") return OpFlow.Op("time");
                else if (((OperatorValue)this).name == "kelvin") return OpFlow.Op("kelvin");
                else if (((OperatorValue)this).name == "celsius") return OpFlow.Op("celsius");
                else if (((OperatorValue)this).name == "volume") return OpFlow.Op("volume");
                else return null;
            }
            else return null;
        }
    }

    public class SampleValue : Value {
        public Symbol symbol;
        private NumberValue volume;                                 // L
        private NumberValue temperature;                            // Kelvin
        public StateMap stateMap;                                  // maps species to mol/L
        private bool produced; // produced by an operation as opposed as being created as a sample
        private bool consumed; // consumed by an operation, including dispose
        private List<ReactionValue> reactionsAsConsumed;
        private double timeAsConsumed;
        private State stateAsConsumed;
        private List<ReportEntry> reports; // reports to generate when this sample is simulated
        public SampleValue(Symbol symbol, StateMap stateMap, NumberValue volume, NumberValue temperature, bool produced) {
            this.type = Type.Sample;
            this.symbol = symbol;
            this.volume = volume;           // L
            this.temperature = temperature; // Kelvin
            this.stateMap = stateMap;
            this.produced = produced;
            this.consumed = false;
            this.reactionsAsConsumed = null;
            this.timeAsConsumed = 0.0;
            this.stateAsConsumed = null;
            this.reports = new List<ReportEntry>();
        }
        public int Count() {
            return stateMap.species.Count;
        }
        public void CheckConsumed(Style style) {
            if (this.consumed) throw new Error("Sample already used: '" + this.symbol.Format(style) + "'");
        }
        public void Consume(List<ReactionValue> reactionsAsConsumed, double timeAsConsumed, State stateAsConsumed, Netlist netlist, Style style) {
            CheckConsumed(style);
            this.consumed = true;
            this.timeAsConsumed = timeAsConsumed;
            this.stateAsConsumed = (stateAsConsumed == null) ? stateMap.state.Clone() : stateAsConsumed;
            this.reactionsAsConsumed = (reactionsAsConsumed == null) ? RelevantReactions(netlist, style) : reactionsAsConsumed;
        }
        public List<ReactionValue> ReactionsAsConsumed(Style style) {
            if (!consumed) throw new Error("Sample '" + symbol.Format(style) + "' should have been equilibrated and consumed");
            return reactionsAsConsumed;
        }
        public bool IsProduced() { return this.produced; }
        public bool IsConsumed() { return this.consumed; }

        public string FormatSymbol(Style style) {
            return symbol.Format(style);
        }
        public string FormatHeader(Style style) {
            return symbol.Format(style) + " {" + Gui.FormatUnit(this.Volume(), "", "L", style.numberFormat) + ", " + temperature.Format(style) + "K}";
        }
        public string FormatContent(Style style, bool breaks = false, bool padding = true, bool lna = true) {
            string pad = padding ? "   " : "";
            string s = "";
            foreach (SpeciesValue sp in this.stateMap.species)
                s += (breaks ? (Environment.NewLine + pad) : "") + sp.Format(style) + " = " + Gui.FormatUnit(stateMap.Mean(sp.symbol), " ", "M", style.numberFormat) + ", ";
            if (lna && this.stateMap.state.lna) { // this may not be assigned until after the simulation
                for (int i = 0; i < stateMap.state.size; i++) {
                    Symbol sp1 = stateMap.species[i].symbol;
                    double mean = stateMap.Mean(sp1);
                    for (int j = i; j < stateMap.state.size; j++) {
                        Symbol sp2 = stateMap.species[j].symbol;
                        double covar = stateMap.Covar(sp1, sp2);
                        if (covar != 0) {
                            s += (breaks ? (Environment.NewLine + "   ") : "");
                            if (i == j) s += "var(" + sp1.Format(style) + ") = " + style.FormatDouble(covar) + ", fano(" + sp1.Format(style) + ") = " + style.FormatDouble(covar / mean);
                            else s += "cov(" + sp1.Format(style) + "," + sp2.Format(style) + ") = " + style.FormatDouble(covar);
                        }
                    }
                }
            }
            return s;
        }
        public string FormatReactions(Style style, bool breaks = false) {
            if (reactionsAsConsumed == null) return "";
            string s = breaks ? (Environment.NewLine + "   consumed") : " (consumed) ";
            foreach (ReactionValue reaction in reactionsAsConsumed)
                s += (breaks ? (Environment.NewLine + "   ") : "") + reaction.Format(style);
            return s;

        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return symbol.Format(style);
            else if (style.dataFormat == "header") return "sample " + FormatHeader(style);
            else if (style.dataFormat == "full") return "sample " + FormatHeader(style) + " {" + FormatContent(style, true) + FormatReactions(style, true) + Environment.NewLine + "}";
            else return "unknown format: " + style.dataFormat;
        }

        public double TemperatureOp(Style style) {
            if (this.consumed) throw new Error("temperature(" + this.symbol.Format(style) + "): sample already disposed");
            return Temperature();
        }
        public double Temperature() {
            return this.temperature.value;
        }
        //public void ChangeTemperature(double newTemperature) {
        //    this.temperature = new NumberValue(newTemperature);
        //}

        public double VolumeOp(Style style) {
            if (this.consumed) throw new Error("volume(" + this.symbol.Format(style) + "): sample already disposed");
            return Volume();
        }
        public double Volume() {
            return volume.value;
        }
        //public void ChangeVolume(double newVolume) {  // evaporate or dilute
        //    double oldVolume = this.Volume();
        //    Dictionary<SpeciesValue, NumberValue> oldspeciesSet = this.speciesSet;
        //    this.volume = new NumberValue(newVolume);
        //    this.speciesSet = new Dictionary<SpeciesValue, NumberValue> ();
        //    double ratio = oldVolume / newVolume;
        //    foreach (KeyValuePair<SpeciesValue, NumberValue> keyPair in oldspeciesSet) {
        //        this.speciesSet.Add(keyPair.Key, new NumberValue(keyPair.Value.value * ratio));
        //    }
        //    RecomputeSpecies();
        //}

        public NumberValue Observe(Flow flow, Netlist netlist, Style style) {
            if (!flow.CoveredBy(this.stateMap.species, out Symbol notCovered)) throw new Error("observe : species '" + notCovered.Format(style) + "' in flow '" + flow.Format(style) + "' is not one of the species in sample '" + this.FormatSymbol(style) + "'");
            double observeTime;
            State observeState;
            List<ReactionValue> observeReactions;
            if (consumed) { // throw new Error("observe(" + this.symbol.Format(style) + ", " + flow.Format(style) + "): sample already disposed");
                observeTime = this.timeAsConsumed;
                observeState = this.stateAsConsumed;
                observeReactions = this.reactionsAsConsumed;
            } else { // throw new Error("observe(" + this.symbol.Format(style) + ", " + flow.Format(style) + "): sample not disposed");
                observeTime = 0;
                observeState = stateMap.state;
                observeReactions = RelevantReactions(netlist, style);
            }
            // maybe we should allow observing only samples that have been consumed; otherwise the RelevantReactions and Flux may be still incomplete
            CRN crn = new CRN(this, observeReactions);
            return new NumberValue(flow.ObserveMean(this, observeTime, observeState, ((double x, Vector st) => { return crn.Flux(x, st, style); }), style));
        }
        public List<ReactionValue> RelevantReactions(Netlist netlist, Style style) { 
            // return the list of reactions in the netlist that can fire in this sample
            // check that those reactions produce only species in this sample, or give error
            List<ReactionValue> reactionList = new List<ReactionValue> { };
            foreach (ReactionValue reaction in netlist.AllReactions()) {
                if (reaction.ReactantsCoveredBy(stateMap.species, out Symbol notCoveredReactant)) {
                    if (reaction.ProductsCoveredBy(stateMap.species, out Symbol notCoveredProduct)) {
                        reactionList.Add(reaction);
                    } // else ignore because it is not relevant
                    //} else throw new Error( // N.B.: this would give a spurious error on reactions like # -> a where reactants are always covered
                    //    "Reaction '" + reaction.Format(style) + "' produces species '" + notCoveredProduct.Format(style) + 
                    //    "' in sample '" + this.symbol.Format(style) + "', but that species is uninitialized in that sample");
                } // else ignore reaction because it cannot fire
            }
            return reactionList;
        }
        public void AddReport(ReportEntry report) {
            this.reports.Add(report);
        }
        public void AddReports(List<ReportEntry> reports) {
            this.reports.AddRange(reports);
        }
        public List<ReportEntry> RelevantReports(Style style) {
            List<SpeciesValue> species = this.stateMap.species;
            // return the list of reports in this sample that involve any of the species in the species list
            // but include only those reports that are fully covered by those species, just ignore the others
            List<ReportEntry> reportList = new List<ReportEntry> { };
            foreach (ReportEntry entry in this.reports) {
                if (entry.flow.CoveredBy(species, out Symbol notCovered)) reportList.Add(entry);
                else {
                    if (this.symbol.Raw() == "vessel")
                        throw new Error("species '" + notCovered.Format(style) + "' used in a report is not present in the default sample '" + this.symbol.Format(style) + ": have you forgotten an '.. in <sample>' in a report?");
                    else throw new Error("species '" + notCovered.Format(style) + "' used in report for sample '" + this.symbol.Format(style) + "' is not present in that sample");
                }
            }
            // if there are no reports, then report all the species
            if (reportList.Count == 0) { 
                foreach (SpeciesValue s in species) reportList.Add(new ReportEntry(null, new SpeciesFlow(s.symbol), null, this));
            }
            return reportList;
        }

    }

    public class SpeciesValue : Value {
        public Symbol symbol;
        private double molarMass; // molar mass (g/mol), or else concentration-based (mol/L) if <= 0
        public SpeciesValue(Symbol symbol, double molarMass) {
            this.type = Type.Species;
            this.symbol = symbol;
            this.molarMass = molarMass;
        }
        public bool SameSpecies(SpeciesValue otherSpecies) {
            return this.symbol.SameSymbol(otherSpecies.symbol);
        }
        public override string Format(Style style) {
            return symbol.Format(style)
                // + (HasMolarMass() ? "#" + style.FormatDouble(molarMass) : "")
                ;
        }
        public double MolarMass() {
            if (molarMass > 0) return molarMass;
            else throw new Error("MolarMass");
        }
        public bool HasMolarMass() {
            return molarMass > 0;
        }
    }

    public class BoolValue : Value {
        public bool value;
        public BoolValue(bool value) {
            this.type = Type.Bool;
            this.value = value;
        }
        public override string Format(Style style) {
            if (this.value) return "true"; else return "false";
        }
    }

    public class NumberValue : Value {
        public double value;
        public NumberValue(double value) {
            this.type = Type.Number;
            this.value = value;
        }
        public override string Format(Style style) {
            return style.FormatDouble(this.value);
        }
        public static bool EqualDouble(double n, double m) {
            if (double.IsNaN(n) && double.IsNaN(m)) return true; // allow "if n = NaN then ... " clearly violating IEEE 754
            if (double.IsPositiveInfinity(n) && double.IsPositiveInfinity(m)) return true;
            if (double.IsNegativeInfinity(n) && double.IsNegativeInfinity(m)) return true;
            return n == m;
        }
    }

    public class StringValue : Value {
        public string value;
        public StringValue(string value) {
            this.type = Type.String;
            this.value = value;
        }
        public override string Format(Style style) {
            return Parser.FormatString(this.value);
        }
    }

    public class ListValue<T> : Value where T : Value {
        public List<T> elements;
        public ListValue(List<T> elements) {
            this.type = Type.List;
            this.elements = elements;
        }
        public static ListValue<T> empty = new ListValue<T>(new List<T>());
        public override string Format(Style style) {
            return "[" + Style.FormatSequence(elements, ", ", x => (x is Value xV) ? xV.Format(style) : (x is Flow xF) ? xF.Format(style) : "", "", 1000) + "]";
        }
        public T Select(Value arg, Style style) {
            if (!(arg is NumberValue)) throw new Error("List index is not a number: " + this.Format(style) + "(" + arg.Format(style) + ")");
            int n = Convert.ToInt32((arg as NumberValue).value);
            if (n < 0 || n >= elements.Count) throw new Error("List index out of range: " + this.Format(style) + "(" + arg.Format(style) + ")");
            return elements[n];
        }
        public ListValue<T> Sublist(Value arg1, Value arg2, Style style) {
            if (!(arg1 is NumberValue) || !(arg2 is NumberValue)) throw new Error("List index is not a number: " + this.Format(style) + "(" + arg1.Format(style) + ", " + arg2.Format(style) + ")");
            int n = Convert.ToInt32((arg1 as NumberValue).value);
            int m = Convert.ToInt32((arg2 as NumberValue).value);
            if (n < 0 || m < 0 || n + m > elements.Count) throw new Error("List index out of range: " + this.Format(style) + "(" + arg1.Format(style) + ", " + arg2.Format(style) + ")");
            List<T> result = new List<T>();
            for (int i = 0; i < m; i++) result.Add(elements[n + i]);
            return new ListValue<T>(result);
        }
        public ListValue<T> Append(ListValue<T> other) {
            return new ListValue<T>(elements.Concat<T>(other.elements).ToList());
        }
        public bool EqualList(ListValue<T> other, Style style) {
            List<T> otherElements = other.elements;
            if (elements.Count != otherElements.Count) return false;
            for (int i = 0; i < elements.Count; i++) {
                if (!elements[i].EqualValue(otherElements[i], style, out bool hasFlows)) return false;
            }
            return true;
        }
        public double[] ToDoubleArray(string error) {
            double[] result = new double[elements.Count];
            for (int i = 0; i < elements.Count; i++) {
                T element = elements[i]; if (!(element is NumberValue)) throw new Error(error);
                result[i] = (element as NumberValue).value;
            }
            return result;
        }
        public static ListValue<T> Map(Func<T, T> f, ListValue<T> l) {
            List<T> res = new List<T>();
            foreach (T e in l.elements) res.Add(f(e));
            return new ListValue<T>(res);
        }
        public static ListValue<T> Filter(Func<T, bool> f, ListValue<T> l) {
            List<T> res = new List<T>();
            foreach (T e in l.elements) if (f(e)) res.Add(e);
            return new ListValue<T>(res);
        }
        public static ListValue<T> Filter(Func<T, Value> f, ListValue<T> l) {
            List<T> res = new List<T>();
            foreach (T e in l.elements) if (f(e) is BoolValue eAs) { if (eAs.value == true) res.Add(e); } else throw new Error("filter predicate should return a boolean");
            return new ListValue<T>(res);
        }
        public static ListValue<T> Sort(Func<T, T, Value> lessEqual, ListValue<T> l, Style style) {
            T[] res = l.elements.ToArray();
            Array.Sort(res, (T a, T b) => {
                if (lessEqual(a, b) is BoolValue asb) {
                    if (asb.value) return -1; else return +1;
                } else throw new Error("sort predicate should return a boolean");
            });
            return new ListValue<T>(new List<T>(res));
        }
        public static T FoldL(Func<T,T,T> f, T z, ListValue<T> l) {
            T res = z;
            foreach (T e in l.elements) res = f(res, e);
            return res;
        }
        public static T FoldR(Func<T,T,T> f, T z, ListValue<T> l) {
            T res = z;
            foreach (T e in l.elements) res = f(e, res);
            return res;
        }
        public static void Each(Action<Value> f, ListValue<Value> l) {
            for (int i = 0; i < l.elements.Count; i++) f(l.elements[i]);
        }
        public static ListValue<T> Reverse(ListValue<T> list) {
            return new ListValue<T>(new List<T>(list.elements.ToArray().Reverse()));
        }
        public static ListValue<Value> Transpose(ListValue<Value> list) {
            List<Value> inRows = list.elements;
            List<Value> outRows = new List<Value>();
            for (int i = 0; i < inRows.Count; i++) {
                if (inRows[i] is ListValue<Value> inRowI) {
                    List<Value> inRow = inRowI.elements;
                    if (inRow.Count < outRows.Count) throw new Error("transpose requires a rectangular list of lists");
                    for (int j = 0; j < inRow.Count; j++) {
                        if (outRows.Count - 1 < j) outRows.Add(new ListValue<Value>(new List<Value>()));
                        List<Value> outRow = (outRows[j] as ListValue<Value>).elements;
                        if (outRow.Count < i) throw new Error("transpose requires a rectangular list of lists");
                        outRow.Add(inRow[j]);
                    }
                } else throw new Error("transpose requires a rectangular list of lists");
            }
            return new ListValue<Value>(outRows);
        }
    }

    public class SampleSpace {
        private static Random random = new Random();
        private Dictionary<SubSampleSpace, Dictionary<int, double>> dimensions;
        public SampleSpace () {
            dimensions = new Dictionary<SubSampleSpace, Dictionary<int, double>>();
        }
        public double Access(SubSampleSpace rv, int dimension) {
            if (!dimensions.ContainsKey(rv)) dimensions[rv] = new Dictionary<int, double>();
            var rvDimensions = dimensions[rv];
            if (!rvDimensions.ContainsKey(dimension)) rvDimensions[dimension] = random.NextDouble();
            return rvDimensions[dimension];
        }
    }

    public class SubSampleSpace {
        public SubSampleSpace() { }
    }

    public class OmegaValue : Value {
        public SampleSpace sampleSpace;
        private SubSampleSpace subSampleSpace;
        public OmegaValue(SampleSpace sampleSpace, SubSampleSpace subSampleSpace) {
            this.type = Type.Omega;
            this.sampleSpace = sampleSpace;
            this.subSampleSpace = subSampleSpace;
        }
        public double Access(int dimension) {
            return this.sampleSpace.Access(this.subSampleSpace, dimension);
        }
        public override string Format(Style style) {
            return "omega";
        }
    }

    public class DistributionValue : Value {
        public SubSampleSpace subSampleSpace; // unique to each random variable, paired to a SampleSpace to obtain an OmegaValue
        private Symbol symbol; // can be null, until it is bound to the symbol of an environment variable
        private string description; // can be null, or can be a special distribution format
        private Symbol anon;   // unique backup symbol used if this.symbol is null and this.format is null
        public DistributionValue(Symbol symbol, string description) {
            this.subSampleSpace = new SubSampleSpace();
            this.type = Type.Random;
            this.symbol = symbol;
            this.anon = new Symbol("<anon>");
            this.description = description;
        }
        public void BindSymbol(Symbol symbol) {
            if (this.symbol == null) this.symbol = symbol; // will now ignore anon
        }
        public string FormatSymbol(Style style)   {
            return ((this.symbol == null) ? this.anon : this.symbol).Format(style);
        }
        public string FormatDescription() {
            return (description == null) ? "" : this.description;
        }
        public override string Format(Style style) { // needs to be unique in case we used as the name of a series for ploting
            if (this.symbol == null && this.description == null) return FormatSymbol(style);
            if (this.symbol == null && this.description != null) return FormatDescription();
            if (this.symbol != null && this.description == null) return FormatSymbol(style);
            return FormatSymbol(style) + " ≡ " + FormatDescription();
        }

        public virtual Value Generate(OmegaValue omega, Style style) { return null; }                           // may produce a sample in the distribution or a rejection (Reject exception)
        public virtual Value Draw(Style style) { return null; }                                                 // produce a non-rejection sample, or loop while trying to
        public virtual DistributionValue ConditionOn(DistributionValue condition, Style style) { return null; } // may produce a sample in the distribution or a rejection (Reject exception)
        public virtual DistributionValue Pred(Func<Value, BoolValue> pred, Style style) { return null; }        // reject the elements of the distribution that do not satisfy the predicate
        public virtual DistributionValue NumberPred(Func<double, double> pred, Style style) { return null; }    // reject the elements of the numerical distribution that do not satisfy the predicate

        protected virtual (int rej, double sample) DrawBool(Style style) { return (0, double.NaN); }
        protected virtual (int rej, double sample) DrawNumber(Style style) { return (0, double.NaN); }

        private static DateTime lastUpdate = DateTime.MinValue;
        private static int paletteNo = 0;
        private static void DensityPlotUpdate(double lo, double hi, int listCount, string[] series, int samplesNo, double[][] samples, Style style, bool incremental) {
            if (!style.chartOutput) return; // should be redundant
            if (TimeLib.Precedes(lastUpdate, DateTime.Now.AddSeconds(-0.2))) {
                KChartHandler.ChartClearData(style); // but not the series
                if (hi == lo) { hi = hi + 1; lo = lo - 1; } // deal with single-point density plots
                double scan = lo;
                double step = (hi - lo) / 100.0;
                if (step > 0)
                    while (scan <= hi) {
                        for (int d = 0; d < listCount; d++) {
                            if (series[d] != null) { // may be null if there were duplicate series names
                                double est = MathNet.Numerics.Statistics.KernelDensity.EstimateGaussian(scan, 3 * step, samples[d]);
                                KChartHandler.ChartAddPoint(series[d], scan, est, 0.0, Noise.None);
                            }
                        }
                        scan += step;
                    }
                KChartHandler.ChartUpdate(style, incremental);
                lastUpdate = DateTime.Now; 
            }
        }
        public static void DensityPlot(int samplesNo, List<DistributionValue> list, Style style) {
            if (!style.chartOutput) return;
            if (samplesNo > 0 && list.Count > 0) {
                KChartHandler.ChartClear("", "", "", style);
                lastUpdate = DateTime.MinValue;
                string[] series = new string[list.Count];
                paletteNo = list.Count - 1;
                for (int d = list.Count-1; d >= 0; d--) {
                    string firstName = list[d].Format(style);
                    string name = firstName; int i = 0;
                    while (Array.Exists(series, (e => e == name))) { name = firstName + Exec.defaultVarchar + i; i++; }
                    series[d] = name;
                    KChartHandler.ChartAddSeries(series[d], null, Palette.GetColor(paletteNo), KLineMode.FillUnder, KLineStyle.Thick);
                    paletteNo--;
                }
                KChartHandler.LegendUpdate(style);
                double[][] samples = new double[list.Count][];
                for (int d = 0; d < list.Count; d++) samples[d] = new double[samplesNo];
                double hi = double.MinValue;
                double lo = double.MaxValue;
                for (int i = 0; i < samplesNo; i++) {
                    for (int d = 0; d < list.Count; d++) {
                        (int rej, double sample) = list[d].DrawNumber(style.RestyleAsChartOutput(false)); // suppress chart output during evaluation of sample 
                        hi = Math.Max(hi, sample);
                        lo = Math.Min(lo, sample);
                        samples[d][i] = sample;
                    }
                    DensityPlotUpdate(lo, hi, list.Count, series, samplesNo, samples, style, true);
                }
                lastUpdate = DateTime.MinValue;
                DensityPlotUpdate(lo, hi, list.Count, series, samplesNo, samples, style, false);
            }
        }

        public static Value Enumerate(int samplesNo, List<DistributionValue> list, bool single, Style style) {
            if (single) return Enumerate(samplesNo, list[0], style);
            else {
                List<Value> elements = new List<Value>();
                foreach (var dist in list) elements.Add(Enumerate(samplesNo, dist, style));
                return new ListValue<Value>(elements);
            }
        }

        public static Value Enumerate(int samplesNo, DistributionValue dist, Style style) {
            List<Value> elements = new List<Value>();
            for (int i = 0; i < samplesNo; i++) elements.Add(dist.Draw(style));
            return new ListValue<Value>(elements);
        }

    }

    public class HiDistributionValue : DistributionValue {
        private Func<OmegaValue, Style, Value> generator; //pass the Style to inhibit chart output of inner equilibrate during Plot/DensityPlot chart output
        public HiDistributionValue(Symbol symbol, string format, Func<OmegaValue, Style, Value> generator) : base(symbol, format) {
            this.generator = generator;
        }
        public override Value Generate(OmegaValue omega, Style style) {   // may produce a sample in the distribution or null = reject
            return Generate(omega.sampleSpace, style); // discard the subsamplespace (apply inverse projection) and reproject on this.subsamplespace
        }
        public Value Generate(SampleSpace sampleSpace, Style style) {   // may produce a sample in the distribution or null = reject
            return this.generator(new OmegaValue(sampleSpace, this.subSampleSpace), style); // reproject the sample space on this.subsamplespace
        }
        public override Value Draw(Style style) { // does not return null = reject
            (int rej, Value sample) = DrawHiValue(style);
            return sample;
        }
        private (int rej, Value sample) DrawHiValue(Style style) { // does not return null = reject
            Value sample = null;
            int rej = 0;
            do {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                sample = Generate(new SampleSpace(), style);
                if (sample == null) rej++;
            } while (sample == null);
            return (rej, sample);
        }
        protected override (int rej, double sample) DrawBool(Style style) { // does not return NaN = reject
            (int rej, Value v) = DrawHiValue(style);
            if (v is BoolValue vAs) return (rej, vAs.value ? double.PositiveInfinity : double.NegativeInfinity);
            else throw new Error("draw: expecting a boolean");
        }
        protected override (int rej, double sample) DrawNumber(Style style) { // does not return NaN = reject
            (int rej, Value v) = DrawHiValue(style);
            if (v is BoolValue vAsB) return (rej, vAsB.value == false ? 0 : 1); // bool as 0/1
            else if (v is NumberValue vAs) return (rej, vAs.value);
            else throw new Error("draw: expecting a number");
        }
        public override DistributionValue ConditionOn(DistributionValue condition, Style style) {
            return new HiDistributionValue(null, null,
                (OmegaValue omega, Style dynamicStyle) => {
                    bool cond;
                    if (condition is LoDistributionValue conditionAs) {
                        double sample = conditionAs.LoGenerate(omega, dynamicStyle);
                        if (double.IsNaN(sample)) { ExecutionInstance.Reject(); return null; } //reject
                        if (double.IsPositiveInfinity(sample)) cond = true;
                        else if(double.IsNegativeInfinity(sample)) cond = false;
                        else throw new Error("'|': expecting a boolean");
                    } else {
                        Value v = (condition as HiDistributionValue).Generate(omega, dynamicStyle);
                        if (v == null) { ExecutionInstance.Reject(); return null; } //reject
                        if (v is BoolValue vAs) cond = vAs.value;
                        else throw new Error("'|': expecting a boolean");
                    } 
                    if (cond) return this.Generate(omega, dynamicStyle);
                    else { ExecutionInstance.Reject(); return null; } //reject
                });
        }
        public override DistributionValue Pred(Func<Value, BoolValue> pred, Style style) {
            return new HiDistributionValue(null, null, 
                (OmegaValue omega, Style dynamicStyle) => {
                    Value sample = this.Generate(omega, dynamicStyle);
                    if (sample == null) return null; //reject
                    BoolValue result = pred(sample);
                    return result;
                });
        }
        public override DistributionValue NumberPred(Func<double, double> pred, Style style) { //pred should take a good number and return a +/-inf encoded boolean
            return new LoDistributionValue(null, null, 
                (OmegaValue omega, Style dynamicStyle) => {
                    Value v = this.Generate(omega, dynamicStyle);
                    if (v == null) return double.NaN; //reject 
                    double sample = ((v is NumberValue n) ? n.value : throw new Error("NumberPred: number expected"));
                    double result = pred(sample);
                    if (double.IsNaN(result)) return double.NaN; //reject
                    if (double.IsInfinity(result)) return result;
                    else throw new Error("NumberPred: pred did not return a boolean");
                });
        }
    }

    public class LoDistributionValue : DistributionValue {
        private Func<OmegaValue, Style, double> generator;
        // a generator from (implicitly) the global random hypercube to a value; it draws samples from the distribution
        // note that the generator can invoke this.omegaIndexer.Access(i) multiple times, always getting the same random number for the same i
        // but different random variables will get different random numbers for same i

        public LoDistributionValue(Symbol symbol, string format, Func<OmegaValue, Style, double> generator) : base(symbol, format) {
            this.generator = generator;
        }
        public override Value Generate(OmegaValue omega, Style style) {  // may produce a sample in the distribution or NaN = reject
            double sample = LoGenerate(omega, style);
            if (double.IsNaN(sample)) return null; //reject
            if (double.IsInfinity(sample)) return new BoolValue(double.IsPositiveInfinity(sample));
            else return new NumberValue(sample);
        }
        public double LoGenerate(OmegaValue omega, Style style) {
            return LoGenerate(omega.sampleSpace, style); // discard the subsamplespace (apply inverse projection) and reproject on this.subsamplespace
        }
        public double LoGenerate(SampleSpace sampleSpace, Style style) {   // may produce a sample in the distribution or NaN = reject
            return this.generator(new OmegaValue(sampleSpace, this.subSampleSpace), style);  // reproject the sample space on this.subsamplespace
        }
        public override Value Draw(Style style) { // does not return NaN = reject
            (int rej, double sample) = DrawLoValue(style);
            if (double.IsInfinity(sample)) return new BoolValue(double.IsPositiveInfinity(sample));
            else return new NumberValue(sample);
        }
        private (int rej, double sample) DrawLoValue(Style style) { // does not return NaN = reject
            double sample = double.NaN;
            int rej = 0;
            do {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                sample = LoGenerate(new SampleSpace(), style);
                if (double.IsNaN(sample)) rej++;
            } while (double.IsNaN(sample));
            return (rej, sample);
        }
        protected override (int rej, double sample) DrawBool(Style style) {
            (int rej, double sample) = DrawLoValue(style);
            if (double.IsInfinity(sample)) return (rej, sample); 
            else throw new Error("DrawBool: expecting a boolean instead of a number");
        }
        protected override (int rej, double sample) DrawNumber(Style style) {
            (int rej, double sample) = DrawLoValue(style);
            if (double.IsNegativeInfinity(sample)) return (rej, 0); // false as 0
            else if (double.IsPositiveInfinity(sample)) return (rej, 1); // true as 1
            else if (!double.IsInfinity(sample)) return (rej, sample); 
            else throw new Error("DrawNumber: expecting a number instead of a boolean");
        }

        public override DistributionValue ConditionOn(DistributionValue condition, Style style) {
            return new LoDistributionValue(null, null,
                (OmegaValue omega, Style dynamicStyle) => {
                    bool cond;
                    if (condition is LoDistributionValue conditionAs) {
                        double sample = conditionAs.LoGenerate(omega, dynamicStyle);
                        if (double.IsNaN(sample)) { ExecutionInstance.Reject(); return double.NaN; } //reject
                        if (double.IsPositiveInfinity(sample)) cond = true;
                        else if(double.IsNegativeInfinity(sample)) cond = false;
                        else throw new Error("'|': expecting a boolean");
                    } else {
                        Value v = (condition as HiDistributionValue).Generate(omega, dynamicStyle);
                        if (v == null) { ExecutionInstance.Reject(); return double.NaN; } //reject
                        if (v is BoolValue vAs) cond = vAs.value;
                        else throw new Error("'|': expecting a boolean");
                    } 
                    if (cond) return this.LoGenerate(omega, dynamicStyle);
                    else { ExecutionInstance.Reject(); return double.NaN; } // reject
                });
        }
        public override DistributionValue Pred(Func<Value, BoolValue> pred, Style style) {
            return new LoDistributionValue(null, null, 
                (OmegaValue omega, Style dynamicStyle) => {
                    double sample = this.LoGenerate(omega, dynamicStyle); 
                    if (double.IsNaN(sample)) return double.NaN; //reject
                    Value value =
                        (double.IsPositiveInfinity(sample)) ? (Value)new BoolValue(true) :
                        (double.IsNegativeInfinity(sample)) ? (Value)new BoolValue(false) :
                        (Value)new NumberValue(sample);
                    BoolValue result = pred(value);
                    if (result == null) return double.NaN; //reject
                    if (result.value) return double.PositiveInfinity; else return double.NegativeInfinity;
                });
        }
        public override DistributionValue NumberPred(Func<double, double> pred, Style style) { //pred should take a good number and return a +/-inf encoded boolean
            return new LoDistributionValue(null, null, 
                (OmegaValue omega, Style dynamicStyle) => {
                    double sample = this.LoGenerate(omega, dynamicStyle); 
                    if (double.IsNaN(sample)) return double.NaN; // reject
                    if (double.IsInfinity(sample)) throw new Error("NumberPred: number expected");
                    double result = pred(sample);
                    if (double.IsNaN(result)) return double.NaN; //reject
                    if (double.IsInfinity(result)) return result;
                    else throw new Error("NumberPred: pred did not return a boolean");
                });
        }

        public static DistributionValue Uniform(double lo, double hi, Style style) {
            if (lo <= hi) {
                return new LoDistributionValue(null,
                    "uniform(" + style.FormatDouble(lo) + ", " + style.FormatDouble(hi) + ")",
                    (OmegaValue omega, Style dynamicStyle) => {
                        return omega.Access(0) * (hi - lo) + lo; 
                    }
                );
            } else throw new Error("Bad distribution arguments: " + "uniform("+lo.ToString()+", "+hi.ToString()+")");
        }
        public static DistributionValue Normal(double mean, double stdev, Style style) {
            if (stdev >= 0) {
                return new LoDistributionValue(null,
                    "normal(" + style.FormatDouble(mean) + ", " + style.FormatDouble(stdev) + ")",
                    (OmegaValue omega, Style dynamicStyle) => {
                        double u1 = 1.0 - omega.Access(0);
                        double u2 = 1.0 - omega.Access(1);
                        double normal01 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                        return mean + stdev * normal01;
                    }
                );
            } else throw new Error("Bad distribution arguments: " + "normal(" + mean.ToString() + ", " + stdev.ToString() + ")");
        }
        public static DistributionValue Exponential(double lambda, Style style) {
            if (lambda > 0) {
                return new LoDistributionValue(null,
                    "exponential(" + style.FormatDouble(lambda) + ")",
                    (OmegaValue omega, Style dynamicStyle) => {
                        return Math.Log(1 - omega.Access(0)) / (-lambda); 
                    }
                );
            } else throw new Error("Bad distribution arguments: " + "exponential(" + lambda.ToString() + ")");
        }
        public static DistributionValue Parabolic(double center, double halfwidth, Style style) {
            if (halfwidth >= 0) {
                return new LoDistributionValue(null,
                    "parabolic(" + style.FormatDouble(center) + ", " + style.FormatDouble(halfwidth) + ")",
                    (OmegaValue omega, Style dynamicStyle) => {
                        //https://stats.stackexchange.com/questions/173637/generating-a-sample-from-epanechnikovs-kernel
                        double u1 = 2 * omega.Access(0) - 1.0;
                        double u2 = 2 * omega.Access(1) - 1.0;
                        double u3 = 2 * omega.Access(2) - 1.0;
                        double sample01 = (Math.Abs(u3) >= Math.Abs(u2) && Math.Abs(u3) >= Math.Abs(u1)) ? u2 : u3;
                        return center + halfwidth * sample01;
                    });
            } else throw new Error("Bad distribution arguments: " + "parabolic(" + center.ToString() + ", " + halfwidth.ToString() + ")");
        }
        public static DistributionValue Bernoulli(double p, Style style) {
            if (p >= 0 && p <= 1) {
                return new LoDistributionValue(null,
                    "bernoulli(" + style.FormatDouble(p) + ")",
                    (OmegaValue omega, Style dynamicStyle) => {
                        return (omega.Access(0) <= p) ? double.PositiveInfinity : double.NegativeInfinity; 
                    });
            } else throw new Error("Bad distribution arguments: " + "bernoulli(" + p.ToString() + ")");
        }
    }

    public class SymbolMultiset {
        private List<Symbol> mset;
        public SymbolMultiset() {
            mset = new List<Symbol>();
        }
        public SymbolMultiset(List<Symbol> set) {
            mset = new List<Symbol>();
            foreach (Symbol symbol in set) mset.Add(symbol);
        }
        public int Sum() {
            return mset.Count;
        }
        public int Cardinality(Symbol symbol) {
            int n = 0;
            foreach (Symbol s in mset)
                if (s.SameSymbol(symbol)) n += 1;
            return n;
        }
        public bool Has(Symbol symbol) {
            return Cardinality(symbol) > 0;
        }
        public List<Symbol> ToList() {
            var list = new List<Symbol>();
            foreach (Symbol symbol in mset) list.Add(symbol);
            return list;
        }
        public List<Symbol> ToSet() {
            var set = new List<Symbol>();
            foreach (Symbol symbol in mset) if (!set.Contains(symbol, SymbolComparer.comparer)) set.Add(symbol);
            return set;
        }
        public void Add(Symbol symbol, int count) {
            for (int i = 0; i < count; i++) mset.Add(symbol);
        }
        public void Subtract(Symbol symbol, int count) {
            for (int i = 0; i < count; i++)
                for (int j = 0; j < mset.Count; j++)
                    if (mset[j].SameSymbol(symbol)) { mset.RemoveAt(j); break; }
        }
        public string Format(Style style) {
            string s = "";
            List<Symbol> set = ToSet();
            for (int i = 0; i < set.Count; i++) {
                int card = Cardinality(set[i]);
                if (card > 1) s += card.ToString();
                s += set[i].Format(style);
                if (i < set.Count - 1) s += " + ";
            }
            return (s == "") ? "Ø" : s;
        }
    }

    public class ReactionValue : Value {
        public List<Symbol> reactants;
        public List<Symbol> products;
        public RateValue rate;
        public ReactionValue(List<Symbol> reactants, List<Symbol> products, RateValue rate) {
            this.type = Type.NONE; // not a first-class value
            this.reactants = reactants;
            this.products = products;
            this.rate = rate;
        }
        public override string Format(Style style) {
            string reactants = Style.FormatSequence(this.reactants, " + ", x => x.Format(style), empty: "Ø");
            string products = Style.FormatSequence(this.products, " + ", x => x.Format(style), empty: "Ø");
            string rate = this.rate.Format(style);
            return reactants + " -> " + products + " " + rate;
        }
        public string FormatNormal(Style style) {
            (SymbolMultiset reactants, SymbolMultiset products) = NormalForm();
            return reactants.Format(style) + " -> " + products.Format(style) + " " + this.rate.Format(style);
        }
        public int Stoichiometry(Symbol species, List<Symbol> complex) {
            int n = 0;
            foreach (Symbol complexSpecies in complex)
                if (complexSpecies.SameSymbol(species)) n += 1;
            return n;
        }
        public int NetStoichiometry(Symbol species) {
            return Stoichiometry(species, this.products) - Stoichiometry(species, this.reactants);
        }
        public double Action(SampleValue sample, double time, Vector state, double temperature, Style style) {    // the mass action of this reaction in this state and temperature
            return this.rate.Action(sample, reactants, time, state, temperature, style);
        }
        public bool Involves(List<SpeciesValue> species) {
            foreach (SpeciesValue s in species) {
                if (this.reactants.Exists(x => x.SameSymbol(s.symbol)) || this.products.Exists(x => x.SameSymbol(s.symbol))) return true;
            }
            if (this.rate.Involves(species)) return true;
            return false;
        }
        public bool InvolvesAsReactants(List<SpeciesValue> species) {
            foreach (SpeciesValue s in species) {
                if (this.reactants.Exists(x => x.SameSymbol(s.symbol))) return true;
            }
            if (this.rate.Involves(species)) return true;
            return false;
        }
        public bool InvolvesAsProducts(List<SpeciesValue> species) {
            foreach (SpeciesValue s in species) {
                if (this.products.Exists(x => x.SameSymbol(s.symbol))) return true;
            }
            return false;
        }
        public bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            foreach (Symbol rs in this.reactants)
                if (!species.Exists(x => x.symbol.SameSymbol(rs))) { notCovered = rs; return false; };
            foreach (Symbol rs in this.products)
                if (!species.Exists(x => x.symbol.SameSymbol(rs))) { notCovered = rs; return false; };
            return rate.CoveredBy(species, out notCovered);
        }
        public bool ReactantsCoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            foreach (Symbol rs in this.reactants)
                if (!species.Exists(x => x.symbol.SameSymbol(rs))) { notCovered = rs; return false; };
            return rate.CoveredBy(species, out notCovered);
        }
        public bool ProductsCoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            foreach (Symbol rs in this.products)
                if (!species.Exists(x => x.symbol.SameSymbol(rs))) { notCovered = rs; return false; };
            notCovered = null; return true;
        }
        public (SymbolMultiset reactants, SymbolMultiset products) NormalForm() {
            return (new SymbolMultiset(this.reactants), new SymbolMultiset(this.products));
        }
        public (SymbolMultiset catalysts, SymbolMultiset catalyzedRactants, SymbolMultiset catalyzedProducts) CatalystForm() {
            var catalysts = new SymbolMultiset();
            var catalyzedRactants = new SymbolMultiset(reactants); 
            var catalyzedProducts = new SymbolMultiset(products); 
            foreach (Symbol symbol in reactants) {
                int n = Math.Min(catalyzedRactants.Cardinality(symbol), catalyzedProducts.Cardinality(symbol));
                if (n > 0) {
                    catalysts.Add(symbol, 1);
                    catalyzedRactants.Subtract(symbol, 1);
                    catalyzedProducts.Subtract(symbol, 1);
                }
            }
            return (catalysts, catalyzedRactants, catalyzedProducts);
        }
        public HashSet<Symbol> ReactantsSet() { return new HashSet<Symbol>(reactants, SymbolComparer.comparer); }
        public HashSet<Symbol> ProductsSet() { return new HashSet<Symbol>(products, SymbolComparer.comparer); }
    }

    public abstract class RateValue {
        public const RateValue REJECT = null; // return REJECT=null from Mass Action rate evaluation to mean "sample is rejected" in random variable sampling (instead of an exception, trapping which is way too expensive)
        public abstract string Format(Style style);
        public abstract double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style);  // the mass action of this reaction in this state and temperature
        public abstract bool Involves(List<SpeciesValue> species);
        public abstract bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered);
    }

    public class GeneralFlowRate : RateValue {
        public Flow rateFunction;
        public GeneralFlowRate(Flow rateFunction) {
            this.rateFunction = rateFunction;
        }
        public override string Format(Style style) {
            return "{{" + rateFunction.TopFormat(style) + "}}";
        }
        public override double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style) {
            // We earlier checked that rateFunction HasDeterministicMean. If it is not a numeric flow, we now get an error from ObserveMean.
            return rateFunction.ObserveMean(sample, time, new State(state.Length, false).InitAll(state), null, style);  // flux=null: we cannot evaluate derivative for rates, which affect those derivatives
        }
        public override bool Involves(List<SpeciesValue> species) {
            return rateFunction.Involves(species);
        }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            return rateFunction.CoveredBy(species, out notCovered);
        }
    }

    public class MassActionFlowRate : RateValue {
        public Flow rateFunction;
        public MassActionFlowRate(Flow rateFunction) {
            this.rateFunction = rateFunction;
        }
        public override string Format(Style style) {
            return "{" + rateFunction.TopFormat(style) + "}";
        }
        public override double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style) {
            // We earlier checked that rateFunction HasDeterministicMean. If it is not a numeric flow, we now get an error from ObserveMean.
            double action = rateFunction.ObserveMean(sample, time, new State(state.Length, false).InitAll(state), null, style);  // flux=null: we cannot evaluate derivative for rates, which affect those derivatives
            foreach (Symbol rs in reactants) action = action * state[sample.stateMap.index[rs]];
            return action;
        }
        public override bool Involves(List<SpeciesValue> species) {
            return rateFunction.Involves(species);
        }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            return rateFunction.CoveredBy(species, out notCovered);
        }
    }

    public class MassActionNumericalRate : RateValue {
        private double collisionFrequency; // dimensionless
        private double activationEnergy; // J·mol^-1  (J=Joules, not kJoules)
        const double R = 8.3144598; // J·mol^-1·K^-1  (K=Kelvin)
        public MassActionNumericalRate(double collisionFrequency, double activationEnergy) {
            this.collisionFrequency = collisionFrequency;
            this.activationEnergy = activationEnergy;
        }
        public MassActionNumericalRate(double collisionFrequency) {
            this.collisionFrequency = collisionFrequency;
            this.activationEnergy = 0.0; // default
        }
        public override string Format(Style style) {
            if (collisionFrequency == 1.0 && activationEnergy == 0.0) return "";
            string cf = style.FormatDouble(collisionFrequency);
            if (activationEnergy == 0.0) return "{" + cf + "}";
            string ae = style.FormatDouble(activationEnergy);
            return "{" + cf + ", " + ae + "}";
        }
        public double Rate(double temperature) {
            // Program.Log("Arrhenius " + collisionFrequency + ", " + activationEnergy + " = " + collisionFrequency * Math.Exp(-(activationEnergy / (R * temperature))));
            if (activationEnergy == 0.0) return collisionFrequency;
            else return collisionFrequency * Math.Exp(-(activationEnergy / (R * temperature)));
        }
        public override double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style) {
            double action = this.Rate(temperature);
            foreach (Symbol rs in reactants) action = action * state[sample.stateMap.index[rs]];
            return action;
        }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
    }

    public class FunctionValue : Value {
        public Symbol symbol; // just for formatting, may be null if produced by a nameless network abstraction
        public Parameters parameters;
        public Expression body;
        public Env env;
        public FunctionValue(Symbol symbol, Parameters parameters, Expression body, Env env) {
            this.type = Type.Function;
            this.symbol = symbol;
            this.parameters = parameters;
            this.body = body;
            this.env = env;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return (symbol == null) ? "λ" : symbol.Format(style);
            else if (style.dataFormat == "header") return ((symbol == null) ? "λ" : symbol.Format(style)) + "(" + parameters.Format() + ")";
            else if (style.dataFormat == "full") return ((symbol == null) ? "λ" : symbol.Format(style)) + "(" + parameters.Format() + ") {" + Environment.NewLine + body.Format() + Environment.NewLine + "}";
            else return "unknown format: " + style.dataFormat;
        }
        public Value ApplyReject(List<Value> arguments, Netlist netlist, Style style, int s) {
            return body.EvalReject(env.ExtendValues(parameters.parameters, arguments, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style, s + 1), netlist, style, s + 1); 
        }
        public Value ApplyReject(Value argument, Netlist netlist, Style style, int s) {
            return body.EvalReject(env.ExtendValue(parameters.parameters, argument, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style, s + 1), netlist, style, s + 1); 
        }
        public Value ApplyReject(Value argument1, Value argument2, Netlist netlist, Style style, int s) {
            return body.EvalReject(env.ExtendValue(parameters.parameters, argument1, argument2, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style, s + 1), netlist, style, s + 1); 
        }
        //public Value ApplyFlow(List<Value> arguments, Style style, int s) {
        //    return body.EvalFlow(env.ExtendValues<Value>(parameters.parameters, arguments, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style, s + 1), style, s + 1);
        //}
        //public Flow BuildFlow(List<Flow> arguments, Style style, int s) {
        //    return body.BuildFlow(env.ExtendValues<Flow>(parameters.parameters, arguments, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style, s + 1), style, s + 1); // note that even in this case the arguments are Values, not Flows
        //}      
        public static Value Enumerate(int samplesNo, List<FunctionValue> list, bool single, Netlist netlist, Style style, int s) {
            if (single) return Enumerate(samplesNo, list[0], netlist, style, s);
            else {
                List<Value> elements = new List<Value>();
                foreach (var func in list) {
                    var element = Enumerate(samplesNo, func, netlist, style, s);
                    if (element == Value.REJECT) return Value.REJECT;
                    elements.Add(element);
                }
                return new ListValue<Value>(elements);
            }
        }
        public static Value Enumerate(int samplesNo, FunctionValue func, Netlist netlist, Style style, int s) {
            List<Value> elements = new List<Value>();
            for (int i = 0; i < samplesNo; i++) {
                var element = func.ApplyReject(new List<Value>() { new NumberValue(i) }, netlist, style, s);
                if (element == Value.REJECT) return Value.REJECT;
                elements.Add(element);
            }
            return new ListValue<Value>(elements);
        }

        private static DateTime lastUpdate = DateTime.MinValue;
        private static int paletteNo = 0;
        public static void Plot(int samplesNo, List<FunctionValue> list, Netlist netlist, Style style, int s) {
            if (!style.chartOutput) return;
            if (samplesNo > 0 && list.Count > 0) {
                KChartHandler.ChartClear("", "", "", style);
                lastUpdate = DateTime.MinValue;
                string[] series = new string[list.Count];
                paletteNo = list.Count - 1;
                for (int d = list.Count-1; d >= 0; d--) {
                    string firstName = list[d].Format(style.RestyleAsDataFormat("symbol"));
                    string name = firstName; int i = 0;
                    while (Array.Exists(series, (e => e == name))) { name = firstName + Exec.defaultVarchar + i; i++; }
                    series[d] = name;
                    KChartHandler.ChartAddSeries(series[d], null, Palette.GetColor(paletteNo), KLineMode.FillUnder, KLineStyle.Thick);
                    paletteNo--;
                }
                KChartHandler.LegendUpdate(style);
                double[][] samples = new double[list.Count][];
                for (int d = 0; d < list.Count; d++) samples[d] = new double[samplesNo];
                for (int i = 0; i < samplesNo; i++) {
                    for (int d = 0; d < list.Count; d++) {
                        Value sample = list[d].ApplyReject(new List<Value>() { new NumberValue(i) }, netlist, style.RestyleAsChartOutput(false), s); // suppress chart output during evaluation of function
                        if (sample != Value.REJECT && sample is NumberValue sampleAs) {
                            samples[d][i] = sampleAs.value;
                        } else {
                            samples[d][i] = double.NaN;
                        }
                    }
                    PlotUpdate(list.Count, series, samplesNo, samples, style, true);
                }
                lastUpdate = DateTime.MinValue;
                PlotUpdate(list.Count, series, samplesNo, samples, style, false);
            }
        }
        private static void PlotUpdate(int listCount, string[] series, int samplesNo, double[][] samples, Style style, bool incremental) {
            if (!style.chartOutput) return; // should be redundant
            if (TimeLib.Precedes(lastUpdate, DateTime.Now.AddSeconds(-0.2))) {
                KChartHandler.ChartClearData(style); // but not the series
                for (int i = 0; i < samplesNo; i++) {
                    for (int d = 0; d < listCount; d++) {
                        if (series[d] != null) { // may be null if there were duplicate series names
                            KChartHandler.ChartAddPoint(series[d], i, samples[d][i], 0.0, Noise.None);
                        }
                    }
                }
                KChartHandler.ChartUpdate(style, incremental);
                lastUpdate = DateTime.Now; 
            }
        }
    }

    public class NetworkValue : Value {
        private Symbol symbol; // just for formatting, may be null if produced by a nameless network abstraction
        public Parameters parameters;
        public Statements body;
        public Env env;
        public NetworkValue(Symbol symbol, Parameters parameters, Statements body, Env env) {
            this.type = Type.Network;
            this.symbol = symbol;
            this.parameters = parameters;
            this.body = body;
            this.env = env;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return (symbol == null) ? "η" : symbol.Format(style);
            else if (style.dataFormat == "header") return ((symbol == null) ? "η" : symbol.Format(style)) + "(" + parameters.Format() + ")";
            else if (style.dataFormat == "full") return ((symbol == null) ? "η" : symbol.Format(style)) + "(" + parameters.Format() + ") {" + Environment.NewLine + body.Format() + Environment.NewLine + "}";
            else return "unknown format: " + style.dataFormat;
        }
        public Env ApplyReject(Value argument1, Netlist netlist, Style style, int s) {
            Env ignoreEnv = body.EvalReject(env.ExtendValue(parameters.parameters, argument1, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style, s + 1), netlist, style, s + 1);
            return ignoreEnv;
        }
        public Env ApplyReject(Value argument1, Value argument2, Netlist netlist, Style style, int s) {
            Env ignoreEnv = body.EvalReject(env.ExtendValue(parameters.parameters, argument1, argument2, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style, s + 1), netlist, style, s + 1);
            return ignoreEnv;
        }
        public Env ApplyReject(List<Value> arguments, Netlist netlist, Style style, int s) {
            string source = symbol == null ? "<nameless>" : symbol.Format(style);
            Env extEnv = env.ExtendValues(parameters.parameters, arguments, null, source, style, s + 1);
            Env ignoreEnv = body.EvalReject(extEnv, netlist, style, s + 1);
            return ignoreEnv;
        }
        public static Env Enumerate(int samplesNo, List<NetworkValue> list, bool single, Netlist netlist, Style style, int s) {
            if (single) return Enumerate(samplesNo, list[0], netlist, style, s);
            else {
                foreach (var netw in list) {
                    Env env = Enumerate(samplesNo, netw, netlist, style, s);
                    if (env == Env.REJECT) return Env.REJECT;
                }
                return new NullEnv(); // not to return Env.REJECT;
            }
        }
        public static Env Enumerate(int samplesNo, NetworkValue netw, Netlist netlist, Style style, int s) {
            for (int i = 0; i < samplesNo; i++) {
                Env env = netw.ApplyReject(new List<Value>() { new NumberValue(i) }, netlist, style, s);
                if (env == Env.REJECT) return Env.REJECT;
            }
            return new NullEnv(); // not to return Env.REJECT;
        }
    }

}




//public class OperatorValue : Value {
//    public string name;
//    public OperatorValue(string name) {
//        this.type = Type.Function;
//        this.name = name;
//    }
//    public override string Format(Style style) {
//        return name;
//    }
//    public Value Apply(List<Value> arguments, bool infix, Netlist netlist, Style style, int s) {  //######## This can now be combined with ApplyFlow
//        string BadArgs() { return "Not acceptable: '" + name + "' with arguments: " + Style.FormatSequence(arguments, ", ", x => x.Format(style)); }
//        if (arguments.Count == 0) {
//            throw new Error(BadArgs());
//        } else if (arguments.Count == 1) {
//            return ApplyFlow(arguments, infix, style);
//        } else if (arguments.Count == 2) {
//            if (name == "map") if (arguments[0] is FunctionValue arg1As && arguments[1] is ListValue<Value> arg2As) return ListValue<Value>.Map((Value e) => arg1As.ApplyReject(e, netlist, style, s), arg2As); else throw new Error(BadArgs());
//            else if (name == "filter") if (arguments[0] is FunctionValue arg1As && arguments[1] is ListValue<Value> arg2As) return ListValue<Value>.Filter((Value e) => arg1As.ApplyReject(e, netlist, style, s), arg2As); else throw new Error(BadArgs());
//            else if (name == "foreach") if (arguments[0] is NetworkValue arg1As && arguments[1] is ListValue<Value> arg2As) return ListValue<Value>.Foreach((Value i, Value e) => arg1As.ApplyReject(i, e, netlist, style, s), arg2As); else throw new Error(BadArgs());
//            else return ApplyFlow(arguments, infix, style);
//        } else if (arguments.Count == 3) {
//            if (name == "argmin") return Protocol.Argmin(arguments[0], arguments[1], arguments[2], netlist, style, s); // BFGF
//            else if (name == "foldr") if (arguments[0] is FunctionValue arg1As && arguments[2] is ListValue<Value> arg3As) return ListValue<Value>.FoldR((Value e1, Value e2) => arg1As.ApplyReject(e1, e2, netlist, style, s), arguments[1], arg3As); else throw new Error(BadArgs());
//            else if (name == "foldl") if (arguments[0] is FunctionValue arg1As && arguments[2] is ListValue<Value> arg3As) return ListValue<Value>.FoldL((Value e1, Value e2) => arg1As.ApplyReject(e1, e2, netlist, style, s), arguments[1], arg3As); else throw new Error(BadArgs());
//            else return ApplyFlow(arguments, infix, style);
//            //} else if (arguments.Count == 4) {
//            //    if (name == "argmin") return Protocol.Argmin(arguments[0], arguments[1], arguments[2], arguments[3], netlist, style); // GoldenSection
//            //else throw new Error(BadArgs());
//        } else throw new Error(BadArgs());
//    }
//    public Value ApplyFlow(List<Value> arguments, bool infix, Style style) { // a subset of Apply
//        string BadArgs() { return "Not acceptable: '" + name + "' with arguments: " + Style.FormatSequence(arguments, ", ", x => x.Format(style)); }
//        if (arguments.Count == 0) {
//            throw new Error(BadArgs());
//        } else if (arguments.Count == 1) {
//            Value arg1 = arguments[0];
//            if (name == "not") if (arg1 is BoolValue) return new BoolValue(!((BoolValue)arg1).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "-") if (arg1 is NumberValue) return new NumberValue(-((NumberValue)arg1).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "<-") if (arg1 is DistributionValue arg1As) return arg1As.Draw(style); else throw new Error(BadArgs());
//            else if (name == "abs") if (arg1 is NumberValue) return new NumberValue(Math.Abs(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "arccos") if (arg1 is NumberValue) return new NumberValue(Math.Acos(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "arcsin") if (arg1 is NumberValue) return new NumberValue(Math.Asin(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "arctan") if (arg1 is NumberValue) return new NumberValue(Math.Atan(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "ceiling") if (arg1 is NumberValue) return new NumberValue(Math.Ceiling(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "cos") if (arg1 is NumberValue) return new NumberValue(Math.Cos(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "cosh") if (arg1 is NumberValue) return new NumberValue(Math.Cosh(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "exp") if (arg1 is NumberValue) return new NumberValue(Math.Exp(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "floor") if (arg1 is NumberValue) return new NumberValue(Math.Floor(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "int") if (arg1 is NumberValue) { double num1 = ((NumberValue)arg1).value; return new NumberValue(Math.Round(num1)); } else return ApplyCoerceFlows(arguments, style);          // convert number to integer number by rounding
//            else if (name == "log") if (arg1 is NumberValue) return new NumberValue(Math.Log(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "pos") if (arg1 is NumberValue) { double num1 = ((NumberValue)arg1).value; return new NumberValue((num1 > 0) ? num1 : 0); } else return ApplyCoerceFlows(arguments, style);     // convert number to positive number by returning 0 if negative
//            else if (name == "sign") if (arg1 is NumberValue) return new NumberValue(Math.Sign(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "sin") if (arg1 is NumberValue) return new NumberValue(Math.Sin(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "sinh") if (arg1 is NumberValue) return new NumberValue(Math.Sinh(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "sqrt") if (arg1 is NumberValue) return new NumberValue(Math.Sqrt(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "tan") if (arg1 is NumberValue) return new NumberValue(Math.Tan(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "tanh") if (arg1 is NumberValue) return new NumberValue(Math.Tanh(((NumberValue)arg1).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "exponential") if (arg1 is NumberValue arg1As) return LoDistributionValue.Exponential(arg1As.value, style); else throw new Error(BadArgs());
//            else if (name == "bernoulli") if (arg1 is NumberValue arg1As) return LoDistributionValue.Bernoulli(arg1As.value, style); else throw new Error(BadArgs());
//            else if (name == "∂" || name == "sdiff" || name == "var" || name == "poisson") return ApplyCoerceFlows(arguments, style); // "observe" is handled earlier although now it could probably be done here
//            else throw new Error(BadArgs());
//        } else if (arguments.Count == 2) {
//            Value arg1 = arguments[0];
//            Value arg2 = arguments[1];
//            if (name == "or") if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value || ((BoolValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "and") if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value && ((BoolValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "+")
//                if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value + ((NumberValue)arg2).value);
//                else if (arg1 is StringValue && arg2 is StringValue) return new StringValue(((StringValue)arg1).value + ((StringValue)arg2).value);
//                else return ApplyCoerceFlows(arguments, style); //throw new Error(BadArgs());
//            else if (name == "-") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value - ((NumberValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "*") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value * ((NumberValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "/") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value / ((NumberValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "^") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Pow(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "++") if (arg1 is ListValue<Value> && arg2 is ListValue<Value>) return (((ListValue<Value>)arg1).Append((ListValue<Value>)arg2)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "=") {
//                bool eq = arg1.EqualValue(arg2, style, out bool hasFlows);
//                if (hasFlows) return ApplyCoerceFlows(arguments, style);
//                else return new BoolValue(eq);
//            } else if (name == "<>") {
//                bool eq = arg1.EqualValue(arg2, style, out bool hasFlows);
//                if (hasFlows) return ApplyCoerceFlows(arguments, style);
//                else return new BoolValue(!eq);
//            } else if (name == "<=") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value <= ((NumberValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "<") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value < ((NumberValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == ">=") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value >= ((NumberValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            else if (name == ">") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value > ((NumberValue)arg2).value); else return ApplyCoerceFlows(arguments, style);
//            //else if (arg1 is DistributionValue arg1As && arg2 is NumberValue arg2As) return arg1As.NumberPred((v) => { return (v > arg2As.value) ? 1.0 : 0.0; }, style);
//            else if (name == "|") if (arg1 is DistributionValue arg1As && arg2 is DistributionValue arg2As) return arg1As.ConditionOn(arg2As, style); else throw new Error(BadArgs());
//            else if (name == "arctan2") if (arg1 is NumberValue arg1As && arg2 is NumberValue arg2As) return new NumberValue(Math.Atan2(arg1As.value, arg2As.value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "max") if (arg1 is NumberValue arg1As && arg2 is NumberValue arg2As) return new NumberValue(Math.Max(arg1As.value, arg2As.value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "min") if (arg1 is NumberValue arg1As && arg2 is NumberValue arg2As) return new NumberValue(Math.Min(arg1As.value, arg2As.value)); else return ApplyCoerceFlows(arguments, style);
//            else if (name == "uniform") if (arg1 is NumberValue arg1As && arg2 is NumberValue arg2As) return LoDistributionValue.Uniform(arg1As.value, arg2As.value, style); else throw new Error(BadArgs());
//            else if (name == "normal") if (arg1 is NumberValue arg1As && arg2 is NumberValue arg2As) return LoDistributionValue.Normal(arg1As.value, arg2As.value, style); else throw new Error(BadArgs());
//            else if (name == "parabolic") if (arg1 is NumberValue arg1As && arg2 is NumberValue arg2As) return LoDistributionValue.Parabolic(arg1As.value, arg2As.value, style); else throw new Error(BadArgs());
//            else if (name == "cov" || name == "gauss") return ApplyCoerceFlows(arguments, style); // "observe" is handled earlier although now it could probably be done here
//            else throw new Error(BadArgs());
//        } else if (arguments.Count == 3) {
//            if (name == "cond") return ApplyCoerceFlows(arguments, style);
//            else throw new Error(BadArgs());
//        } else throw new Error(BadArgs());
//    }
//    public Flow ApplyCoerceFlows(List<Value> arguments, Style style) {
//        Flow Bad() { throw new Error("Not acceptable: '" + name + "' with arguments: " + Style.FormatSequence(arguments, ", ", x => x.Format(style))); }
//        List<Flow> flows = new List<Flow>();
//        bool allValues = true;
//        foreach (Value value in arguments) {
//            if (value is Flow || value is SpeciesValue) allValues = false;
//            Flow flow = value.ToFlow();
//            if (flow != null) flows.Add(flow);
//            else return Bad();
//        }
//        if (allValues) return Bad(); // we coerce to flows only if at least one of the arguments is a flow or species, otherwise 3+true would be coerced to flow.
//        return BuildFlow(flows, style);
//    }
//    public Flow BuildFlow(List<Flow> arguments, Style style) {
//        string BadArgs() { return "Flow expression: Not acceptable: '" + name + "' with arguments: " + Style.FormatSequence(arguments, ", ", x => x.Format(style)); }
//        if (arguments.Count == 0) {
//            if (name == "time" || name == "kelvin" || name == "celsius" || name == "volume")
//                return OpFlow.Op(name);
//            // "time", "kelvin", "celsius", "volume" are placed in the initial environment as Operators and converted to OpFlow when fetched as variables
//            else throw new Error(BadArgs());
//        } else if (arguments.Count == 1) {
//            Flow arg1 = arguments[0];
//            if (name == "not" || name == "-" || name == "∂") {
//                return OpFlow.Op(name, arg1);
//            } else if (name == "sdiff") { // a Flow will never contain sdiff: it is expanded out here
//                return arg1.Differentiate(null, style); // null means time differentiation
//            } else if (name == "var" || name == "poisson" || name == "abs" || name == "arccos" || name == "arcsin" || name == "arctan" || name == "ceiling"
//                    || name == "cos" || name == "cosh" || name == "exp" || name == "floor" || name == "int" || name == "log"
//                    || name == "pos" || name == "sign" || name == "sin" || name == "sinh" || name == "sqrt" || name == "tan" || name == "tanh") {
//                return OpFlow.Op(name, arg1);
//            //} else if (name == "asflow") {
//            //    return arg1;
//            } else if (name == "observe") { // observe(f) = f:  observing the current sample
//                return arg1;
//            } else throw new Error(BadArgs());
//        } else if (arguments.Count == 2) {
//            Flow arg1 = arguments[0];
//            Flow arg2 = arguments[1];
//            if (name == "or" || name == "and" || name == "+" || name == "-" || name == "*" || name == "/" || name == "^"
//                || name == "=" || name == "<>" || name == "<=" || name == "<" || name == ">=" || name == ">" || name == "++") {
//                return OpFlow.Op(name, arg1, arg2);
//            } else if (name == "cov" || name == "gauss" || name == "arctan2" || name == "min" || name == "max" || name == "observe") {
//                return OpFlow.Op(name, arg1, arg2);
//            } else throw new Error(BadArgs());
//        } else if (arguments.Count == 3) {
//            if (name == "cond") {
//                return OpFlow.Op(name, arguments[0], arguments[1], arguments[2]);
//            } else throw new Error(BadArgs());
//        } else throw new Error(BadArgs());
//    }
//}