using System;
using System.Collections.Generic;
using System.Linq;

namespace Kaemika {

    public abstract class Env {
        public const Env REJECT = null; // return REJECT=null from Statement evaluation to mean "sample is rejected" in random variable sampling (instead of an exception, trapping which is way too expensive)
        public abstract Symbol LookupSymbol(string name);
        public abstract Value LookupValue(string name);
        public abstract void AssignValue(Symbol symbol, Value value, bool reassign = false);
        public abstract string Format(Style style);
        public abstract Scope ToScope();

        public static string FormatTopLevel(Env env, Style style) {
            style = style.RestyleAsDataFormat("header");
            Env next = env;
            string s = "";
            while (next is ValueEnv curr) { // will stop at BuiltinEnv
                string last;
                if (curr.symbol.Raw() == "vessel") last = "";
                else if (curr.value is FunctionValue || curr.value is NetworkValue || curr.value is DistributionValue) last = Types.Format(curr.type) + " " + curr.value.Format(style) + " {..}" + Environment.NewLine;
                else if (curr.value is SampleValue) last = curr.value.Format(style) + " {..}" + Environment.NewLine;
                else if (curr.value is SpeciesValue) last = Types.Format(curr.type) + " " + curr.value.Format(style) + Environment.NewLine;
                else last = Types.Format(curr.type) + " " + curr.symbol.Format(style) + " = " + curr.value.Format(style) + Environment.NewLine;

                ////####Polynomize
                //if (curr.value is Flow flow) {
                //    last = last + "polynomial: " + Polynomial.ToPolynomial(flow, style).Format(style);
                //}

                s = last + s;
                next = curr.next;
            }
            return s + Environment.NewLine;
        }

        public Env ExtendValues<T>(List<Pattern> parameters, List<T> arguments, Netlist netlist, string source, Style style, int s) where T : Value {  // bounded polymorphism :)
            if (parameters.Count != arguments.Count) throw new Error("Different number of variables and values for '" + source + "'");
            Env env = this;
            for (int i = 0; i < parameters.Count; i++) {
                env = env.ExtendValue<T>(parameters[i], arguments[i], netlist, source, style, s + 1);
            }
            return env;
        }
        public Env ExtendValue<T>(List<Pattern> parameters, T argument, Netlist netlist, string source, Style style, int s) where T : Value {  // bounded polymorphism :)
            if (parameters.Count != 1) throw new Error("Different number of variables and values for '" + source + "'");
            return this.ExtendValue<T>(parameters[0], argument, netlist, source, style, s + 1);
        }
        public Env ExtendValue<T>(List<Pattern> parameters, T argument1, T argument2, Netlist netlist, string source, Style style, int s) where T : Value {  // bounded polymorphism :)
            if (parameters.Count != 2) throw new Error("Different number of variables and values for '" + source + "'");
            Env env = this.ExtendValue<T>(parameters[0], argument1, netlist, source, style, s + 1);
            env = env.ExtendValue<T>(parameters[1], argument2, netlist, source, style, s + 1);
            return env;
        }
        public Env ExtendValue<T>(Pattern pattern, T argument, Netlist netlist, string source, Style style, int s) where T : Value {  // bounded polymorphism :)
            Env env = this;
            if (pattern is SinglePattern) {
                SinglePattern parameter = pattern as SinglePattern;
                env = new ValueEnv(parameter.name, parameter.type, argument, netlist, env);
            } else if (pattern is ListPattern) {
                List<Pattern> subPatterns = (pattern as ListPattern).list.parameters;
                if (!(argument is ListValue<T>)) throw new Error("A list pattern is bound to a non-list value: '" + source + "'");
                List<T> subArguments = (argument as ListValue<T>).elements;
                env = env.ExtendValues(subPatterns, subArguments, netlist, source, style, s + 1);
            } else if (pattern is HeadConsPattern) {
                List<Pattern> headPatterns = (pattern as HeadConsPattern).list.parameters;
                Pattern singlePattern = (pattern as HeadConsPattern).single;
                if (!(argument is ListValue<T>)) throw new Error("A list pattern is bound to a non-list value: '" + source + "'");
                List<T> subArguments = (argument as ListValue<T>).elements;
                if (headPatterns.Count > subArguments.Count) throw new Error("In a list pattern variables exceed values: '" + source + "'");
                List<T> headArguments = subArguments.Take(headPatterns.Count).ToList();
                List<T> tailArguments = subArguments.Skip(headPatterns.Count).ToList();
                env = env.ExtendValues(headPatterns, headArguments, netlist, source, style, s + 1);
                env = env.ExtendValue(singlePattern, new ListValue<T>(tailArguments), netlist, source, style, s + 1);
            } else if (pattern is TailConsPattern) {
                Pattern singlePattern = (pattern as TailConsPattern).single;
                List<Pattern> tailPatterns = (pattern as TailConsPattern).list.parameters;
                if (!(argument is ListValue<T>)) throw new Error("A list pattern is bound to a non-list value: '" + source + "'");
                List<T> subArguments = (argument as ListValue<T>).elements;
                if (tailPatterns.Count > subArguments.Count) throw new Error("In a list pattern variables exceed values: '" + source + "'");
                List<T> headArguments = subArguments.Take(subArguments.Count - tailPatterns.Count).ToList();
                List<T> tailArguments = subArguments.Skip(subArguments.Count - tailPatterns.Count).ToList();
                env = env.ExtendValue(singlePattern, new ListValue<T>(headArguments), netlist, source, style, s + 1);
                env = env.ExtendValues(tailPatterns, tailArguments, netlist, source, style, s + 1);
            } else throw new Error("Pattern");
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
        public override void AssignValue(Symbol symbol, Value value, bool reassign = false) {
            throw new Error("UNDEFINED Assign of name: " + symbol.Format(new Style())); // this should be prevented by scoping analysis
        }
        public override string Format(Style style) {
            return "";
        }
        public override Scope ToScope() {
            return new NullScope();
        }
    }

