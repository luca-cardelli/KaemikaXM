using System;
using System.Collections.Generic;
using System.Linq;

namespace Kaemika {

    public abstract class Env {
        public const Env REJECT = null; // return REJECT=null from Statement evaluation to mean "sample is rejected" in random variable sampling (instead of an exception, trapping which is way too expensive)
        public abstract Symbol LookupSymbol(string name);
        public abstract Value LookupValue(string name);
        public abstract void AssignValue(Symbol symbol, Value value);
        public abstract string Format(Style style);

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
                builtIn = new ValueEnv("|", null, new OperatorValue("|"), builtIn);             // conditional distribution
                builtIn = new ValueEnv("not", null, new OperatorValue("not"), builtIn);
                builtIn = new ValueEnv("or", null, new OperatorValue("or"), builtIn);
                builtIn = new ValueEnv("and", null, new OperatorValue("and"), builtIn);
                builtIn = new ValueEnv("+", null, new OperatorValue("+"), builtIn);
                builtIn = new ValueEnv("-", null, new OperatorValue("-"), builtIn);           // both prefix and infix
                builtIn = new ValueEnv("*", null, new OperatorValue("*"), builtIn);
                builtIn = new ValueEnv("/", null, new OperatorValue("/"), builtIn);
                builtIn = new ValueEnv("++", null, new OperatorValue("++"), builtIn);
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
                builtIn = new ValueEnv("uniform", null, new OperatorValue("uniform"), builtIn);
                builtIn = new ValueEnv("normal", null, new OperatorValue("normal"), builtIn);
                builtIn = new ValueEnv("exponential", null, new OperatorValue("exponential"), builtIn);
                builtIn = new ValueEnv("parabolic", null, new OperatorValue("parabolic"), builtIn);
                builtIn = new ValueEnv("bernoulli", null, new OperatorValue("bernoulli"), builtIn);
                builtIn = new ValueEnv("<-", null, new OperatorValue("<-"), builtIn);
                builtIn = new ValueEnv("map", null, new OperatorValue("map"), builtIn);
                builtIn = new ValueEnv("filter", null, new OperatorValue("filter"), builtIn);
                builtIn = new ValueEnv("foldl", null, new OperatorValue("foldl"), builtIn);
                builtIn = new ValueEnv("foldr", null, new OperatorValue("foldr"), builtIn);
                builtIn = new ValueEnv("foreach", null, new OperatorValue("foreach"), builtIn);
                // primitive recursion over lists: https://www.cs.cmu.edu/~fp/courses/15317-f00/handouts/primrec.pdf
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

}
