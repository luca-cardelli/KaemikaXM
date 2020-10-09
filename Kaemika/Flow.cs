using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Research.Oslo;

namespace Kaemika {

    // A flow expression should evaluate to NormalFlow (BoolFlow, NumberFlow, SpeciesFlow, or OpFlow combining them)
    // on the way to producing those, OperatorFlow, and FunctionFlow are also used, but they must be expanded to the above by FunctionInstance
    // if non-normal Flows survive, errors are give at simulation time (for '{{...}}' flows) or report time (for 'report ...' flows)

    public abstract class Flow : Value {
        public new const Flow REJECT = null; // return REJECT=null from Expression evaluation to mean "sample is rejected" in random variable sampling (instead of an exception, trapping which is way too expensive)
        public abstract bool EqualFlow(Flow other);
        public abstract bool Precedes(Flow other, Style style); // in lexical order
        public abstract Flow Normalize(Style style);
        public abstract Flow Expand(Style style);
        public abstract Flow Simplify(Style style);
        public abstract Flow ReGroup(Style style);
        public abstract bool Involves(List<SpeciesValue> species);
        public abstract bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered);
        public abstract bool IsOp(string op, int arity);
        public bool IsNumber(double n) { return this is NumberFlow num && num.value == n; }
        public bool IsNegative() { return this is NumberFlow num && num.value < 0.0; }
        public abstract bool IsNumericConstantExpression(); // including 'Constant" flows

        public abstract Flow Arg(int i);