    public class BuiltinEnv : Env {
        private static Dictionary<string, Value> builtin;
        private Env next;
        public BuiltinEnv(Env next) { // typically, next will be a NullEnv
            this.next = next;
            if (builtin == null) {
                builtin = new Dictionary<string, Value>();
                this.Populate();
            }
        }
        private void Add(string name, Value v) {
            builtin.Add(name, v);
        }
        private void AddOp(OperatorValue op) {
            builtin.Add(op.name, op);
        }
        private void Populate() {
            Add("maxNumber", new NumberValue(Double.MaxValue));
            Add("minNumber", new NumberValue(Double.MinValue));
            Add("positiveInfinity", new NumberValue(Double.PositiveInfinity));
            Add("negativeInfinity", new NumberValue(Double.NegativeInfinity));
            Add("NaN", new NumberValue(Double.NaN));
            Add("pi", new NumberValue(Math.PI));
            Add("e", new NumberValue(Math.E));
            AddOp(new Op_If());          // conditional pseudo-operator
            AddOp(new Op_Cond());      // flow-expression conditional pseudo-operator
            AddOp(new Op_ConditionOn());            // conditional distribution
            AddOp(new Op_Not());
            AddOp(new Op_Or());
            AddOp(new Op_And());
            AddOp(new Op_Plus());
            AddOp(new Op_Minus());            // both prefix and infix
            AddOp(new Op_Mult());
            AddOp(new Op_Div());
            AddOp(new Op_Conc());
            AddOp(new Op_Pow());
            AddOp(new Op_Eq());
            AddOp(new Op_Neq());
            AddOp(new Op_LessEq());
            AddOp(new Op_Less());
            AddOp(new Op_GreatEq());
            AddOp(new Op_Great());
            AddOp(new Op_Diff());
            //AddOp(new Op_DiffAlso());
            AddOp(new Op_Sdiff());
            AddOp(new Op_Abs());
            AddOp(new Op_Arccos());
            AddOp(new Op_Arcsin());
            AddOp(new Op_Arctan());
            AddOp(new Op_Arctan2());
            AddOp(new Op_Ceiling());
            AddOp(new Op_Cos());
            AddOp(new Op_Cosh());
            AddOp(new Op_Exp());
            AddOp(new Op_Floor());
            AddOp(new Op_Int());         // convert number to integer number by rounding
            AddOp(new Op_Log());
            AddOp(new Op_Max());
            AddOp(new Op_Min());
            AddOp(new Op_Pos());         // convert number to positive number by returning 0 if negative
            AddOp(new Op_Sign());
            AddOp(new Op_Sin());
            AddOp(new Op_Sinh());
            AddOp(new Op_Sqrt());
            AddOp(new Op_Tan());
            AddOp(new Op_Tanh());
            AddOp(new Op_Transpose());
            AddOp(new Op_Observe());       // evaluate flow expressions
            Add("time", new OpFlow("time", false));             // for flow expressions
            Add("kelvin", new OpFlow("kelvin", false));           // for flow expressions
            Add("celsius", new OpFlow("celsius", false));          // for flow expressions
            Add("volume", new OpFlow("volume", false));          // for flow expressions
            AddOp(new Op_Poisson());          // for flow expressions
            AddOp(new Op_Gauss());            // for flow expressions
            AddOp(new Op_Var());              // for flow expressions
            AddOp(new Op_Cov());              // for flow expressions
            AddOp(new Op_Argmin());
            AddOp(new Op_Uniform());
            AddOp(new Op_Normal());
            AddOp(new Op_Exponential());
            AddOp(new Op_Parabolic());
            AddOp(new Op_Bernoulli());
            AddOp(new Op_Basename());
            AddOp(new Op_Drawsample());
            AddOp(new Op_Map());
            AddOp(new Op_Filter());
            AddOp(new Op_Foldl());
            AddOp(new Op_Foldr());
            AddOp(new Op_Reverse());
            AddOp(new Op_Sort());
            AddOp(new Op_Each());
            AddOp(new Op_MassCompile());
            // primitive recursion over lists: https://www.cs.cmu.edu/~fp/courses/15317-f00/handouts/primrec.pdf
        }
        public override Value LookupValue(string name) {
            if (builtin.ContainsKey(name)) return builtin[name];
            else return next.LookupValue(name);
        }
        public override Symbol LookupSymbol(string name) {
            return next.LookupSymbol(name);
        }
        public override void AssignValue(Symbol symbol, Value value, bool reassign = false) {
            next.AssignValue(symbol, value, reassign);
        }
        public override string Format(Style style) {
            return next.Format(style);
        }
        public override Scope ToScope() {
            Scope scope = next.ToScope();
            foreach (var kvp in builtin) scope = new ConsScope(kvp.Key, scope);
            return scope;
        }
    }

