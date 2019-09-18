using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Research.Oslo;

namespace Kaemika
{

    public enum ExportTarget : int { LBS, CRN, Standard };

    public class Style {
        private string varchar;                 // The non-inputable character used to distinguish symbol variants
                                                // can be null if we do not show the variants
        private SwapMap swap;                   // The strings used to replace special chars in export to other systems
                                                // cannot be null
        private AlphaMap map;                   // The map used to alpha-convert conflicting symbols in printout
                                                // can be null if we do not alpha-convert
        public string numberFormat;             // Number format
                                                // can be null for default (full precision)
        public string dataFormat;               // How to display complex data
                                                // "symbol", "header", or "full"
        public ExportTarget exportTarget;       // How to format for external tools

        public bool traceComputational;         // Weather to format of TraceComputational or TraceChemical

        public Style(string varchar, SwapMap swap, AlphaMap map, string numberFormat, string dataFormat, ExportTarget exportTarget, bool traceComputational) {
            this.varchar = varchar;
            this.swap = swap;
            this.map = map;
            this.numberFormat = numberFormat;
            this.dataFormat = dataFormat;
            this.exportTarget = exportTarget;
            this.traceComputational = traceComputational;
        }
        public Style() : this(null, null, null, null, "full", ExportTarget.Standard, false) {
        }
        public Style RestyleAsDataFormat(string dataFormat) {
            return new Style(this.varchar, this.swap, this.map, this.numberFormat, dataFormat, this.exportTarget, this.traceComputational);
        }
        public Style RestyleAsNumberFormat(string numberFormat) {
            return new Style(this.varchar, this.swap, this.map, numberFormat, this.dataFormat, this.exportTarget, this.traceComputational);
        }
        public Style RestyleAsTraceComputational(bool traceComputational) {
            return new Style(this.varchar, this.swap, this.map, this.numberFormat, this.dataFormat, this.exportTarget, traceComputational);
        }
        public string Varchar() { return this.varchar; }
        public SwapMap Swap() { return this.swap; }
        public AlphaMap Map() { return this.map; }
        public string FormatDouble(double n) { if (this.numberFormat != null) return n.ToString(this.numberFormat); else return n.ToString(); }
    }

    // SCOPES

    public class Symbol {
        private string name;
        private int variant;    // global variant of symbol name, making it unique
        public Symbol(string name) {
            this.name = name;
            this.variant = Exec.NewUID();
        }
        public string Raw() { return name; }
        public bool SameSymbol(Symbol otherSymbol) {
            return this.variant == otherSymbol.variant;
        }
        public string Replace(string name, string content, string replacement) {
            if (name.Contains(content) && name.Contains(replacement)) throw new Error("Cannot replace '" + content + "' with '" + replacement + "' in '" + name + "'");
            else return name.Replace(content, replacement);
        }
        public string Replace(string name, SwapMap swap) {
            foreach (var keypair in swap.Pairs()) name = Replace(name, keypair.Key, keypair.Value);
            return name;
        }
        public string Format(Style style) {
            string varchar = style.Varchar();
            if (varchar == null) return this.name;                                           // don't show the variant
            else {
                string sname = Replace(this.name, style.Swap());
                AlphaMap map = style.Map();
                if (map == null) return sname + varchar + this.variant.ToString();           // show the variant, don't remap it
                else {                                                                       // remap the variant
                    if (!map.ContainsKey(this.variant)) {           // never encountered this variant before: it is name unique?
                        int variantNo = -1;
                        string variantName = "";
                        do {
                            variantNo += 1;
                            variantName = (variantNo == 0) ? sname : sname + varchar + variantNo.ToString();
                        } while (map.ContainsValue(variantName));
                        map.Assign(this.variant, variantName);            // assign a unique name to this variant
                    }
                    return map.Extract(this.variant);
                }
            }
        }
    }
    public class SymbolComparer : EqualityComparer<Symbol> {
        public override bool Equals(Symbol a, Symbol b) { return a.SameSymbol(b); }
        public override int GetHashCode(Symbol a) { return a.Raw().GetHashCode(); }
        public static SymbolComparer comparer = new SymbolComparer();
    }

    public abstract class Scope {
        public abstract bool Lookup(string var); // return true if var is defined
        public abstract string Format();
        public Scope Extend(List<NewParameter> parameters) {
            Scope scope = this;
            foreach (NewParameter parameter in parameters) { //  (a,b,c)+this = c,b,a,this
                if (parameter is SingleParameter)
                    scope = new ConsScope((parameter as SingleParameter).name, scope);
                else if (parameter is ListParameter)
                    scope = scope.Extend((parameter as ListParameter).list.parameters);
                else throw new Error("Parameter");
            }
            return scope;
        }
    }
    public class NullScope : Scope {
        public override bool Lookup(string var) {
            return false;
        }
        public override string Format() {
            return "";
        }
        private Scope builtIn = null;
        private Scope CopyBuiltIn(Env builtInEnv) {
            if (builtInEnv is NullEnv) return new NullScope();
            else {
                ValueEnv consEnv = (ValueEnv)builtInEnv;
                return new ConsScope(consEnv.symbol.Raw(), CopyBuiltIn(consEnv.next));
            }
        }
        public Scope BuiltIn(SampleValue vessel) { //we park this method inside NullScope for convenience
            if (builtIn == null) builtIn = CopyBuiltIn(new NullEnv().BuiltIn(vessel));
            return builtIn;
        }
    }
    public class ConsScope : Scope {
        public string name;
        public Scope next;
        public ConsScope(string name, Scope next) {
            this.name = name;
            this.next = next;
        }
        public override bool Lookup(string name) {
            if (name == this.name) return true;
            else return next.Lookup(name);
        }
        public override string Format() {
            string first = next.Format();
            string last = this.name;
            if (first == "") return last; else return first + Environment.NewLine + last;
        }
    }

    // TYPES

    public class Type {
        private string type;
        public Type(string name) {
            this.type = name;
        }
        public bool Is(string name) {
            return this.type == name;
        }
        public bool Matches(Value value) {
            return
                ((this.type == "bool") && (value is BoolValue)) ||
                ((this.type == "number") && (value is NumberValue)) ||
                ((this.type == "string") && (value is StringValue)) ||
                ((this.type == "list") && (value is ListValue<Value> || value is ListValue<Flow>)) ||
                ((this.type == "flow") && (value is Flow)) ||
                ((this.type == "function") && (value is FunctionValue || value is OperatorValue)) ||
                ((this.type == "network") && (value is NetworkValue)) ||
                ((this.type == "species") && (value is SpeciesValue)) ||
                ((this.type == "sample") && (value is SampleValue))
                ;
        }
        public string Format() { return this.type; }
    }

    // ENVIRONMENTS

    public abstract class Env {
        public abstract Symbol LookupSymbol(string name);
        public abstract Value LookupValue(string name);
        public abstract void AssignValue(Symbol symbol, Value value);
        public abstract string Format(Style style);
        public Env ExtendValues<T>(List<NewParameter> parameters, List<T> arguments, Netlist netlist, string source, Style style) where T : Value {  // bounded polymorphism :)
            if (parameters.Count != arguments.Count) throw new Error("Different number of parameters and arguments for '" + source + "'");
            Env env = this;
            for (int i = 0; i < parameters.Count; i++) {
                if (parameters[i] is SingleParameter) {
                    SingleParameter parameter = parameters[i] as SingleParameter;
                    env = new ValueEnv(parameter.name, parameter.type, arguments[i], netlist, env);
                } else if (parameters[i] is ListParameter) {
                    List<NewParameter> subParameters = (parameters[i] as ListParameter).list.parameters;
                    if (!(arguments[i] is ListValue<T>)) throw new Error("xxxx");
                    List<T> subArguments = (arguments[i] as ListValue<T>).elements;
                    if (subParameters.Count != subArguments.Count) throw new Error("Different number of list pattern parameters and arguments for '" + source + "'");
                    env = env.ExtendValues(subParameters, subArguments, netlist, source, style);
                } else throw new Error("Parameter");
            }
            return env;
        }
    }

    public class NullEnv : Env {
        public override Value LookupValue(string name) {
            throw new Error("UNDEFINED Lookup of name: " + name); // this should be prevented by scoping analysis
        }
        public override Symbol LookupSymbol(string name) {
            throw new Error("UNDEFINED LookupSymbol of name: " + name); // this should be prevented by scoping analysis
        }
        public override void AssignValue(Symbol symbol, Value value) {
            throw new Error("UNDEFINED Assign of name: " + symbol.Format(new Style())); // this should be prevented by scoping analysis
        }
        public override string Format(Style style) {
            return "";
        }
        private Env builtIn = null;
        public Env BuiltIn(SampleValue vessel) { //we park this method inside NullEnv for convenience
            if (builtIn == null) {
                builtIn = new NullEnv();
                builtIn = new ValueEnv("vessel", null, vessel, builtIn);
                builtIn = new ValueEnv("if", null, new OperatorValue("if"), builtIn);          // conditional pseudo-operator
                builtIn = new ValueEnv("cond", null, new OperatorValue("cond"), builtIn);        // flow-expression conditional pseudo-operator
                builtIn = new ValueEnv("not", null, new OperatorValue("not"), builtIn);
                builtIn = new ValueEnv("or", null, new OperatorValue("or"), builtIn);
                builtIn = new ValueEnv("and", null, new OperatorValue("and"), builtIn);
                builtIn = new ValueEnv("+", null, new OperatorValue("+"), builtIn);
                builtIn = new ValueEnv("-", null, new OperatorValue("-"), builtIn);           // both prefix and infix
                builtIn = new ValueEnv("*", null, new OperatorValue("*"), builtIn);
                builtIn = new ValueEnv("/", null, new OperatorValue("/"), builtIn);
                builtIn = new ValueEnv("^", null, new OperatorValue("^"), builtIn);
                builtIn = new ValueEnv("=", null, new OperatorValue("="), builtIn);
                builtIn = new ValueEnv("<>", null, new OperatorValue("<>"), builtIn);
                builtIn = new ValueEnv("<=", null, new OperatorValue("<="), builtIn);
                builtIn = new ValueEnv("<", null, new OperatorValue("<"), builtIn);
                builtIn = new ValueEnv(">=", null, new OperatorValue(">="), builtIn);
                builtIn = new ValueEnv(">", null, new OperatorValue(">"), builtIn);
                builtIn = new ValueEnv("∂", null, new OperatorValue("∂"), builtIn);
                builtIn = new ValueEnv("diff", null, new OperatorValue("∂"), builtIn);
                builtIn = new ValueEnv("sdiff", null, new OperatorValue("sdiff"), builtIn);
                builtIn = new ValueEnv("maxNumber", null, new NumberValue(Double.MaxValue), builtIn);
                builtIn = new ValueEnv("minNumber", null, new NumberValue(Double.MinValue), builtIn);
                builtIn = new ValueEnv("positiveInfinity", null, new NumberValue(Double.PositiveInfinity), builtIn);
                builtIn = new ValueEnv("negativeInfinity", null, new NumberValue(Double.NegativeInfinity), builtIn);
                builtIn = new ValueEnv("NaN", null, new NumberValue(Double.NaN), builtIn);
                builtIn = new ValueEnv("pi", null, new NumberValue(Math.PI), builtIn);
                builtIn = new ValueEnv("e", null, new NumberValue(Math.E), builtIn);
                builtIn = new ValueEnv("abs", null, new OperatorValue("abs"), builtIn);
                builtIn = new ValueEnv("arccos", null, new OperatorValue("arccos"), builtIn);
                builtIn = new ValueEnv("arcsin", null, new OperatorValue("arcsin"), builtIn);
                builtIn = new ValueEnv("arctan", null, new OperatorValue("arctan"), builtIn);
                builtIn = new ValueEnv("arctan2", null, new OperatorValue("arctan2"), builtIn);
                builtIn = new ValueEnv("ceiling", null, new OperatorValue("ceiling"), builtIn);
                builtIn = new ValueEnv("cos", null, new OperatorValue("cos"), builtIn);
                builtIn = new ValueEnv("cosh", null, new OperatorValue("cosh"), builtIn);
                builtIn = new ValueEnv("exp", null, new OperatorValue("exp"), builtIn);
                builtIn = new ValueEnv("floor", null, new OperatorValue("floor"), builtIn);
                builtIn = new ValueEnv("int", null, new OperatorValue("int"), builtIn);         // convert number to integer number by rounding
                builtIn = new ValueEnv("log", null, new OperatorValue("log"), builtIn);
                builtIn = new ValueEnv("max", null, new OperatorValue("max"), builtIn);
                builtIn = new ValueEnv("min", null, new OperatorValue("min"), builtIn);
                builtIn = new ValueEnv("pos", null, new OperatorValue("pos"), builtIn);         // convert number to positive number by returning 0 if negative
                builtIn = new ValueEnv("sign", null, new OperatorValue("sign"), builtIn);
                builtIn = new ValueEnv("sin", null, new OperatorValue("sin"), builtIn);
                builtIn = new ValueEnv("sinh", null, new OperatorValue("sinh"), builtIn);
                builtIn = new ValueEnv("sqrt", null, new OperatorValue("sqrt"), builtIn);
                builtIn = new ValueEnv("tan", null, new OperatorValue("tan"), builtIn);
                builtIn = new ValueEnv("tanh", null, new OperatorValue("tanh"), builtIn);
                builtIn = new ValueEnv("observe", null, new OperatorValue("observe"), builtIn);       // evaluate flow expressions
                builtIn = new ValueEnv("time", null, new OperatorValue("time"), builtIn);             // for flow expressions
                builtIn = new ValueEnv("kelvin", null, new OperatorValue("kelvin"), builtIn);           // for flow expressions
                builtIn = new ValueEnv("celsius", null, new OperatorValue("celsius"), builtIn);          // for flow expressions
                builtIn = new ValueEnv("volume", null, new OperatorValue("volume"), builtIn);          // for flow expressions
                builtIn = new ValueEnv("poisson", null, new OperatorValue("poisson"), builtIn);          // for flow expressions
                builtIn = new ValueEnv("gauss", null, new OperatorValue("gauss"), builtIn);            // for flow expressions
                builtIn = new ValueEnv("var", null, new OperatorValue("var"), builtIn);              // for flow expressions
                builtIn = new ValueEnv("cov", null, new OperatorValue("cov"), builtIn);              // for flow expressions
                builtIn = new ValueEnv("argmin", null, new OperatorValue("argmin"), builtIn);
                //### map, foldl, foldr, filter, length, append    http://www.cse.unsw.edu.au/~en1000/haskell/hof.html
                //### filter could be given the index number to emulate range selection
                //### init(f,n) = [f(0),f(1),...,f(n-1)]
            }
            return builtIn;
        }
    }

    public class ValueEnv : Env {
        public Symbol symbol;
        public Value value;
        public Type type;
        public Env next;
        public ValueEnv(Symbol symbol, Type type, Value value, Env next) {
            this.symbol = symbol;
            this.type = type;
            this.value = value;
            this.next = next;
            if ((value != null) &&         // dont give type errors when creating null recursive environment stubs
                (type != null) &&          // when we know that we do not need to check the type of the value
                (!type.Matches(value)))
                throw new Error("Binding var " + symbol.Raw() + " of type " + type.Format() + " to value " + value.Format(new Style()) + " of type " + value.type.Format());
        }
        public ValueEnv(string name, Type type, Value value, Env next) : this(new Symbol(name), type, value, next) {
        }
        public ValueEnv(string name, Type type, Value value, Netlist netlist, Env next) : this(new Symbol(name), type, value, next) {  
            if (netlist != null) netlist.Emit(new ValueEntry(this.symbol, type, value));   // also emit the new binding to netlist
        }
        public override Symbol LookupSymbol(string name) {
            if (name == this.symbol.Raw()) return this.symbol;
            else return next.LookupSymbol(name);
        }
        public override Value LookupValue(string name) {
            if (name == this.symbol.Raw())
                if (this.value != null) return this.value;
                else throw new Error("UNASSIGNED name: " + name); // this should be prevented by scoping checks
            else return next.LookupValue(name);
        }
        public override void AssignValue(Symbol symbol, Value value) {
            if (symbol.SameSymbol(this.symbol))
                if (this.value == null) this.value = value;
                else throw new Error("REASSIGNMENT of name: " + symbol.Format(new Style()) + ", old value: " + this.value.Format(new Style()) + ", new value: " + value.Format(new Style())); // this should be prevented by scoping analysis
            else next.AssignValue(symbol, value);
        }
        public override string Format(Style style) {
            string first = next.Format(style);
            SpeciesValue species = (SpeciesValue)value;
            string last = type.Format() + " " + symbol.Format(style) + " = " + value.Format(style);
            if (first == "") return last; else return first + Environment.NewLine + last;
        }
    }

