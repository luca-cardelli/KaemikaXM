using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Research.Oslo;
using System.Drawing;
using System.Threading;

namespace Kaemika
{

    public enum ExportTarget : int { LBS, CRN, Standard };

    public class Style {
        private string varchar;         // The non-inputable character used to distinguish symbol variants
                                            // can be null if we do not show the variants
        private string prime;           // The string used to replace prime "'" in export to other systems
                                            // can be null if we do not replace it
        private string underbar;        // The string used to replace underbar "_" in export to other systems
                                            // can be null if we do not replace it
        private AlphaMap map;           // The map used to alpha-convert conflicting symbols in printout
                                            // can be null if we do not alpha-convert
        public string numberFormat;     // Number format
                                            // can be null for default (full precision)
        public string dataFormat;       // How to display complex data
                                            // "symbol", "header", or "full"
        public ExportTarget exportTarget; // How to format for external tools

        public Style(string varchar, string prime, AlphaMap map, string numberFormat, string dataFormat, ExportTarget exportTarget) {
            this.varchar = varchar;
            this.prime = prime;
            this.underbar = null;
            this.map = map;
            this.numberFormat = numberFormat;
            this.dataFormat = dataFormat;
            this.exportTarget = exportTarget;
        }
        public Style() : this(null, null, null, null, "full", ExportTarget.Standard) {
        }
        public Style RestyleAsDataFormat(string dataFormat) {
            return new Style(this.varchar, this.prime, this.map, this.numberFormat, dataFormat, this.exportTarget);
        }
        public Style RestyleAsNumberFormat(string numberFormat) {
            return new Style(this.varchar, this.prime, this.map, numberFormat, this.dataFormat, this.exportTarget);
        }
        public string Varchar() { return this.varchar; }
        public string Prime() { return this.prime; }
        public string Underbar() { return this.underbar; }
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
        public string DeApostrophe(string name, string replacement) {
            if (name.Contains("'") && name.Contains(replacement)) throw new Error("Cannot replace \"'\" with '" + replacement + "' in '" + name + "'");
            else return name.Replace("'", replacement);
        }
        public string DeUnderbar(string name, string replacement) {
            if (name.Contains("_") && name.Contains(replacement)) throw new Error("Cannot replace '_' with '" + replacement + "' in '" + name + "'");
            return name.Replace("_", replacement);
        }
        public string Format(Style style) {
            string varchar = style.Varchar();
            if (varchar == null) return this.name;                                           // don't show the variant
            else {
                string sname = this.name;
                string prime = style.Prime();
                sname = (prime == null) ? sname : DeApostrophe(sname, prime); // use prime to replace apostrophes
                string underbar = style.Underbar();
                sname = (underbar == null) ? sname : DeUnderbar(sname, underbar);    // use underbar to replace underbars
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

    public abstract class Scope  {
        public abstract bool Lookup(string var); // return true if var is defined
        public abstract string Format();
        public Scope Extend(List<Parameter> parameters) {
            Scope scope = this;
            foreach (Parameter parameter in parameters) {
                scope = new ConsScope(parameter.name, scope);
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
                ((this.type == "flow") && (value is Flow)) ||
                ((this.type == "function") && (value is FunctionValue || value is OperatorValue)) ||
                ((this.type == "network") && (value is NetworkValue)) ||
                ((this.type == "species") && (value is SpeciesValue)) ||
                ((this.type == "sample") && (value is SampleValue))
                ;
        }
        public string Format() { return this.type;  }
    }

    // ENVIRONMENTS

    public abstract class Env {
        public abstract Symbol LookupSymbol(string name);
        public abstract Value LookupValue(string name);
        public abstract void AssignValue(Symbol symbol, Value value);
        public abstract string Format(Style style);
        public Env ExtendValues<T>(Symbol symbol, List<Parameter> parameters, List<T> arguments, Style style) where T : Value {  // bounded polymorphism :)
            if (parameters.Count != arguments.Count) throw new Error("Different number of parameters and arguments for '" + (symbol == null ? "<nameless>" : symbol.Format(style)) + "'");
            Env env = this;
            for (int i = 0; i < parameters.Count; i++) {
                env = new ValueEnv(parameters[i].name, parameters[i].type, arguments[i], env);
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
                builtIn = new ValueEnv("vessel",     null, vessel, builtIn);
                builtIn = new ValueEnv("if",         null, new OperatorValue("if"), builtIn);          // conditional pseudo-operator
                builtIn = new ValueEnv("cond",       null, new OperatorValue("cond"), builtIn);        // flow-expression conditional pseudo-operator
                builtIn = new ValueEnv("not",        null, new OperatorValue("not"), builtIn);
                builtIn = new ValueEnv("or",         null, new OperatorValue("or"), builtIn);
                builtIn = new ValueEnv("and",        null, new OperatorValue("and"), builtIn);
                builtIn = new ValueEnv("+",          null, new OperatorValue("+"), builtIn);
                builtIn = new ValueEnv("-",          null, new OperatorValue("-"), builtIn);           // both prefix and infix
                builtIn = new ValueEnv("*",          null, new OperatorValue("*"), builtIn);
                builtIn = new ValueEnv("/",          null, new OperatorValue("/"), builtIn);
                builtIn = new ValueEnv("^",          null, new OperatorValue("^"), builtIn);
                builtIn = new ValueEnv("=",          null, new OperatorValue("="), builtIn);
                builtIn = new ValueEnv("<>",         null, new OperatorValue("<>"), builtIn);
                builtIn = new ValueEnv("<=",         null, new OperatorValue("<="), builtIn);
                builtIn = new ValueEnv("<",          null, new OperatorValue("<"), builtIn);
                builtIn = new ValueEnv(">=",         null, new OperatorValue(">="), builtIn);
                builtIn = new ValueEnv(">",          null, new OperatorValue(">"), builtIn);
                builtIn = new ValueEnv("pi",         null, new NumberValue(Math.PI), builtIn);
                builtIn = new ValueEnv("e",          null, new NumberValue(Math.E), builtIn);
                builtIn = new ValueEnv("abs",        null, new OperatorValue("abs"), builtIn);
                builtIn = new ValueEnv("arccos",     null, new OperatorValue("arccos"), builtIn);
                builtIn = new ValueEnv("arcsin",     null, new OperatorValue("arcsin"), builtIn);
                builtIn = new ValueEnv("arctan",     null, new OperatorValue("arctan"), builtIn);
                builtIn = new ValueEnv("arctan2",    null, new OperatorValue("arctan2"), builtIn);
                builtIn = new ValueEnv("ceiling",    null, new OperatorValue("ceiling"), builtIn);
                builtIn = new ValueEnv("cos",        null, new OperatorValue("cos"), builtIn);
                builtIn = new ValueEnv("cosh",       null, new OperatorValue("cosh"), builtIn);
                builtIn = new ValueEnv("exp",        null, new OperatorValue("exp"), builtIn);
                builtIn = new ValueEnv("floor",      null, new OperatorValue("floor"), builtIn);
                builtIn = new ValueEnv("int",        null, new OperatorValue("int"), builtIn);         // convert number to integer number by rounding
                builtIn = new ValueEnv("log",        null, new OperatorValue("log"), builtIn);
                builtIn = new ValueEnv("max",        null, new OperatorValue("max"), builtIn);
                builtIn = new ValueEnv("min",        null, new OperatorValue("min"), builtIn);
                builtIn = new ValueEnv("pos",        null, new OperatorValue("pos"), builtIn);         // convert number to positive number by returning 0 if negative
                builtIn = new ValueEnv("sign",       null, new OperatorValue("sign"), builtIn);
                builtIn = new ValueEnv("sin",        null, new OperatorValue("sin"), builtIn);
                builtIn = new ValueEnv("sinh",       null, new OperatorValue("sinh"), builtIn);
                builtIn = new ValueEnv("sqrt",       null, new OperatorValue("sqrt"), builtIn);
                builtIn = new ValueEnv("tan",        null, new OperatorValue("tan"), builtIn);
                builtIn = new ValueEnv("tanh",       null, new OperatorValue("tanh"), builtIn);
                builtIn = new ValueEnv("volume",     null, new OperatorValue("volume"), builtIn);
                builtIn = new ValueEnv("temperature",null, new OperatorValue("temperature"), builtIn);
                builtIn = new ValueEnv("molarity",   null, new OperatorValue("molarity"), builtIn);         // one or two arguments
                builtIn = new ValueEnv("time",       null, new OperatorValue("time"), builtIn);             // for flow expressions
                builtIn = new ValueEnv("kelvin",     null, new OperatorValue("kelvin"), builtIn);           // for flow expressions
                builtIn = new ValueEnv("celsius",    null, new OperatorValue("celsius"), builtIn);          // for flow expressions
                builtIn = new ValueEnv("poisson",    null, new OperatorValue("poisson"), builtIn);          // for flow expressions
                builtIn = new ValueEnv("gauss",      null, new OperatorValue("gauss"), builtIn);            // for flow expressions
                builtIn = new ValueEnv("var",        null, new OperatorValue("var"), builtIn);              // for flow expressions
                builtIn = new ValueEnv("cov",        null, new OperatorValue("cov"), builtIn);              // for flow expressions
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
            if (((!lna) && init.Length != size) || (lna && init.Length != size+size*size)) throw new Error("InitAll: wrong size");
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
                    this.state[size + (i * size) + j] += x[i,j];
        }
        public string FormatSpecies(List<SpeciesValue> species, Style style) {
            string s = "";
            for (int i=0; i < this.size; i++) {
                s += species[i].Format(style) + "=" + Mean(i).ToString() + ", ";
            }
            if (this.lna) {
                for (int i=0; i < this.size; i++)
                    for (int j=0; j < this.size; j++) {
                        s += "(" + species[i].Format(style) + "," + species[j].Format(style) + ")=" + Covar(i,j).ToString() + ", ";
                    }
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2);
            return s;
        }
        public string FormatReports(List<ReportEntry> reports, SampleValue sample, double time, Noise noise, string[] series, string[] seriesLNA, Style style) {
            string s = "";
            for (int i = 0; i < reports.Count; i++) {
                if (series[i] != null) { // if a series was actually generated from this report
                    if ((noise == Noise.None && reports[i].flow.HasDeterministicMean()) ||
                        (noise != Noise.None && reports[i].flow.HasStochasticMean())) {
                        double mean = reports[i].flow.ReportMean(sample, time, this, style);
                        s += Gui.gui.ChartAddPointAsString(series[i], time, mean, 0.0, Noise.None) + ", ";
                    }
                    if (noise != Noise.None && reports[i].flow.HasStochasticMean() && !reports[i].flow.HasNullVariance()) {
                        double mean = reports[i].flow.ReportMean(sample, time, this, style);
                        double variance = reports[i].flow.ReportVariance(sample, time, this, style);
                        s += Gui.gui.ChartAddPointAsString(seriesLNA[i], time, mean, variance, noise) + ", ";
                    }
                }
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2);
            return s;
        }
    }

    public abstract class ProtocolActuator {
        // just a container for static methods
        // the used property is set by the Eval methods, not here

        public static bool continueExecution = true;

        public static string[] noiseString = new string[7] { " μ", " ±σ", " σ", " σ/μ", " ±σ²", " σ²", " σ²/μ" }; //Enum.GetNames(typeof(Noise)).Length

        public static Noise NoiseOfString(string selection) {
            if (selection == null) return Noise.None;
            if (selection == noiseString[0]) return Noise.None; // " μ"
            if (selection == noiseString[1]) return Noise.SigmaRange; // " ±σ"
            if (selection == noiseString[2]) return Noise.Sigma; // " σ"
            if (selection == noiseString[3]) return Noise.CV; // " σ/μ"
            if (selection == noiseString[4]) return Noise.SigmaSqRange; // " ±σ²"
            if (selection == noiseString[5]) return Noise.SigmaSq; // " σ²"
            if (selection == noiseString[6]) return Noise.Fano; // " σ²/μ"
            return Noise.None;
        }

        public static SampleValue Mix(Symbol symbol, SampleValue mixFst, SampleValue mixSnd, Style style) {
            mixFst.Consume(style);
            mixSnd.Consume(style);
            double fstVolume = mixFst.Volume();
            double sndVolume = mixSnd.Volume();
            NumberValue volume = new NumberValue(fstVolume + sndVolume);
            NumberValue temperature = new NumberValue((fstVolume * mixFst.Temperature() + sndVolume * mixSnd.Temperature()) / (fstVolume + sndVolume));
            SampleValue result = new SampleValue(symbol, volume, temperature);
            result.AddSpecies(mixFst, volume.value, fstVolume);
            result.AddSpecies(mixSnd, volume.value, sndVolume);
            return result;
        }

        public static (SampleValue, SampleValue) Split(Symbol symbol1, Symbol symbol2, SampleValue sample, double proportion, Style style) {
            sample.Consume(style);
            double sampleVolume = sample.Volume();

            NumberValue volume1 = new NumberValue(sampleVolume * proportion);
            NumberValue temperature1 = new NumberValue(sample.Temperature());
            SampleValue result1 = new SampleValue(symbol1, volume1, temperature1);
            result1.AddSpecies(sample, sampleVolume, sampleVolume); // add species from other sample without changing their concentations

            NumberValue volume2 = new NumberValue(sampleVolume * (1-proportion));
            NumberValue temperature2 = new NumberValue(sample.Temperature());
            SampleValue result2 = new SampleValue(symbol2, volume2, temperature2);
            result2.AddSpecies(sample, sampleVolume, sampleVolume); // add species from other sample without changing their concentations

            return (result1, result2);
        }

        public static void Dispose(SampleValue sample, Style style) {
            sample.Consume(style);
        }

        private static Color[] palette = { Color.Red, Color.Green, Color.Blue, Color.Gold, Color.Cyan, Color.GreenYellow, Color.Violet, Color.Purple };
        private static int paletteNo = 0;

        public static double NormalizeVolume(double volume, string unit) {
            if (unit == "L") { return volume;  } // ok
            else if (unit == "mL") { return volume * 1e-3; }
            else if (unit == "muL") { return volume * 1e-6; }
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
            else if (dimension == "ug") return weight * 1e-6;
            else if (dimension == "ng") return weight * 1e-9;
            else return -1;
        }

        public static double NormalizeMole(double mole, string dimension) {
            if (dimension == "kmol") return mole * 1e3;
            else if (dimension == "mol") return mole;
            else if (dimension == "mmol") return mole * 1e-3;
            else if (dimension == "umol") return mole * 1e-6;
            else if (dimension == "nmol") return mole * 1e-9;
            else return -1;
        }

        public static double NormalizeMolarity(double molarity, string dimension) {
            if (dimension == "kM") return molarity * 1e3;
            else if (dimension == "M") return molarity;
            else if (dimension == "mM") return molarity * 1e-3;
            else if (dimension == "uM") return molarity * 1e-6;
            else if (dimension == "nM") return molarity * 1e-9;
            else return -1;
        }

        public static SampleValue Equilibrate(Symbol symbol, SampleValue sample, double fortime, Netlist netlist, Style style) {
            while ((!continueExecution) && Gui.gui.StopEnabled()) {
                if (!Gui.gui.ContinueEnabled()) Gui.gui.OutputAppendText(netlist.Format(style));
                Gui.gui.ContinueEnable(true);
                Thread.Sleep(100);
            }
            Gui.gui.ContinueEnable(false);  continueExecution = false;

            sample.Consume(style);
            NumberValue volume = new NumberValue(sample.Volume());
            NumberValue temperature = new NumberValue(sample.Temperature());
            SampleValue resultSample = new SampleValue(symbol, volume, temperature);

            Gui.gui.OutputSetText(""); // clear last results in preparation for the next
            Gui.gui.ChartClear(
                (resultSample.symbol.Raw() == "vessel") ? "" 
                : "Sample " + resultSample.symbol.Format(style));
            Gui.gui.ChartListboxClear();

            Noise noise = Gui.gui.NoiseSeries();
            List<SpeciesValue> species = sample.Species(out double[] speciesState);
            State initialState = new State(species.Count, noise != Noise.None).InitMeans(speciesState);
            List<ReactionValue> reactions = netlist.RelevantReactions(sample, species, style);
            CRN crn = new CRN(sample, reactions);
            List<ReportEntry> reports = netlist.Reports(species);

            // Program.Log(sample.Format(netlist.style));
            // Program.Log(crn.Format(netlist.style));
            // Program.Log(crn.FormatAsODE(netlist.style));
            // Program.Log("InitialState = (" + FormatVector(initialState) + ")");

            Func<double, double, Vector, Func<double, Vector, Vector>, IEnumerable<SolPoint>> Solver;
            if (Gui.gui.Solver() == "OSLO GearBDF") Solver = Ode.GearBDF;
            else if (Gui.gui.Solver() == "OSLO RK547M") Solver = Ode.RK547M;
            else throw new Error("No solver");

            Func<double, Vector, Vector> Flux;
            if (noise != Noise.None) Flux = (t, x) => crn.LNAFlux(t, x, style);
            else Flux = (t, x) => crn.Flux(t, x, style);

            double initialTime = 0.0;
            double finalTime = fortime;
            IEnumerable<SolPoint> solution; 
            if (species.Count > 0   // we don't want to run on the empty species list: Oslo crashes
                && (!crn.trivial)   // we don't want to run trivial ODEs: some Oslo solvers hang on very small stepping
                && fortime > 0      // we don't want to run when fortime==0
               ) {
                try {
                    IEnumerable<SolPoint> solver;
                    solver = Solver(initialTime, finalTime, initialState.ToArray(), Flux);
                    solution = OdeHelpers.SolveTo(solver, finalTime);
                }
                catch (Error e) { throw new Error(e.Message);  }
                catch (Exception e) { throw new Error("ODE Solver FAILED: " + e.Message); }
            } else { // build a dummy point series, in case we want to report and plot just some numerical expressions
                List<SolPoint> list = new List<SolPoint> { }; // SolPoint constructor was changed to public from internal
                if (fortime <= 0) list.Add(new SolPoint(0.0, initialState.ToArray()));
                else for (double t = initialTime; t <= initialTime + fortime; t += (fortime / 1000.0)) list.Add(new SolPoint(t, initialState.ToArray()));
                solution = list;
            }

            string[] seriesLNA = new string[reports.Count]; // can contain nulls if series are duplicates
            paletteNo = (reports.Count-1) % palette.Length; // because we scan palette backwards
            for (int i = reports.Count-1; i >= 0; i--) {    // add series backwards so that Red is in front
                ReportEntry entry = reports[i];
                if ((noise != Noise.None) && entry.flow.HasStochasticMean() && !entry.flow.HasNullVariance()) {
                    string reportName = (entry.asLabel != null) ? entry.asLabel : entry.flow.TopFormat(style.RestyleAsNumberFormat("G4"));
                    string seriesName = reportName + noiseString[(int)noise];
                    seriesLNA[i] = Gui.gui.ChartAddSeries(seriesName, palette[paletteNo % palette.Length], noise);
                }
                paletteNo--; if (paletteNo < 0) paletteNo += palette.Length; // decrement out here to keep colors coordinated
            }

            string[] series = new string[reports.Count]; // can contain nulls if series are duplicates
            paletteNo = (reports.Count - 1) % palette.Length; // because we scan palette backwards
            for (int i = reports.Count-1; i >= 0; i--) {      // add series backwards so that Red is in front
                ReportEntry entry = reports[i];
                if ((noise == Noise.None && entry.flow.HasDeterministicMean()) || 
                    ((noise != Noise.None) && entry.flow.HasStochasticMean())) {
                    string reportName = (entry.asLabel != null) ? entry.asLabel : entry.flow.TopFormat(style.RestyleAsNumberFormat("G4"));
                    string seriesName = reportName + ((noise == Noise.None) ? "" : noiseString[(int)Noise.None]);
                    series[i] = Gui.gui.ChartAddSeries(seriesName, palette[paletteNo % palette.Length], Noise.None);
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

            Gui.gui.ChartListboxRestore();

            double lastTime = finalTime;
            State lastState = null;
            int pointsCounter; int renderedCounter;
            Integrate(solution, initialTime, finalTime, out lastTime, out lastState, sample, reports, noise, series, seriesLNA, netlist, style, out pointsCounter, out renderedCounter);

            if (lastState == null) lastState = initialState;
            for (int i = 0; i < species.Count; i++) {
                double molarity = lastState.Mean(i);
                if (molarity < 0) molarity = 0; // the ODE solver screwed up
                resultSample.SetMolarity(species[i], new NumberValue(molarity), style);
            }
            Exec.lastReport = "======= Last report: time=" + lastTime.ToString() + ", " + lastState.FormatReports(reports, sample, lastTime, noise, series, seriesLNA, style);
            Exec.lastState =  "======= Last state: total points=" + pointsCounter + ", drawn points=" + renderedCounter + ", time=" + lastTime.ToString() + ", " + lastState.FormatSpecies(species, style);
            return resultSample;
        }

        private static void Integrate(IEnumerable<SolPoint> solution, double initialTime, double finalTime, 
                                      out double lastTime, out State lastState,
                                      SampleValue sample, List<ReportEntry> reports, 
                                      Noise noise, string[] series, string[] seriesLNA,
                                      Netlist netlist, Style style, out int pointsCounter, out int renderedCounter) {
            double redrawTick = initialTime; double redrawStep = (finalTime - initialTime) / 50;
            double densityTick = initialTime; double densityStep = (finalTime - initialTime) / 1000;
            pointsCounter = 0;
            renderedCounter = 0;
            lastTime = finalTime;
            lastState = null;

            Gui.gui.LegendUpdate();

            // BEGIN foreach (SolPoint solPoint in solution)  -- done by hand to catch exceptions in MoveNext()
            var enumerator = solution.GetEnumerator();
            do {
                SolPoint solPoint;
                try {
                    if (!enumerator.MoveNext()) break;
                    solPoint = enumerator.Current;
                }
                catch (Error e) { throw new Error(e.Message); }
                catch (Exception e) { throw new Error("ODE Solver FAILED: " + e.Message); }
                pointsCounter++;

                // LOOP BODY of foreach (SolPoint solPoint in solution):
                if (!Gui.gui.StopEnabled()) break; // clicking the Stop button disables it
                State state = new State(sample.species.Count, noise != Noise.None).InitAll(solPoint.X);
                if (solPoint.T >= densityTick) { // avoid drawing too many points
                    for (int i = 0; i < reports.Count; i++) {
                        if ((noise == Noise.None && reports[i].flow.HasDeterministicMean()) ||
                            (noise != Noise.None && reports[i].flow.HasStochasticMean())) {
                            double mean = reports[i].flow.ReportMean(sample, solPoint.T, state, style);
                            Gui.gui.ChartAddPoint(series[i], solPoint.T, mean, 0.0, Noise.None);
                        }
                        if (noise != Noise.None && reports[i].flow.HasStochasticMean() && !reports[i].flow.HasNullVariance()) {
                            double mean = reports[i].flow.ReportMean(sample, solPoint.T, state, style);
                            double variance = reports[i].flow.ReportVariance(sample, solPoint.T, state, style);
                            Gui.gui.ChartAddPoint(seriesLNA[i], solPoint.T, mean, variance, noise);
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
                lastState = state;

            // END foreach (SolPoint solPoint in solution)
            } while (true);

            Gui.gui.ChartUpdate();
        }
    }

    // VALUES

    public abstract class Value {
        public Type type;
        public abstract string Format(Style style);
        public string TopFormat(Style style) {
            string s = this.Format(style);
            if (s.Length > 0 && s.Substring(0, 1) == "(" && s.Substring(s.Length-1, 1) == ")") s = s.Substring(1, s.Length - 2);
            return s;
        }
    }

    public class SampleValue : Value {
        public Symbol symbol;
        private NumberValue volume;                                 // L
        private NumberValue temperature;                            // Kelvin
        private Dictionary<SpeciesValue, NumberValue> speciesSet;   // mol/L
        public List<SpeciesValue> species;
        public Dictionary<Symbol, int> speciesIndex;
        private bool disposed;
        public SampleValue asConsumed;
        public SampleValue(Symbol symbol, NumberValue volume, NumberValue temperature) {
            this.type = new Type("sample");
            this.symbol = symbol;
            this.volume = volume;           // mL
            this.temperature = temperature; // Kelvin
            this.speciesSet = new Dictionary<SpeciesValue, NumberValue> { };
            this.species = new List<SpeciesValue> { };
            this.speciesIndex = new Dictionary<Symbol, int> { };
            this.disposed = false;
            this.asConsumed = null;
        }
        public void Consume(Style style) {
            if (this.disposed) throw new Error("Sample already used: '" + this.symbol.Format(style) + "'");
            this.asConsumed = this.Copy(); // save it for export purposes
            this.disposed = true;
        }
        public SampleValue Copy() {
            SampleValue copy = new SampleValue(this.symbol, this.volume, this.temperature);
            foreach (var pair in this.speciesSet) copy.SetMolarity(pair.Key, pair.Value, null, recompute: false);
            copy.RecomputeSpecies();
            return copy;
        }
        public string FormatHeader(Style style) {
            double vol = this.Volume();
            string volUnit;
            if (Math.Round(vol * 1e6) < 1) { vol = vol * 1e9; volUnit = "nL"; } // this test avoids producing '1000nL'
            else if (Math.Round(vol * 1e3) < 1) { vol = vol * 1e6; volUnit = "muL"; } // this test avoids producing '1000muL'
            else if (Math.Round(vol) < 1) { vol = vol * 1e3; volUnit = "mL"; } // this test avoids producing '1000mL'
            else { volUnit = "L"; }
            return symbol.Format(style)
                + " {" + style.FormatDouble(vol) + volUnit + ", " + temperature.Format(style) + "K}";
        }
        public string FormatContent(Style style) {
            string s = "";
            foreach (KeyValuePair<SpeciesValue, NumberValue> keyPair in this.speciesSet) {
                double molarity = keyPair.Value.value;
                string unit;
                if (molarity == 0.0) { unit = "M"; }
                else if (Math.Round(molarity*1e6) < 1) { molarity = molarity * 1e9; unit = "nM"; } // this test avoids producing '1000nM'
                else if (Math.Round(molarity*1e3) < 1) { molarity = molarity * 1e6; unit = "uM"; } // this test avoids producing '1000muM'
                else if (Math.Round(molarity) < 1)     { molarity = molarity * 1e3; unit = "mM"; } // this test avoids producing '1000mM'
                else { unit = "M"; }
                s += keyPair.Key.Format(style) + " = " + style.FormatDouble(molarity) + unit + ", ";
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2); // remove last comma
            return s;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return symbol.Format(style);
            else if (style.dataFormat == "header") return "sample " + FormatHeader(style);
            else if (style.dataFormat == "full") return "sample " + FormatHeader(style) + " {" + FormatContent(style) + "}";
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
                if (this.HasSpecies(keyPair.Key.symbol, out NumberValue number)) this.speciesSet[keyPair.Key] = new NumberValue(number.value + newConcentration);
                else {
                    this.speciesSet.Add(keyPair.Key, new NumberValue(newConcentration));
                    RecomputeSpecies();
                }
            }
        }

        public double Temperature() {
            return this.temperature.value;
        }
        public void ChangeTemperature(double newTemperature) {
            this.temperature = new NumberValue(newTemperature);
        }

        public double Volume() {
            return volume.value;
        }
        public void ChangeVolume(double newVolume) {  // evaporate or dilute
            double oldVolume = this.Volume();
            Dictionary<SpeciesValue, NumberValue> oldspeciesSet = this.speciesSet;
            this.volume = new NumberValue(newVolume);
            this.speciesSet = new Dictionary<SpeciesValue, NumberValue> ();
            double ratio = oldVolume / newVolume;
            foreach (KeyValuePair<SpeciesValue, NumberValue> keyPair in oldspeciesSet) {
                this.speciesSet.Add(keyPair.Key, new NumberValue(keyPair.Value.value * ratio));
            }
            RecomputeSpecies();
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
            else if (this.HasSpecies(species.symbol, out NumberValue value))  {
                this.speciesSet[species] = new NumberValue(Normalize(species, mol, dimension, style));
            } else throw new Error("Species to change not found '" + species.Format(style) + "' in sample '" + this.symbol.Format(style) + "' with value " + mol.ToString());
        }
        private double Normalize(SpeciesValue species, double value, string dimension, Style style) {
            double normal;
            normal = ProtocolActuator.NormalizeMolarity(value, dimension);
            if (normal >= 0) return normal; // value had dimension M = mol/L
            normal = ProtocolActuator.NormalizeMole(value, dimension);
            if (normal >= 0) return normal / this.Volume(); // value had dimension mol, convert it to M = mol/L
            normal = ProtocolActuator.NormalizeWeight(value, dimension);
            if (normal >= 0) {
                if (species.HasMolarMass())
                    return (normal / species.MolarMass()) / this.Volume();    // value had dimension g, convert it to M = (g/(g/M))/L
                throw new Error("Species '" + species.Format(style)
                    + "' was given no molar mass, hence its amount in sample '" + this.symbol.Format(style)
                    + "' should have dimension 'M' (concentration) or 'mol' (mole), not '" + dimension + "'");
            }
            throw new Error("Invalid dimension '" + dimension + "'");
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
        public BoolValue(bool value){
            this.type = new Type("bool");
            this.value = value;
        }
        public override string Format(Style style) {
            if (this.value) return "true"; else return "false";
        }
    }

    public class NumberValue : Value {
        public double value;
        public NumberValue(double value){
            this.type = new Type("number");
            this.value = value;
        }
        public override string Format(Style style) {
            return style.FormatDouble(this.value);
        }
    }

    public class StringValue : Value {
        public string value;
        public StringValue(string value){
            this.type = new Type("string");
            this.value = value;
        }
        public override string Format(Style style) {
            return Parser.FormatString(this.value);
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
        public bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) {
            foreach (Symbol rs in this.reactants)
                if (!species.Exists(x => x.symbol.SameSymbol(rs))) { notCovered = rs; return false; };
            foreach (Symbol rs in this.products)
                if (!species.Exists(x => x.symbol.SameSymbol(rs))) { notCovered = rs; return false; };
            return rate.CoveredBy(species, out notCovered);
        }
    }

    public abstract class RateValue {
        public abstract string Format(Style style);
        public abstract double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style);  // the mass action of this reaction in this state and temperature
        public abstract bool Involves(List<SpeciesValue> species);
        public abstract bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered);
    }

    public class GeneralRateValue : RateValue {
        private Flow rateFunction;
        public GeneralRateValue(Flow rateFunction) {
            this.rateFunction = rateFunction;
        }
        public override string Format(Style style) {
            return "{{" + rateFunction.Format(style) + "}}";
        }
        public override double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style) {
            // We earlier checked that rateFunction HasDeterministicMean. If it is not a numeric flow, we now get an error from ReportMean.
            return rateFunction.ReportMean(sample, time, new State(state.Length, false).InitAll(state), style);
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
            return this.Arrhenius(temperature);
        }
        public override double Action(SampleValue sample, List<Symbol> reactants, double time, Vector state, double temperature, Style style) {
            double action = this.Rate(temperature);
            foreach (Symbol rs in reactants) action = action * state[sample.speciesIndex[rs]];
            return action;
        }
        public double Arrhenius(double temperature) { // temperature in Kelvin
            // Program.Log("Arrhenius " + collisionFrequency + ", " + activationEnergy + " = " + collisionFrequency * Math.Exp(-(activationEnergy / (R * temperature))));
            if (activationEnergy == 0.0) return collisionFrequency;
            else return collisionFrequency * Math.Exp(-(activationEnergy/(R*temperature)));
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
            return body.Eval(env.ExtendValues(symbol, parameters.parameters, arguments, style), netlist, style);
        }
        public Value ApplyFlow(List<Value> arguments, Style style) {
            return body.EvalFlow(env.ExtendValues(symbol, parameters.parameters, arguments, style), style);
        }
        public Flow BuildFlow(List<Flow> arguments, Style style) {
            return body.BuildFlow(env.ExtendValues(symbol, parameters.parameters, arguments, style), style); // note that even in this case the arguments are Values, not Flows
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
        public Value Apply(List<Value> arguments, Style style, Value vessel) {
            const string BadArguments = "Bad arguments to: ";
            if (arguments.Count == 0) {
                throw new Error(BadArguments + name);
            } else if (arguments.Count == 1) {
                Value arg1 = arguments[0];
                if (name == "temperature") if (arg1 is SampleValue) return new NumberValue(((SampleValue)arg1).Temperature()); else throw new Error(BadArguments + name);
                else if (name == "molarity") if (vessel is SampleValue && arg1 is SpeciesValue) return ((SampleValue)vessel).Molarity(((SpeciesValue)arg1).symbol, style); else throw new Error(BadArguments + name);
                else return ApplyFlow(arguments, style);
            } else if (arguments.Count == 2) {
                Value arg1 = arguments[0];
                Value arg2 = arguments[1];
                if (name == "molarity") if (arg1 is SampleValue && arg2 is SpeciesValue) return ((SampleValue)arg1).Molarity(((SpeciesValue)arg2).symbol, style); else throw new Error(BadArguments + name);
                else return ApplyFlow(arguments, style);
            } else throw new Error(BadArguments + name);
        }
        public Value ApplyFlow(List<Value> arguments, Style style) { // a subset of Apply
            const string BadArguments = "Bad arguments to: ";
            if (arguments.Count == 0) {
                throw new Error(BadArguments + name);
            } else if (arguments.Count == 1) {
                Value arg1 = arguments[0];
                if (name == "not") if (arg1 is BoolValue) return new BoolValue(!((BoolValue)arg1).value); else throw new Error(BadArguments + name);
                else if (name == "-") if (arg1 is NumberValue) return new NumberValue(-((NumberValue)arg1).value); else throw new Error(BadArguments + name);
                else if (name == "abs") if (arg1 is NumberValue) return new NumberValue(Math.Abs(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "arccos") if (arg1 is NumberValue) return new NumberValue(Math.Acos(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "arcsin") if (arg1 is NumberValue) return new NumberValue(Math.Asin(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "arctan") if (arg1 is NumberValue) return new NumberValue(Math.Atan(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "ceiling") if (arg1 is NumberValue) return new NumberValue(Math.Ceiling(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "cos") if (arg1 is NumberValue) return new NumberValue(Math.Cos(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "cosh") if (arg1 is NumberValue) return new NumberValue(Math.Cosh(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "exp") if (arg1 is NumberValue) return new NumberValue(Math.Exp(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "floor") if (arg1 is NumberValue) return new NumberValue(Math.Floor(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "int") if (arg1 is NumberValue) { double num1 = ((NumberValue)arg1).value; return new NumberValue(Math.Round(num1)); } else throw new Error(BadArguments + name);          // convert number to integer number by rounding
                else if (name == "log") if (arg1 is NumberValue) return new NumberValue(Math.Log(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "pos") if (arg1 is NumberValue) { double num1 = ((NumberValue)arg1).value; return new NumberValue((num1 > 0) ? num1 : 0); } else throw new Error(BadArguments + name);     // convert number to positive number by returning 0 if negative
                else if (name == "sign") if (arg1 is NumberValue) return new NumberValue(Math.Sign(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "sin") if (arg1 is NumberValue) return new NumberValue(Math.Sin(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "sinh") if (arg1 is NumberValue) return new NumberValue(Math.Sinh(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "sqrt") if (arg1 is NumberValue) return new NumberValue(Math.Sqrt(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "tan") if (arg1 is NumberValue) return new NumberValue(Math.Tan(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "tanh") if (arg1 is NumberValue) return new NumberValue(Math.Tanh(((NumberValue)arg1).value)); else throw new Error(BadArguments + name);
                else if (name == "volume") if (arg1 is SampleValue) return new NumberValue(((SampleValue)arg1).Volume()); else throw new Error(BadArguments + name);
                else throw new Error(BadArguments + name);
            } else if (arguments.Count == 2) {
                Value arg1 = arguments[0];
                Value arg2 = arguments[1];
                if (name == "or") if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value || ((BoolValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == "and") if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value && ((BoolValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == "+")
                    if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value + ((NumberValue)arg2).value);
                    else if (arg1 is StringValue && arg2 is StringValue) return new StringValue(((StringValue)arg1).value + ((StringValue)arg2).value);
                    else throw new Error(BadArguments + name);
                else if (name == "-") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value - ((NumberValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == "*") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value * ((NumberValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == "/") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(((NumberValue)arg1).value / ((NumberValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == "^") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Pow(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArguments + name);
                else if (name == "=")
                    if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value == ((BoolValue)arg2).value);
                    else if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value == ((NumberValue)arg2).value);
                    else if (arg1 is StringValue && arg2 is StringValue) return new BoolValue(((StringValue)arg1).value == ((StringValue)arg2).value);
                    else throw new Error(BadArguments + name);
                else if (name == "<>")
                    if (arg1 is BoolValue && arg2 is BoolValue) return new BoolValue(((BoolValue)arg1).value != ((BoolValue)arg2).value);
                    else if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value != ((NumberValue)arg2).value);
                    else if (arg1 is StringValue && arg2 is StringValue) return new BoolValue(((StringValue)arg1).value != ((StringValue)arg2).value);
                    else throw new Error(BadArguments + name);
                else if (name == "<=") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value <= ((NumberValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == "<") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value < ((NumberValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == ">=") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value >= ((NumberValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == ">") if (arg1 is NumberValue && arg2 is NumberValue) return new BoolValue(((NumberValue)arg1).value > ((NumberValue)arg2).value); else throw new Error(BadArguments + name);
                else if (name == "arctan2") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Atan2(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArguments + name);
                else if (name == "max") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Max(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArguments + name);
                else if (name == "min") if (arg1 is NumberValue && arg2 is NumberValue) return new NumberValue(Math.Min(((NumberValue)arg1).value, ((NumberValue)arg2).value)); else throw new Error(BadArguments + name);
                else throw new Error(BadArguments + name);
            } else throw new Error(BadArguments + name);
        }
        public Flow BuildFlow(List<Flow> arguments, Style style) {
            const string BadArguments = "Flow-expression: Bad arguments to: ";
            if (arguments.Count == 0) {
                // "time", "kelvin", and "celsius" are placed in the initial environment as Operators and converted to OpFlow when fetched as variables
                throw new Error(BadArguments + name);
            } else if (arguments.Count == 1) {
                Flow arg1 = arguments[0];
                if (name == "not" || name == "-") {
                    return new OpFlow(name, 1, true, new List<Flow> { arg1 });
                } else if (name == "var" || name == "poisson" || name == "abs" || name == "arccos" || name == "arcsin" || name == "arctan" || name == "ceiling" 
                        || name == "cos" || name == "cosh" || name == "exp" || name == "floor" || name == "int" || name == "log"
                        || name == "pos" || name == "sign" || name == "sin" || name == "sinh" || name == "sqrt" || name == "tan" || name == "tanh") {
                    return new OpFlow(name, 1, false, new List<Flow> { arg1 });
                } else throw new Error(BadArguments + name);
            } else if (arguments.Count == 2) {
                Flow arg1 = arguments[0];
                Flow arg2 = arguments[1];
                if (name == "or" || name == "and" || name == "+" || name == "-" || name == "*" || name == "/" || name == "^"
                    || name == "=" || name == "<>" || name == "<=" || name == "<" || name == ">=" || name == ">") {
                    return new OpFlow(name, 2, true, new List<Flow> { arg1, arg2 });
                } else if (name == "cov" || name == "gauss" || name == "arctan2" || name == "min" || name == "max") {
                    return new OpFlow(name, 2, false, new List<Flow> { arg1, arg2 });
                } else throw new Error(BadArguments + name);
            } else if (arguments.Count == 3) {
                if (name == "cond") {
                    return new OpFlow(name, 3, false, new List<Flow> { arguments[0], arguments[1], arguments[2] });
                } else throw new Error(BadArguments + name);
            } else throw new Error(BadArguments + name);
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
            Env ignoreEnv = body.Eval(env.ExtendValues(symbol, parameters.parameters, arguments, style), netlist, style);
        }
    }

    // FLOWS

    // A flow expression should evaluate to NormalFlow (BoolFlow, NumberFlow, SpeciesFlow, or OpFlow combining them)
    // on the way to producing those, OperatorFlow, and FunctionFlow are also used, but they must be expanded to the above by FunctionInstance
    // if non-normal Flows survive, errors are give at simulation time (for '{{...}}' flows) or report time (for 'report ...' flows)

    public abstract class Flow : Value {
        public abstract bool Involves(List<SpeciesValue> species);
        public abstract bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered);
        public abstract bool ReportBool(SampleValue sample, double time, State state, Style style); // for boolean flow-subexpressions
        public abstract double ReportMean(SampleValue sample, double time, State state, Style style); // for numeric flow-subexpressions
        public abstract double ReportVariance(SampleValue sample, double time, State state, Style style);
        public abstract double ReportCovariance(Flow other, SampleValue sample, double time, State state, Style style);
        public abstract bool HasDeterministicMean();
        // = Can appear in non-LNA charts and in generalized rates {{ ... }}. Excludes var/cov, poisson/gauss.
        public abstract bool HasStochasticMean();
        // = Can appear in LNA charts as means and variance. Includes everyting except that inside var/cov only linear combinations 
        // of species are allowed, also because of issues like cov(poisson(X),poisson(X)) =? var(poisson(X))
        public abstract bool LinearCombination();
        // = Is a linear combination of species and time/kelvin/celsius flows only
        public abstract bool HasNullVariance();
        // Has stochastic variance identically zero, used to detect and rule out non-linear products.  
    }

    public class BoolFlow : Flow {
        public bool value;
        public BoolFlow(bool value) { this.type = new Type("flow"); this.value = value; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override string Format(Style style) { if (this.value) return "true"; else return "false"; }
        public override bool ReportBool(SampleValue sample, double time, State state, Style style) { return this.value; }
        public override double ReportMean(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of bool: " + Format(style)); }
        public override double ReportVariance(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of bool: " + Format(style)); }
        public override double ReportCovariance(Flow other, SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of bool: " + Format(style)); }
        public override bool HasDeterministicMean() { return true; } // can appear in rate-expressions in a cond
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; } // but if it appears in a cond it will not be a linear combination anyway
        public override bool HasNullVariance() { return true; }
    }

    public class NumberFlow : Flow {
        public double value;
        public NumberFlow(double value) { this.type = new Type("flow"); this.value = value; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override string Format(Style style) { return style.FormatDouble(this.value); }
        public override bool ReportBool(SampleValue sample, double time, State state, Style style) {  throw new Error("Flow expression: bool expected instead of number: " + Format(style)); }
        public override double ReportMean(SampleValue sample, double time, State state, Style style) { return this.value; }
        public override double ReportVariance(SampleValue sample, double time, State state, Style style) { return 0.0; } // Var(number) = 0
        public override double ReportCovariance(Flow other, SampleValue sample, double time, State state, Style style) { return 0.0; } // Cov(number,Y) = 0
        public override bool HasDeterministicMean() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return true; }
    }

    public class StringFlow : Flow { // this is probably not very useful, but e.g. cond("a"="b",a,b)
        public string value;
        public StringFlow(string value) { this.type = new Type("flow"); this.value = value; }
        public override bool Involves(List<SpeciesValue> species) { return false; }
        public override bool CoveredBy(List<SpeciesValue> species, out Symbol notCovered) { notCovered = null; return true; }
        public override string Format(Style style) { return Parser.FormatString(this.value); }
        public override bool ReportBool(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: bool expected instead of string: " + Format(style)); }
        public override double ReportMean(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of string: " + Format(style)); }
        public override double ReportVariance(SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of string: " + Format(style)); }
        public override double ReportCovariance(Flow other, SampleValue sample, double time, State state, Style style) { throw new Error("Flow expression: number expected instead of string: " + Format(style)); }
        public override bool HasDeterministicMean() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return true; }
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
        public override bool ReportBool(SampleValue sample, double time, State state, Style style) {
            throw new Error("Flow expression: bool expected instead of species: " + Format(style));
        }
        public override double ReportMean(SampleValue sample, double time, State state, Style style) {
            return state.Mean(sample.speciesIndex[this.species]);
        }
        public override double ReportVariance(SampleValue sample, double time, State state, Style style) {
            int i = sample.speciesIndex[this.species];
            return state.Covar(i, i);
        }
        public override double ReportCovariance(Flow other, SampleValue sample, double time, State state, Style style) {
            if (other is SpeciesFlow)
                return state.Covar(sample.speciesIndex[this.species], sample.speciesIndex[((SpeciesFlow)other).species]);
            else return other.ReportCovariance(this, sample, time, state, style);
        }
        public override bool HasDeterministicMean() { return true; }
        public override bool HasStochasticMean() { return true; }
        public override bool LinearCombination() { return true; }
        public override bool HasNullVariance() { return false; }
    }

    public class OpFlow : Flow {
        public string op;
        public int arity;
        public bool infix;
        public List<Flow> args;
        public OpFlow(string op, int arity, bool infix, List<Flow> args) {
            this.type = new Type("flow");
            this.op = op;
            this.arity = arity;
            this.infix = infix;
            this.args = args;
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
                if (this.infix) return "(" + op + " " + args[0].Format(style) + ")";
                else return op + "(" + args[0].TopFormat(style) + ")";
            } else if (arity == 2) {
                if (this.infix) {
                    string arg1 = args[0].Format(style);
                    string arg2 = args[1].Format(style);
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
        public override bool ReportBool(SampleValue sample, double time, State state, Style style) {
            const string BadArguments = "Flow expression: Bad arguments to: ";
            const string BadResult = "Flow expression: boolean operator expected instead of: ";
            if (arity == 0) {
                throw new Error(BadResult + op);
            } else if (arity == 1) {
                bool arg1 = args[0].ReportBool(sample, time, state, style);
                if (op == "not") return !arg1;
                else throw new Error(BadResult + op);
            } else if (arity == 2) {
                if (op == "or") return args[0].ReportBool(sample, time, state, style) || args[1].ReportBool(sample, time, state, style);
                else if (op == "and") return args[0].ReportBool(sample, time, state, style) && args[1].ReportBool(sample, time, state, style);
                else if (op == "<=") return args[0].ReportMean(sample, time, state, style) <= args[1].ReportMean(sample, time, state, style);
                else if (op == "<") return args[0].ReportMean(sample, time, state, style) < args[1].ReportMean(sample, time, state, style);
                else if (op == ">=") return args[0].ReportMean(sample, time, state, style) >= args[1].ReportMean(sample, time, state, style);
                else if (op == ">") return args[0].ReportMean(sample, time, state, style) > args[1].ReportMean(sample, time, state, style);
                else if (op == "=") {
                    if (args[0] is BoolFlow && args[1] is BoolFlow) return ((BoolFlow)args[0]).value == ((BoolFlow)args[1]).value;
                    else if (args[0] is StringFlow && args[1] is StringFlow) return ((StringFlow)args[0]).value == ((StringFlow)args[1]).value;
                    else if ((args[0] is NumberFlow || args[0] is SpeciesFlow) && (args[1] is NumberFlow || args[1] is SpeciesFlow)) return args[0].ReportMean(sample, time, state, style) == args[1].ReportMean(sample, time, state, style);
                    else throw new Error(BadArguments + op);
                } else if (op == "<>") {
                    if (args[0] is BoolFlow && args[1] is BoolFlow) return ((BoolFlow)args[0]).value != ((BoolFlow)args[1]).value;
                    else if (args[0] is StringFlow && args[1] is StringFlow) return ((StringFlow)args[0]).value != ((StringFlow)args[1]).value;
                    else if ((args[0] is NumberFlow || args[0] is SpeciesFlow) && (args[1] is NumberFlow || args[1] is SpeciesFlow)) return args[0].ReportMean(sample, time, state, style) != args[1].ReportMean(sample, time, state, style);
                    else throw new Error(BadArguments + op);
                } else throw new Error(BadResult + op);
            } else if (arity == 3) {
                if (op == "cond") {
                    if (args[0].ReportBool(sample, time, state, style))
                        return args[1].ReportBool(sample, time, state, style);
                    else return args[2].ReportBool(sample, time, state, style);
                } else throw new Error(BadResult + op);
            } else throw new Error(BadArguments + op);
        }
        public override double ReportMean(SampleValue sample, double time, State state, Style style) {
            const string BadArguments = "Flow expression: Bad arguments to: ";
            const string BadResult = "Flow expression: numerical operator expected instead of: ";
            if (arity == 0) {
                if (op == "time") return time;
                else if (op == "kelvin") return sample.Temperature();
                else if (op == "celsius") return sample.Temperature() - 273.15;
                else throw new Error(BadResult + op);
            } else if (arity == 1) {
                double arg1 = args[0].ReportMean(sample, time, state, style);
                if (op == "var") {
                    return args[0].ReportVariance(sample, time, state, style);              // Mean(var(X)) = var(X)  since var(X) is a number
                } else if (op == "poisson") {
                    return args[0].ReportMean(sample, time, state, style);                  // Mean(poisson(X)) = X
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
                    else if (op == "log") if (arg1 > 0) return Math.Log(arg1); else return 0.0;
                    else if (op == "pos") return (arg1 > 0) ? arg1 : 0;
                    else if (op == "sign") return Math.Sign(arg1);
                    else if (op == "sin") return Math.Sin(arg1);
                    else if (op == "sinh") return Math.Sinh(arg1);
                    else if (op == "sqrt") if (arg1 >= 0) return Math.Sqrt(arg1); else return 0.0;
                    else if (op == "tan") return Math.Tan(arg1);
                    else if (op == "tanh") return Math.Tanh(arg1);
                    else throw new Error(BadResult + op);
                }
            } else if (arity == 2) {
                if (op == "cov") {
                    return args[0].ReportCovariance(args[1], sample, time, state, style);        // Mean(cov(X,Y)) = cov(X,Y)   since cov(X,Y) is a number
                } else if (op == "gauss") {
                    return args[0].ReportMean(sample, time, state, style);                       // Mean(gauss(X,Y)) = X
                } else {
                    double arg1 = args[0].ReportMean(sample, time, state, style);
                    double arg2 = args[1].ReportMean(sample, time, state, style);
                    if (op == "+") return arg1 + arg2;
                    else if (op == "-") return arg1 - arg2;
                    else if (op == "*") return arg1 * arg2;
                    else if (op == "/") if (arg2 != 0.0) return arg1 / arg2; else return 0.0;
                    else if (op == "^") return Math.Pow(arg1, arg2);
                    else if (op == "arctan2") return Math.Atan2(arg1, arg2);
                    else if (op == "min") return Math.Min(arg1, arg2);
                    else if (op == "max") return Math.Max(arg1, arg2);
                    else throw new Error(BadResult + op);
                }
            } else if (arity == 3) {
                if (op == "cond") {
                    if (args[0].ReportBool(sample, time, state, style))
                        return args[1].ReportMean(sample, time, state, style);
                    else return args[2].ReportMean(sample, time, state, style);
                } else throw new Error(BadResult + op);
            } else throw new Error(BadArguments + op);
        }
        public override bool HasDeterministicMean() {
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius"
            }  else if (arity == 1) {
                return (op != "var" && op != "poisson") && args[0].HasDeterministicMean();
                // Although var(X) is a number, we need the LNA info to compute it, so we say it is not deterministic
                // poisson is not allowed in determinstic plots or general rates
            } else if (arity == 2) {
                return (op != "cov" && op != "gauss") && args[0].HasDeterministicMean() && args[1].HasDeterministicMean();
                // Although cov(X,Y) is a number, we need the LNA info to compute it, so we say it is not deterministic
                // gauss is not allowed in determinstic plots or general rates
            }  else if (arity == 3) { // including "cond"
                return args[0].HasDeterministicMean() && args[1].HasDeterministicMean() && args[2].HasDeterministicMean();
            }  else throw new Error("HasDeterministicMean: " + op);
        }
        public override bool HasStochasticMean() {
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius"
            } else if (arity == 1) {                                     // exclude (op == "var" || op == "poisson")
                    if (op == "var") return args[0].LinearCombination();
                    else return args[0].HasStochasticMean();
            }  else if (arity == 2) {                                     // exclude (op == "cov" || op == "gauss")
                    if (op == "cov") return args[0].LinearCombination() && args[1].LinearCombination();
                    else return args[0].HasStochasticMean() && args[1].HasStochasticMean();
            } else if (arity == 3) { // including "cond"
                return args[0].HasStochasticMean() && args[1].HasStochasticMean() && args[2].HasStochasticMean();
            } else throw new Error("LinearCombination: " + op);
        }
        public override bool LinearCombination() { // returns true for linear combinations of species and zero-variance flows (time, kelvin, celsius)
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius"
            } else if (arity == 1) {                                     // exclude (op == "var" || op == "poisson")
                if (op == "-") return args[0].LinearCombination();
                else return false;
            } else if (arity == 2) {                                     // exclude (op == "cov" || op == "gauss")
                if (op == "+" || op == "-") return args[0].LinearCombination() && args[1].LinearCombination();
                else if (op == "*") return args[0].LinearCombination() && args[1].LinearCombination() && (args[0].HasNullVariance() || args[1].HasNullVariance());
                else return false;
            } else if (arity == 3) {
                if (op == "cond") return false; // we do not support "cond", what would be the var(cond(...)) or cov(cond(...), something) ?
                else throw new Error("LinearCombination: " + op);
            } else throw new Error("LinearCombination: " + op);
        }
        public override bool HasNullVariance() {
            if (arity == 0) {
                return true; // "time", "kelvin", "celsius"
            } else if (arity == 1) {
                if (op == "var") return true;                                    // var(X) is a number so it has a zero variance
                else if (op == "poisson") return false;
                else return args[0].HasNullVariance();
            } else if (arity == 2) {
                if (op == "cov") return true;                                    // cov(X,Y) is a number so it has a zero variance
                else if (op == "gauss") return false;
                else return args[0].HasNullVariance() && args[1].HasNullVariance();
            } else if (arity == 3) {
                if (op == "cond") return args[0].HasNullVariance() && args[1].HasNullVariance() && args[2].HasNullVariance();
                else throw new Error("HasNullVariance: " + op);
            } else throw new Error("HasNullVariance: " + op);
        }
        public override double ReportVariance(SampleValue sample, double time, State state, Style style) {
            const string BadArguments = "Flow expression: Bad arguments to: ";
            const string BadResult = "Flow expression: Variance invalid for operator: ";
            if (arity == 0) {
                return 0.0; // "time", "kelvin", "celsius"                                                // Var(constant) = 0
            } else if (arity == 1) {
                if (op == "var") {
                    return 0.0;      // yes needed for e.g. "report a + var(a)"                            // Var(var(X)) = 0 since var(X) is a number
                } else if(op == "poisson")
                    return Math.Abs(args[0].ReportMean(sample, time, state, style));                       // Var(poisson(X)) = Abs(mean(X))
                else if (op == "-") {
                    return args[0].ReportVariance(sample, time, state, style);                             // Var(-X) = Var(X)
                } else throw new Error(BadResult + op); // all other arithmetic operators: we only handle linear combinations
            } else if (arity == 2) {
                if (op == "cov") {
                    return 0.0;      // yes needed                                                          // Var(cov(X,Y)) = 0 since cov(X,Y) is a number
                } else if (op == "gauss") {
                    return Math.Abs(args[1].ReportMean(sample, time, state, style));                       // Var(gauss(X,Y)) = Abs(mean(Y))
                } else if (op == "+") {                                                                     // Var(X+Y) = Var(X) + Var(Y) + 2*Cov(X,Y)
                    double arg1 = args[0].ReportVariance(sample, time, state, style);
                    double arg2 = args[1].ReportVariance(sample, time, state, style);
                    return arg1 + arg2 + 2 * args[0].ReportCovariance(args[1], sample, time, state, style);
                } else if (op == "-") {                                                                 // Var(X-Y) = Var(X) + Var(Y) - 2*Cov(X,Y)
                    double arg1 = args[0].ReportVariance(sample, time, state, style);
                    double arg2 = args[1].ReportVariance(sample, time, state, style);
                    return arg1 + arg2 - 2 * args[0].ReportCovariance(args[1], sample, time, state, style);
                } else if (op == "*") {
                    if (args[0].HasNullVariance() && args[1].HasNullVariance())
                        return 0.0;
                    else if (args[0].HasNullVariance() && (!args[1].HasNullVariance())) {                 // Var(n*X) = n^2*Var(X)
                        double arg1 = args[0].ReportMean(sample, time, state, style);
                        double arg2 = args[1].ReportVariance(sample, time, state, style);
                        return arg1 * arg1 * arg2;
                    } else if ((!args[0].HasNullVariance()) && args[1].HasNullVariance()) {                // Var(X*n) = Var(X)*n^2
                        double arg1 = args[0].ReportVariance(sample, time, state, style);
                        double arg2 = args[1].ReportMean(sample, time, state, style);
                        return arg1 * arg2 * arg2;
                    } else throw new Error(BadResult + op); // this will be prevented by checking ahead of time
                } else throw new Error(BadResult + op); // all other operators, including "/" , "^" , "arctan2" , "min" , "max"
            } else if (arity == 3) {
                if (op == "cond") {
                    if (args[0].ReportBool(sample, time, state, style))
                        return args[1].ReportVariance(sample, time, state, style);
                    else return args[2].ReportVariance(sample, time, state, style);
                } else throw new Error(BadResult + op);
            } else throw new Error(BadArguments + op);
        }
        public override double ReportCovariance(Flow other, SampleValue sample, double time, State state, Style style) {
            const string BadArguments = "Flow expression: Bad arguments to: ";
            const string BadResult = "Flow expression: Covariance invalid for operator: ";
            if (arity == 0) {
                return 0.0; // "time", "kelvin", "celsius"                                                    // Cov(number,Y) = 0
            } else if (arity == 1) {
                if (op == "var") {
                    return 0.0;      // yes needed for e.g. "report a + cov(a,a)"                            // Cov(var(X),Z) = 0 since var(X) is a number
                } else if (op == "poisson")
                    return 0.0;
                else if (op == "-")
                    return 0.0 - args[0].ReportCovariance(other, sample, time, state, style);                // Cov(-X,Y) = -Cov(X,Y)
                else throw new Error(BadResult + op); // all other arithmetic operators: we only handle linear combinations
            } else if (arity == 2) {
                if (op == "cov") {
                    return 0.0;      // yes needed                                                           // Cov(cov(X,Y),Z) = 0 since cov(X,Y) is a number
                } else if (op == "gauss") {
                    return 0.0;
                } else if (op == "+") {                                                                      // Cov(X+Z,Y) = Cov(X,Y) + Cov(Z,Y)
                    return args[0].ReportCovariance(other, sample, time, state, style) +
                        + args[1].ReportCovariance(other, sample, time, state, style);
                } else if (op == "-") {                                                                       // Cov(X-Z,Y) = Cov(X,Y) - Cov(Z,Y)
                    return args[0].ReportCovariance(other, sample, time, state, style) +
                        - args[1].ReportCovariance(other, sample, time, state, style);
                } else if (op == "*") {
                    if (args[0].HasNullVariance() && args[1].HasNullVariance())                               // Cov(n*m,Y) = 0
                        return 0.0;
                    else if (args[0].HasNullVariance() && (!args[1].HasNullVariance())) {                      // Cov(n*X,Y) = n*Cov(X,Y) 
                        return args[0].ReportMean(sample, time, state, style) *
                            args[1].ReportCovariance(other, sample, time, state, style);
                    } else if ((!args[0].HasNullVariance()) && args[1].HasNullVariance()) {                    // Cov(X*n,Y) = Cov(X,Y)*n
                        return args[0].ReportCovariance(other, sample, time, state, style) *
                            args[1].ReportMean(sample, time, state, style);
                    } else throw new Error(BadResult + op); // all other operators, including "/" , "^" , "arctan2" , "min" , "max"
                } else throw new Error(BadResult + op);
            } else if (arity == 3) {
                if (op == "cond") { 
                    if (args[0].ReportBool(sample, time, state, style))
                        return args[1].ReportCovariance(other, sample, time, state, style);
                    else return args[2].ReportCovariance(other, sample, time, state, style);
                } else throw new Error(BadResult + op);
            } else throw new Error(BadArguments + op);
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
        public Variable(string name){
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
            if (value is Flow) return (Flow)value; // this must have been a variable declared of type flow
            else if (value is BoolValue) return new BoolFlow(((BoolValue)value).value);
            else if (value is NumberValue) return new NumberFlow(((NumberValue)value).value);
            else if (value is SpeciesValue) return new SpeciesFlow(((SpeciesValue)value).symbol);
            else if (value is OperatorValue) { // handle the nullary operators from the built-in environment
                if (((OperatorValue)value).name == "time") return new OpFlow("time", 0, false, new List<Flow>());
                else if (((OperatorValue)value).name == "kelvin") return new OpFlow("kelvin", 0, false, new List<Flow>());
                else if (((OperatorValue)value).name == "celsius") return new OpFlow("celsius", 0, false, new List<Flow>());
                else throw new Error("Flow expression: Variable '" + this.Format() + "' should denote a flow");
            } else throw new Error("Flow expression: Variable '" + this.Format() + "' should denote a flow");
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
        public NumberLiteral(double value){ this.value = value; }
        public override string Format() { return this.value.ToString(); }
        public override void Scope(Scope scope) { }
        public override Value Eval(Env env, Netlist netlist, Style style) { return new NumberValue(this.value); }
        public override Value EvalFlow(Env env, Style style) { return new NumberValue(this.value); }
        public override Flow BuildFlow(Env env, Style style) { return new NumberFlow(this.value); }
    }

    public class StringLiteral : Expression {
        public string value;
        public StringLiteral(string value){ this.value = value; }
        public override string Format() { return this.value; }
        public override void Scope(Scope scope) { }
        public override Value Eval(Env env, Netlist netlist, Style style) { return new StringValue(this.value); }
        public override Value EvalFlow(Env env, Style style) { return new StringValue(this.value); }
        public override Flow BuildFlow(Env env, Style style) { return new StringFlow(this.value); }
    }

    public class FunctionAbstraction : Expression {
        private Parameters parameters;
        private Expression body; // the body will be a BlockExpression
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
            if (statements.statements.Count() == 0) return " return " + this.expression.Format();
            else return this.statements.Format() + Environment.NewLine + " return " + this.expression.Format();
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
            throw new Error("BlockExpression EvalFlow");
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
        private Arguments arguments;
        public FunctionInstance(Expression function, Arguments arguments) {
            this.function = function;
            this.arguments = arguments;
        }
        public override string Format() {
            return function.Format() + "(" + arguments.Format() + ")";
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
                if (Gui.gui.TraceComputational()) {
                    Style restyle = style.RestyleAsDataFormat("symbol");
                    invocation = closure.Format(restyle) + "(" + this.arguments.FormatValues(arguments, restyle) + ")";
                    netlist.Emit(new CommentEntry("BEGIN " + invocation));
                }
                Value result = closure.Apply(arguments, netlist, style);
                if (Gui.gui.TraceComputational()) {
                    netlist.Emit(new CommentEntry("END " + invocation));
                }
                return result;
            } else if (value is OperatorValue) {
                OperatorValue oper = (OperatorValue)value;
                if (oper.name == "if") { // it was surely parsed with 3 arguments
                    List<Expression> actuals = this.arguments.arguments;
                    Value cond = actuals[0].Eval(env, netlist, style);
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].Eval(env, netlist, style); else return actuals[2].Eval(env, netlist, style);
                    else throw new Error("'if' predicate should be a bool: " + Format());
                } else {
                    List<Value> arguments = this.arguments.Eval(env, netlist, style);
                    return oper.Apply(arguments, style, env.LookupValue("vessel"));
                }
            } else throw new Error("Invocation of a non-function or non-operator: " + Format());
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
                    List<Expression> actuals = this.arguments.arguments;
                    Value cond = actuals[0].EvalFlow(env, style);
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].EvalFlow(env, style); else return actuals[2].EvalFlow(env, style);
                    else throw new Error("Flow expression: 'if' predicate should be a bool: " + Format());
                } else {
                    List<Value> arguments = this.arguments.EvalFlow(env, style);
                    return oper.ApplyFlow(arguments, style);
                }
            } else throw new Error("Flow expression: Invocation of a non-function or non-operator: " + Format());
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
                    List<Expression> actuals = this.arguments.arguments;
                    Value cond = actuals[0].EvalFlow(env, style); // this is a real boolean value, not a flow
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].BuildFlow(env, style); else return actuals[2].BuildFlow(env, style);
                    else throw new Error("Flow expression: 'if' predicate should be a bool: " + Format());
                } else {
                    List<Flow> arguments = this.arguments.BuildFlow(env, style); // operator arguments are Flows that are composed with the operator
                    return oper.BuildFlow(arguments, style);
                }
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
            Symbol symbol = new Symbol(this.name);                                                             // create a new symbol from name
            Value value = type.Is("flow") ? definee.BuildFlow(env, style) : definee.Eval(env, netlist, style); // evaluate
            Env extEnv = new ValueEnv(symbol, type, value, env);                                               // checks that the types match
            netlist.Emit(new ValueEntry(symbol, type, value));                                                  // embed the new symbol also in the netlist
            return extEnv;                                                                                     // return the extended environment
        }
        public Env BuildFlow(Env env, Style style) {   // special case: only value definitions among all statements support BuildFlow
            Symbol symbol = new Symbol(this.name);                                      // create a new symbol from name
            Flow flow = definee.BuildFlow(env, style);                                  // evaluate
            Env extEnv = new ValueEnv(symbol, new Type("flow"), flow, env);             // checks that the ("flow") types match
            return extEnv;                                                              // return the extended environment
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
            double volumeValue = ProtocolActuator.NormalizeVolume(((NumberValue)volume).value, this.volumeUnit);
            double temperatureValue = ProtocolActuator.NormalizeTemperature(((NumberValue)temperature).value, this.temperatureUnit);
            if (volumeValue <= 0) throw new Error("Sample volume must be positive: " + this.name);
            if (temperatureValue < 0) throw new Error("Sample temperature must be non-negative: " + this.name);
            Symbol symbol = new Symbol(name);
            SampleValue sample = new SampleValue(symbol, new NumberValue(volumeValue), new NumberValue(temperatureValue)); 
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
        private Arguments arguments;
        public NetworkInstance(Variable network, Arguments arguments) {
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
                if (Gui.gui.TraceComputational()) {
                    Style restyle = style.RestyleAsDataFormat("symbol");
                    invocation = closure.Format(restyle) + "(" + this.arguments.FormatValues(arguments, restyle) + ")";
                    netlist.Emit(new CommentEntry("BEGIN " + invocation));
                }
                closure.Apply(arguments, netlist, style);
                if (Gui.gui.TraceComputational()) {
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
            if (!flow.HasDeterministicMean()) throw new Error("This flow-expression cannot appear in {{ ... }} rate: " + rateFunction.Format());
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
            if (!(cf is NumberValue)) throw new Error("Reaction rate collision frequency must be a number");
            if (!(ae is NumberValue)) throw new Error("Reaction rate activation energy must be a number");
            double cfv = ((NumberValue)cf).value;
            double aev = ((NumberValue)ae).value;
            if (cfv < 0) throw new Error("Reaction rate collision frequency must be non-negative");
            if (aev < 0) throw new Error("Reaction rate activation energy must be non-negative");
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
            SampleValue sample = ProtocolActuator.Mix(symbol, fstSample, sndSample, style);
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
            (SampleValue sample1, SampleValue sample2) = ProtocolActuator.Split(symbol1, symbol2, fromSample, prop, style);
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
            ProtocolActuator.Dispose(dispSample, style);
            netlist.Emit(new DisposeEntry(dispSample));
            return env;
        }
    }
   
    public class Equilibrate : Statement {
        private string name;
        private Expression sample;
        private Expression fortime;
        public Equilibrate(string name, Expression sample, Expression fortime) {
            this.name = name;
            this.sample = sample;
            this.fortime = fortime;
        }
        public override string Format() {       
            return "equilibrate" +  " " + name + " := " + sample.Format() + " for " + fortime.Format();
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = new ConsScope(name, scope);
            sample.Scope(scope);
            fortime.Scope(scope);
            return extScope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value inSampleValue = this.sample.Eval(env, netlist, style);
            if (!(inSampleValue is SampleValue)) throw new Error("equilibrate '" + name + "' requires a sample as first value");
            SampleValue inSample = (SampleValue)inSampleValue;
            Value forTimeValue = this.fortime.Eval(env, netlist, style);
            if (!(forTimeValue is NumberValue)) throw new Error("equilibrate '" + name + "' requires a number as second value");
            double forTime = ((NumberValue)forTimeValue).value;
            if (forTime < 0) throw new Error("equilibrate '" + name + "' requires a nonnegative number second value");
            Symbol symbol = new Symbol(name);
            SampleValue outSample = ProtocolActuator.Equilibrate(symbol, inSample, forTime, netlist, style);
            netlist.Emit(new EquilibrateEntry(outSample, inSample, (NumberValue)forTimeValue));
            return new ValueEnv(symbol, null, outSample, env);
        }
    }

    public class ChangeSample : Statement {
        private Expression sample;
        private Expression volume;
        private string volumeUnit;
        private Expression temperature;
        private string temperatureUnit;
        public ChangeSample(Expression sample, Expression volume, string volumeUnit, Expression temperature, string temperatureUnit) {
            this.sample = sample;
            this.volume = volume;
            this.volumeUnit = volumeUnit;
            this.temperature = temperature;
            this.temperatureUnit = temperatureUnit;
        }
        public override string Format() {
            return "change" + sample.Format() + " { " + volume.Format() + volumeUnit + temperature.Format() + temperatureUnit + " }";
        }
        public override Scope Scope(Scope scope) {
            sample.Scope(scope);
            volume.Scope(scope);
            temperature.Scope(scope);
            return scope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value sampleValue = this.sample.Eval(env, netlist, style);
            if (!(sampleValue is SampleValue)) throw new Error("'change' requires a sample as first value");
            SampleValue outSample = (SampleValue)sampleValue;

            Value volumeValue = this.volume.Eval(env, netlist, style);
            if (!(volumeValue is NumberValue)) throw new Error("'change' requires a number for volume");
            double newVolume = ProtocolActuator.NormalizeVolume(((NumberValue)volumeValue).value, volumeUnit);
            if (newVolume <= 0) throw new Error("'change' volume must be positive: " + outSample.symbol.Format(style));

            Value temperatureValue = this.temperature.Eval(env, netlist, style);
            if (!(temperatureValue is NumberValue)) throw new Error("'change' requires a number for temperature");
            double newTemperature = ProtocolActuator.NormalizeTemperature(((NumberValue)temperatureValue).value, temperatureUnit);
            if (newTemperature < 0) throw new Error("'change' temperature must be non-negative: " + outSample.symbol.Format(style));

            outSample.ChangeTemperature(newTemperature); // ### these operations must now generate new samples
            outSample.ChangeVolume(newVolume);
            netlist.Emit(new ChangeSampleEntry(outSample));
            return env;
        }
    }

    public class ChangeSpecies : Statement {
        private Expression species;
        private Expression amount;
        private string dimension;
        private Expression sample;
        public ChangeSpecies(Expression species, Expression amount, string dimension, Expression sample) {
            this.species = species;
            this.amount = amount;
            this.dimension = dimension;
            this.sample = sample;
        }
        public override string Format() {
            return "change" + species.Format() + " @ " + amount.Format() + dimension + " in " + sample.Format();
        }
        public override Scope Scope(Scope scope) {
            species.Scope(scope);
            amount.Scope(scope);
            sample.Scope(scope);
            return scope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            Value speciesValue = this.species.Eval(env, netlist, style);
            if (!(speciesValue is SpeciesValue)) throw new Error("'change' requires a species as first value");
            Value newValue = this.amount.Eval(env, netlist, style);
            if (!(newValue is NumberValue)) throw new Error("'change' requires a number value for concentration");
            Value sampleValue = this.sample.Eval(env, netlist, style);
            if (!(sampleValue is SampleValue)) throw new Error("'change' requires a sample to change");
            ((SampleValue)sampleValue).ChangeMolarity((SpeciesValue)speciesValue, (NumberValue)newValue, this.dimension, style);
            netlist.Emit(new ChangeSpeciesEntry((SpeciesValue)speciesValue, (NumberValue)newValue, (SampleValue)sampleValue));
            return env;
        }
    }

    public class Report : Statement {
        public Expression expression;   // just a subset of numerical arithmetic expressions that can be plotted
        public List<Expression> asList; // can be null
        public Report(Expression expression, List<Expression> asList) {
            this.expression = expression;
            this.asList = asList;
        }
        public override string Format() {
            string s = "report " + this.expression.Format();
            if (asList != null) {
                s += " as [";
                string l = "";
                foreach (Expression item in this.asList) l += item.Format() + ", ";
                if (l.Length > 0) l = l.Substring(0, l.Length - 2);
                s += l + "]";
            }
            return s;
        }
        public override Scope Scope(Scope scope) {
            this.expression.Scope(scope);
            if (this.asList != null) foreach (Expression item in this.asList) item.Scope(scope);
            return scope;
        }
        public override Env Eval(Env env, Netlist netlist, Style style) {
            string asLabel = null;
            if (this.asList != null) {
                asLabel = "";
                foreach (Expression item in this.asList) {
                    Value value = item.EvalFlow(env, style);
                    if (value is StringValue) asLabel += ((StringValue)value).value; // the raw string contents, unquoted
                    else asLabel += value.Format(style);
                }
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
        public List<Parameter> parameters;
        public Parameters() {
            this.parameters = new List<Parameter> { };
        }
        public Parameters Add(Parameter param) {
            this.parameters.Add(param);
            return this;
        }
        public override string Format() {
            return this.parameters.Aggregate("", (a, b) => (a == "") ? b.Format() : a + ", " + b.Format());
        }
    }
    public class Parameter : Tree {
        public Type type;
        public string name;
        public Parameter(Type type, string id) {
            this.type = type;
            this.name = id;
        }
        public override string Format() {
            return this.type.Format() + " " + this.name;
        }
    } 

    // ARGUMENTS

    public class Arguments : Tree {
        public List<Expression> arguments;
        public Arguments() {
            this.arguments = new List<Expression> { };
        }
        public Arguments Add(Expression argument) {
            this.arguments.Add(argument);
            return this;
        }
        public override string Format() {
            return this.arguments.Aggregate("", (a, b) => (a == "") ? b.Format() : a + ", " + b.Format());
        }
        public void Scope(Scope scope) {
            foreach (Expression argument in this.arguments) { argument.Scope(scope); }
        }
        public List<Value> Eval(Env env, Netlist netlist, Style style) {
            List<Value> arguments = new List<Value>();
            foreach (Expression argument in this.arguments) { arguments.Add(argument.Eval(env, netlist, style)); }
            return arguments;
        }
        public List<Value> EvalFlow(Env env, Style style) {
            List<Value> arguments = new List<Value>();
            foreach (Expression argument in this.arguments) { arguments.Add(argument.EvalFlow(env, style)); }
            return arguments;
        }
        public List<Flow> BuildFlow(Env env, Style style) {
            List<Flow> arguments = new List<Flow>();
            foreach (Expression argument in this.arguments) { arguments.Add(argument.BuildFlow(env, style)); }
            return arguments;
        }
        public string FormatValues(List<Value> values, Style style) {
            string s = "";
            foreach (Value value in values) { s += value.Format(style) + ", "; }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2); // remove last comma
            return s;
        }
    }

}