    public class ValueEnv : Env {
        public Symbol symbol;
        public Value value;
        public Type type;
        public Env next;
        public ValueEnv(Symbol symbol, Type type, Value value, Env next, bool noCheck = false) {
            string Bad() { throw new Error("Binding var " + symbol.Raw() + " of type " + Types.Format(type) + " to value " + value.Format(new Style()) + " of type " + Types.Format(value.type)); }
            if (type == Type.Flow && !(value is Flow)) {
                Flow flow = value.ToFlow();
                if (flow == null) Bad();
                else value = flow;
            }
            this.symbol = symbol;
            this.type = type;
            this.value = value;
            this.next = next;
            if ((value != null) &&         // dont give type errors when creating null recursive environment stubs
                (!noCheck) &&              // noCheck means we know that we do not need to check the type of the value
                (!Types.Matches(type, value)))
                Bad();
        }
        public ValueEnv(string name, Type type, Value value, Env next, bool noCheck = false) : this(new Symbol(name), type, value, next, noCheck) {
        }
        public ValueEnv(Symbol symbol, Type type, Value value, Netlist netlist, Env next) : this(symbol, type, value, next) {
            if (netlist != null) netlist.Emit(new ValueEntry(this.symbol, type, value));   // also emit the new binding to netlist
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
        public override void AssignValue(Symbol symbol, Value value, bool reassign = false) {
            if (symbol.SameSymbol(this.symbol))
                if (this.value == null || reassign) this.value = value;
                else throw new Error("REASSIGNMENT of name: " + symbol.Format(new Style()) + ", old value: " + this.value.Format(new Style()) + ", new value: " + value.Format(new Style())); // this should be prevented by scoping analysis
            else next.AssignValue(symbol, value, reassign);
        }
        public override string Format(Style style) {
            string first = next.Format(style);
            string last = Types.Format(type) + " " + symbol.Format(style) + " = " + value.Format(style) + Environment.NewLine;
            return first + last;
        }
        public override Scope ToScope() {
            return new ConsScope(this.symbol.Raw(), this.next.ToScope());
        }

    }

}