    // PROTOCOL ACTUATORS

    public class State {
        private int size;
        private bool lna;
        private double[] state;
        private bool inited;
        public State(int size, bool lna = false) {
            this.size = size;
            this.lna = lna;
            this.inited = false;
        }
        public State InitZero() {
            if (this.inited) throw new Error("InitZero: already inited");
            if (!lna) this.state = new double[size];
            else this.state = new double[size + size * size];
            // for (int i = 0; i < this.state.Length; i++) this.state[i] = 0.0;
            this.inited = true;
            return this;
        }
        public State InitMeans(double[] init) {
            if (this.inited) throw new Error("InitMeans: already inited");
            if (init.Length != size) throw new Error("InitMeans: wrong size");
            if (!lna) this.state = init;
            else {
                this.state = new double[size + size * size];
                for (int i = 0; i < size; i++) this.state[i] = init[i];
                // for (int i = size; i < this.state.Length; i++) this.state[i] = 0.0;
            }
            this.inited = true;
            return this;
        }
        public State InitAll(double[] init) {
            if (this.inited) throw new Error("InitAll: already inited");
            if (((!lna) && init.Length != size) || (lna && init.Length != size + size * size)) throw new Error("InitAll: wrong size");
            this.state = init;
            this.inited = true;
            return this;
        }
        public double[] ToArray() {
            return this.state;
        }
        public bool Lna() {
            return lna;
        }
        public int Size() {
            return size;
        }
        public double Mean(int i) {
            return this.state[i];
        }
        public double[] RawMean() {
            if (!this.lna) return this.state;
            else {
                double[] m = new double[size];
                for (int i = 0; i < size; i++) m[i] = this.state[i];
                return m;
            }
        }
        public Vector MeanVector() {
            return new Vector(this.RawMean());
        }
        public Vector MeanArray() { // danger! abstraction-breaking, only to be used by Report when invoked via a rateFunction
            return this.state;      // it needs to be this way because we need to give a Vector (not a State) to OSLO
        }
        public void AddMean(int i, double x) {
            this.state[i] += x;
        }
        public void AddMean(Vector x) {
            if (x.Length != size) throw new Error("AddMean: wrong size");
            for (int i = 0; i < size; i++) this.state[i] += x[i];
        }
        public double Covar(int i, int j) {
            return this.state[size + (i * size) + j];
        }
        public double[] RawCovar() {
            if (!this.lna) throw new Error("Covars: not lna state");
            double[] c = new double[size * size];
            for (int i = 0; i < size * size; i++) c[i] = this.state[size + i];
            return c;
        }
        public Matrix CovarMatrix() {
            if (!this.lna) throw new Error("Covars: not lna state");
            double[,] m = new double[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    m[i, j] = this.state[size + (i * size) + j];
            return new Matrix(m);
        }
        public void AddCovar(int i, int j, double x) {
            this.state[size + (i * size) + j] += x;
        }
        public void AddCovar(Matrix x) {
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    this.state[size + (i * size) + j] += x[i, j];
        }
        public string FormatSpecies(List<SpeciesValue> species, Style style) {
            string s = "";
            for (int i = 0; i < this.size; i++) {
                s += species[i].Format(style) + "=" + Mean(i).ToString() + ", ";
            }
            if (this.lna) {
                for (int i = 0; i < this.size; i++)
                    for (int j = 0; j < this.size; j++) {
                        s += "(" + species[i].Format(style) + "," + species[j].Format(style) + ")=" + Covar(i, j).ToString() + ", ";
                    }
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2);
            return s;
        }
        public string FormatReports(List<ReportEntry> reports, SampleValue sample, Func<double, Vector, Vector> flux, double time, Noise noise, string[] series, string[] seriesLNA, Style style) {
            string s = "";
            for (int i = 0; i < reports.Count; i++) {
                if (series[i] != null) { // if a series was actually generated from this report
                    // generate deterministic series
                    if ((noise == Noise.None && reports[i].flow.HasDeterministicValue()) ||
                        (noise != Noise.None && reports[i].flow.HasStochasticMean())) {
                        double mean = reports[i].flow.ObserveMean(sample, time, this, flux, style);
                        s += Gui.gui.ChartAddPointAsString(series[i], time, mean, 0.0, Noise.None) + ", ";
                    }
                    // generate LNA-dependent series
                    if (noise != Noise.None && reports[i].flow.HasStochasticVariance() && !reports[i].flow.HasNullVariance()) {
                        double mean = reports[i].flow.ObserveMean(sample, time, this, flux, style);
                        double variance = reports[i].flow.ObserveVariance(sample, time, this, style);
                        s += Gui.gui.ChartAddPointAsString(seriesLNA[i], time, mean, variance, noise) + ", ";
                    }
                }
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2);
            return s;
        }
    }

    // VALUES

    public abstract class Value {
        public Type type;
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
            else if (this is OperatorValue)
            { // handle the nullary operators from the built-in environment
                if (((OperatorValue)this).name == "time") return new OpFlow("time", false, new List<Flow>());
                else if (((OperatorValue)this).name == "kelvin") return new OpFlow("kelvin", false, new List<Flow>());
                else if (((OperatorValue)this).name == "celsius") return new OpFlow("celsius", false, new List<Flow>());
                else if (((OperatorValue)this).name == "volume") return new OpFlow("volume", false, new List<Flow>());
                else return null;
            } else return null;

        }
    }