        public abstract bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style); // for boolean flow-subexpressions
        public abstract double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style); // for numeric flow-subexpressions
        public abstract double ObserveVariance(SampleValue sample, double time, State state, Style style);
        public abstract double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style);
        public abstract double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style); // automatic differentiation

        public abstract bool HasDeterministicValue();
        // = Can appear in non-LNA charts and in generalized rates {{ ... }}. 
        // Excludes var/cov, poisson/gauss.
        public abstract bool HasStochasticMean();
        // = Can appear in LNA charts as means and variance. 
        // Inside var/cov only linear combinations of species are allowed, also because of issues like cov(poisson(X),poisson(X)) =? var(poisson(X))
        public bool HasStochasticVariance() { return LinearCombination(); } // include poisson and gauss in LinearCombination so they can appear in LNA report
        // = Can appear in LNA charts as means and variance. 
        public abstract bool LinearCombination();
        // = Is a linear combination of species and time/kelvin/celsius flows only
        public abstract bool HasNullVariance();
        // = Has stochastic variance identically zero, used to detect and rule out non-linear products.

        public abstract Flow Differentiate(Symbol var, Style style); // symbolic differentiation; var is the partial differentiation variable, or null for time differentiation
        public static Vector nilFlux = new Vector(new double[0]);

        public static Flow zero = new NumberFlow(0.0);
        public static Flow one = new NumberFlow(1.0);
        public static Flow minusOne = new NumberFlow(-1.0);

        public string FormatAsODE(SpeciesValue headSpecies, Style style, string prefixDiff = "∂", string suffixDiff = "") {
            return prefixDiff + headSpecies.Format(style) + suffixDiff + " = " + this.Normalize(style).TopFormat(style);
        }
    }

    public class BoolFlow : Flow {
        public bool value;
        public BoolFlow(bool value) { this.type = Type.Flow; this.value = value; }
        public override Flow Normalize(Style style) { return this;  }
        public override Flow Expand(Style style) { return this; }
        public override Flow Simplify(Style style) { return this; }
        public override Flow ReGroup(Style style) { return this; }
        public override bool EqualFlow(Flow other) { return (other is BoolFlow) && ((other as BoolFlow).value == this.value); }
        public override bool Precedes(Flow other, Style style) { return (other is BoolFlow) && (this.value == false && (other as BoolFlow).value == true); }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override bool IsOp(string op, int arity) { return false; }
        public override bool IsNumericConstantExpression() { return false; }
        public override Flow Arg(int i) { throw new Error("Arg"); }
        public override string Format(Style style) { if (this.value) return "true"; else return "false"; }

        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { return this.value; }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: number expected instead of bool: " + Format(style)); }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of bool: " + Format(style)); }
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of bool: " + Format(style)); }
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Not differentiable, bool: " + Format(style)); }
        public override bool HasDeterministicValue() { return true; } // can appear in rate-expressions in a cond
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; } // but if it appears in a cond it will not be a linear combination anyway
        public override bool HasNullVariance() { return true; }
        public override Flow Differentiate(Symbol var, Style style) { throw new Error("Non differentiable: bool"); }
    }

    public class NumberFlow : Flow {
        public double value;
        public NumberFlow(double value) { this.type = Type.Flow; this.value = value; }
        public override bool EqualFlow(Flow other) { return (other is NumberFlow) && ((other as NumberFlow).value == this.value); }
        public override bool Precedes(Flow other, Style style) { return (other is NumberFlow) && (this.value < (other as NumberFlow).value); }
        public override Flow Normalize(Style style) { return this; }
        public override Flow Expand(Style style) { return this; }
        public override Flow Simplify(Style style) { return this; }
        public override Flow ReGroup(Style style) { return this; }
        public static NumberFlow numberFlowZero = new NumberFlow(0.0);
        public static NumberFlow numberFlowOne = new NumberFlow(1.0);
        public static NumberFlow numberFlowTwo = new NumberFlow(2.0);
        public static NumberFlow numberFlowMinusOne = new NumberFlow(-1.0);
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override bool IsOp(string op, int arity) { return false; }
        public override bool IsNumericConstantExpression() { return true; }
        public override Flow Arg(int i) { throw new Error("Arg"); }
        public override string Format(Style style) { return style.FormatDouble(this.value); }

        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: bool expected instead of number: " + Format(style)); }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { return this.value; }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) { return 0.0; } // Var(number) = 0
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) { return 0.0; } // Cov(number,Y) = 0
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { return 0.0; } // ∂n = 0
        public override bool HasDeterministicValue() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return true; }
        public override Flow Differentiate(Symbol var, Style style) { return NumberFlow.numberFlowZero; }
    }

    public class TimecourseFlowUnassigned: ConstantFlow {
        // set up as a subtype of ConstantFlow to give better error messages when found unassigned
        public TimecourseFlowUnassigned(Symbol timecourse) : base(timecourse) { }
    }

    public class TimecourseFlow : Flow {
        public Symbol name; // the symbol associate to the timecores in the report statement
        public Noise noise; // the noise active when the timecourse was fetched
        public Flow series; // the flow used to fetch a series out of a simulation run and convert it to a TimecourseFlow
        public double[] times; // the times of that series
        public double[] values; // the corresponding values of that series
        public TimecourseFlow(Symbol name, Noise noise, Flow series, double[] times, double[] values) { 
            this.type = Type.Flow;
            this.name = name;
            this.noise = noise;
            this.series = series;  
            this.times = times; 
            this.values = values;
        }
        public override bool EqualFlow(Flow other) {
            return other is TimecourseFlow asOther && this.name.SameSymbol(asOther.name);  // this is important for KChart.ToTimecourse search: comparing name instead of series seems to work
        }
        public override bool Precedes(Flow other, Style style) { return false; }
        public override Flow Normalize(Style style) { return this; }
        public override Flow Expand(Style style) { return this; }
        public override Flow Simplify(Style style) { return this; }
        public override Flow ReGroup(Style style) { return this; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override bool IsOp(string op, int arity) { return false; }
        public override bool IsNumericConstantExpression() { return false; }
        public override Flow Arg(int i) { throw new Error("Arg"); }
        public override string Format(Style style) { 
            return // name.Format(style) + " = " + 
                "<" + series.TopFormat(style) + ">"
                //+ (
                //this.noise == Noise.None ? "" : 
                //(this.noise == Noise.SigmaRange || this.noise == Noise.SigmaSqRange) ? Gui.StringOfNoise(Noise.None) :
                //Gui.StringOfNoise(this.noise)
                ; 
        }

        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: bool expected instead of: " + Format(style)); }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            if (this.times.Length == 0) return double.NaN;
            if (time < this.times[0]) return this.values[0];
            if (time > this.times[this.times.Length - 1]) return this.values[this.times.Length - 1];
            for (int i = 0; i < this.times.Length; i++) {
                if (time <= this.times[i]) { // interpolate or we will get jaggies in the plots
                    if (i == 0) return this.values[0];
                    double t0 = this.times[i - 1];
                    double y0 = this.values[i - 1];
                    double t1 = this.times[i];
                    double y1 = this.values[i];
                    return (t1 == t0) ? y1 : y1 + ((y0 - y1) * (t1 - time)) / (t1 - t0);
                }
            } 
            return this.values[this.times.Length - 1];
        }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: variance not available for timecourses: " + Format(style)); }
        //    if (ranges == null) throw new Error("Variance not supported in current state");
        //    if (this.times.Length == 0) return double.NaN;
        //    if (time < this.times[0]) return this.ranges[0];
        //    if (time > this.times[this.times.Length - 1]) return this.ranges[this.times.Length - 1];
        //    for (int i = 0; i < this.times.Length; i++) {
        //        if (time <= this.times[i]) { // interpolate or we will get jaggies in the plots
        //            if (i == 0) return this.ranges[0];
        //            double t0 = this.times[i - 1];
        //            double y0 = this.ranges[i - 1];
        //            double t1 = this.times[i];
        //            double y1 = this.ranges[i];
        //            return (t1 == t0) ? y1 : y1 + ((y0 - y1) * (t1 - time)) / (t1 - t0);
        //        }
        //    } 
        //    return this.ranges[this.times.Length - 1];
        //} 
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: covariance not available for timecourses: " + Format(style)); } 
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Not differentiable flow: " + Format(style)); } 
        public override bool HasDeterministicValue() { return true; }
        public override bool HasStochasticMean() { return true; }
        //public override bool LinearCombination() { return true; }
        public override bool LinearCombination() { return false; } // prevents invoking the ObserveVariance
        public override bool HasNullVariance() { return false; }
        public override Flow Differentiate(Symbol var, Style style) { return NumberFlow.numberFlowZero; }
    }

    public class StringFlow : Flow { // this is probably not very useful, but e.g. cond("a"="b",a,b)
        public string value;
        public StringFlow(string value) { this.type = Type.Flow; this.value = value; }
        public override bool EqualFlow(Flow other) { return (other is StringFlow) && ((other as StringFlow).value == this.value); }
        public override bool Precedes(Flow other, Style style) { return (other is StringFlow) && (String.Compare(this.value, (other as StringFlow).value) < 0); }
        public override Flow Normalize(Style style) { return this; }
        public override Flow Expand(Style style) { return this; }
        public override Flow Simplify(Style style) { return this; }
        public override Flow ReGroup(Style style) { return this; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override bool IsOp(string op, int arity) { return false; }
        public override bool IsNumericConstantExpression() { return false; }
        public override Flow Arg(int i) { throw new Error("Arg"); }
        public override string Format(Style style) { return Parser.FormatString(this.value); }

        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: bool expected instead of string: " + Format(style)); }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: number expected instead of string: " + Format(style)); }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of string: " + Format(style)); }
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of string: " + Format(style)); }
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Not differentiable, string: " + Format(style)); }
        // public override double ObserveDiffCovariance //### this could be provided with the LNA flux to compute the derivative of a variance/covariance
        public override bool HasDeterministicValue() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return true; }
        public override Flow Differentiate(Symbol var, Style style) { throw new Error("Non differentiable: string"); }
    }

    public class SpeciesFlow : Flow {
        public Symbol species;
        public SpeciesFlow(Symbol species) { this.type = Type.Flow; this.species = species; }
        public bool SameSpecies(SpeciesFlow other) { return this.species.SameSymbol(other.species); }
        public override bool EqualFlow(Flow other) { return (other is SpeciesFlow) && ((other as SpeciesFlow).species.SameSymbol(this.species)); }
        public override bool Precedes(Flow other, Style style) { return (other is SpeciesFlow) && (species.Precedes((other as SpeciesFlow).species)); }
        public override Flow Normalize(Style style) { return this; }
        public override Flow Expand(Style style) { return this; }
        public override Flow Simplify(Style style) { return this; }
        public override Flow ReGroup(Style style) { return this; }
        public override bool Involves(List<SpeciesValue> species) {
            return species.Exists(s => s.symbol.SameSymbol(this.species));
        }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            foreach (SpeciesValue s in species) {
                if (s.symbol.SameSymbol(this.species)) { notCovered = null; return true; }
            }
            notCovered = this.species;
            return false;
        }
        public override bool IsOp(string op, int arity) { return false; }
        public override bool IsNumericConstantExpression() { return false; }
        public override Flow Arg(int i) { throw new Error("Arg"); }
        public override string Format(Style style) {
            string name = this.species.Format(style);
            if (style.exportTarget == ExportTarget.CRN) name = "[" + name + "]";
            return name;
        }
        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            throw new Error("Flow expression: bool expected instead of species: " + Format(style));
        }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            return state.Mean(sample.stateMap.index[this.species]);
        }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) {
            if (!state.lna) throw new Error("Variance not supported in current state");
            int i = sample.stateMap.index[this.species];
            return state.Covar(i, i);
        }
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) {
            if (!state.lna) throw new Error("Covariance not supported in current state");
            if (other is SpeciesFlow)
                return state.Covar(sample.stateMap.index[this.species], sample.stateMap.index[((SpeciesFlow)other).species]);
            else return other.ObserveCovariance(this, sample, time, state, style);
        }
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            if (flux == null) throw new Error("Non differentiable: " + species.Format(style));
            return flux(time, state.ToArray())[sample.stateMap.index[this.species]];   //### to optimize this we should memoize flux(time, state.ToArray()) for the latest time
            // we pass state.ToArray() instead of state.MeanVector() because we want to observe the Mean component also when the lna is on, otherwise we get an error later
        }
        public override bool HasDeterministicValue() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return false; }
        public override Flow Differentiate(Symbol var, Style style) {
            if (var == null) return OpFlow.Op("∂", this);  // time differntiation: a species 'a' is differentiated as '∂a' meaning da/dt
            else if (species.SameSymbol(var)) return NumberFlow.numberFlowOne; // dx/dx = 1
            else return NumberFlow.numberFlowZero; // dx/dy = 0   for y=/=x
        }
    }

    public class SampleFlow : Flow {
        public SampleValue value;
        public SampleFlow(SampleValue value) { this.type = Type.Flow; this.value = value; }
        public override Flow Normalize(Style style) { return this;  }
        public override Flow Expand(Style style) { return this; }
        public override Flow Simplify(Style style) { return this; }
        public override Flow ReGroup(Style style) { return this; }
        public override bool EqualFlow(Flow other) { return (other is SampleFlow) && ((other as SampleFlow).value == this.value); }
        public override bool Precedes(Flow other, Style style) { return (other is SampleFlow) && ((value as SampleValue).symbol.Precedes(((other as SampleFlow).value as SampleValue).symbol)); }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override bool IsOp(string op, int arity) { return false; }
        public override bool IsNumericConstantExpression() { return false; }
        public override Flow Arg(int i) { throw new Error("Arg"); }
        public override string Format(Style style) { return value.FormatSymbol(style); }

        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: bool expected instead of sample: " + Format(style)); }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: number expected instead of sample: " + Format(style)); }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of sample: " + Format(style)); }
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of sample: " + Format(style)); }
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Not differentiable, sample: " + Format(style)); }
        public override bool HasDeterministicValue() { return true; } // can appear in rate-expressions in a cond
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; } // but if it appears in a cond it will not be a linear combination anyway
        public override bool HasNullVariance() { return true; }
        public override Flow Differentiate(Symbol var, Style style) { throw new Error("Non differentiable: sample"); }
    }

    public class ConstantFlow : Flow { // cannot be evaluated (nor simulated), but can be used as a symbolic constant within generalized rates, so it can appear in the extracted differential equations
        public Symbol constant;
        public ConstantFlow(Symbol constant) { this.type = Type.Flow; this.constant = constant; }
        public override bool EqualFlow(Flow other) { return (other is ConstantFlow) && ((other as ConstantFlow).constant.SameSymbol(this.constant)); }
        public override bool Precedes(Flow other, Style style) { return (other is SpeciesFlow) || ((other is ConstantFlow) && (constant.Precedes((other as ConstantFlow).constant))); }
        public override Flow Normalize(Style style) { return this; }
        public override Flow Expand(Style style) { return this; }
        public override Flow Simplify(Style style) { return this; }
        public override Flow ReGroup(Style style) { return this; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override bool IsOp(string op, int arity) { return false; }
        public override bool IsNumericConstantExpression() { return true; }
        public override Flow Arg(int i) { throw new Error("Arg"); }
        public override string Format(Style style) { return this.constant.Format(style); }
        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new Error("Flow expression: bool expected instead of constant: " + Format(style)); }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { throw new ConstantEvaluation(constant.Format(style)); }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) { return 0.0; } // Var(number) = 0
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) { return 0.0; } // Cov(number,Y) = 0
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) { return 0.0; } // ∂n = 0
        public override bool HasDeterministicValue() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return true; }
        public override Flow Differentiate(Symbol var, Style style) { return NumberFlow.numberFlowZero; }
    }

    public class OpFlow : Flow {
        public string op;
        public int arity;
        public bool infix;
        public List<Flow> args;
        private bool hasDeterministicValue;
        private bool hasStochasticMean;
        private bool linearCombination;
        private bool hasNullVariance;

        public OpFlow(string op, bool infix, List<Flow> args) {
            this.type = Type.Flow;
            this.op = op;
            this.arity = args.Count;
            this.infix = infix;
            this.args = args;
            this.hasDeterministicValue = CacheHasDeterministicValue();
            this.hasStochasticMean = CacheHasStochasticMean();
            this.linearCombination = CacheLinearCombination();
            this.hasNullVariance = CacheHasNullVariance();
        }
        public OpFlow(string op, bool infix) : this(op, infix, new List<Flow> {}) { }
        public OpFlow(string op, bool infix, Flow arg) : this(op, infix, new List<Flow> { arg }) { }
        public OpFlow(string op, bool infix, Flow arg1, Flow arg2) : this(op, infix, new List<Flow> { arg1, arg2 }) { }
        public OpFlow(string op, bool infix, Flow arg1, Flow arg2, Flow arg3) : this(op, infix, new List<Flow> { arg1, arg2, arg3 }) { }

        public override bool IsOp(string op, int arity) {
            return op == this.op && arity == this.arity;
        }
        public override Flow Arg(int i) {
            return this.args[i];
        }
        public override bool IsNumericConstantExpression() { 
            if (arity == 0) {
                return false; // "time", "kelvin", "celsius", "volume"
            } else if (arity == 1) {
                return (op == "-") && args[0].IsNumericConstantExpression(); 
            } else if (arity == 2) {
                return (op == "+" || op == "-" || op == "*" || op == "/" || op == "^") && args[0].IsNumericConstantExpression() && args[1].IsNumericConstantExpression();
            } else if (arity == 3) { // including "cond"
                return false;
            } else return false;
        }

        public override bool EqualFlow(Flow other) {
            if (!(other is OpFlow)) return false;
            if ((other as OpFlow).op != this.op) return false;
            if ((other as OpFlow).arity != this.arity) return false;
            for (int i = 0; i < args.Count(); i++) {
                if (!(other as OpFlow).args[i].EqualFlow(this.args[i])) return false;
            }
            return true;
        }

        public override bool Precedes(Flow other, Style style) {
            if (!(other is OpFlow)) return false;
            OpFlow otherFlow = other as OpFlow;
            if (String.Compare(this.op, otherFlow.op) < 0) return true;
            for (int i = 0; i < this.arity && i < otherFlow.arity; i++)
                if (this.args[i].Precedes(otherFlow.args[i], style)) return true;
            return this.arity < otherFlow.arity;
        }

        public override Flow Normalize(Style style) { // do not call this recursively, should Expand only once
            return ReGroup(style).Simplify(style);
            //Gui.Log("ENTER Normalize " + this.Format(style.RestyleAsDataFormat("operator")));
            //Gui.Log("ENTER ReGroup " + this.Format(style.RestyleAsDataFormat("operator")));
            //Flow regroup = ReGroup(style);
            //Gui.Log("EXIT ReGroup " + regroup.Format(style.RestyleAsDataFormat("operator")));
            //Gui.Log("ENTER Simplify " + regroup.Format(style.RestyleAsDataFormat("operator")));
            //Flow result = regroup.Simplify(style);
            //Gui.Log("EXIT Simplify " + result.Format(style.RestyleAsDataFormat("operator")));
            //return result;
        }

        public override Flow Simplify(Style style) {
            // do arithmetic simplifications on the expression tree assuming it has already been regrouped
            Flow expand = Expand(style); if (expand != this) return expand.Simplify(style);
            if (op == "+") { // simplify the "+" spine, further down ReGroup has already called Normalize
                Flow arg0 = args[0].IsOp("+", 2) ? args[0].Simplify(style) : args[0];
                Flow arg1 = args[1].IsOp("+", 2) ? args[1].Simplify(style) : args[1];
                if (arg0 is NumberFlow && arg1 is NumberFlow) return new NumberFlow((arg0 as NumberFlow).value + (arg1 as NumberFlow).value); // n + m = n+m
                if (arg0.IsNumber(0.0)) return arg1; // 0 + a = a
                if (arg1.IsNumber(0.0)) return arg0; // a + 0 = a
                if (arg0.EqualFlow(arg1)) { return Op("*", NumberFlow.numberFlowTwo, arg0).Normalize(style); } // a + a = [2 * a]
                if (arg1.IsOp("*", 2) && (arg1.Arg(0) is NumberFlow) && (arg0.EqualFlow(arg1.Arg(1)))) { return Op("*", new NumberFlow((arg1.Arg(0) as NumberFlow).value + 1.0), arg0).Normalize(style); } // a + n*a = [(n+1)*a]
                if (arg0.IsOp("*", 2) && (arg0.Arg(0) is NumberFlow) && (arg1.EqualFlow(arg0.Arg(1)))) { return Op("*", new NumberFlow((arg0.Arg(0) as NumberFlow).value + 1.0), arg1).Normalize(style); } // n*a + a = [(n+1)*a]
                if (arg0.IsOp("*", 2) && (arg0.Arg(0) is NumberFlow) && arg1.IsOp("*", 2) && (arg1.Arg(0) is NumberFlow) && (arg0.Arg(1).EqualFlow(arg1.Arg(1)))) { return Op("*", new NumberFlow((arg0.Arg(0) as NumberFlow).value + (arg1.Arg(0) as NumberFlow).value), arg0.Arg(1)).Normalize(style); } // n*a + m*a = [(n+m)*a]
                if (arg0 == args[0] && arg1 == args[1]) return this; else return Op(op, arg0, arg1);
            } else if (op == "*") { // simplify the "*" spine, further down ReGroup has already called Normalize
                Flow arg0 = args[0].IsOp("*", 2) ? args[0].Simplify(style) : args[0];
                Flow arg1 = args[1].IsOp("*", 2) ? args[1].Simplify(style) : args[1];
                if (arg0 is NumberFlow && arg1 is NumberFlow) return new NumberFlow((arg0 as NumberFlow).value * (arg1 as NumberFlow).value); // n * m = n*m
                if (arg0.IsNumber(0.0)) return NumberFlow.numberFlowZero; // 0 * a = 0
                if (arg1.IsNumber(0.0)) return NumberFlow.numberFlowZero; // a * 0 = 0
                if (arg0.IsNumber(1.0)) return arg1; // 1 * a = a
                if (arg1.IsNumber(1.0)) return arg0; // a * 1 = a
                if (arg0.EqualFlow(arg1)) { return Op("^", arg0, NumberFlow.numberFlowTwo).Normalize(style); } // a * a = [a^2]
                if (arg1.IsOp("^", 2) && (arg0.EqualFlow(arg1.Arg(0)))) { return Op("^", arg0, Op("+", arg1.Arg(1), NumberFlow.numberFlowOne)).Normalize(style); } // a * a^n = [a^(n+1)]
                if (arg0.IsOp("^", 2) && (arg1.EqualFlow(arg0.Arg(0)))) { return Op("^", arg1, Op("+", arg0.Arg(1), NumberFlow.numberFlowOne)).Normalize(style); } // a^n * a = [a^(n+1)]
                if (arg0.IsOp("^", 2) && arg1.IsOp("^", 2) && (arg0.Arg(0).EqualFlow(arg1.Arg(0)))) { return Op("^", arg0.Arg(0), Op("+", arg0.Arg(1), arg1.Arg(1))).Normalize(style); } // a^n * a^m = [a^(n+m)]
                if (arg0 == args[0] && arg1 == args[1]) return this; else return Op(op, arg0, arg1);
            } else if (op == "^") {
                Flow arg0 = args[0].Simplify(style);
                Flow arg1 = args[1].Simplify(style);
                if (arg0.IsNumber(0.0)) return NumberFlow.numberFlowZero; // 0^a = 0  // even if a = -1 ?
                if (arg0.IsNumber(1.0)) return NumberFlow.numberFlowOne;  // 1^a = 1
                if (arg1.IsNumber(0.0)) return NumberFlow.numberFlowOne;  // a^0 = 1
                if (arg1.IsNumber(1.0)) return arg0;                      // a^1 = a
                if (arg0 is NumberFlow && arg1 is NumberFlow) return new NumberFlow(Math.Pow((arg0 as NumberFlow).value, (arg1 as NumberFlow).value));  // n ^ m = n^m
                if (arg0 == args[0] && arg1 == args[1]) return this; else return Op(op, arg0, arg1);
            } else if (op == "log") {
                Flow arg0 = args[0].Simplify(style);
                if (arg0.IsNumber(1.0)) return NumberFlow.numberFlowZero; // log(1) = 0
                if (arg0.IsNumber(Math.E)) return NumberFlow.numberFlowOne; // log(e) = 1
                if (arg0 is NumberFlow) return new NumberFlow(Math.Log((arg0 as NumberFlow).value)); // log( n ) = log(n)
                if (arg0 == args[0]) return this; else return Op(op, arg0);
            } else if (op == "cond") {
                Flow arg0 = args[0].Simplify(style);
                Flow arg1 = args[1].Simplify(style);
                Flow arg2 = args[2].Simplify(style);
                if (arg0 is BoolFlow && (arg0 as BoolFlow).value == true) return arg1; // cond(true,a,b) = a
                if (arg0 is BoolFlow && (arg0 as BoolFlow).value == false) return arg2; // cond(false,a,b) = b
                if (arg0 == args[0] && arg1 == args[1] && arg2 == args[2]) return this; else return Op(op, arg0, arg1, arg2);
            } else {
                List<Flow> sargs = new List<Flow> { };
                bool noChange = true;
                foreach (Flow arg in args) { Flow sarg = arg.Simplify(style); sargs.Add(sarg); noChange = noChange && arg == sarg; }
                if (noChange) return this; else return new OpFlow(op, infix, sargs);
            }
        }

        public override Flow Expand(Style style) {
            // division a/b is replaced by a*b^-1, subtraction a-b is replaced by a+(-1)*b, and minus -a is replaced by (-1)*a
            // distribute sums and products
            if (op == "-" && arity == 1)
                return Op("*", NumberFlow.numberFlowMinusOne, args[0].Expand(style)); // -(a) = (-1)*a
            if (op == "-" && arity == 2)
                return Op("+", args[0].Expand(style), Op("*", NumberFlow.numberFlowMinusOne, args[1].Expand(style))); // a - b = a + (-1)*b
            if (op == "/")
                return Op("*", args[0].Expand(style), Op("^", args[1].Expand(style), NumberFlow.numberFlowMinusOne)); // a / b = a * b^(-1)
            if (op == "*" && args[0].IsOp("+", 2)) {
                Flow arg1 = args[1].Expand(style);
                return Op("+", Op("*", args[0].Arg(0).Expand(style), arg1), Op("*", args[0].Arg(1).Expand(style), arg1)); // (a + b) * c = a * c + b * c
            }
            if (op == "*" && args[1].IsOp("+", 2)) {
                Flow arg0 = args[0].Expand(style);
                return Op("+", Op("*", arg0, args[1].Arg(0).Expand(style)), Op("*", arg0, args[1].Arg(1).Expand(style))); // a * (b + c) = a * b + a * c
            }
            if (op == "^" && args[0].IsOp("*", 2)) {
                Flow arg1 = args[1].Expand(style);
                return Op("*", Op("^", args[0].Arg(0).Expand(style), arg1), Op("^", args[0].Arg(1).Expand(style), arg1)); // (a * b) ^ c = a ^ c * b ^ c
            }
            if (op == "^" && args[0].IsOp("^", 2)) {
                return Op("^", args[0].Arg(0).Expand(style), Op("*", args[0].Arg(1).Expand(style), args[1].Expand(style))); // (a ^ b) ^ c = a ^ (b * c)
            }
            // a ^ (b + c) = a^b * a^c // DO NOT EXPAND: ReGroup does the inverse expansion
            return this;
        }

        public override Flow ReGroup(Style style) {
            // regroup the expression tree so that simplifications can be done simply on adjacent pairs of items
            // e.g. (a * 1 * b * 2 * a^2)  is regrouped as (1 * 2) * ((a * a^2) * b)
            // note that via GroupOperands this recursively calls Normalize on subtrees off the "+" and "*" spines
            Flow expand = Expand(style); if (expand != this) return expand.ReGroup(style);
            if (op == "+" || op == "*") { // treated the same, their differences are handled in GroupOperands
                var numbersGroup = new List<Flow> { };
                var indexedGroups = new List<Tuple<Flow, List<Flow>>> { };
                var othersGroup = new List<Flow> { };
                GroupOperands(numbersGroup, indexedGroups, othersGroup, style);

                Flow numbersTree = null; 
                foreach (Flow item in numbersGroup) numbersTree = (numbersTree == null) ? item : Op(op, numbersTree, item);
                Flow indexedTrees = null; 
                foreach (Tuple<Flow, List<Flow>> pair in indexedGroups) {
                    Flow indexedTree = null;
                    foreach (Flow item in pair.Item2) {
                        indexedTree = (indexedTree == null) ? item : Op(op, indexedTree, item);
                    }
                    indexedTrees = (indexedTrees == null) ? indexedTree : Op(op, indexedTrees, indexedTree);
                }                                   
                Flow othersTree = null;
                foreach (Flow item in othersGroup) othersTree = (othersTree == null) ? item : Op(op, othersTree, item);

                Flow result = null;
                if (numbersTree != null) result = (result == null) ? numbersTree : Op(op, result, numbersTree);
                if (indexedTrees != null) result = (result == null) ? indexedTrees : Op(op, result, indexedTrees);
                if (othersTree != null) result = (result == null) ? othersTree : Op(op, result, othersTree);
                if (result == null) throw new Error("ReGroup");
                return result;
            } else {
                List<Flow> sargs = new List<Flow> { };
                bool noChange = true;
                foreach (Flow arg in args) { Flow sarg = arg.ReGroup(style); sargs.Add(sarg); noChange = noChange && arg == sarg; }
                if (noChange) return this; else return new OpFlow(op, infix, sargs);
            }
        }

        private void GroupOperands(List<Flow> numbersGroup, List<Tuple<Flow, List<Flow>>> indexedGroups, List<Flow> othersGroup, Style style) {
            // numberGroup collects all numerical arguments
            // indexedGroups collects groups of arguments indexed by related factors or summands
            // othersGroup collects all the other arguments
            if (op == "+")
                foreach (Flow arg in args) {
                    if (arg.IsOp("+", 2)) // keep grouping subexpressions on the "+" spine, without first normalizing them (that leads to exponential explosion)
                        (arg as OpFlow).GroupOperands(numbersGroup, indexedGroups, othersGroup, style); 
                    else {
                        Flow narg = arg.Normalize(style); // normalize the subexpressions off the spine
                        if (narg is NumberFlow) numbersGroup.Add(narg); // group numbers
                        else if (narg is SpeciesFlow) GroupAddOperand(narg, narg, indexedGroups, style); // group species  a  by  a
                        else if (narg is ConstantFlow) GroupAddOperand(narg, narg, indexedGroups, style); // group constants  k  by   k
                        else if (narg.IsOp("*", 2) && narg.Arg(0) is NumberFlow) GroupAddOperand(narg.Arg(1), narg, indexedGroups, style); // group  n*a  by  a
                        else if (narg.IsOp("^", 2)) GroupAddOperand(narg, narg, indexedGroups, style); // group  a^b  by  a^b
                        else if (narg.IsOp("*", 2)) GroupAddOperand(narg, narg, indexedGroups, style); // group  a*b  by  a*b
                        else if (narg.IsOp("+", 2)) (narg as OpFlow).GroupOperands(numbersGroup, indexedGroups, othersGroup, style); // keep grouping subexpressions
                        else othersGroup.Add(narg); // group others
                    }
                }
            else if (op == "*")
                foreach (Flow arg in args) {
                    if (arg.IsOp("*", 2)) // keep grouping subexpressions on the "*" spine, without first normalizing them (that leads to exponential explosion)
                        (arg as OpFlow).GroupOperands(numbersGroup, indexedGroups, othersGroup, style);
                    else {
                        Flow narg = arg.Normalize(style); // normalize the subexpressions off the spine
                        if (narg is NumberFlow) numbersGroup.Add(narg); // group numbers
                        else if (narg is SpeciesFlow) GroupAddOperand(narg, narg, indexedGroups, style); // group species  a  by  a
                        else if (narg is ConstantFlow) GroupAddOperand(narg, narg, indexedGroups, style); // group constants  k  by   k
                        else if (narg.IsOp("^", 2) && narg.Arg(0) is SpeciesFlow) GroupAddOperand(narg.Arg(0), narg, indexedGroups, style); // group  a^b  by  a
                        else if (narg.IsOp("*", 2)) (narg as OpFlow).GroupOperands(numbersGroup, indexedGroups, othersGroup, style); // keep grouping subexpressions
                        else othersGroup.Add(narg); // group others
                    }
                }
            else throw new Error("GroupOperands");
        }

        private static void GroupAddOperand(Flow index, Flow flow, List<Tuple<Flow, List<Flow>>> indexedGroups, Style style) {
            bool found = false; // insert in existing group
            foreach (Tuple<Flow, List<Flow>> speciesGroup in indexedGroups) {
                if (index.EqualFlow(speciesGroup.Item1)) { found = true; speciesGroup.Item2.Add(flow); break; }
            }
            if (!found) { // insert new group lexicographically
                bool inserted = false;
                for (int i = 0; i < indexedGroups.Count(); i++) {
                    Tuple<Flow, List<Flow>> indexedGroup = indexedGroups[i];
                    if (index.Precedes(indexedGroup.Item1, style)) {
                        inserted = true;
                        indexedGroups.Insert(i, new Tuple<Flow, List<Flow>>(index, new List<Flow> { flow }));
                        break;
                    }
                }
                if (!inserted) indexedGroups.Insert(indexedGroups.Count(), new Tuple<Flow, List<Flow>>(index, new List<Flow> { flow }));
            }
        }

        public static Flow Op(string op) {
            return new OpFlow(op, false);
        }
        public static Flow Op(string op, Flow arg) {
            if (op == "-") return Minus(arg);
            if (op == "log") return Log(arg);
            if (op == "∂" || op == "not") return new OpFlow(op, true, arg);
            return new OpFlow(op, false, arg);
        }
        public static Flow Op(string op, Flow arg1, Flow arg2) {
            if (op == "+") return Plus(arg1, arg2);
            if (op == "-") return Minus(arg1, arg2);
            if (op == "*") return Mult(arg1, arg2);
            if (op == "/") return Div(arg1, arg2);
            if (op == "^") return Pow(arg1, arg2);
            if (op == "or" || op == "and" || op == "=" || op == "<>" || op == "<=" || op == "<" || op == ">=" || op == ">" || op == "++") return new OpFlow(op, true, arg1, arg2);
            return new OpFlow(op, false, arg1, arg2);
        }
        public static Flow Op(Flow arg1, string op, Flow arg2) {
            if (op == "+") return Plus(arg1, arg2);
            if (op == "-") return Minus(arg1, arg2);
            if (op == "*") return Mult(arg1, arg2);
            if (op == "/") return Div(arg1, arg2);
            if (op == "^") return Pow(arg1, arg2);
            if (op == "or" || op == "and" || op == "=" || op == "<>" || op == "<=" || op == "<" || op == ">=" || op == ">" || op == "++") return new OpFlow(op, true, arg1, arg2);
            return new OpFlow(op, false, arg1, arg2);
        }
        public static Flow Op(string op, Flow arg1, Flow arg2, Flow arg3) {
            if (op == "cond") return Cond(arg1, arg2, arg3);
            return new OpFlow(op, false, arg1, arg2, arg3);
        }
        private static Flow Plus(Flow arg1, Flow arg2) {
            if (arg1.IsNumber(0.0)) return arg2; // 0 + a = a
            if (arg2.IsNumber(0.0)) return arg1; // a + 0 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value + (arg2 as NumberFlow).value); // n + m = n+m
            return new OpFlow("+", true, arg1, arg2);
        }
        private static Flow Minus(Flow arg) {
            if (arg is NumberFlow) return new NumberFlow(-(arg as NumberFlow).value); // -(n) = -n
            if (arg.IsOp("-", 1)) return arg.Arg(0); // -(-a) = a
            return new OpFlow("-", true, arg);
        }
        private static Flow Minus(Flow arg1, Flow arg2) {
            if (arg1.IsNumber(0.0)) return Minus(arg2); // 0 - a = -a
            if (arg2.IsNumber(0.0)) return arg1; // a - 0 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value - (arg2 as NumberFlow).value); // n - m = n-m
            return new OpFlow("-", true, arg1, arg2);
        }
        private static Flow Mult(Flow arg1, Flow arg2) {
            if (arg1.IsNumber(0.0)) return NumberFlow.numberFlowZero; // 0 * a = 0
            if (arg2.IsNumber(0.0)) return NumberFlow.numberFlowZero; // a * 0 = 0
            if (arg1.IsNumber(1.0)) return arg2; // 1 * a = a
            if (arg2.IsNumber(1.0)) return arg1; // a * 1 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value * (arg2 as NumberFlow).value); // n * m = n*m
            return new OpFlow("*", true, arg1, arg2);
        }
        private static Flow Div(Flow arg1, Flow arg2) {
            if (arg1.IsNumber(0.0)) { return NumberFlow.numberFlowZero; } // 0 / a = 0
            if (arg2.IsNumber(1.0)) { return arg1; } // a / 1 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value / (arg2 as NumberFlow).value); // n / m = n/m  including +/-infinity or NaN if arg2=0
            if (arg2.EqualFlow(arg1)) { return new NumberFlow(1.0); } // a / a = 1
            return new OpFlow("/", true, arg1, arg2);
        }
        private static Flow Pow(Flow arg1, Flow arg2) {
            if (arg2.IsNumber(0.0)) return NumberFlow.numberFlowOne; // a^0 = 1
            if (arg1.IsNumber(0.0)) return arg1; // 0^a = 0
            if (arg1.IsNumber(1.0)) return arg1; // 1^a = 1
            if (arg2.IsNumber(1.0)) return arg1; // a^1 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow(Math.Pow((arg1 as NumberFlow).value, (arg2 as NumberFlow).value)); // n ^ m = n^m
            return new OpFlow("^", true, arg1, arg2);
        }
        private static Flow Log(Flow arg) {
            if (arg is NumberFlow && (arg as NumberFlow).value == 1.0) return NumberFlow.numberFlowZero; // log(1) = 0
            if (arg is NumberFlow && (arg as NumberFlow).value == Math.E) return NumberFlow.numberFlowOne; // log(e) = 1
            if (arg is NumberFlow) return new NumberFlow(Math.Log((arg as NumberFlow).value)); // log( n ) = log(n)
            return new OpFlow("log", false, arg);
        }
        private static Flow Cond(Flow arg1, Flow arg2, Flow arg3) {
            if (arg1 is BoolFlow && (arg1 as BoolFlow).value == true) return arg2; // cond(true,a,b) = a
            if (arg1 is BoolFlow && (arg1 as BoolFlow).value == false) return arg3; // cond(false,a,b) = b
            return new OpFlow("cond", false, new List<Flow> { arg1, arg2, arg3 });
        }

        public override bool Involves(List<SpeciesValue> species) {
            if (this.arity == 0) return false;
            else return args.Exists(x => x.Involves(species));
        }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            if (this.arity == 0) { notCovered = null; return true; }
            else {
                foreach (Flow arg in args) { if (!arg.CoveredBy(species, out notCovered)) return false; }
                notCovered = null;
                return true;
            }
        }
        public override string Format(Style style) {
            if (style.dataFormat == "operator") { // raw unabbreviated format
                string s = op + "("; 
                for (int i = 0; i < args.Count()-1; i++) s += args[i].Format(style) + ", ";
                if (args.Count() > 0) s += args[args.Count()-1].Format(style);
                return s + ")";
            }
            if (this.arity == 0) return op;
            else if (arity == 1) {
                if (this.infix) {
                    string arg1 = SubFormatRight(this, args[0], style); // use parens depending on precedence of suboperator
                    return "(" + op + " " + arg1 + ")";
                } else return op + "(" + args[0].TopFormat(style) + ")";
            } else if (arity == 2) {
                if (this.infix) {
                    // improve presentation of unary and binary minus
                    if (op == "*" && args[0] is NumberFlow && (args[0] as NumberFlow).value == -1.0) return Op("-", args[1]).Format(style); // -1*a = -a
                    if (op == "-" && arity == 2 && args[1].IsOp("*", 2) && args[1].Arg(0) is NumberFlow && (args[1].Arg(0) as NumberFlow).value == -1.0) return Op("+", args[0], args[1].Arg(1)).Format(style); // a - -1*b = a + b
                    if (op == "+" && args[1].IsOp("+", 2) && args[1].Arg(0).IsOp("*",2) && args[1].Arg(0).Arg(0).IsNegative()) return Op("+", Op("+", args[0], args[1].Arg(0)), args[1].Arg(1)).Format(style); // a + (-n*b + c) = (a + -n*b) + c
                    if (op == "+" && args[1].IsOp("*",2) && args[1].Arg(0).IsNegative()) return Op("-", args[0], Op("*", new NumberFlow(-(args[1].Arg(0) as NumberFlow).value), args[1].Arg(1))).Format(style); // a + -n * b = a - n*b
                    if (op == "+" && args[0].IsOp("*", 2) && args[0].Arg(0).IsNegative()) return Op("-", args[1], Op("*", new NumberFlow(-(args[0].Arg(0) as NumberFlow).value), args[0].Arg(1))).Format(style); // -n * b + a = a - n*b
                    // end
                    string arg1 = SubFormatLeft(this, args[0], style); // use parens depending on precedence of suboperator
                    string arg2 = SubFormatRight(this, args[1], style); // use parens depending on precedence of suboperator
                    if (style.exportTarget == ExportTarget.LBS && op == "-") return arg1 + " -- " + arg2; // export exceptions
                    if (style.exportTarget == ExportTarget.LBS) return arg1 + " " + op + " " + arg2;      // export exceptions
                    return "(" + arg1 + " " + (op == "*" ? "·" : op) + " " + arg2 + ")";
                } else {
                    string arg1 = args[0].TopFormat(style);
                    string arg2 = args[1].TopFormat(style);
                    if (op == "cov" && arg1 == arg2) return "var" + "(" + arg1 + ")";
                    else return op + "(" + arg1 + ", " + arg2 + ")";
                }
            } else if (arity == 3) {
                string arg1 = args[0].TopFormat(style);
                string arg2 = args[1].TopFormat(style);
                string arg3 = args[2].TopFormat(style);
                return op + "(" + arg1 + ", " + arg2 + ", " + arg3 + ")";
            } else throw new Error("ReportValueOp.Format");
        }

        private static string SubFormatLeft(Flow supOp, Flow subOp, Style style) {
            // in order of precedence strength
            if (supOp.IsOp("+", 2)   &&   (subOp.IsOp("+", 2) || subOp.IsOp("-", 2) || subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("-", 2)   &&   (subOp.IsOp("+", 2) || subOp.IsOp("-", 2) || subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("*", 2)   &&   (subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("/", 2)   &&   (subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("^", 2)   &&   (subOp.IsOp("-", 1))) return subOp.TopFormat(style);
            return subOp.Format(style);
        }

        private static string SubFormatRight(Flow supOp, Flow subOp, Style style) {
            // in order of precedence strength
            if (supOp.IsOp("+", 2)   &&   (subOp.IsOp("+", 2) || subOp.IsOp("-", 2) || subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("-", 2)   &&   (subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("*", 2)   &&   (subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("/", 2)   &&   (subOp.IsOp("-", 1) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("-", 1)   &&   (subOp.IsOp("-", 1) || subOp.IsOp("*", 2) || subOp.IsOp("/", 2) || subOp.IsOp("^", 2))) return subOp.TopFormat(style);
            if (supOp.IsOp("^", 2)   &&   (subOp.IsOp("-", 1))) return subOp.TopFormat(style);
            return subOp.Format(style);
        }

        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            string BadArgs() { return "Flow expression: Not acceptable: '" + op + "'"; }
            string BadResult() { return "Flow expression: boolean operator expected instead of '" + op + "'"; }
            if (arity == 0) {
                throw new Error(BadResult());
            } else if (arity == 1) {
                bool arg1 = args[0].ObserveBool(sample, time, state, flux, style);
                if (op == "not") return !arg1;
                else throw new Error(BadResult());
            } else if (arity == 2) {
                if (op == "or") return args[0].ObserveBool(sample, time, state, flux, style) || args[1].ObserveBool(sample, time, state, flux, style);
                else if (op == "and") return args[0].ObserveBool(sample, time, state, flux, style) && args[1].ObserveBool(sample, time, state, flux, style);
                else if (op == "<=") return args[0].ObserveMean(sample, time, state, flux, style) <= args[1].ObserveMean(sample, time, state, flux, style);
                else if (op == "<") return args[0].ObserveMean(sample, time, state, flux, style) < args[1].ObserveMean(sample, time, state, flux, style);
                else if (op == ">=") return args[0].ObserveMean(sample, time, state, flux, style) >= args[1].ObserveMean(sample, time, state, flux, style);
                else if (op == ">") return args[0].ObserveMean(sample, time, state, flux, style) > args[1].ObserveMean(sample, time, state, flux, style);
                else if (op == "=") {
                    if (args[0] is BoolFlow && args[1] is BoolFlow) return ((BoolFlow)args[0]).value == ((BoolFlow)args[1]).value;
                    else if (args[0] is StringFlow && args[1] is StringFlow) return ((StringFlow)args[0]).value == ((StringFlow)args[1]).value;
                    else return NumberValue.EqualDouble(args[0].ObserveMean(sample, time, state, flux, style), args[1].ObserveMean(sample, time, state, flux, style));
                } else if (op == "<>") {
                    if (args[0] is BoolFlow && args[1] is BoolFlow) return ((BoolFlow)args[0]).value != ((BoolFlow)args[1]).value;
                    else if (args[0] is StringFlow && args[1] is StringFlow) return ((StringFlow)args[0]).value != ((StringFlow)args[1]).value;
                    else return !NumberValue.EqualDouble(args[0].ObserveMean(sample, time, state, flux, style), args[1].ObserveMean(sample, time, state, flux, style));
                } else throw new Error(BadResult());
            } else if (arity == 3) {
                if (op == "cond") {
                    if (args[0].ObserveBool(sample, time, state, flux, style))
                        return args[1].ObserveBool(sample, time, state, flux, style);
                    else return args[2].ObserveBool(sample, time, state, flux, style);
                } else throw new Error(BadResult());
            } else throw new Error(BadArgs());
        }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            string BadArgs() { return "Flow expression: Not acceptable: '" + op + "'"; }
            string BadResult() { return "Flow expression: numerical operator expected instead of '" + op + "'"; }
            if (arity == 0) {
                if (op == "time") return time;
                else if (op == "kelvin") return sample.Temperature();
                else if (op == "celsius") return sample.Temperature() - 273.15;
                else if (op == "volume") return sample.Volume();
                else throw new Error(BadResult());
            } else if (arity == 1) {
                if (op == "var") {
                    return args[0].ObserveVariance(sample, time, state, style);              // Mean(var(X)) = var(X)  since var(X) is a number
                } else if (op == "∂") {
                    return args[0].ObserveDiff(sample, time, state, flux, style);
                } else {
                    double arg1 = args[0].ObserveMean(sample, time, state, flux, style);
//                    if (op == "asflow") return arg1;  else 
                    if (op == "poisson") return arg1;                                  // Mean(poisson(X)) = X
                    else if (op == "-") return -arg1;
                    else if (op == "abs") return Math.Abs(arg1);
                    else if (op == "arccos") return Math.Acos(arg1);
                    else if (op == "arcsin") return Math.Asin(arg1);
                    else if (op == "arctan") return Math.Atan(arg1);
                    else if (op == "ceiling") return Math.Ceiling(arg1);
                    else if (op == "cos") return Math.Cos(arg1);
                    else if (op == "cosh") return Math.Cosh(arg1);
                    else if (op == "exp") return Math.Exp(arg1);
                    else if (op == "floor") return Math.Floor(arg1);
                    else if (op == "int") return Math.Round(arg1);
                    else if (op == "log") return Math.Log(arg1);
                    else if (op == "pos") return (double.IsNaN(arg1) || (arg1 < 0)) ? 0 : arg1;
                    else if (op == "sign") return Math.Sign(arg1);
                    else if (op == "sin") return Math.Sin(arg1);
                    else if (op == "sinh") return Math.Sinh(arg1);
                    else if (op == "sqrt") return Math.Sqrt(arg1);
                    else if (op == "tan") return Math.Tan(arg1);
                    else if (op == "tanh") return Math.Tanh(arg1);
                    else throw new Error(BadResult());
                }
            } else if (arity == 2) {
                if (op == "cov") {
                    return args[0].ObserveCovariance(args[1], sample, time, state, style);        // Mean(cov(X,Y)) = cov(X,Y)   since cov(X,Y) is a number
                } else if (op == "gauss") {
                    return args[0].ObserveMean(sample, time, state, flux, style);                 // Mean(gauss(X,Y)) = X
                } else if (op == "observe") {
                    if (!(args[1] is SampleFlow)) throw new Error(BadArgs());
                    return args[0].ObserveMean((args[1] as SampleFlow).value, time, state, flux, style);  // observe mean in other sample
                } else {
                    double arg1 = args[0].ObserveMean(sample, time, state, flux, style);
                    double arg2 = args[1].ObserveMean(sample, time, state, flux, style);
                    if (op == "+") return arg1 + arg2;
                    else if (op == "-") return arg1 - arg2;
                    else if (op == "*") return arg1 * arg2;
                    else if (op == "/") return arg1 / arg2;
                    else if (op == "^") return Math.Pow(arg1, arg2);
                    else if (op == "arctan2") return Math.Atan2(arg1, arg2);
                    else if (op == "min") return Math.Min(arg1, arg2);
                    else if (op == "max") return Math.Max(arg1, arg2);
                    else throw new Error(BadResult());
                }
            } else if (arity == 3) {
                if (op == "cond") {
                    if (args[0].ObserveBool(sample, time, state, flux, style))
                        return args[1].ObserveMean(sample, time, state, flux, style);
                    else return args[2].ObserveMean(sample, time, state, flux, style);
                } else throw new Error(BadResult());
            } else throw new Error(BadArgs());
        }
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            const string Bad = "Non differentiable: ";
            if (arity == 0) {
                if (op == "time") // ∂time = 1.0
                    return 1.0; 
                else return 0.0; // "pi, "e", "kelvin", "celsius", "volume" // ∂k = 0.0
            } else if (arity == 1) {
                if (op == "-") // ∂-f(time) = -∂f(time)
                    return -args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "exp") // ∂(e^f(time)) = e^f(time) * ∂f(time)
                    return this.ObserveMean(sample, time, state, flux, style) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "log") // ∂ln(f(time)) = 1/time * ∂f(time), for time > 0
                    return 1.0/time * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "sqrt") // ∂sqrt(f(time)) = 1/(2*sqrt(f(time))) * ∂f(time)
                    return (1/(2*Math.Sqrt(args[0].ObserveMean(sample, time, state, flux, style)))) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "sign") // ∂sign(f(time)) = 0
                    return 0.0;
                else if (op == "abs") // ∂abs(f(time)) = sign(f(time))) * ∂f(time)
                    return Math.Sign(args[0].ObserveMean(sample, time, state, flux, style)) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "sin") // ∂sin(f(time)) = cos(f(time)) * ∂f(time);   e.g. ∂sin(s) = cos(s)*∂s for a species s
                    return Math.Cos(args[0].ObserveMean(sample, time, state, flux, style)) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "cos") // ∂cos(f(time)) = -sin(f(time)) * ∂f(time)
                    return -Math.Sin(args[0].ObserveMean(sample, time, state, flux, style)) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "tan") // ∂tan(f(time)) = 1/cos(f(time))^2 * ∂f(time)
                    return (1/Math.Pow(Math.Cos(args[0].ObserveMean(sample, time, state, flux, style)), 2.0)) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "sinh") // ∂sinh(f(time)) = cosh(f(time)) * ∂f(time)
                    return Math.Cosh(args[0].ObserveMean(sample, time, state, flux, style)) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "cosh") // ∂cosh(f(time)) = sinh(f(time)) * ∂f(time)
                    return Math.Sinh(args[0].ObserveMean(sample, time, state, flux, style)) * args[0].ObserveDiff(sample, time, state, flux, style);
                else if (op == "tanh") // ∂tanh(f(time)) = (1-tanh(f(time))^2) * ∂f(time)
                    return (1 - Math.Pow(Math.Tanh(args[0].ObserveMean(sample, time, state, flux, style)), 2.0)) * args[0].ObserveDiff(sample, time, state, flux, style);
                // ### etc.
                else throw new Error(Bad + this.Format(style)); // "var", "poisson", "∂" cannot support second derivative
            } else if (arity == 2) {
                if (op == "+") // ∂(f(time)+g(time)) = ∂f(time)+∂g(time)
                    return args[0].ObserveDiff(sample, time, state, flux, style) + args[1].ObserveDiff(sample, time, state, flux, style);
                else if (op == "-") // ∂(f(time)-g(time)) = ∂f(time)-∂g(time)
                    return args[0].ObserveDiff(sample, time, state, flux, style) - args[1].ObserveDiff(sample, time, state, flux, style);
                else if (op == "*") // ∂(f(time)*g(time)) = ∂f(time)*g(time) + f(time)*∂g(time)
                    return
                        args[0].ObserveDiff(sample, time, state, flux, style) * args[1].ObserveMean(sample, time, state, flux, style) +
                        args[0].ObserveMean(sample, time, state, flux, style) * args[1].ObserveDiff(sample, time, state, flux, style);
                else if (op == "/") { // ∂(f(time)/g(time)) = (∂f(time)*g(time) - f(time)*∂g(time)) / g(time)^2
                    double arg0 = args[0].ObserveMean(sample, time, state, flux, style);
                    double arg1 = args[1].ObserveMean(sample, time, state, flux, style);
                    return
                        (args[0].ObserveDiff(sample, time, state, flux, style) * arg1 -
                         arg0 * args[1].ObserveDiff(sample, time, state, flux, style))
                        / (arg1 * arg1);
                } else if (op == "^") {
                    if (args[0] is NumberFlow && (args[0] as NumberFlow).value == Math.E) { // ∂(e^f(time)) = e^f(time) * ∂f(time)  // special case if base is e
                        return this.ObserveMean(sample, time, state, flux, style) * args[1].ObserveDiff(sample, time, state, flux, style);
                    } else if (args[1] is NumberFlow) { // ∂(f(time)^n) = n*(f(time)^(n-1))*∂f(time) // special case if exponent is constant
                        double power = (args[1] as NumberFlow).value;
                        return power * Math.Pow(args[0].ObserveMean(sample, time, state, flux, style), power-1) 
                            * args[0].ObserveDiff(sample, time, state, flux, style);
                    } else { // ∂(f(time)^g(time)) = g(time)*(f(time)^(g(time)-1))*∂f(time) + (f(time)^g(time))*ln(f(time))*∂g(time)
                             //   = (f(time)^(g(time)-1)) * (g(time)*∂f(time) + f(time)*ln(f(time))*∂g(time))
                        double arg0 = args[0].ObserveMean(sample, time, state, flux, style);
                        double arg1 = args[1].ObserveMean(sample, time, state, flux, style);
                        return Math.Pow(arg0, arg1 - 1.0) *
                            (arg1 * args[0].ObserveDiff(sample, time, state, flux, style) +
                             arg0 * Math.Log(arg0) * args[1].ObserveDiff(sample, time, state, flux, style));
                    };
                } else throw new Error(Bad + this.Format(style));
            } else if (arity == 3) {
                if (op == "cond") {
                    bool arg0 = args[0].ObserveBool(sample, time, state, flux, style);
                    if (arg0) return args[1].ObserveDiff(sample, time, state, flux, style);
                    else return args[2].ObserveDiff(sample, time, state, flux, style);
                } else  throw new Error(Bad + op);
            } else throw new Error(Bad + op);
        }
        public override bool HasDeterministicValue() {
            return this.hasDeterministicValue;
        }
        private bool CacheHasDeterministicValue() {
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius", "volume"
            } else if (arity == 1) {
                return (op != "var" && op != "poisson") && args[0].HasDeterministicValue();  // includes ∂
                // Although var(X) is a number, we need the LNA info to compute it, so we say it is not deterministic
                // poisson is not allowed in determinstic plots or general rates
            } else if (arity == 2) {
                return (op != "cov" && op != "gauss") && args[0].HasDeterministicValue() && args[1].HasDeterministicValue();
                // Although cov(X,Y) is a number, we need the LNA info to compute it, so we say it is not deterministic
                // gauss is not allowed in determinstic plots or general rates
            } else if (arity == 3) { // including "cond"
                return args[0].HasDeterministicValue() && args[1].HasDeterministicValue() && args[2].HasDeterministicValue();
            } else throw new Error("HasDeterministicValue: " + op);
        }
        public override bool HasStochasticMean() {
            return this.hasStochasticMean;
        }
        private bool CacheHasStochasticMean() {
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius", "volume"
            } else if (arity == 1) { 
                if (op == "∂") return true; // so can appear as mean in lna plots
                else if (op == "var") return args[0].LinearCombination();
                else return args[0].HasStochasticMean();  // including "poisson"
            } else if (arity == 2) { 
                if (op == "cov") return args[0].LinearCombination() && args[1].LinearCombination();
                else return args[0].HasStochasticMean() && args[1].HasStochasticMean();  // including "gauss"
            } else if (arity == 3) { // including "cond"  // should "cond" require HasDeterministicMean in args[0] ??
                return args[0].HasStochasticMean() && args[1].HasStochasticMean() && args[2].HasStochasticMean();
            } else throw new Error("HasStochasticMean: " + op);
        }
        public override bool LinearCombination() { // returns true for linear combinations of species and zero-variance flows (time, kelvin, celsius, volume)
            return this.linearCombination;
        }
        private bool CacheLinearCombination() {
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius", "volume"
            } else if (arity == 1) {                                     // exclude (op == "var" || op == "∂" )
                if (op == "-" || op == "poisson") return args[0].LinearCombination();
                else return false;
            } else if (arity == 2) {                                     // exclude (op == "cov")
                if (op == "+" || op == "-" || op == "gauss") return args[0].LinearCombination() && args[1].LinearCombination();
                else if (op == "*") return args[0].LinearCombination() && args[1].LinearCombination() && (args[0].HasNullVariance() || args[1].HasNullVariance());
                else return false;
            } else if (arity == 3) {
                if (op == "cond") return args[1].LinearCombination() && args[2].LinearCombination();
                else throw new Error("LinearCombination: " + op);
            } else throw new Error("LinearCombination: " + op);
        }
        public override bool HasNullVariance() {
            return this.hasNullVariance;
        }
        private bool CacheHasNullVariance() {
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius", "volume"
            } else if (arity == 1) {
                if (op == "var") return true;                                    // var(X) is a number so it has a zero variance
                else if (op == "poisson") return false;
                else return args[0].HasNullVariance();  // including "∂"
            } else if (arity == 2) {
                if (op == "cov") return true;                                    // cov(X,Y) is a number so it has a zero variance
                else if (op == "gauss") return false;
                else return args[0].HasNullVariance() && args[1].HasNullVariance();
            } else if (arity == 3) {
                if (op == "cond") return args[0].HasNullVariance() && args[1].HasNullVariance() && args[2].HasNullVariance();
                else throw new Error("HasNullVariance: " + op);
            } else throw new Error("HasNullVariance: " + op);
        }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) {
            string BadArgs() { return "Flow expression: Not acceptable: '" + op + "'"; }
            string BadResult() { return "Flow expression: Variance invalid for operator '" + op + "'"; }
            Func<double, Vector, Vector> flux = null; // disallow "∂", we can't allow "∂(var(a))", but we could build in "∂var(a)" evaluated via flux.CovarMatrix
            if (arity == 0) {
                return 0.0; // "time", "kelvin", "celsius", "volume"                                       // Var(constant) = 0
            } else if (arity == 1) {
                if (op == "var") {
                    return 0.0;      // yes needed for e.g. "report a + var(a)"                            // Var(var(X)) = 0 since var(X) is a number
                } else if (op == "poisson") {
                    return Math.Abs(args[0].ObserveMean(sample, time, state, flux, style));                 // Var(poisson(X)) = Abs(mean(X))
                //} else if (op == "asflow") {
                //    return args[0].ObserveVariance(sample, time, state, style);
                } else if (op == "-") {
                    return args[0].ObserveVariance(sample, time, state, style);                             // Var(-X) = Var(X)
                } else throw new Error(BadResult()); // all other arithmetic operators and "∂": we only handle linear combinations
            } else if (arity == 2) {
                if (op == "cov") {
                    return 0.0;      // yes needed                                                          // Var(cov(X,Y)) = 0 since cov(X,Y) is a number
                } else if (op == "gauss") {
                    return Math.Abs(args[1].ObserveMean(sample, time, state, flux, style));                 // Var(gauss(X,Y)) = Abs(mean(Y))
                } else if (op == "+") {                                                                     // Var(X+Y) = Var(X) + Var(Y) + 2*Cov(X,Y)
                    double arg1 = args[0].ObserveVariance(sample, time, state, style);
                    double arg2 = args[1].ObserveVariance(sample, time, state, style);
                    return arg1 + arg2 + 2 * args[0].ObserveCovariance(args[1], sample, time, state, style);
                } else if (op == "-") {                                                                 // Var(X-Y) = Var(X) + Var(Y) - 2*Cov(X,Y)
                    double arg1 = args[0].ObserveVariance(sample, time, state, style);
                    double arg2 = args[1].ObserveVariance(sample, time, state, style);
                    return arg1 + arg2 - 2 * args[0].ObserveCovariance(args[1], sample, time, state, style);
                } else if (op == "*") {
                    if (args[0].HasNullVariance() && args[1].HasNullVariance())
                        return 0.0;
                    else if (args[0].HasNullVariance() && (!args[1].HasNullVariance())) {                 // Var(n*X) = n^2*Var(X)
                        double arg1 = args[0].ObserveMean(sample, time, state, flux, style);
                        double arg2 = args[1].ObserveVariance(sample, time, state, style);
                        return arg1 * arg1 * arg2;
                    } else if ((!args[0].HasNullVariance()) && args[1].HasNullVariance()) {                // Var(X*n) = Var(X)*n^2
                        double arg1 = args[0].ObserveVariance(sample, time, state, style);
                        double arg2 = args[1].ObserveMean(sample, time, state, flux, style);
                        return arg1 * arg2 * arg2;
                    } else throw new Error(BadResult()); // this will be prevented by checking ahead of time
                } else throw new Error(BadResult()); // all other operators, including "/" , "^" , "arctan2" , "min" , "max"
            } else if (arity == 3) {
                if (op == "cond") {
                    if (args[0].ObserveBool(sample, time, state, flux, style))
                        return args[1].ObserveVariance(sample, time, state, style);
                    else return args[2].ObserveVariance(sample, time, state, style);
                } else throw new Error(BadResult());
            } else throw new Error(BadArgs());
        }
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) {
            string BadArgs() { return "Flow expression: Not acceptable: '" + op + "'"; }
            string BadResult() { return "Flow expression: Covariance invalid for operator '" + op + "'"; }
            Func<double, Vector, Vector> flux = null; // disallow "∂", we can't allow "∂(cov(a,b))", but we could build in "∂cov(a,b)" evaluated via flux.CovarMatrix
            if (arity == 0) {
                return 0.0; // "time", "kelvin", "celsius", "volume"                                         // Cov(number,Y) = 0
            } else if (arity == 1) {
                //if (op == "asflow") {
                //    return args[0].ObserveCovariance(other, sample, time, state, style);
                //} else 
                if (op == "var") {
                    return 0.0;      // yes needed for e.g. "report a + cov(a,a)"                            // Cov(var(X),Z) = 0 since var(X) is a number
                } else if (op == "poisson")
                    return 0.0;                                                                              // Cov(poisson(X),Y) = 0  (even if Y = poisson(X)!)
                else if (op == "-")
                    return 0.0 - args[0].ObserveCovariance(other, sample, time, state, style);                // Cov(-X,Y) = -Cov(X,Y)
                else throw new Error(BadResult()); // all other arithmetic operators and "∂": we only handle linear combinations
            } else if (arity == 2) {
                if (op == "cov") {
                    return 0.0;      // yes needed                                                           // Cov(cov(X,Y),Z) = 0 since cov(X,Y) is a number
                } else if (op == "gauss") {                                                                  // Cov(gauss(X,Y),Z) = 0   (even if Z = gauss(X,Y)!)
                    return 0.0;
                } else if (op == "+") {                                                                      // Cov(X+Z,Y) = Cov(X,Y) + Cov(Z,Y)
                    return args[0].ObserveCovariance(other, sample, time, state, style) +
                        +args[1].ObserveCovariance(other, sample, time, state, style);
                } else if (op == "-") {                                                                       // Cov(X-Z,Y) = Cov(X,Y) - Cov(Z,Y)
                    return args[0].ObserveCovariance(other, sample, time, state, style) +
                        -args[1].ObserveCovariance(other, sample, time, state, style);
                } else if (op == "*") {
                    if (args[0].HasNullVariance() && args[1].HasNullVariance())                               // Cov(n*m,Y) = 0
                        return 0.0;
                    else if (args[0].HasNullVariance() && (!args[1].HasNullVariance())) {                      // Cov(n*X,Y) = n*Cov(X,Y) 
                        return args[0].ObserveMean(sample, time, state, flux, style) *
                            args[1].ObserveCovariance(other, sample, time, state, style);
                    } else if ((!args[0].HasNullVariance()) && args[1].HasNullVariance()) {                    // Cov(X*n,Y) = Cov(X,Y)*n
                        return args[0].ObserveCovariance(other, sample, time, state, style) *
                            args[1].ObserveMean(sample, time, state, flux, style);
                    } else throw new Error(BadResult()); // all other operators, including "/" , "^" , "arctan2" , "min" , "max"
                } else throw new Error(BadResult());
            } else if (arity == 3) {
                if (op == "cond") {
                    if (args[0].ObserveBool(sample, time, state, flux, style))
                        return args[1].ObserveCovariance(other, sample, time, state, style);
                    else return args[2].ObserveCovariance(other, sample, time, state, style);
                } else throw new Error(BadResult());
            } else throw new Error(BadArgs());
        }
        public override Flow Differentiate(Symbol var, Style style) { // symbolic differentiation w.r.t. "time" (if var = null) or a variable var. 
            const string Bad = "Non differentiable: ";
            if (arity == 0) {
                if (var == null && op == "time") // ∂time = 1.0
                    return NumberFlow.numberFlowOne;
                else return NumberFlow.numberFlowZero; // "pi", "e", "kelvin", "celsius", "volume" // ∂k = 0.0
            } else if (arity == 1) {
                if (op == "-") // ∂-f(time) = -∂f(time)
                    return Minus(args[0].Differentiate(var, style));
                else if (op == "exp") // ∂(e^f(time)) = e^f(time) * ∂f(time)
                    return Mult(this, args[0].Differentiate(var, style));
                else if (op == "log") // ∂ln(f(time)) = 1/time * ∂f(time), for time > 0
                    return Mult(Div(NumberFlow.numberFlowOne, OpFlow.Op("time")), args[0].Differentiate(var, style));
                else if (op == "sqrt") // ∂sqrt(f(time)) = 1/(2*sqrt(f(time))) * ∂f(time)
                    return Mult(Div(NumberFlow.numberFlowOne, Mult(NumberFlow.numberFlowTwo, OpFlow.Op("sqrt", args[0]))), args[0].Differentiate(var, style));
                else if (op == "sign") // ∂sign(f(time)) = 0
                    return NumberFlow.numberFlowZero;
                else if (op == "abs") // ∂abs(f(time)) = sign(f(time)) * ∂f(time)
                    return Mult(OpFlow.Op("sign", args[0]), args[0].Differentiate(var, style));
                else if (op == "sin") // ∂sin(f(time)) = cos(f(time)) * ∂f(time);   e.g. ∂sin(s) = cos(s)*∂s for a species s
                    return Mult(OpFlow.Op("cos", args[0]), args[0].Differentiate(var, style));
                else if (op == "cos") // ∂cos(f(time)) = -sin(f(time)) * ∂f(time)
                    return Mult(Minus(OpFlow.Op("sin", args[0])), args[0].Differentiate(var, style));
                else if (op == "tan") // ∂tan(f(time)) = 1/cos(f(time))^2 * ∂f(time)
                    return Mult(Div(NumberFlow.numberFlowOne, Pow(OpFlow.Op("cos", args[0]), NumberFlow.numberFlowTwo)), args[0].Differentiate(var, style));
                else if (op == "sinh") // ∂sinh(f(time)) = cosh(f(time)) * ∂f(time)
                    return Mult(OpFlow.Op("cosh", args[0]), args[0].Differentiate(var, style));
                else if (op == "cosh") // ∂cosh(f(time)) = sinh(f(time)) * ∂f(time)
                    return Mult(OpFlow.Op("sinh", args[0]), args[0].Differentiate(var, style));
                else if (op == "tanh") // ∂tanh(f(time)) = (1-tanh(f(time))^2) * ∂f(time)
                    return Mult(Minus(NumberFlow.numberFlowOne, Pow(OpFlow.Op("tanh", args[0]), NumberFlow.numberFlowTwo)), args[0].Differentiate(var, style));
                // ### etc.
                else throw new Error(Bad + op); // "var", "poisson", "∂" cannot support second derivative
            } else if (arity == 2) {
                if (op == "+") // ∂(f(time)+g(time)) = ∂f(time)+∂g(time)
                    return Plus(args[0].Differentiate(var, style), args[1].Differentiate(var, style));
                else if (op == "-") // ∂(f(time)-g(time)) = ∂f(time)-∂g(time)
                    return Minus(args[0].Differentiate(var, style), args[1].Differentiate(var, style));
                else if (op == "*") // ∂(f(time)*g(time)) = ∂f(time)*g(time) + f(time)*∂g(time)
                    return Plus(
                        Mult(args[0].Differentiate(var, style), args[1]),
                        Mult(args[0], args[1].Differentiate(var, style)));
                else if (op == "/") // ∂(f(time)/g(time)) = (∂f(time)*g(time) - f(time)*∂g(time)) / g(time)^2
                    return
                        Div(
                            Minus(
                               Mult(args[0].Differentiate(var, style), args[1]),
                               Mult(args[0], args[1].Differentiate(var, style))),
                            Pow(args[1], NumberFlow.numberFlowTwo));              
                else if (op == "^")  
                    if (args[0] is NumberFlow && (args[0] as NumberFlow).value == Math.E) { // ∂(e^f(time)) = e^f(time) * ∂f(time)  // special case if base is e
                        return Mult(this, args[1].Differentiate(var, style));
                    } else if (args[1] is NumberFlow) { // ∂(f(time)^n) = n*(f(time)^(n-1))*∂f(time) // special case if exponent is constant
                        double power = (args[1] as NumberFlow).value;
                        return
                            Mult(
                                Mult(args[1],
                                    Pow(args[0], new NumberFlow(power-1))),
                                args[0].Differentiate(var, style));
                    } else { // ∂(f(time)^g(time)) = g(time)*(f(time)^(g(time)-1))*∂f(time) + (f(time)^g(time))*ln(f(time))*∂g(time)
                             //   = (f(time)^(g(time)-1)) * (g(time)*∂f(time) + f(time)*ln(f(time))*∂g(time))
                        return
                           Mult(
                              Pow(args[0], Minus(args[1], NumberFlow.numberFlowOne)),
                              Plus(
                                 Mult(args[1], args[0].Differentiate(var, style)),
                                 Mult(args[0],
                                    Mult(
                                       Log(args[0]), 
                                       args[1].Differentiate(var, style)
                                    )
                                 )
                              )
                           );
                    }
                else throw new Error(Bad + op);
            } else if (arity == 3) {
                if (op == "cond")
                    return Cond(args[0], args[1].Differentiate(var, style), args[2].Differentiate(var, style));
                else  throw new Error(Bad + op);
            } else throw new Error(Bad + op);
        }
    }

}