    public class SampleValue : Value {
        public Symbol symbol;
        private NumberValue volume;                                 // L
        private NumberValue temperature;                            // Kelvin
        private Dictionary<SpeciesValue, NumberValue> speciesSet;   // mol/L
        public List<SpeciesValue> species;
        public Dictionary<Symbol, int> speciesIndex;
        private bool produced; // produced by an operation as opposed as being created as a sample
        private bool consumed; // consumed by an operation, including dispose
        private List<ReactionValue> reactionsAsConsumed;
        private double timeAsConsumed;
        private State stateAsConsumed;
        public SampleValue(Symbol symbol, NumberValue volume, NumberValue temperature, bool produced) {
            this.type = new Type("sample");
            this.symbol = symbol;
            this.volume = volume;           // mL
            this.temperature = temperature; // Kelvin
            this.speciesSet = new Dictionary<SpeciesValue, NumberValue> { };
            this.species = new List<SpeciesValue> { };
            this.speciesIndex = new Dictionary<Symbol, int> { };
            this.produced = produced;
            this.consumed = false;
            this.reactionsAsConsumed = null;
            this.timeAsConsumed = 0.0;
            this.stateAsConsumed = null;
        }
        public void Consume(List<ReactionValue> reactionsAsConsumed, double timeAsConsumed, State stateAsConsumed, Netlist netlist, Style style) {
            if (this.consumed) throw new Error("Sample already used: '" + this.symbol.Format(style) + "'");
            this.consumed = true;
            this.timeAsConsumed = timeAsConsumed;
            this.stateAsConsumed = (stateAsConsumed == null) ? SpeciesState() : stateAsConsumed;
            this.reactionsAsConsumed = (reactionsAsConsumed == null) ? RelevantReactions(netlist, style) : reactionsAsConsumed;
        }
        public List<ReactionValue> ReactionsAsConsumed(Style style) {
            if (!consumed) throw new Error("Sample '" + symbol.Format(style) + "' should have been equilibrated and consumed");
            return reactionsAsConsumed;
        }
        public bool IsProduced() { return this.produced; }
        public bool IsConsumed() { return this.consumed; }
        public SampleValue Copy() {
            SampleValue copy = new SampleValue(this.symbol, this.volume, this.temperature, this.produced);
            foreach (var pair in this.speciesSet) copy.SetMolarity(pair.Key, pair.Value, null, recompute: false);
            copy.RecomputeSpecies();
            return copy;
        }
        public string FormatSymbol(Style style) {
            return symbol.Format(style);
        }
        public string FormatHeader(Style style) {
            return symbol.Format(style) + " {" + Gui.FormatUnit(this.Volume(), "", "L", style.numberFormat) + ", " + temperature.Format(style) + "K}";
        }
        public string FormatContent(Style style, bool breaks = false) {
            string s = "";
            foreach (KeyValuePair<SpeciesValue, NumberValue> keyPair in this.speciesSet)
                s += (breaks ? (Environment.NewLine + "   ") : "") + keyPair.Key.Format(style) + " = " + Gui.FormatUnit(keyPair.Value.value, "", "M", style.numberFormat);
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

        public List<SpeciesValue> Species(out double[] state) {
            int i = 0;
            List<SpeciesValue> species = new List<SpeciesValue> { };
            state = new double[speciesSet.Count];
            foreach (var entry in this.speciesSet) {
                species.Add(entry.Key);
                state[i] = entry.Value.value;
                i++;
            }
            return species;
        }
        public bool HasSpecies(Symbol species, out NumberValue value) { // check that speciesSet contains this species, by checking its variant by SameSymbol
            foreach (KeyValuePair<SpeciesValue, NumberValue> keyPair in this.speciesSet) {
                if (keyPair.Key.symbol.SameSymbol(species)) {
                    value = keyPair.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }
        private void RecomputeSpecies() {
            this.species = new List<SpeciesValue> { };
            foreach (var entry in this.speciesSet) species.Add(entry.Key);
            this.speciesIndex = new Dictionary<Symbol, int> { };
            for (int i = 0; i < this.species.Count; i++) { this.speciesIndex[this.species[i].symbol] = i; }
        }
        public void AddSpecies(SampleValue other, double thisVolume, double otherVolume) { // add the species of another sample into this one
            foreach (KeyValuePair<SpeciesValue, NumberValue> keyPair in other.speciesSet) {
                double newConcentration = (keyPair.Value.value * otherVolume) / thisVolume;
                if (this.HasSpecies(keyPair.Key.symbol, out NumberValue number))
                    this.speciesSet[keyPair.Key] = new NumberValue(number.value + newConcentration);
                else {
                    this.speciesSet.Add(keyPair.Key, new NumberValue(newConcentration));
                    RecomputeSpecies();
                }
            }
        }
        public State SpeciesState() {
            var species = this.Species(out double[] s);
            return new State(speciesSet.Count, true).InitMeans(s); // initialize all covariances to 0
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
            if (!flow.CoveredBy(this.species, out Symbol notCovered)) throw new Error("observe : species '" + notCovered.Format(style) + "' in flow '" + flow.Format(style) + "' is not one of the species in sample '" + this.FormatSymbol(style));
            double observeTime;
            State observeState;
            List<ReactionValue> observeReactions;
            if (consumed) { // throw new Error("observe(" + this.symbol.Format(style) + ", " + flow.Format(style) + "): sample already disposed");
                observeTime = this.timeAsConsumed;
                observeState = this.stateAsConsumed;
                observeReactions = this.reactionsAsConsumed;
            } else { // throw new Error("observe(" + this.symbol.Format(style) + ", " + flow.Format(style) + "): sample not disposed");
                observeTime = 0;
                observeState = SpeciesState();
                observeReactions = RelevantReactions(netlist, style);
            }
            // maybe we should allow observing only samples that have been consumed; otherwise the RelevantReactions and Flux may be still incomplete
            CRN crn = new CRN(this, observeReactions);
            return new NumberValue(flow.ObserveMean(this, observeTime, observeState, ((double x, Vector st) => { return crn.Flux(x, st, style); }), style));
        }

        public NumberValue Molarity(Symbol species, Style style) {
            if (this.HasSpecies(species, out NumberValue value)) return value;
            else throw new Error("Uninitialized species '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "'");
        }
        public void SetMolarity(SpeciesValue species, NumberValue init, Style style, bool recompute = true) {
            if (this.HasSpecies(species.symbol, out NumberValue value))
                throw new Error("SetMolarity: Repeated amount of '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "' with value " + init.Format(style));
            else if (init.value < 0)
                throw new Error("SetMolarity: Amount of '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "' must be non-negative: " + init.Format(style));
            else {
                this.speciesSet.Add(species, init);
                if (recompute) RecomputeSpecies();
            }
        }
        public void InitMolarity(SpeciesValue species, NumberValue molarity, string dimension, Style style) {
            double mol = molarity.value;
            if (this.HasSpecies(species.symbol, out NumberValue value))
                throw new Error("Repeated amount of '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "' with value " + mol.ToString());
            else if (mol < 0)
                throw new Error("Amount of '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "' must be non-negative: " + mol.ToString());
            else {
                this.speciesSet.Add(species, new NumberValue(Normalize(species, mol, dimension, style)));
                RecomputeSpecies();
            }
        }
        public void ChangeMolarity(SpeciesValue species, NumberValue molarity, string dimension, Style style) {
            double mol = molarity.value;
            if (mol < 0)
                throw new Error("Species to change '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "' must be non-negative: " + mol.ToString());
            else if (this.HasSpecies(species.symbol, out NumberValue value)) {
                this.speciesSet[species] = new NumberValue(Normalize(species, mol, dimension, style));
            } else throw new Error("Species to change not found '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "' with value " + mol.ToString());
        }
        private double Normalize(SpeciesValue species, double value, string dimension, Style style) {
            double normal;
            normal = Protocol.NormalizeMolarity(value, dimension);
            if (normal >= 0) return normal; // value had dimension M = mol/L
            normal = Protocol.NormalizeMole(value, dimension);
            if (normal >= 0) return normal / this.Volume(); // value had dimension mol, convert it to M = mol/L
            normal = Protocol.NormalizeWeight(value, dimension);
            if (normal >= 0) {
                if (species.HasMolarMass())
                    return (normal / species.MolarMass()) / this.Volume();    // value had dimension g, convert it to M = (g/(g/M))/L
                throw new Error("Species '" + species.Format(style)
                    + "' was given no molar mass, hence its amount in sample '" + this.symbol.Format(style)
                    + "' should have dimension 'M' (concentration) or 'mol' (mole), not '" + dimension + "'");
            }
            throw new Error("Invalid dimension '" + dimension + "'" + " or dimension value " + style.FormatDouble(value));
        }
        public List<ReactionValue> RelevantReactions(Netlist netlist, Style style) { 
            // return the list of reactions in the netlist that can fire in this sample
            // check that those reactions produce only species in this sample, or give error
            List<ReactionValue> reactionList = new List<ReactionValue> { };
            foreach (ReactionValue reaction in netlist.AllReactions()) {
                if (reaction.ReactantsCoveredBy(species, out Symbol notCoveredReactant)) {
                    if (reaction.ProductsCoveredBy(species, out Symbol notCoveredProduct)) {
                        reactionList.Add(reaction);
                    } // else ignore because it is not relevant
                    //} else throw new Error( // N.B.: this would give a spurious error on reactions like # -> a where reactants are always covered
                    //    "Reaction '" + reaction.Format(style) + "' produces species '" + notCoveredProduct.Format(style) + 
                    //    "' in sample '" + this.symbol.Format(style) + "', but that species is uninitialized in that sample");
                } // else ignore reaction because it cannot fire
            }
            return reactionList;
        }
    }

    public class SpeciesValue : Value {
        public Symbol symbol;
        private double molarMass; // molar mass (g/mol), or else concentration-based (mol/L) if <= 0
        public SpeciesValue(Symbol symbol, double molarMass) {
            this.type = new Type("species");
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
            this.type = new Type("bool");
            this.value = value;
        }
        public override string Format(Style style) {
            if (this.value) return "true"; else return "false";
        }
    }

    public class NumberValue : Value {
        public double value;
        public NumberValue(double value) {
            this.type = new Type("number");
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
            this.type = new Type("string");
            this.value = value;
        }
        public override string Format(Style style) {
            return Parser.FormatString(this.value);
        }
    }

    public class ListValue<T> : Value {
        public List<T> elements;
        public ListValue(List<T> elements) {
            this.type = new Type("list");
            this.elements = elements;
        }
        public override string Format(Style style) {
            string s = "";
            foreach (T element in elements) s += ((element is Value) ? (element as Value).Format(style) : (element is Flow) ? (element as Flow).Format(style) : "") + ", ";
            if (s.Length > 0) s = s.Substring(0, s.Length - 2);
            return "[" + s + "]";
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
        public double[] ToDoubleArray(string error) {
            double[] result = new double[elements.Count];
            for (int i = 0; i < elements.Count; i++) {
                T element = elements[i]; if (!(element is NumberValue)) throw new Error(error);
                result[i] = (element as NumberValue).value;
            }
            return result;
        }
    }

    public class DistributionValue : Value {
        public Symbol parameter;
        public double drawn;
        public string distribution;
        public double[] arguments;
        public DistributionValue(Symbol parameter, double value, string distribution, double[] arguments) {
            this.parameter = parameter;
            this.type = new Type("distribution");
            this.drawn = value;
            this.distribution = distribution;
            this.arguments = arguments;
        }
        public override string Format(Style style) {
            string s = "";
            foreach (double value in arguments) { s += style.FormatDouble(value) + ", "; }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2); // remove last comma
            return parameter.Format(style) + " = " + style.FormatDouble(drawn) + " drawn from " + distribution + "(" + s + ")";
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
        public int Count(Symbol symbol) {
            int n = 0;
            foreach (Symbol s in mset)
                if (s.SameSymbol(symbol)) n += 1;
            return n;
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
    }

    public class ReactionValue : Value {
        public List<Symbol> reactants;
        public List<Symbol> products;
        public RateValue rate;
        public ReactionValue(List<Symbol> reactants, List<Symbol> products, RateValue rate) {
            this.type = null; // not a first-class value
            this.reactants = reactants;
            this.products = products;
            this.rate = rate;
        }
        public override string Format(Style style) {
            string reactants = (this.reactants.Count() == 0) ? "#" : this.reactants.Aggregate("", (a, b) => (a == "") ? b.Format(style) : a + " + " + b.Format(style));
            string products = (this.products.Count() == 0) ? "#" : this.products.Aggregate("", (a, b) => (a == "") ? b.Format(style) : a + " + " + b.Format(style));
            string rate = this.rate.Format(style);
            return reactants + " -> " + products + " " + rate;
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
        public (SymbolMultiset catalysts, SymbolMultiset catalyzedRactants, SymbolMultiset catalyzedProducts) CatalistForm() {
            var catalysts = new SymbolMultiset();
            var catalyzedRactants = new SymbolMultiset(reactants); 
            var catalyzedProducts = new SymbolMultiset(products); 
            foreach (Symbol symbol in reactants) {
                int n = Math.Min(catalyzedRactants.Count(symbol), catalyzedProducts.Count(symbol));
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
        public abstract string Format(Style style);
        public abstract double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style);  // the mass action of this reaction in this state and temperature
        public abstract bool Involves(List<SpeciesValue> species);
        public abstract bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered);
    }

    public class GeneralRateValue : RateValue {
        public Flow rateFunction;
        public GeneralRateValue(Flow rateFunction) {
            this.rateFunction = rateFunction;
        }
        public override string Format(Style style) {
            return "{{" + rateFunction.Format(style) + "}}";
        }
        public override double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style) {
            // We earlier checked that rateFunction HasDeterministicMean. If it is not a numeric flow, we now get an error from ReportMean.
            return rateFunction.ObserveMean(sample, time, new State(state.Length, false).InitAll(state), null, style);  // flux=null: we cannot evaluate derivative for rates, which affect those derivatives
        }
        public override bool Involves(List<SpeciesValue> species) {
            return rateFunction.Involves(species);
        }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            return rateFunction.CoveredBy(species, out notCovered);
        }
    }

    public class MassActionRateValue : RateValue {
        private double collisionFrequency; // dimensionless
        private double activationEnergy; // J⋅mol^−1  (J=Joules, not kJoules)
        const double R = 8.3144598; // J⋅mol^−1⋅K^−1  (K=Kelvin)
        public MassActionRateValue(double collisionFrequency, double activationEnergy) {
            this.collisionFrequency = collisionFrequency;
            this.activationEnergy = activationEnergy;
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
            foreach (Symbol rs in reactants) action = action * state[sample.speciesIndex[rs]];
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
            this.type = new Type("function");
            this.symbol = symbol;
            this.parameters = parameters;
            this.body = body;
            this.env = env;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return (symbol == null) ? "<function>" : symbol.Format(style);
            else if (style.dataFormat == "header") return ((symbol == null) ? "<function>" : symbol.Format(style)) + "(" + parameters.Format() + ")";
            else if (style.dataFormat == "full") return ((symbol == null) ? "<function>" : symbol.Format(style)) + "(" + parameters.Format() + ") {" + Environment.NewLine + body.Format() + Environment.NewLine + "}";
            else return "unknown format: " + style.dataFormat;
        }
        public Value Apply(List<Value> arguments, Netlist netlist, Style style) {
            return body.Eval(env.ExtendValues(parameters.parameters, arguments, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style), netlist, style); 
        }
        public Value ApplyFlow(List<Value> arguments, Style style) {
            return body.EvalFlow(env.ExtendValues<Value>(parameters.parameters, arguments, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style), style);
        }
        public Flow BuildFlow(List<Flow> arguments, Style style) {
            return body.BuildFlow(env.ExtendValues<Flow>(parameters.parameters, arguments, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style), style); // note that even in this case the arguments are Values, not Flows
        }
    }

    public class OperatorValue : Value {
        public string name;
        public OperatorValue(string name) {
            this.type = new Type("operator"); // a subtype of "function"
            this.name = name;
        }
        public override string Format(Style style) {
            return name;
        }
        public Value Apply(List<Value> arguments, Netlist netlist, Style style) {
            string BadArgs() { return "Bad arguments to '" + name + "': " + Expressions.FormatValues(arguments, style); }
            if (arguments.Count == 0) {
                throw new Error(BadArgs());
            } else if (arguments.Count == 1) {
                return ApplyFlow(arguments, style);
            } else if (arguments.Count == 2) {
                return ApplyFlow(arguments, style);
            } else if (arguments.Count == 3) {
                if (name == "argmin") return Protocol.Argmin(arguments[0], arguments[1], arguments[2], netlist, style); // BFGF
                else return ApplyFlow(arguments, style);
                //} else if (arguments.Count == 4) {
                //    if (name == "argmin") return Protocol.Argmin(arguments[0], arguments[1], arguments[2], arguments[3], netlist, style); // GoldenSection
                //else throw new Error(BadArgs());
            } else throw new Error(BadArgs());
        }
        public Value ApplyFlow(List<Value> arguments, Style style) { // a subset of Apply
            string BadArgs() { return "Bad arguments to '" + name + "': " + Expressions.FormatValues(arguments, style); }
            if (arguments.Count == 0) {
                throw new Error(BadArgs());
            } else if (arguments.Count == 1) {
                Value arg1 = arguments[0];
                if (name == "not") if (arg1 is BoolValue) return new BoolValue(!((BoolValue)arg1).value); else throw new Error(BadArgs());
                else if (name == "-") if (arg1 is NumberValue) return new NumberValue(-((NumberValue)arg1).value); else throw new Error(BadArgs());
                else if (name == "abs") if (arg1 is NumberValue) return new NumberValue(Math.Abs(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "arccos") if (arg1 is NumberValue) return new NumberValue(Math.Acos(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "arcsin") if (arg1 is NumberValue) return new NumberValue(Math.Asin(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "arctan") if (arg1 is NumberValue) return new NumberValue(Math.Atan(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "ceiling") if (arg1 is NumberValue) return new NumberValue(Math.Ceiling(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "cos") if (arg1 is NumberValue) return new NumberValue(Math.Cos(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "cosh") if (arg1 is NumberValue) return new NumberValue(Math.Cosh(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "exp") if (arg1 is NumberValue) return new NumberValue(Math.Exp(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "floor") if (arg1 is NumberValue) return new NumberValue(Math.Floor(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "int") if (arg1 is NumberValue) { double num1 = ((NumberValue)arg1).value; return new NumberValue(Math.Round(num1)); } else throw new Error(BadArgs());          // convert number to integer number by rounding
                else if (name == "log") if (arg1 is NumberValue) return new NumberValue(Math.Log(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "pos") if (arg1 is NumberValue) { double num1 = ((NumberValue)arg1).value; return new NumberValue((num1 > 0) ? num1 : 0); } else throw new Error(BadArgs());     // convert number to positive number by returning 0 if negative
                else if (name == "sign") if (arg1 is NumberValue) return new NumberValue(Math.Sign(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "sin") if (arg1 is NumberValue) return new NumberValue(Math.Sin(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "sinh") if (arg1 is NumberValue) return new NumberValue(Math.Sinh(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "sqrt") if (arg1 is NumberValue) return new NumberValue(Math.Sqrt(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "tan") if (arg1 is NumberValue) return new NumberValue(Math.Tan(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else if (name == "tanh") if (arg1 is NumberValue) return new NumberValue(Math.Tanh(((NumberValue)arg1).value)); else throw new Error(BadArgs());
                else throw new Error(BadArgs());
            } else if (arguments.Count == 2) {
                Value arg1 = arguments[0];
                Value arg2 = arguments[1];
                if (name == "or") if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value || ((BoolValue)arg2).value); else throw new Error(BadArgs());
                else if (name == "and") if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value && ((BoolValue)arg2).value); else throw new Error(BadArgs());
                else if (name == "+")
                    if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value + ((NumberValue)arg2).value);
                    else if (arg1 is StringValue && arg2 is StringValue) return new StringValue(((StringValue)arg1).value + ((StringValue)arg2).value);
                    else throw new Error(BadArgs());
                else if (name == "-") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value - ((NumberValue)arg2).value); else throw new Error(BadArgs());
                else if (name == "*") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value * ((NumberValue)arg2).value); else throw new Error(BadArgs());
                else if (name == "/") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value / ((NumberValue)arg2).value); else throw new Error(BadArgs());
                else if (name == "^") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Pow(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArgs());
                else if (name == "=")
                    if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value == ((BoolValue)arg2).value);
                    else if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(NumberValue.EqualDouble(((NumberValue)arg1).value, ((NumberValue)arg2).value));
                    else if (arg1 is StringValue && arg2 is StringValue) return new BoolValue(((StringValue)arg1).value == ((StringValue)arg2).value);
                    else throw new Error(BadArgs());
                else if (name == "<>")
                    if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value != ((BoolValue)arg2).value);
                    else if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(!NumberValue.EqualDouble(((NumberValue)arg1).value, ((NumberValue)arg2).value));
                    else if (arg1 is StringValue && arg2 is StringValue) return new BoolValue(((StringValue)arg1).value != ((StringValue)arg2).value);
                    else throw new Error(BadArgs());
                else if (name == "<=") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value <= ((NumberValue)arg2).value); else throw new Error(BadArgs());
                else if (name == "<") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value < ((NumberValue)arg2).value); else throw new Error(BadArgs());
                else if (name == ">=") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value >= ((NumberValue)arg2).value); else throw new Error(BadArgs());
                else if (name == ">") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value > ((NumberValue)arg2).value); else throw new Error(BadArgs());
                else if (name == "arctan2") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Atan2(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArgs());
                else if (name == "max") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Max(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArgs());
                else if (name == "min") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Min(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArgs());
                else throw new Error(BadArgs());
            } else throw new Error(BadArgs());
        }
        public Flow BuildFlow(List<Flow> arguments, Style style) {
            string BadArgs() { return "Flow-expression: Bad arguments to '" + name + "': " + Expressions.FormatFlows(arguments, style); }
            if (arguments.Count == 0) {
                // "time", "kelvin", "celsius", "volume" are placed in the initial environment as Operators and converted to OpFlow when fetched as variables
                throw new Error(BadArgs());
            } else if (arguments.Count == 1) {
                Flow arg1 = arguments[0];
                if (name == "not" || name == "-" || name == "∂") {
                    return new OpFlow(name, true, new List<Flow> { arg1 });
                } else if (name == "sdiff") { // a Flow will never contain sdiff: it is expanded out here
                    return arg1.Differentiate(style);
                } else if (name == "var" || name == "poisson" || name == "abs" || name == "arccos" || name == "arcsin" || name == "arctan" || name == "ceiling"
                        || name == "cos" || name == "cosh" || name == "exp" || name == "floor" || name == "int" || name == "log"
                        || name == "pos" || name == "sign" || name == "sin" || name == "sinh" || name == "sqrt" || name == "tan" || name == "tanh") {
                    return new OpFlow(name, false, new List<Flow> { arg1 });
                } else throw new Error(BadArgs());
            } else if (arguments.Count == 2) {
                Flow arg1 = arguments[0];
                Flow arg2 = arguments[1];
                if (name == "or" || name == "and" || name == "+" || name == "-" || name == "*" || name == "/" || name == "^"
                    || name == "=" || name == "<>" || name == "<=" || name == "<" || name == ">=" || name == ">") {
                    return new OpFlow(name, true, new List<Flow> { arg1, arg2 });
                } else if (name == "cov" || name == "gauss" || name == "arctan2" || name == "min" || name == "max") {
                    return new OpFlow(name, false, new List<Flow> { arg1, arg2 });
                } else throw new Error(BadArgs());
            } else if (arguments.Count == 3) {
                if (name == "cond") {
                    return new OpFlow(name, false, new List<Flow> { arguments[0], arguments[1], arguments[2] });
                } else throw new Error(BadArgs());
            } else throw new Error(BadArgs());
        }
    }

    public class NetworkValue : Value {
        private Symbol symbol; // just for formatting, may be null if produced by a nameless network abstraction
        public Parameters parameters;
        public Statements body;
        public Env env;
        public NetworkValue(Symbol symbol, Parameters parameters, Statements body, Env env) {
            this.type = new Type("network");
            this.symbol = symbol;
            this.parameters = parameters;
            this.body = body;
            this.env = env;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return (symbol == null) ? "<network>" : symbol.Format(style);
            else if (style.dataFormat == "header") return ((symbol == null) ? "<network>" : symbol.Format(style)) + "(" + parameters.Format() + ")";
            else if (style.dataFormat == "full") return ((symbol == null) ? "<network>" : symbol.Format(style)) + "(" + parameters.Format() + ") {" + Environment.NewLine + body.Format() + Environment.NewLine + "}";
            else return "unknown format: " + style.dataFormat;
        }
        public void Apply(List<Value> arguments, Netlist netlist, Style style) {
            Env ignoreEnv = body.Eval(env.ExtendValues(parameters.parameters, arguments, null, (symbol == null ? "<nameless>" : symbol.Format(style)), style), netlist, style);
        }
    }

    // FLOWS

    // A flow expression should evaluate to NormalFlow (BoolFlow, NumberFlow, SpeciesFlow, or OpFlow combining them)
    // on the way to producing those, OperatorFlow, and FunctionFlow are also used, but they must be expanded to the above by FunctionInstance
    // if non-normal Flows survive, errors are give at simulation time (for '{{...}}' flows) or report time (for 'report ...' flows)

    public abstract class Flow : Value {
        public abstract bool Involves(List<SpeciesValue> species);
        public abstract bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered);

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
        public bool HasStochasticVariance() { return HasStochasticMean(); } // currently defined as HasStochasticMean
        // public bool HasStochasticVariance() { return LinearCombination(); } // this prevents poisson(3) to appear in LNA report
        // = Can appear in LNA charts as means and variance. 
        public abstract bool LinearCombination();
        // = Is a linear combination of species and time/kelvin/celsius flows only
        public abstract bool HasNullVariance();
        // = Has stochastic variance identically zero, used to detect and rule out non-linear products.

        public abstract Flow Differentiate(Style style); // symbolic differentiation
        public static Vector nilFlux = new Vector(new double[0]);
    }

    public class BoolFlow : Flow {
        public bool value;
        public BoolFlow(bool value) { this.type = new Type("flow"); this.value = value; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
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
        public override Flow Differentiate(Style style) { throw new Error("Non differentiable: bool"); }
    }

    public class NumberFlow : Flow {
        public double value;
        public NumberFlow(double value) { this.type = new Type("flow"); this.value = value; }
        public static NumberFlow numberFlowZero = new NumberFlow(0.0);
        public static NumberFlow numberFlowOne = new NumberFlow(1.0);
        public static NumberFlow numberFlowTwo = new NumberFlow(2.0);
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
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
        public override Flow Differentiate(Style style) { return NumberFlow.numberFlowZero; }
    }

    public class StringFlow : Flow { // this is probably not very useful, but e.g. cond("a"="b",a,b)
        public string value;
        public StringFlow(string value) { this.type = new Type("flow"); this.value = value; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
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
        public override Flow Differentiate(Style style) { throw new Error("Non differentiable: string"); }
    }

    public class SpeciesFlow : Flow {
        public Symbol species;
        public SpeciesFlow(Symbol species) { this.type = new Type("flow"); this.species = species; }
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
        public override string Format(Style style) {
            string name = this.species.Format(style);
            if (style.exportTarget == ExportTarget.CRN) name = "[" + name + "]";
            return name;
        }
        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            throw new Error("Flow expression: bool expected instead of species: " + Format(style));
        }
        public override double ObserveMean(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            return state.Mean(sample.speciesIndex[this.species]);
        }
        public override double ObserveVariance(SampleValue sample, double time, State state, Style style) {
            if (!state.Lna()) throw new Error("Variance not supported in current state");
            int i = sample.speciesIndex[this.species];
            return state.Covar(i, i);
        }
        public override double ObserveCovariance(Flow other, SampleValue sample, double time, State state, Style style) {
            if (!state.Lna()) throw new Error("Covariance not supported in current state");
            if (other is SpeciesFlow)
                return state.Covar(sample.speciesIndex[this.species], sample.speciesIndex[((SpeciesFlow)other).species]);
            else return other.ObserveCovariance(this, sample, time, state, style);
        }
        public override double ObserveDiff(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            if (flux == null) throw new Error("Non differentiable: " + species.Format(style));
            return flux(time, state.MeanVector())[sample.speciesIndex[this.species]];   //### to optimize this we should memoize flux(time, state.MeanVector()) for the latest time
        }
        public override bool HasDeterministicValue() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return false; }
        public override Flow Differentiate(Style style) { return new OpFlow("∂", true, this); } // a species 'a' is differentiated as '∂a'
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
            this.type = new Type("flow");
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

        // Construct and simplify flow operators:
        // 1) reduce the number of operators, or else
        // 2) reduce the total number of operator arguments, or else
        // 3) move numbers to the left, or else
        // 4) move minus to the outside, or else
        // 5) reduce the number of divisions and exponentiations
        public static Flow Op(string op) {
            return new OpFlow(op, false);
        }
        public static Flow Op(string op, Flow arg) {
            if (op == "-") return Minus(arg);
            if (op == "log") return Log(arg);
            if (op == "∂") return new OpFlow(op, true, arg);
            return new OpFlow(op, false, arg);
        }
        public static Flow Op(string op, Flow arg1, Flow arg2) {
            if (op == "+") return Plus(arg1, arg2);
            if (op == "-") return Minus(arg1, arg2);
            if (op == "*") return Mult(arg1, arg2);
            if (op == "/") return Div(arg1, arg2);
            if (op == "^") return Pow(arg1, arg2);
            return new OpFlow(op, false, arg1, arg2);
        }
        public static Flow Op(string op, Flow arg1, Flow arg2, Flow arg3) {
            if (op == "cond") return Cond(arg1, arg2, arg3);
            return new OpFlow(op, false, arg1, arg2, arg3);
        }
        private static Flow Plus(Flow arg1, Flow arg2) {
            if (arg1 is NumberFlow && (arg1 as NumberFlow).value == 0.0) return arg2; // 0 + a = a
            if (arg2 is NumberFlow && (arg2 as NumberFlow).value == 0.0) return arg1; // a + 0 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value + (arg2 as NumberFlow).value); // n + m = n+m
            if ((!(arg1 is NumberFlow)) && arg2 is NumberFlow) return Plus(arg2, arg1); // a + n = n + a
            if (arg1 is OpFlow && (arg1 as OpFlow).op == "-" && (arg1 as OpFlow).arity == 1) return Minus(arg2, (arg1 as OpFlow).args[0]); // (-a) + b = b - a
            if (arg2 is OpFlow && (arg2 as OpFlow).op == "-" && (arg2 as OpFlow).arity == 1) return Minus(arg1, (arg2 as OpFlow).args[0]); // a + (-b) = a - b
            return new OpFlow("+", true, arg1, arg2);
        }
        private static Flow Minus(Flow arg) {
            if (arg is NumberFlow) return new NumberFlow(-(arg as NumberFlow).value); // -(n) = -n
            if (arg is OpFlow && (arg as OpFlow).op == "-" && (arg as OpFlow).arity == 1) return (arg as OpFlow).args[0]; // -(-a) = a
            return new OpFlow("-", true, arg);
        }
        private static Flow Minus(Flow arg1, Flow arg2) {
            if (arg1 is NumberFlow && (arg1 as NumberFlow).value == 0.0) return Minus(arg2); // 0 - a = -a
            if (arg2 is NumberFlow && (arg2 as NumberFlow).value == 0.0) return arg1; // a - 0 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value - (arg2 as NumberFlow).value); // n - m = n-m
            if ((!(arg1 is NumberFlow)) && arg2 is NumberFlow) return Plus(new NumberFlow(-(arg2 as NumberFlow).value), arg1); // a - n = -n + a
            if (arg1 is OpFlow && (arg1 as OpFlow).op == "-" && (arg1 as OpFlow).arity == 1) return Minus(Plus((arg1 as OpFlow).args[0], arg2)); // (-a) - b = -(a + b)
            if (arg2 is OpFlow && (arg2 as OpFlow).op == "-" && (arg2 as OpFlow).arity == 1) return Plus(arg1, (arg2 as OpFlow).args[0]); // a - (-b) = a + b
            return new OpFlow("-", true, arg1, arg2);
        }
        private static Flow Mult(Flow arg1, Flow arg2) {
            if (arg1 is NumberFlow && (arg1 as NumberFlow).value == 0.0) return NumberFlow.numberFlowZero; // 0 * a = 0
            if (arg2 is NumberFlow && (arg2 as NumberFlow).value == 0.0) return NumberFlow.numberFlowZero; // a * 0 = 0
            if (arg1 is NumberFlow && (arg1 as NumberFlow).value == 1.0) return arg2; // 1 * a = a
            if (arg2 is NumberFlow && (arg2 as NumberFlow).value == 1.0) return arg1; // a * 1 = a
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value * (arg2 as NumberFlow).value); // n * m = n*m
            if ((!(arg1 is NumberFlow)) && arg2 is NumberFlow) return Mult(arg2, arg1); // a * n = n * a
            if (arg1 is NumberFlow && (arg1 as NumberFlow).value == -1.0) return Minus(arg2); // -1 * a = -a
            if (arg1 is OpFlow && (arg1 as OpFlow).op == "-" && (arg1 as OpFlow).arity == 1) return Minus(Mult((arg1 as OpFlow).args[0], arg2)); // (-a) * b = -(a*b)
            if (arg2 is OpFlow && (arg2 as OpFlow).op == "-" && (arg2 as OpFlow).arity == 1) return Minus(Mult(arg1, (arg2 as OpFlow).args[0])); // a * (-b) = -(a*b)
            return new OpFlow("*", true, arg1, arg2);
        }
        private static Flow Div(Flow arg1, Flow arg2) {
            if (arg1 is NumberFlow && arg2 is NumberFlow) return new NumberFlow((arg1 as NumberFlow).value / (arg2 as NumberFlow).value); // n / m = n/m  including +/-infinity or NaN if arg2=0
            if (arg1 is OpFlow && (arg1 as OpFlow).op == "-" && (arg1 as OpFlow).arity == 1) return Minus(Div((arg1 as OpFlow).args[0], arg2)); // (-a) / b = -(a/b)
            if (arg2 is OpFlow && (arg2 as OpFlow).op == "-" && (arg2 as OpFlow).arity == 1) return Minus(Div(arg1, (arg2 as OpFlow).args[0])); // a / (-b) = -(a/b)
            if (arg2 is OpFlow && (arg2 as OpFlow).op == "/") return Mult(arg1, Div((arg2 as OpFlow).args[1], (arg2 as OpFlow).args[0]));  // a / (b/c) = a * (c/b)
            if (arg1 is OpFlow && (arg1 as OpFlow).op == "/") return Div((arg1 as OpFlow).args[0], Mult((arg1 as OpFlow).args[1], arg2));  // (a / b) / c = a / (b*c)
            return new OpFlow("/", true, arg1, arg2);
        }
        private static Flow Pow(Flow arg1, Flow arg2) {
            if (arg2 is NumberFlow && (arg2 as NumberFlow).value == 0.0) return NumberFlow.numberFlowOne; // a^0 = 1
            if (arg1 is NumberFlow && (arg1 as NumberFlow).value == 0.0) return arg1; // 0^a = 0
            if (arg1 is NumberFlow && (arg1 as NumberFlow).value == 1.0) return arg1; // 1^a = 1
            if (arg2 is NumberFlow && (arg2 as NumberFlow).value == 1.0) return arg1; // a^1 = a
            if (arg1 is OpFlow && (arg1 as OpFlow).op == "^") return Pow((arg1 as OpFlow).args[0], Mult((arg1 as OpFlow).args[1], arg2));  // (a ^ b) ^ c = a ^ (b * c)
            return new OpFlow("^", true, arg1, arg2);
        }
        private static Flow Log(Flow arg) {
            if (arg is NumberFlow && (arg as NumberFlow).value == 1.0) return NumberFlow.numberFlowZero; // log(1) = 0
            if (arg is NumberFlow && (arg as NumberFlow).value == Math.E) return NumberFlow.numberFlowOne; // log(e) = 1
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
            if (this.arity == 0) return op;
            else if (arity == 1) {
                if (this.infix) return "(" + op + " " + SubFormat(op, args[0], style) + ")";
                else return op + "(" + args[0].TopFormat(style) + ")";
            } else if (arity == 2) {
                if (this.infix) {
                    string arg1 = SubFormat(op, args[0], style);
                    string arg2 = SubFormat(op, args[1], style);
                    if (op == "-" && style.exportTarget == ExportTarget.LBS) return arg1 + " -- " + arg2;
                    else if (style.exportTarget == ExportTarget.LBS) return arg1 + " " + op + " " + arg2;
                    else return "(" + arg1 + " " + op + " " + arg2 + ")";
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

        private static string SubFormat(string op, Flow subOp, Style style) {
            if ((op == "+" || op == "-") && subOp is OpFlow && 
                ((subOp as OpFlow).op == "+" || (subOp as OpFlow).op == "-" ||
                 (subOp as OpFlow).op == "*" || (subOp as OpFlow).op == "/" ||
                 (subOp as OpFlow).op == "^")
               )
               return subOp.TopFormat(style);
            if ((op == "*" || op == "/") && subOp is OpFlow && 
                ((subOp as OpFlow).op == "*" || (subOp as OpFlow).op == "/" ||
                 (subOp as OpFlow).op == "^")
               )
               return subOp.TopFormat(style);
            return subOp.Format(style);
        }

        public override bool ObserveBool(SampleValue sample, double time, State state, Func<double, Vector, Vector> flux, Style style) {
            string BadArgs() { return "Flow expression: Bad arguments to '" + op + "'"; }
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
                    else if ((args[0] is NumberFlow || args[0] is SpeciesFlow) && (args[1] is NumberFlow || args[1] is SpeciesFlow))
                        return NumberValue.EqualDouble(args[0].ObserveMean(sample, time, state, flux, style), args[1].ObserveMean(sample, time, state, flux, style));
                    else throw new Error(BadArgs());
                } else if (op == "<>") {
                    if (args[0] is BoolFlow && args[1] is BoolFlow) return ((BoolFlow)args[0]).value != ((BoolFlow)args[1]).value;
                    else if (args[0] is StringFlow && args[1] is StringFlow) return ((StringFlow)args[0]).value != ((StringFlow)args[1]).value;
                    else if ((args[0] is NumberFlow || args[0] is SpeciesFlow) && (args[1] is NumberFlow || args[1] is SpeciesFlow))
                        return !NumberValue.EqualDouble(args[0].ObserveMean(sample, time, state, flux, style), args[1].ObserveMean(sample, time, state, flux, style));
                    else throw new Error(BadArgs());
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
            string BadArgs() { return "Flow expression: Bad arguments to '" + op + "'"; }
            string BadResult() { return "Flow expression: numerical operator expected instead of '" + op + "'"; }
            if (arity == 0) {
                if (op == "time") return time;
                else if (op == "kelvin") return sample.Temperature();
                else if (op == "celsius") return sample.Temperature() - 273.15;
                else if (op == "volume") return sample.Volume();
                else throw new Error(BadResult());
            } else if (arity == 1) {
                double arg1 = args[0].ObserveMean(sample, time, state, flux, style);
                if (op == "var") {
                    return args[0].ObserveVariance(sample, time, state, style);              // Mean(var(X)) = var(X)  since var(X) is a number
                } else if (op == "poisson") {
                    return args[0].ObserveMean(sample, time, state, flux, style);            // Mean(poisson(X)) = X
                } else if (op == "∂") {
                    return args[0].ObserveDiff(sample, time, state, flux, style);
                } else {
                    if (op == "-") return -arg1;
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
                if (op == "∂") return true;
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
            } else if (arity == 1) {                                     // exclude (op == "var" || op == "poisson" || op == "∂" )
                if (op == "-") return args[0].LinearCombination();
                else return false;
            } else if (arity == 2) {                                     // exclude (op == "cov" || op == "gauss")
                if (op == "+" || op == "-") return args[0].LinearCombination() && args[1].LinearCombination();
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
            string BadArgs() { return "Flow expression: Bad arguments to '" + op + "'"; }
            string BadResult() { return "Flow expression: Variance invalid for operator '" + op + "'"; }
            Func<double, Vector, Vector> flux = null; // disallow "∂", we can't allow "∂(var(a))", but we could build in "∂var(a)" evaluated via flux.CovarMatrix
            if (arity == 0) {
                return 0.0; // "time", "kelvin", "celsius", "volume"                                       // Var(constant) = 0
            } else if (arity == 1) {
                if (op == "var") {
                    return 0.0;      // yes needed for e.g. "report a + var(a)"                            // Var(var(X)) = 0 since var(X) is a number
                } else if (op == "poisson")
                    return Math.Abs(args[0].ObserveMean(sample, time, state, flux, style));                 // Var(poisson(X)) = Abs(mean(X))
                else if (op == "-") {
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
            string BadArgs() { return "Flow expression: Bad arguments to '" + op + "'"; }
            string BadResult() { return "Flow expression: Covariance invalid for operator '" + op + "'"; }
            Func<double, Vector, Vector> flux = null; // disallow "∂", we can't allow "∂(cov(a,b))", but we could build in "∂cov(a,b)" evaluated via flux.CovarMatrix
            if (arity == 0) {
                return 0.0; // "time", "kelvin", "celsius", "volume"                                         // Cov(number,Y) = 0
            } else if (arity == 1) {
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
        public override Flow Differentiate(Style style) { // symbolic differentiation w.r.t. "time". 
            const string Bad = "Non differentiable: ";
            if (arity == 0) {
                if (op == "time") // ∂time = 1.0
                    return NumberFlow.numberFlowOne;
                else return NumberFlow.numberFlowZero; // "pi", "e", "kelvin", "celsius", "volume" // ∂k = 0.0
            } else if (arity == 1) {
                if (op == "-") // ∂-f(time) = -∂f(time)
                    return Minus(args[0].Differentiate(style));
                else if (op == "exp") // ∂(e^f(time)) = e^f(time) * ∂f(time)
                    return Mult(this, args[0].Differentiate(style));
                else if (op == "log") // ∂ln(f(time)) = 1/time * ∂f(time), for time > 0
                    return Mult(Div(NumberFlow.numberFlowOne, new OpFlow("time", false)), args[0].Differentiate(style));
                else if (op == "sqrt") // ∂sqrt(f(time)) = 1/(2*sqrt(f(time))) * ∂f(time)
                    return Mult(Div(NumberFlow.numberFlowOne, Mult(new NumberFlow(2.0), new OpFlow("sqrt", false, args[0]))), args[0].Differentiate(style));
                else if (op == "sign") // ∂sign(f(time)) = 0
                    return NumberFlow.numberFlowZero;
                else if (op == "abs") // ∂abs(f(time)) = sign(f(time)) * ∂f(time)
                    return Mult(OpFlow.Op("sign", args[0]), args[0].Differentiate(style));
                else if (op == "sin") // ∂sin(f(time)) = cos(f(time)) * ∂f(time);   e.g. ∂sin(s) = cos(s)*∂s for a species s
                    return Mult(new OpFlow("cos", false, args[0]), args[0].Differentiate(style));
                else if (op == "cos") // ∂cos(f(time)) = -sin(f(time)) * ∂f(time)
                    return Mult(Minus(new OpFlow("sin", false, args[0])), args[0].Differentiate(style));
                else if (op == "tan") // ∂tan(f(time)) = 1/cos(f(time))^2 * ∂f(time)
                    return Mult(Div(NumberFlow.numberFlowOne, Pow(new OpFlow("cos", false, args[0]), NumberFlow.numberFlowTwo)), args[0].Differentiate(style));
                else if (op == "sinh") // ∂sinh(f(time)) = cosh(f(time)) * ∂f(time)
                    return Mult(new OpFlow("cosh", false, args[0]), args[0].Differentiate(style));
                else if (op == "cosh") // ∂cosh(f(time)) = sinh(f(time)) * ∂f(time)
                    return Mult(new OpFlow("sinh", false, args[0]), args[0].Differentiate(style));
                else if (op == "tanh") // ∂tanh(f(time)) = (1-tanh(f(time))^2) * ∂f(time)
                    return Mult(Minus(NumberFlow.numberFlowOne, Pow(new OpFlow("tanh", false, args[0]), NumberFlow.numberFlowTwo)), args[0].Differentiate(style));
                // ### etc.
                else throw new Error(Bad + op); // "var", "poisson", "∂" cannot support second derivative
            } else if (arity == 2) {
                if (op == "+") // ∂(f(time)+g(time)) = ∂f(time)+∂g(time)
                    return Plus(args[0].Differentiate(style), args[1].Differentiate(style));
                else if (op == "-") // ∂(f(time)-g(time)) = ∂f(time)-∂g(time)
                    return Minus(args[0].Differentiate(style), args[1].Differentiate(style));
                else if (op == "*") // ∂(f(time)*g(time)) = ∂f(time)*g(time) + f(time)*∂g(time)
                    return Plus(
                        Mult(args[0].Differentiate(style), args[1]),
                        Mult(args[0], args[1].Differentiate(style)));
                else if (op == "/") // ∂(f(time)/g(time)) = (∂f(time)*g(time) - f(time)*∂g(time)) / g(time)^2
                    return
                        Div(
                            Minus(
                               Mult(args[0].Differentiate(style), args[1]),
                               Mult(args[0], args[1].Differentiate(style))),
                            Pow(args[1], new NumberFlow(2.0)));              
                else if (op == "^")  
                    if (args[0] is NumberFlow && (args[0] as NumberFlow).value == Math.E) { // ∂(e^f(time)) = e^f(time) * ∂f(time)  // special case if base is e
                        return Mult(this, args[1].Differentiate(style));
                    } else if (args[1] is NumberFlow) { // ∂(f(time)^n) = n*(f(time)^(n-1))*∂f(time) // special case if exponent is constant
                        double power = (args[1] as NumberFlow).value;
                        return
                            Mult(
                                Mult(args[1],
                                    Pow(args[0], new NumberFlow(power-1))),
                                args[0].Differentiate(style));
                    } else { // ∂(f(time)^g(time)) = g(time)*(f(time)^(g(time)-1))*∂f(time) + (f(time)^g(time))*ln(f(time))*∂g(time)
                             //   = (f(time)^(g(time)-1)) * (g(time)*∂f(time) + f(time)*ln(f(time))*∂g(time))
                        return
                           Mult(
                              Pow(args[0], Minus(args[1], NumberFlow.numberFlowOne)),
                              Plus(
                                 Mult(args[1], args[0].Differentiate(style)),
                                 Mult(args[0],
                                    Mult(
                                       Log(args[0]), 
                                       args[1].Differentiate(style)
                                    )
                                 )
                              )
                           );
                    }
                else throw new Error(Bad + op);
            } else if (arity == 3) {
                if (op == "cond")
                    return Cond(args[0], args[1].Differentiate(style), args[2].Differentiate(style));
                else  throw new Error(Bad + op);
            } else throw new Error(Bad + op);
        }
    }

    // ABSTRACT SYNTAX TREES

    public abstract class Tree {
        public abstract string Format();
    }

    // EXPRESSION

    public abstract class Expression : Tree {
        public abstract void Scope(Scope scope);
        public abstract Value Eval(Env env, Netlist netlist, Style style);
        public abstract Value EvalFlow(Env env, Style style); // does the same as Eval but with restriction so needs no netlist. Used in building Flows, but it returns a Value not a Flow
        public abstract Flow BuildFlow(Env env, Style style); // builds a Flow, may call EvalFlow to expand funtion invocations and if-then-else into Flows
    }

    public class Variable : Expression {
        private string name;
        public Variable(string name) {
            this.name = name;
        }
        public override string Format() {
            return name;
        }
        public override void Scope(Scope scope) {
            if (!scope.Lookup(this.name)) throw new Error("UNDEFINED variable: " + this.name);
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            return env.LookupValue(this.name);
        }
        public override Value EvalFlow(Env env, Style style) {
            return env.LookupValue(this.name);
        }
        public override Flow BuildFlow(Env env, Style style) {
            Value value = env.LookupValue(this.name); // we must convert this Value into a Flow
            Flow flow = value.ToFlow();
            if (flow == null) new Error("Flow expression: Variable '" + this.Format() + "' should denote a flow");
            return flow;
        }
    }

    public class BoolLiteral : Expression {
        public bool value;
        public BoolLiteral(bool value) { this.value = value; }
        public override string Format() { if (this.value) return "true"; else return "false"; }
        public override void Scope(Scope scope) { }
        public override Value Eval(Env env, Netlist netlist, Style style) { return new BoolValue(this.value); }
        public override Value EvalFlow(Env env, Style style) { return new BoolValue(this.value); }
        public override Flow BuildFlow(Env env, Style style) { return new BoolFlow(this.value); }
    }

    public class NumberLiteral : Expression {
        public double value;
        public NumberLiteral(double value) { this.value = value; }
        public override string Format() { return this.value.ToString(); }
        public override void Scope(Scope scope) { }
        public override Value Eval(Env env, Netlist netlist, Style style) { return new NumberValue(this.value); }
        public override Value EvalFlow(Env env, Style style) { return new NumberValue(this.value); }
        public override Flow BuildFlow(Env env, Style style) { return new NumberFlow(this.value); }
    }

    public class StringLiteral : Expression {
        public string value;
        public StringLiteral(string value) { this.value = value; }
        public override string Format() { return this.value; }
        public override void Scope(Scope scope) { }
        public override Value Eval(Env env, Netlist netlist, Style style) { return new StringValue(this.value); }
        public override Value EvalFlow(Env env, Style style) { return new StringValue(this.value); }
        public override Flow BuildFlow(Env env, Style style) { return new StringFlow(this.value); }
    }

    public class ListLiteral : Expression {
        public Expressions elements;
        public ListLiteral(Expressions elements) { this.elements = elements; }
        public override string Format() { return "[" + elements.Format() + "]";}
        public override void Scope(Scope scope) {
            foreach (Expression element in elements.expressions) element.Scope(scope);
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            List<Value> values = new List<Value>();
            foreach (Expression element in elements.expressions) values.Add(element.Eval(env, netlist, style));
            return new ListValue<Value>(values);
        }
        public override Value EvalFlow(Env env, Style style) {
            List<Value> values = new List<Value>();
            foreach (Expression element in elements.expressions) values.Add(element.EvalFlow(env, style));
            return new ListValue<Value>(values);
        }
        public override Flow BuildFlow(Env env, Style style) {
            throw new Error("Flow expression: a list is not a flow: " + this.Format());
        }
    }

    public class Distribution {
        private static Random random = new Random();
        string distribution;
        Expressions arguments;
        public Distribution(string distribution, Expressions arguments) {
            this.distribution = distribution;
            this.arguments = arguments;
        }
        public string Format() { return this.distribution + '(' + this.arguments.Format() + ')'; }
        public void Scope(Scope scope) {
            arguments.Scope(scope);
        }
        public DistributionValue Eval(Symbol parameter, Env env, Netlist netlist, Style style) {
            return DistrEval(parameter, this.arguments.Eval(env, netlist, style), style);
        }
        public DistributionValue EvalFlow(Symbol parameter, Env env, Style style) {
            return DistrEval(parameter, this.arguments.EvalFlow(env, style), style);
        }
        private DistributionValue DistrEval(Symbol parameter, List<Value> arguments, Style style) {
            double[] args = new double[arguments.Count];
            for (int i = 0; i < args.Length; i++) args[i] = (arguments[i] as NumberValue).value;
            double oracle = Gui.gui.ParameterOracle(parameter.Format(style)); // returns NaN unless the value has been locked in the Gui
            if (!double.IsNaN(oracle)) {
                return new DistributionValue(parameter, oracle, distribution, args);
            } else if (distribution == "uniform" && arguments.Count == 2) {
                if (arguments[0] is NumberValue && arguments[1] is NumberValue) {
                    double lo = (arguments[0] as NumberValue).value;
                    double hi = (arguments[1] as NumberValue).value;
                    if (lo <= hi) return new DistributionValue(parameter, random.NextDouble() * (hi - lo) + lo, distribution, args);
                    else throw new Error("Bad distribution: " + this.Format());
                } else throw new Error("Bad distribution: " + this.Format());
            } else if (distribution == "normal" && arguments.Count == 2) {
                if (arguments[0] is NumberValue && arguments[1] is NumberValue) {
                    double mean = (arguments[0] as NumberValue).value;
                    double stdev = (arguments[1] as NumberValue).value;
                    if (stdev >= 0) {
                        double u1 = 1.0 - random.NextDouble();
                        double u2 = 1.0 - random.NextDouble();
                        double normal01 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                        return new DistributionValue(parameter, mean + stdev * normal01, distribution, args);
                    } else throw new Error("Bad distribution: " + this.Format());
                } else throw new Error("Bad distribution: " + this.Format());
            } else if (distribution == "exponential" && arguments.Count == 1) {
                if (arguments[0] is NumberValue) {
                    double lambda = (arguments[0] as NumberValue).value;
                    if (lambda > 0) return new DistributionValue(parameter, Math.Log(1 - random.NextDouble()) / (-lambda), distribution, args);
                    else throw new Error("Bad distribution: " + this.Format());
                } else throw new Error("Bad distribution: " + this.Format());
            } else if (distribution == "parabolic" && arguments.Count == 2) {
                if (arguments[0] is NumberValue && arguments[1] is NumberValue) {
                    double center = (arguments[0] as NumberValue).value;
                    double halfwidth = (arguments[1] as NumberValue).value;
                    if (halfwidth >= 0) { //https://stats.stackexchange.com/questions/173637/generating-a-sample-from-epanechnikovs-kernel
                        double u1 = 2 * random.NextDouble() - 1.0;
                        double u2 = 2 * random.NextDouble() - 1.0;
                        double u3 = 2 * random.NextDouble() - 1.0;
                        double sample01 = (Math.Abs(u3) >= Math.Abs(u2) && Math.Abs(u3) >= Math.Abs(u1)) ? u2 : u3; 
                        return new DistributionValue(parameter, center + halfwidth * sample01, distribution, args);
                    } else throw new Error("Bad distribution: " + this.Format());
                } else throw new Error("Bad distribution: " + this.Format());
            } else if (distribution == "bernoulli" && arguments.Count == 1) {
                if (arguments[0] is NumberValue) {
                    double p = (arguments[0] as NumberValue).value;
                    if (p >= 0 && p <= 1) return new DistributionValue(parameter, (random.NextDouble() >= (1 - p) ? 1 : 0), distribution, args);
                    else throw new Error("Bad distribution: " + this.Format());
                } else throw new Error("Bad distribution: " + this.Format());
            } else throw new Error("Bad distribution: " + this.Format());
        } 
    }

    public class ParameterInfo {
        public string parameter;
        public double drawn;
        public string distribution;
        public double[] arguments;
        public double rangeMin;
        public double rangeMax;
        public double range;
        public bool locked;
        public ParameterInfo(string parameter, double drawn, string distribution, double[] arguments) {
            this.parameter = parameter;
            this.drawn = drawn;
            this.distribution = distribution;
            this.arguments = arguments;
            this.locked = false;
            if (distribution == "uniform") {
                this.rangeMin = Math.Min(arguments[0], drawn);
                this.rangeMax = Math.Max(arguments[1], drawn);
            } else if (distribution == "normal") {
                this.rangeMin = Math.Min(arguments[0] - 5 * arguments[1], drawn);
                this.rangeMax = Math.Max(arguments[0] + 5 * arguments[1], drawn);
            } else if (distribution == "exponential") {
                this.rangeMin = 0;
                this.rangeMax = Math.Max(5 / arguments[0], drawn);
            } else if (distribution == "parabolic") {
                this.rangeMin = Math.Min(arguments[0] - arguments[1], drawn);
                this.rangeMax = Math.Max(arguments[0] + arguments[1], drawn);
            } else if (distribution == "bernoulli") {
                this.rangeMin = 0;
                this.rangeMax = 1;
            }
            this.range = this.rangeMax - this.rangeMin;
        }
        public string ParameterLabel(bool twoLines) {
            string args = "";
            for (int i = 0; i < arguments.Length; i++) args += arguments[i].ToString("G4") + ", ";
            if (args != "") args = args.Substring(0, args.Length - 2);
            args = "(" + args + ")";
            return parameter + " = " + drawn.ToString("G3") + ((twoLines)? Environment.NewLine : ". Drawn from ") + distribution + args;
        }
    }

    public class FunctionAbstraction : Expression {
        private Parameters parameters;
        private Expression body;
        public FunctionAbstraction(Parameters parameters, Expression body) {
            this.parameters = parameters;
            this.body = body;
        }
        public override string Format() {
            return "fun (" + parameters.Format() + ") {" + body.Format() + "}";
        }
        public override void Scope(Scope scope) {
            body.Scope(scope.Extend(parameters.parameters));
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            return new FunctionValue(null, parameters, body, env);
        }
        public override Value EvalFlow(Env env, Style style) {
            return new FunctionValue(null, parameters, body, env);
        }
        public override Flow BuildFlow(Env env, Style style) {
            throw new Error("Flow expression: function abstraction is not a flow: " + this.Format());
        }
    }

    public class NetworkAbstraction : Expression {
        private Parameters parameters;
        private Statements body;
        public NetworkAbstraction(Parameters parameters, Statements body) {
            this.parameters = parameters;
            this.body = body;
        }
        public override string Format() {
            return "net (" + parameters.Format() + ") {" + body.Format() + "}";
        }
        public override void Scope(Scope scope) {
            body.Scope(scope.Extend(parameters.parameters));
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            return new NetworkValue(null, parameters, body, env);
        }
        public override Value EvalFlow(Env env, Style style) {
            throw new Error("Flow expression: network abstractions is not a flow: " + this.Format());
        }
        public override Flow BuildFlow(Env env, Style style) {
            throw new Error("Flow expression: network abstractions is not a flow: " + this.Format());
        }
    }

    public class BlockExpression : Expression {
        public Statements statements;
        public Expression expression;
        public BlockExpression(Statements statements, Expression expression) {
            this.statements = statements;
            this.expression = expression;
        }
        public override string Format() {
            if (statements.statements.Count() == 0) return this.expression.Format();
            else return "define " + Environment.NewLine + this.statements.Format() + Environment.NewLine + "return " + this.expression.Format();
        }
        public override void Scope(Scope scope) {
            this.expression.Scope(this.statements.Scope(scope));
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            Env extEnv = this.statements.Eval(env, netlist, style);
            return this.expression.Eval(extEnv, netlist, style);
        }
        public override Value EvalFlow(Env env, Style style) {
            // this should never happen because BuildFlow of a BlockExpression will directly call BuildFlow of the value definition statements and of the final expression
            throw new Error("BlockExpression EvalFlow " + this.Format());
        }
        public override Flow BuildFlow(Env env, Style style) {
            Env extEnv = env;
            foreach (Statement statement in statements.statements) {
                if (statement is ValueDefinition) extEnv = ((ValueDefinition)statement).BuildFlow(extEnv, style);
                else throw new Error("Flow expression: function bodies can contain only value definitions (including flow definitions) and a final flow expression; functions with flow parameters to be invoked there can be defined externally: " + Format());
            }
            return this.expression.BuildFlow(extEnv, style);
        }
    }

    public class FunctionInstance : Expression {
        private Expression function;
        private Expressions arguments;
        private bool infix; // just for Format
        private int arity; // just for Format
        public FunctionInstance(Expression function, Expressions arguments, bool infix = false, int arity = 0) {
            this.function = function;
            this.arguments = arguments;
            this.infix = infix;
            this.arity = arity;
        }
        public override string Format() {
            if (!infix) return function.Format() + "(" + arguments.Format() + ")";
            List<Expression> args = arguments.expressions;
            if (arity == 1) return "(" + function.Format() + " " + args[0].Format() + ")";
            if (arity == 2) return "(" + args[0].Format() + " " + function.Format() + " " + args[1].Format() + ")";
            if (arity == 3) return "if " + args[0].Format() + " then " + args[1].Format() + " else " + args[2].Format() + " end";
            return "???";
        }
        public override void Scope(Scope scope) {
            function.Scope(scope);
            arguments.Scope(scope);
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            Value value = this.function.Eval(env, netlist, style);
            if (value is FunctionValue) {
                FunctionValue closure = (FunctionValue)value;
                List<Value> arguments = this.arguments.Eval(env, netlist, style);
                string invocation = "";
                if (style.traceComputational) {
                    Style restyle = style.RestyleAsDataFormat("symbol");
                    invocation = closure.Format(restyle) + "(" + Expressions.FormatValues(arguments, restyle) + ")";
                    netlist.Emit(new CommentEntry("BEGIN " + invocation));
                }
                Value result = closure.Apply(arguments, netlist, style);
                if (style.traceComputational) {
                    netlist.Emit(new CommentEntry("END " + invocation));
                }
                return result;
            } else if (value is OperatorValue) {
                OperatorValue oper = (OperatorValue)value;
                if (oper.name == "if") { // it was surely parsed with 3 arguments
                    List<Expression> actuals = this.arguments.expressions;
                    Value cond = actuals[0].Eval(env, netlist, style);
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].Eval(env, netlist, style); else return actuals[2].Eval(env, netlist, style);
                    else throw new Error("'if' predicate should be a bool: " + Format());
                } else if (oper.name == "observe") {
                    List<Expression> actuals = this.arguments.expressions;
                    if (actuals.Count != 1 && actuals.Count != 2) throw new Error("'observe' wrong number of arguments " + Format()); ;
                    Flow flow = actuals[0].BuildFlow(env, style);
                    Value sample = env.LookupValue("vessel");
                    if (actuals.Count == 2) sample = actuals[1].Eval(env, netlist, style);
                    if (!(sample is SampleValue)) throw new Error("'observe' second argument should be a sample: " + Format());
                    return (sample as SampleValue).Observe(flow, netlist, style);
                } else {
                    List<Value> arguments = this.arguments.Eval(env, netlist, style);
                    return oper.Apply(arguments, netlist, style);
                }
            } else if (value is ListValue<Value>) {
                ListValue<Value> list = (ListValue<Value>)value;
                List<Value> arguments = this.arguments.Eval(env, netlist, style);
                if (arguments.Count == 1) return list.Select(arguments[0], style);
                else if (arguments.Count == 2) return list.Sublist(arguments[0], arguments[1], style);
                else throw new Error("Wrong number of parameters to list selection: " + Format());
            }
            else throw new Error("Invocation of a non-function, non-list, or non-operator: " + Format());
        }
        public override Value EvalFlow(Env env, Style style) {
            Value value = this.function.EvalFlow(env, style);
            if (value is FunctionValue) {
                FunctionValue closure = (FunctionValue)value;
                List<Value> arguments = this.arguments.EvalFlow(env, style);
                return closure.ApplyFlow(arguments, style);
            } else if (value is OperatorValue) {
                OperatorValue oper = (OperatorValue)value;
                if (oper.name == "if") { // it was surely parsed with 3 arguments
                    List<Expression> actuals = this.arguments.expressions;
                    Value cond = actuals[0].EvalFlow(env, style);
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].EvalFlow(env, style); else return actuals[2].EvalFlow(env, style);
                    else throw new Error("Flow expression: 'if' predicate should be a bool: " + Format());
                } else {
                    List<Value> arguments = this.arguments.EvalFlow(env, style);
                    return oper.ApplyFlow(arguments, style);
                }
            } else if (value is ListValue<Value>) {
                ListValue<Value> list = (ListValue<Value>)value;
                List<Value> arguments = this.arguments.EvalFlow(env, style);
                if (arguments.Count == 1) return list.Select(arguments[0], style);
                else if (arguments.Count == 2) return list.Sublist(arguments[0], arguments[1], style);
                else throw new Error("Flow expression: Wrong number of parameters to list selection: " + Format());
            } else throw new Error("Flow expression: Invocation of a non-function, non-list, or non-operator: " + Format());
        }
        public override Flow BuildFlow(Env env, Style style) {
            Value value = this.function.EvalFlow(env, style);
            if (value is FunctionValue) {
                FunctionValue closure = (FunctionValue)value;
                List<Flow> arguments = this.arguments.BuildFlow(env, style); 
                return closure.BuildFlow(arguments, style);
            } else if (value is OperatorValue) {
                OperatorValue oper = (OperatorValue)value;
                if (oper.name == "if") { // it was surely parsed with 3 arguments
                    List<Expression> actuals = this.arguments.expressions;
                    Value cond = actuals[0].EvalFlow(env, style); // this is a real boolean value, not a flow
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].BuildFlow(env, style); else return actuals[2].BuildFlow(env, style);
                    else throw new Error("Flow expression: 'if' predicate should be a bool: " + Format());
                } else {
                    List<Flow> arguments = this.arguments.BuildFlow(env, style); // operator arguments are Flows that are composed with the operator
                    return oper.BuildFlow(arguments, style);
                }
            } else if (value is ListValue<Value>) {
                ListValue<Value> list = (ListValue<Value>)value;
                List<Value> arguments = this.arguments.EvalFlow(env, style);
                Value selection = null; // we must convert this Value into a Flow
                if (arguments.Count == 1) selection = list.Select(arguments[0], style);
                else if (arguments.Count == 2) selection = list.Sublist(arguments[0], arguments[1], style);
                else throw new Error("Flow expression: Wrong number of parameters to list selection: " + Format());
                Flow flow = selection.ToFlow();
                if (flow == null) new Error("Flow expression: list selection '" + this.Format() + "' should denote a flow");
                return flow;
            } else throw new Error("Flow expression: Invocation of a non-function or non-operator: " + Format());
        }
    }

    // STATEMENTS
    // definitions can be mutually recursive within a statement list, but only for contiguous closure definitions (functions, networks) not for value definitions, which are incremental
    // non-closure definitions are scoped incrementally (they can use only previously occurring value and closure definitions)
    // closure definitions are scoped recursively if they are contiguous (they can use all closure definitions in the same contiguous block, and previously occurring value and closure definitions)
    // duplicated names are not allowed within a contiguous block of closure definitions, they are allowed for value definitions or discontinous closure definitions
    // any value definition will "break" a block of closure definition and not allow them to be scoped mutually recursively

    public class Statements : Tree {
        public List<Statement> statements;
        public Statements() {
            this.statements = new List<Statement> { };
        }
        public int Count() {
            return this.statements.Count();
        }
        public Statements Add(Statement clause) {
            this.statements.Add(clause);
            return this;
        }
        public Statements Append(List<Statement> clauses) {
            this.statements.AddRange(clauses);
            return this;
        }
        public override string Format() {
            string str = "";
            foreach (Statement stat in this.statements) {
                string s = (stat == null) ? "<null statement>" : stat.Format();
                if (str == "") str = s;
                else str = str + Environment.NewLine + s;
            }
            return str;
            //return this.statements.Aggregate("", (a, b) => (a == "") ? b.Format() : a + Environment.NewLine + b.Format());
        }
        //// For reference, simple version of Scope for non-recursive statements:
        //public Scope Scope(Scope scope) { 
        //    Scope extScope = scope;
        //    foreach (Statement statement in this.statements) {
        //        extScope = statement.Scope(extScope);
        //    }
        //    return extScope;
        //}
        public Scope Scope(Scope scope) { // more complex version of Scope for recursive statement blocks, mutually recursive definitions must be contiguous
            return ScopeInc(this.statements, 0, scope);
        }
        public Scope ScopeInc(List<Statement> statements, int i, Scope scope) { // incremental Scope
            if (i >= statements.Count) return scope;
            Statement statement = statements[i];
            if (statement is FunctionDefinition || statement is NetworkDefinition)
                return ScopeRec(statements, i, scope); // switch to recursive Scope
            else {
                Scope incScope = statement.Scope(scope);
                return ScopeInc(statements, i + 1, incScope);
            }
        }
        public Scope ScopeRec(List<Statement> statements, int i, Scope scope) { // recursive Scope
            Scope recScope = scope;
            Scope dupScope = new NullScope(); // for duplication checks
            int j = i;
            while ((j < statements.Count) && (statements[j] is FunctionDefinition || statements[j] is NetworkDefinition)) {
                Statement statement = statements[j];
                string name = (statement is FunctionDefinition) ? ((FunctionDefinition)statement).Name() : ((NetworkDefinition)statement).Name();
                recScope = new ConsScope(name, recScope);
                if (dupScope.Lookup(name)) throw new Error("DUPLICATED variable in same recursive block: " + name); // check that defined names are unique
                dupScope = new ConsScope(name, dupScope);
                j = j + 1;
            }
            j = i;
            while ((j < statements.Count) && (statements[j] is FunctionDefinition || statements[j] is NetworkDefinition)) {
                Statement statement = statements[j];
                Scope ignoreScope = statement.Scope(recScope);
                j = j + 1;
            }
            return ScopeInc(statements, j, recScope); // switch to incremental Scope
        }
        //// For reference, simple version of Eval for non-recursive statement blocks
        //public void Eval(Env env, Netlist netlist) { 
        //    Env extEnv = env;
        //    foreach (Statement statement in this.statements) {
        //        extEnv = statement.Eval(extEnv, netlist);
        //    }
        //    // extEnv is not returned: the scope block is closed
        //}
        public Env Eval(Env env, Netlist netlist, Style style) { // more complex version of Eval for recursive statement blocks, mutually recursive definitions must be contiguous
            return EvalInc(this.statements, 0, env, netlist, style);
        }
        public Env EvalInc(List<Statement> statements, int i, Env env, Netlist netlist, Style style) { // incremental Eval
            if (i >= statements.Count) return env;
            Statement statement = statements[i];
            if (statement is FunctionDefinition || statement is NetworkDefinition)
                return EvalRec(statements, i, env, netlist, style); // switch to recursive eval
            else {
                Env incEnv = statement.Eval(env, netlist, style);
                return EvalInc(statements, i + 1, incEnv, netlist, style);
            }
        }
        public Env EvalRec(List<Statement> statements, int i, Env env, Netlist netlist, Style style) { // recursive Eval
            Env recEnv = env;
            int j = i;
            while ((j < statements.Count) && (statements[j] is FunctionDefinition || statements[j] is NetworkDefinition)) {
                Statement statement = statements[j];
                string name = (statement is FunctionDefinition) ? ((FunctionDefinition)statement).Name() : ((NetworkDefinition)statement).Name();
                recEnv = new ValueEnv(name, null, null, recEnv);
                j = j + 1;
            }
            j = i;
            while ((j < statements.Count) && (statements[j] is FunctionDefinition || statements[j] is NetworkDefinition)) {
                Statement statement = statements[j];
                if (statement is FunctionDefinition) {
                    Symbol symbol = recEnv.LookupSymbol(((FunctionDefinition)statement).Name());
                    FunctionValue value = ((FunctionDefinition)statement).FunctionClosure(symbol, recEnv);
                    recEnv.AssignValue(symbol, value);
                    netlist.Emit(new FunctionEntry(symbol, value));     // insert the closures in the netlist
                }
                if (statement is NetworkDefinition) {
                    Symbol symbol = recEnv.LookupSymbol(((NetworkDefinition)statement).Name());
                    NetworkValue value = ((NetworkDefinition)statement).NetworkClosure(symbol, recEnv);
                    recEnv.AssignValue(symbol, value);
                    netlist.Emit(new NetworkEntry(symbol, value));     // insert the closures in the netlist
                }
                j = j + 1;
            }
            return EvalInc(statements, j, recEnv, netlist, style); // switch to incremental eval
        }
    }

    // STATEMENT

    public abstract class Statement : Tree {
        public abstract Scope Scope(Scope scope);
        public abstract Env Eval(Env env, Netlist netlist, Style style);
    }

    //public class Directive : Statement {
    //    private string type;
    //    private string directive;
    //    private Expression expression;
    //    public Directive(string directive, Expression expression) {
    //        this.type = "directive";
    //        this.directive = directive;
    //        this.expression = expression;
    //    }
    //    public override List<string> Names() { return new List<string> { }; }
    //    public override string Type() { return this.type; }
    //    public override string Format() {
    //        return type + " " + directive + " = " + expression.Format();
    //    }
    //    public override Scope Scope(Scope scope) {
    //        expression.Scope(scope);
    //        return scope;
    //    }
    //    public override Env Eval(Env env, Netlist netlist) {
    //        Value value = this.expression.Eval(env, netlist);
    //        netlist.Emit(new DirectiveEntry(Symbol(this.directive), value));
    //        return env;
    //    }
    //}

    public class ValueDefinition : Statement {
        private string name;
        public Type type;
        private Expression definee;
        public ValueDefinition(string name, Type type, Expression definee) {
            this.name = name;
            this.type = type;
            this.definee = definee;
        }
        public override string Format() {
            return type.Format() + " " + name + " = " + definee.Format();
        }
        public override Scope Scope(Scope scope) {
            definee.Scope(scope);
            return new ConsScope(this.name, scope);
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value value = (type.Is("flow")) ? definee.BuildFlow(env, style) : definee.Eval(env, netlist, style);     // evaluate
            //Symbol symbol = new Symbol(this.name);                                                                   // create a new symbol from name    
            //Env extEnv = new ValueEnv(symbol, type, value, env);                                                     // checks that the types match
            //netlist.Emit(new ValueEntry(symbol, type, value));                                                       // embed the new symbol also in the netlist
            //return extEnv;                                                                                           // return the extended environment
            return new ValueEnv(this.name, type, value, netlist, env);  // make new symbol, check that types match, emit also in the netlist, return extended env
        }
        public Env BuildFlow(Env env, Style style) {   // special case: only value definitions among all statements support BuildFlow
            Flow flow = definee.BuildFlow(env, style);                                   // evaluate
            return new ValueEnv(this.name, new Type("flow"), flow, env);                   // checks that the ("flow") types match
        }
    }

    public class ListDefinition : Statement {
        private Parameters ids;
        private Expression definee;
        public ListDefinition(Parameters ids, Expression definee) {
            this.ids = ids;
            this.definee = definee;
        }
        public override string Format() {
            return "[" + ids.Format() + "]" + " = " + definee.Format();
        }
        public override Scope Scope(Scope scope) {
            definee.Scope(scope);
            return scope.Extend(ids.parameters);
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value value = definee.Eval(env, netlist, style); 
            if (!(value is ListValue<Value>)) throw new Error("Binding a list definition to a non-list: " + this.Format());
            List<Value> arguments = (value as ListValue<Value>).elements;
            return env.ExtendValues<Value>(ids.parameters, arguments, netlist, ids.Format(), style);
        }
        public Env BuildFlow(Env env, Style style) {   // special case: only value definitions among all statements support BuildFlow
            throw new Error("Flow expression: a list definition is not a flow definition: " + this.Format()); // would have to support ListFlow first
            //Flow flow = definee.BuildFlow(env, style);
            //if (!(flow is ListFlow)) throw new Error("Binding a list of flows definition to a non list of flows: " + this.Format()");
            //List<Flow> arguments = (flow as ListFlow).elements;
            //return env.ExtendValues<Flow>(ids.ids, arguments, null, ids.Format(), style);
        }
    }

    public class DistributionDefinition : Statement {
        private string name;
        public Type type;
        private Distribution definee;
        private Type numberType;
        public DistributionDefinition(string name, Type type, Distribution definee) {
            this.name = name;
            this.type = type;
            this.numberType = new Type("number");
            this.definee = definee;
        }
        public override string Format() {
            return numberType.Format() + " " + name + " =? " + definee.Format();
        }
        public override Scope Scope(Scope scope) {
            definee.Scope(scope);
            return new ConsScope(this.name, scope);
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Symbol symbol = new Symbol(this.name);                                                               // create a new symbol from name
            DistributionValue distribution = definee.Eval(symbol, env, netlist, style);                          // evaluate
            Env extEnv = new ValueEnv(symbol, numberType, new NumberValue(distribution.drawn), env);             // checks that the types match
            netlist.Emit(new ValueEntry(symbol, numberType, new NumberValue(distribution.drawn), distribution)); // embed the new symbol and distribution also in the netlist
            return extEnv;                                                                                        // return the extended environment
        }
        public Env BuildFlow(Env env, Style style) {
            throw new Error("A distribution cannot be a flow: " + this.Format());
        }
    }

    public class SampleDefinition : Statement {
        private string name;
        private Expression volume;
        private string volumeUnit;
        private Expression temperature;
        private string temperatureUnit;
        public SampleDefinition(string name, Expression volume, string volumeUnit, Expression temperature, string temperatureUnit) {
            this.name = name;
            this.volume = volume;
            this.volumeUnit = volumeUnit;
            this.temperature = temperature;
            this.temperatureUnit = temperatureUnit;
        }
        public override string Format() {
            return "sample " + name + " {" + volume.Format() + " " + volumeUnit + ", " + temperature.Format() + " " + temperatureUnit;
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = new ConsScope(name, scope);
            volume.Scope(scope);
            temperature.Scope(scope);
            return extScope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value volume = this.volume.Eval(env, netlist, style);
            Value temperature = this.temperature.Eval(env, netlist, style);
            if ((!(volume is NumberValue)) || (!(temperature is NumberValue))) throw new Error("Bad arg types to sample " + this.name);
            double volumeValue = Protocol.NormalizeVolume(((NumberValue)volume).value, this.volumeUnit);
            double temperatureValue = Protocol.NormalizeTemperature(((NumberValue)temperature).value, this.temperatureUnit);
            if (volumeValue <= 0) throw new Error("Sample volume must be positive: " + this.name);
            if (temperatureValue < 0) throw new Error("Sample temperature must be non-negative: " + this.name);
            Symbol symbol = new Symbol(name);
            SampleValue sample = new SampleValue(symbol, new NumberValue(volumeValue), new NumberValue(temperatureValue), produced: false); 
            netlist.Emit(new SampleEntry(sample));
            return new ValueEnv(symbol, null, sample, env);
        }
    }

    public class SpeciesDefinition : Statement {
        private List<Substance> substances;
        private Statements statements; // used for abbreviated form
        public SpeciesDefinition(List<Substance> substances, Statements statements) {
            this.substances = substances;
            this.statements = statements;
        }
        public override string Format() {
            string s = "";
            foreach (Substance substance in substances) s += substance.Format() + ", ";
            if (s.Length > 0) s = s.Substring(0, s.Length - 2); // remove last comma
            s = "new species" + "{" + s + "}";
            if (statements.Count() > 0) s += " " + statements.Format();
            return s;
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = scope;
            foreach (Substance substance in substances) {
                substance.Scope(scope);
                extScope = new ConsScope(substance.name, extScope);
            }
            statements.Scope(extScope);
            return extScope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Env extEnv = env;
            foreach (Substance substance in this.substances) {
                Symbol symbol = new Symbol(substance.name);                         // create a new symbol from name
                Value molarmassValue = substance.Eval(env, netlist, style);         // eval molarmass
                double molarmass;
                if (molarmassValue == null) molarmass = -1.0; // molarmass not specified
                else {
                    if (!(molarmassValue is NumberValue)) throw new Error("Molar mass must be a number, for species: " + substance.name);
                    molarmass = ((NumberValue)molarmassValue).value;
                    if (molarmass <= 0) throw new Error("Molar mass must be positive, for species: " + substance.name);
                }
                SpeciesValue species = new SpeciesValue(symbol, molarmass);         // use the new symbol for the uninitialized species value
                extEnv = new ValueEnv(symbol, null, species, extEnv);                // extend environment
                netlist.Emit(new SpeciesEntry(species));                            // put the species in the netlist (its initial value goes into a sample)
            }
            Env ignoreEnv = this.statements.Eval(extEnv, netlist, style);          // eval the statements in the new environment
            return extEnv;                                                         // return the environment with the new species definitions (only)
        }
    }

    public abstract class Substance : Expression {
        public string name;
        public override Value EvalFlow(Env env, Style style) { throw new Error("Substance.EvalFlow"); }
        public override Flow BuildFlow(Env env, Style style) { throw new Error("Substance.BuildFlow"); }
    }
    public class SubstanceConcentration : Substance {
        public SubstanceConcentration(string name) {
            this.name = name;
        }
        public override string Format() {
            return name;
        }
        public override void Scope(Scope scope) {
            return;
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            return null;
        }
    }
    public class SubstanceMolarmass : Substance {
        public Expression molarmass;
        public SubstanceMolarmass(string name, Expression molarmass) {
            this.name = name;
            this.molarmass = molarmass;
        }
        public override string Format() {
            return name + " # " + molarmass.Format();
        }
        public override void Scope(Scope scope) {
            molarmass.Scope(scope);
        }
        public override Value Eval(Env env, Netlist netlist, Style style) {
            return molarmass.Eval(env, netlist, style);
        }
    }

    public class FunctionDefinition : Statement {
        private string name;
        private Parameters parameters;
        private Expression body;
        public FunctionDefinition(string name, Parameters parameters, Expression expression) {
            this.name = name;
            this.parameters = parameters;
            this.body = expression;
        }
        public string Name() { return this.name; }
        public override string Format() {
            return "new function " + name + "(" + parameters.Format() + ") {" + Environment.NewLine + body.Format() + Environment.NewLine + "}";
        }
        public override Scope Scope(Scope scope) {
            body.Scope(scope.Extend(parameters.parameters));
            return new ConsScope(this.name, scope);
        }
        public FunctionValue FunctionClosure(Symbol symbol, Env env) {
            return new FunctionValue(symbol, parameters, body, env);
        }
        public override Env Eval(Env env, Netlist netlist, Style style) { // this and the related Emit are probably never executed because of the separate handing of recursive environments
            Symbol symbol = new Symbol(this.name);                              // create a new symbol from name
            FunctionValue value = this.FunctionClosure(symbol, env);
            Env extEnv = new ValueEnv(symbol, null, value, env);            // checks that the types match
            netlist.Emit(new FunctionEntry(symbol, value));                     // embed the new symbol also in the netlist
            return extEnv;                                                      // return the extended environment
        }
    }

    public class NetworkDefinition : Statement {
        private string name;
        private Parameters parameters;
        private Statements body;
        public NetworkDefinition(string name, Parameters parameters, Statements network) {
            this.name = name;
            this.parameters = parameters;
            this.body = network;
        }
        public string Name() { return this.name; }
        public override string Format() {
            return "new network " + name + "(" + parameters.Format() + ") {" + Environment.NewLine + body.Format() + Environment.NewLine + "}";
        }
        public override Scope Scope(Scope scope) {
            body.Scope(scope.Extend(parameters.parameters));
            return new ConsScope(this.name, scope);
        }
        public NetworkValue NetworkClosure(Symbol symbol, Env env) {
            return new NetworkValue(symbol, parameters, body, env);
        }
        public override Env Eval(Env env, Netlist netlist, Style style) { // this and the related Emit are probably never executed because of the separate handing of recursive environments
            Symbol symbol = new Symbol(this.name);                              // create a new symbol from name
            NetworkValue value = this.NetworkClosure(symbol, env);
            Env extEnv = new ValueEnv(symbol, null, value, env);            // checks that the types match
            netlist.Emit(new NetworkEntry(symbol, value));                      // embed the new symbol also in the netlist
            return extEnv;                                                      // return the extended environment
        }
    }

    public class IfThenElse : Statement {
        private Expression ifExpr;
        private Statements thenStatements;
        private Statements elseStatements;
        public IfThenElse(Expression ifExpr, Statements thenStatements, Statements elseStatements) {
            this.ifExpr = ifExpr;
            this.thenStatements = thenStatements;
            this.elseStatements = elseStatements;
        }
        public override string Format() {
            return "if " + ifExpr.Format() + " then " + thenStatements.Format() + " else " + elseStatements.Format() + " end";
        }
        public override Scope Scope(Scope scope) {
            ifExpr.Scope(scope);
            Scope thenScope = thenStatements.Scope(scope);
            Scope elseScope = elseStatements.Scope(scope);
            return scope; // the exended thenScope,elseScope do not affect the scope of the following statements
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value cond = this.ifExpr.Eval(env, netlist, style);
            if (cond is BoolValue) {
                if (((BoolValue)cond).value) { Env ignoreEnv = this.thenStatements.Eval(env, netlist, style); }
                else { Env ignoreEnv = this.elseStatements.Eval(env, netlist, style); }
                return env;
            } else throw new Error("Bad predicate type to 'if'");
        }
    }

    public class NetworkInstance : Statement {
        private Variable network;
        private Expressions arguments;
        public NetworkInstance(Variable network, Expressions arguments) {
            this.network = network;
            this.arguments = arguments;
        }
        public override string Format() {
            return network.Format() + "(" + arguments.Format() + ")";
        }
        public override Scope Scope(Scope scope) {
            network.Scope(scope);
            arguments.Scope(scope);
            return scope; // the exended environment resulting from the network instance does not affect the scope of the following statements
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value value = this.network.Eval(env, netlist, style);
            List<Value> arguments = this.arguments.Eval(env, netlist, style);
            if (value is NetworkValue) {
                NetworkValue closure = (NetworkValue)value;
                string invocation = "";
                if (style.traceComputational) {
                    Style restyle = style.RestyleAsDataFormat("symbol");
                    invocation = closure.Format(restyle) + "(" + Expressions.FormatValues(arguments, restyle) + ")";
                    netlist.Emit(new CommentEntry("BEGIN " + invocation));
                }
                closure.Apply(arguments, netlist, style);
                if (style.traceComputational) {
                    netlist.Emit(new CommentEntry("END " + invocation));
                }
                return env;
            } else throw new Error("Invocation of a network expected, instead of: " + value.type.Format());
        }
    }

    public class ReactionDefinition : Statement {
        public Complex reactants;
        public Complex products;
        public Rate rate;
        public ReactionDefinition(Complex reactants, Complex products, Rate rate) {
            this.reactants = reactants;
            this.products = products;
            this.rate = rate;
        }
        public override string Format() {
            return this.reactants.Format() + " -> " + this.products.Format() + " {" + this.rate.Format() + "} ";
        }
        public override Scope Scope(Scope scope) {
            reactants.Scope(scope);
            products.Scope(scope);
            rate.Scope(scope);
            return scope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            List<Symbol> reactants = this.reactants.Eval(env, netlist, style);
            List<Symbol> products = this.products.Eval(env, netlist, style);
            RateValue rate = this.rate.Eval(env, netlist, style);
            ReactionValue reaction = new ReactionValue(reactants, products, rate);
            netlist.Emit(new ReactionEntry(reaction));
            return env;
        }
    }

    public abstract class Rate {
        public abstract string Format();
        public abstract void Scope(Scope scope);
        public abstract RateValue Eval(Env env, Netlist netlist, Style style);
    }

    public class GeneralRate : Rate {
        private Expression rateFunction;
        public GeneralRate(Expression rateFunction) {
            this.rateFunction = rateFunction;
        }
        public override string Format() {
            return "{" + rateFunction.Format() + "}";
        }
        public override void Scope(Scope scope) {
            rateFunction.Scope(scope);
        }
        public override RateValue Eval(Env env, Netlist netlist, Style style) {
            Flow flow = rateFunction.BuildFlow(env, style);  // whether this is a numeric flow is checked later
            if (!flow.HasDeterministicValue()) throw new Error("This flow-expression cannot appear in {{ ... }} rate: " + rateFunction.Format());
            return new GeneralRateValue(flow); 
        }
    }

    public class MassActionRate : Rate {
        private Expression collisionFrequency;
        private Expression activationEnergy;
        public MassActionRate(Expression collisionFrequency, Expression activationEnergy) {
            this.collisionFrequency = collisionFrequency;
            this.activationEnergy = activationEnergy;
        }
        public MassActionRate(Expression collisionFrequency) {
            this.collisionFrequency = collisionFrequency;
            this.activationEnergy = new NumberLiteral(0.0);   // default
        }
        public MassActionRate() {
            this.collisionFrequency = new NumberLiteral(1.0); // default
            this.activationEnergy = new NumberLiteral(0.0);   // default
        }
        public override string Format() {
            return collisionFrequency.Format() + ", " + activationEnergy.Format();
        }
        public override void Scope(Scope scope) {
            collisionFrequency.Scope(scope);
            activationEnergy.Scope(scope);
        }
        public override RateValue Eval(Env env, Netlist netlist, Style style) {
            Value cf = collisionFrequency.Eval(env, netlist, style);
            Value ae = activationEnergy.Eval(env, netlist, style);
            if (!(cf is NumberValue)) throw new Error("Reaction rate collision frequency must be a number: " + collisionFrequency.Format());
            if (!(ae is NumberValue)) throw new Error("Reaction rate activation energy must be a number: " + activationEnergy.Format());
            double cfv = ((NumberValue)cf).value;
            double aev = ((NumberValue)ae).value;
            if (cfv < 0) throw new Error("Reaction rate collision frequency must be non-negative: " + collisionFrequency.Format() + " = " + style.FormatDouble(cfv));
            if (aev < 0) throw new Error("Reaction rate activation energy must be non-negative: " + activationEnergy.Format() + " = " + style.FormatDouble(aev));
            return new MassActionRateValue(cfv, aev);
        }
    }
        
    public class Amount : Statement {
        private List<Variable> vars;
        private Expression initial;
        private string dimension; // Mass unit, or concentration unit
        private Expression sample;
        public Amount(List<string> names, Expression initial, string dimension, Expression sample) {
            this.vars = new List<Variable> { };
            foreach (string name in names) this.vars.Add(new Variable(name));
            this.initial = initial;
            this.dimension = dimension;
            this.sample = sample;
        }
        private string FormatVars() {
            string ids = "";
            foreach (Variable var in this.vars) ids = ids + " " + var.Format();
            return ids;
        }
        public override string Format() {       
            return "amount" + this.FormatVars() + " @ " + initial.Format() + " " + dimension + " in " + sample.Format();
        }
        public override Scope Scope(Scope scope) {
            foreach (Variable var in this.vars) var.Scope(scope);
            initial.Scope(scope);
            sample.Scope(scope);
            return scope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value initialValue = this.initial.Eval(env, netlist, style);
            if (!(initialValue is NumberValue)) throw new Error("Amount " + this.FormatVars() + " requires a number value for concentration");
            Value sampleValue = this.sample.Eval(env, netlist, style);
            if (!(sampleValue is SampleValue)) throw new Error("Amount " + this.FormatVars() + " requires a sample value");
            foreach (Variable var in this.vars) {
                Value speciesValue = var.Eval(env, netlist, style);
                if (!(speciesValue is SpeciesValue)) throw new Error("Amount " + this.FormatVars() + "has a non-species in the list of variables");
                ((SampleValue)sampleValue).InitMolarity((SpeciesValue)speciesValue, (NumberValue)initialValue, this.dimension, style);
                netlist.Emit(new AmountEntry((SpeciesValue)speciesValue, (NumberValue)initialValue, this.dimension, (SampleValue)sampleValue));
            }
            return env;
        }
    }
       
    public class Mix : Statement {
        private string name;
        private Expression fst;
        private Expression snd;
        public Mix(string name, Expression fst, Expression snd) {
            this.name = name;
            this.fst = fst;
            this.snd = snd;
        }
        public override string Format() {       
            return "mix " + name + " := " + fst.Format() + " with " + snd.Format();
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = new ConsScope(name, scope);
            fst.Scope(scope);
            snd.Scope(scope);
            return extScope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value fstValue = this.fst.Eval(env, netlist, style);
            if (!(fstValue is SampleValue)) throw new Error("Mix '" + name + "' requires a sample as first value");
            SampleValue fstSample = (SampleValue)fstValue;
            Value sndValue = this.snd.Eval(env, netlist, style);
            if (!(sndValue is SampleValue)) throw new Error("Mix '" + name + "' requires a sample as second value");
            SampleValue sndSample = (SampleValue)sndValue;
            Symbol symbol = new Symbol(name);
            SampleValue sample = Protocol.Mix(symbol, fstSample, sndSample, netlist, style);
            netlist.Emit(new MixEntry(sample, fstSample, sndSample));
            return new ValueEnv(symbol, null, sample, env);
        }
    }
      
    public class Split : Statement {
        private string name1;
        private string name2;
        private Expression from;
        private Expression proportion;
        public Split(string name1, string name2, Expression from, Expression proportion) {
            this.name1 = name1;
            this.name2 = name2;
            this.from = from;
            this.proportion = proportion;
        }
        public override string Format() {       
            return "split " + name1 + ", " + name2 + " := " + from.Format() + " by " + proportion.Format();
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = new ConsScope(name1, new ConsScope(name2, scope));
            from.Scope(scope);
            proportion.Scope(scope);
            return extScope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value fromValue = this.from.Eval(env, netlist, style);
            if (!(fromValue is SampleValue)) throw new Error("Split '" + name1 + "','" + name2 + "' requires a sample as first value");
            SampleValue fromSample = (SampleValue)fromValue;
            Value proportionValue = this.proportion.Eval(env, netlist, style);
            if (!(proportionValue is NumberValue)) throw new Error("Split '" + name1 + "','" + name2 + "' requires a number as second value");
            double prop = ((NumberValue)proportionValue).value;
            if ((prop <= 0) || (prop >= 1)) throw new Error("Split '" + name1 + "','" + name2 + "' requires a number strictly between 0 and 1 as second value");
            Symbol symbol1 = new Symbol(name1);
            Symbol symbol2 = new Symbol(name2);
            (SampleValue sample1, SampleValue sample2) = Protocol.Split(symbol1, symbol2, fromSample, prop, netlist, style);
            netlist.Emit(new SplitEntry(sample1, sample2, fromSample, (NumberValue)proportionValue));
            return new ValueEnv(symbol1, null, sample1, new ValueEnv(symbol2, null, sample2, env));
        }
    }
 
    public class Dispose : Statement {
        private Expression sample;
        public Dispose(Expression sample) {
            this.sample = sample;
        }
        public override string Format() {       
            return "dispose " + sample.Format();
        }
        public override Scope Scope(Scope scope) {
            sample.Scope(scope);
            return scope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value fstValue = this.sample.Eval(env, netlist, style);
            if (!(fstValue is SampleValue)) throw new Error("Dispose requires a sample");
            SampleValue dispSample = (SampleValue)fstValue;
            Protocol.Dispose(dispSample, netlist, style);
            netlist.Emit(new DisposeEntry(dispSample));
            return env;
        }
    }
   
    public class Equilibrate : Statement {
        private string name;
        private Expression sample;
        private EndCondition endcondition;
        public Equilibrate(string name, Expression sample, EndCondition endcondition) {
            this.name = name;
            this.sample = sample;
            this.endcondition = endcondition;
        }
        public override string Format() {       
            return "equilibrate" +  " " + name + " := " + sample.Format() + endcondition.Format();
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = new ConsScope(name, scope);
            sample.Scope(scope);
            endcondition.Scope(scope);
            return extScope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value inSampleValue = this.sample.Eval(env, netlist, style);
            if (!(inSampleValue is SampleValue)) throw new Error("equilibrate '" + name + "' requires a sample as first value");
            SampleValue inSample = (SampleValue)inSampleValue;
            Noise noise = Gui.gui.NoiseSeries();
            Value forTimeValue = endcondition.fortime.Eval(env, netlist, style);
            if (!(forTimeValue is NumberValue)) throw new Error("equilibrate '" + name + "' requires a number as second value");
            double forTime = ((NumberValue)forTimeValue).value;
            if (forTime < 0) throw new Error("equilibrate '" + name + "' requires a nonnegative number second value");
            Symbol outSymbol = new Symbol(name);
            if (endcondition is EndConditionSimple) {
                Protocol.PauseEquilibrate(netlist, style); // Gui pause between successive equilibrate, if enabled
                SampleValue outSample = Protocol.Equilibrate(outSymbol, inSample, noise, forTime, netlist, style);
                netlist.Emit(new EquilibrateEntry(outSample, inSample, forTime));
                return new ValueEnv(outSymbol, null, outSample, env);
            }
            throw new Error("Equilibrate");
        }
    }

    public abstract class EndCondition {
        public Expression fortime;
        public abstract string Format();
        public abstract void Scope(Scope scope);
    }
    public class EndConditionSimple : EndCondition {
        public EndConditionSimple(Expression fortime) { this.fortime = fortime; }
        public override string Format() { return fortime.Format(); }
        public override void Scope(Scope scope) { fortime.Scope(scope); }
    }

    public class TransferSample : Statement {
        private string name;
        private Expression volume;
        private string volumeUnit;
        private Expression temperature;
        private string temperatureUnit;
        private Expression sample;
        public TransferSample(string name, Expression volume, string volumeUnit, Expression temperature, string temperatureUnit, Expression sample) {
            this.name = name;
            this.volume = volume;
            this.volumeUnit = volumeUnit;
            this.temperature = temperature;
            this.temperatureUnit = temperatureUnit;
            this.sample = sample;
        }
        public override string Format() {
            return "transfer " + name + "{ " + volume.Format() + volumeUnit + ", " + temperature.Format() + temperatureUnit + " } ::= " + sample.Format();
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = new ConsScope(name, scope);
            volume.Scope(scope);
            temperature.Scope(scope);
            sample.Scope(scope);
            return extScope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value volume = this.volume.Eval(env, netlist, style);
            Value temperature = this.temperature.Eval(env, netlist, style);
            if ((!(volume is NumberValue)) || (!(temperature is NumberValue))) throw new Error("Bad arg types to transfer '" + this.name + "'");
            double volumeValue = Protocol.NormalizeVolume(((NumberValue)volume).value, this.volumeUnit);
            double temperatureValue = Protocol.NormalizeTemperature(((NumberValue)temperature).value, this.temperatureUnit);
            if (volumeValue <= 0) throw new Error("Sample volume must be positive on transfer '" + this.name + "'");
            if (temperatureValue < 0) throw new Error("Sample temperature must be non-negative on transfer '" + this.name + "'");
            Value inSampleValue = this.sample.Eval(env, netlist, style);
            if (!(inSampleValue is SampleValue)) throw new Error("transfer '" + name + "' requires a sample");
            Symbol symbol = new Symbol(name);
            SampleValue outSampleValue = Protocol.Transfer(symbol, volumeValue, temperatureValue, (SampleValue)inSampleValue, netlist, style);
            netlist.Emit(new TransferEntry(outSampleValue, (SampleValue)inSampleValue));
            return new ValueEnv(symbol, null, outSampleValue, env);
        }
    }

    ////                     | 'change' <Expression> '@' <Expression> <Quantity> <Allocation>    
    //public class ChangeSpecies : Statement {
    //    private Expression species;
    //    private Expression amount;
    //    private string dimension;
    //    private Expression sample;
    //    public ChangeSpecies(Expression species, Expression amount, string dimension, Expression sample) {
    //        this.species = species;
    //        this.amount = amount;
    //        this.dimension = dimension;
    //        this.sample = sample;
    //    }
    //    public override string Format() {
    //        return "change" + species.Format() + " @ " + amount.Format() + dimension + " in " + sample.Format();
    //    }
    //    public override Scope Scope(Scope scope) {
    //        species.Scope(scope);
    //        amount.Scope(scope);
    //        sample.Scope(scope);
    //        return scope;
    //    }
    //    public override Env Eval(Env env, Netlist netlist, Style style) {
    //        Value speciesValue = this.species.Eval(env, netlist, style);
    //        if (!(speciesValue is SpeciesValue)) throw new Error("'change' requires a species as first value");
    //        Value newValue = this.amount.Eval(env, netlist, style);
    //        if (!(newValue is NumberValue)) throw new Error("'change' requires a number value for concentration");
    //        Value sampleValue = this.sample.Eval(env, netlist, style);
    //        if (!(sampleValue is SampleValue)) throw new Error("'change' requires a sample to change");
    //        ((SampleValue)sampleValue).ChangeMolarity((SpeciesValue)speciesValue, (NumberValue)newValue, this.dimension, style);
    //        netlist.Emit(new ChangeSpeciesEntry((SpeciesValue)speciesValue, (NumberValue)newValue, (SampleValue)sampleValue));
    //        return env;
    //    }
    //}

    public class Report : Statement {
        public Expression expression;   // just a subset of numerical arithmetic expressions that can be plotted
        public Expression asExpr; // can be null
        public Report(Expression expression, Expression asExpr) {
            this.expression = expression;
            this.asExpr = asExpr;
        }
        public override string Format() {
            string s = "report " + this.expression.Format();
            if (asExpr != null) s += " as " + this.asExpr.Format();
            return s;
        }
        public override Scope Scope(Scope scope) {
            this.expression.Scope(scope);
            if (this.asExpr != null) this.asExpr.Scope(scope);
            return scope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            string asLabel = null;
            if (this.asExpr != null) {
                Value value = this.asExpr.EvalFlow(env, style);
                if (value is StringValue) asLabel = ((StringValue)value).value; // the raw string contents, unquoted
                else asLabel = value.Format(style);
            }
            netlist.Emit(new ReportEntry(expression.BuildFlow(env, style), asLabel));
            return env;
        }
    }

    // COMPLEX

    public abstract class Complex : Tree {
        public abstract void Scope(Scope scope);
        public abstract List<Symbol> Eval(Env env, Netlist netlist, Style style);
    }

    public class Simplex : Complex {
        private Expression stoichiometry; // may be null if stoichiometry is 1
        private Variable species; // may be null is stoichiometry is 0 OR if species is #
        public Simplex(Expression stoichiometry, Variable species) {
            this.stoichiometry = stoichiometry;
            this.species = species;
        }
        public override string Format() {
            if (stoichiometry == null && species == null) return "#";
            if (stoichiometry != null && species == null) return stoichiometry.Format() + " * #";
            if (stoichiometry == null && species != null) return species.Format();
            if (stoichiometry != null && species != null) return stoichiometry.Format() + " * " + species.Format();
            return "";
        }
        public override void Scope(Scope scope) {
            if (stoichiometry != null) stoichiometry.Scope(scope);
            if (species != null) species.Scope(scope);
        }
        public override List<Symbol> Eval(Env env, Netlist netlist, Style style) {
            List<Symbol> list = new List<Symbol> { };
            int count = 1;
            Value stoichValue = (stoichiometry == null) ? null : stoichiometry.Eval(env, netlist, style);
            if (stoichValue != null) {
                if (!(stoichValue is NumberValue))
                    throw new Error("Stoichiometry value '" + stoichiometry.Format() + "' must denote number, not: " + stoichValue.Format(style));
                else if ((stoichValue as NumberValue).value % 1 != 0) // not an integer
                    throw new Error("Stoichiometry value '" + stoichiometry.Format() + "' must denote integer, not: " + stoichValue.Format(style));
                else count = (int)((stoichValue as NumberValue).value);
            }
            if (species != null) { 
                Value speciesValue = species.Eval(env, netlist, style);
                if (!(speciesValue is SpeciesValue))
                    throw new Error("Species variable '" + species.Format() + "' must denote species, not: " + speciesValue.Format(style));
                for (int i = 1; i <= count; i++) { list.Add(((SpeciesValue)species.Eval(env, netlist, style)).symbol); }
            }
            return list;  // count could be 0; list could be empty!
        }
    }

    public class SumComplex : Complex {
        public Complex complex1;
        public Complex complex2;
        public SumComplex(Complex complex1, Complex complex2) {
            this.complex1 = complex1;
            this.complex2 = complex2;
        }
         public override string Format() {
            return complex1.Format() + " + " + complex2.Format();
        }
        public override void Scope(Scope scope) {
            complex1.Scope(scope);
            complex2.Scope(scope);
        }
        public override List<Symbol> Eval(Env env, Netlist netlist, Style style) {
            List<Symbol> list = complex1.Eval(env, netlist, style);
            list.AddRange(complex2.Eval(env, netlist, style));
            return list;
        }
    }

    // PARAMETERS

    public class Parameters : Tree {
        public List<NewParameter> parameters;
        public Parameters() {
            this.parameters = new List<NewParameter> { };
        }
        public Parameters Add(NewParameter param) {
            this.parameters.Add(param);
            return this;
        }
        public override string Format() {
            return this.parameters.Aggregate("", (a, b) => (a == "") ? b.Format() : a + ", " + b.Format());
        }
    }
    public abstract class NewParameter : Tree {
    }
    public class SingleParameter : NewParameter {
        public Type type;
        public string name;
        public SingleParameter(Type type, string id) {
            this.type = type;
            this.name = id;
        }
        public override string Format() {
            return this.type.Format() + " " + this.name;
        }
    }
    public class ListParameter : NewParameter {
        public Parameters list;
        public ListParameter(Parameters list) {
            this.list = list;
        }
        public override string Format() {
            return "[" + this.list.Format() + "]";
        }

    }


    // ARGUMENTS

    public class Expressions : Tree {
        public List<Expression> expressions;
        public Expressions() {
            this.expressions = new List<Expression> { };
        }
        public Expressions Add(Expression expression) {
            this.expressions.Add(expression);
            return this;
        }
        public override string Format() {
            return this.expressions.Aggregate("", (a, b) => (a == "") ? b.Format() : a + ", " + b.Format());
        }
        public void Scope(Scope scope) {
            foreach (Expression expression in this.expressions) { expression.Scope(scope); }
        }
        public List<Value> Eval(Env env, Netlist netlist, Style style) {
            List<Value> expressions = new List<Value>();
            foreach (Expression expression in this.expressions) { expressions.Add(expression.Eval(env, netlist, style)); }
            return expressions;
        }
        public List<Value> EvalFlow(Env env, Style style) {
            List<Value> expressions = new List<Value>();
            foreach (Expression expression in this.expressions) { expressions.Add(expression.EvalFlow(env, style)); }
            return expressions;
        }
        public List<Flow> BuildFlow(Env env, Style style) {
            List<Flow> expressions = new List<Flow>();
            foreach (Expression expression in this.expressions) { expressions.Add(expression.BuildFlow(env, style)); }
            return expressions;
        }
        public static string FormatValues(List<Value> values, Style style) {
            string s = "";
            foreach (Value value in values) { s += value.Format(style) + ", "; }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2); // remove last comma
            return s;
        }
        public static string FormatFlows(List<Flow> values, Style style) {
            string s = "";
            foreach (Flow value in values) { s += value.Format(style) + ", "; }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2); // remove last comma
            return s;
        }
        public static string FormatValues(Value[] values, Style style) {
            string s = "";
            for (int i = 0; i < values.Length; i++) { s += values[i].Format(style) + ", "; }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2); // remove last comma
            return s;
        }
    }

}

