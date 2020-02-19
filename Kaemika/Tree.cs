using System;
using System.Collections.Generic;
using System.Linq;

namespace Kaemika {

    public enum ExportTarget : int { LBS, CRN, WolframNotebook, Standard };

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
                                                // "symbol", "header", "full", "operator"
        public ExportTarget exportTarget;       // How to format for external tools

        public bool traceComputational;         // Whether to format for TraceComputational or TraceChemical

        public bool chartOutput;                // Whether to produce a chart

        public Style(string varchar, SwapMap swap, AlphaMap map, string numberFormat, string dataFormat, ExportTarget exportTarget, bool traceComputational, bool chartOutput) {
            this.varchar = varchar;
            this.swap = swap;
            this.map = map;
            this.numberFormat = numberFormat;
            this.dataFormat = dataFormat;
            this.exportTarget = exportTarget;
            this.traceComputational = traceComputational;
            this.chartOutput = chartOutput;
        }
        public Style() : this(null, null, null, null, "full", ExportTarget.Standard, false, true) {
        }
        public static Style nil = new Style();
        public Style RestyleAsDataFormat(string dataFormat) {
            return new Style(this.varchar, this.swap, this.map, this.numberFormat, dataFormat, this.exportTarget, this.traceComputational, this.chartOutput);
        }
        public Style RestyleAsExportTarget(ExportTarget exportTarget) {
            return new Style(this.varchar, this.swap, this.map, this.numberFormat, this.dataFormat, exportTarget, this.traceComputational, this.chartOutput);
        }
        public Style RestyleAsNumberFormat(string numberFormat) {
            return new Style(this.varchar, this.swap, this.map, numberFormat, this.dataFormat, this.exportTarget, this.traceComputational, this.chartOutput);
        }
        public Style RestyleAsTraceComputational(bool traceComputational) {
            return new Style(this.varchar, this.swap, this.map, this.numberFormat, this.dataFormat, this.exportTarget, traceComputational, this.chartOutput);
        }
        public Style RestyleAsChartOutput(bool chartOutput) {
            return new Style(this.varchar, this.swap, this.map, this.numberFormat, this.dataFormat, this.exportTarget, this.traceComputational, chartOutput);
        }
        public string Varchar() { return this.varchar; }
        public SwapMap Swap() { return this.swap; }
        public AlphaMap Map() { return this.map; }

        public string FormatDouble(double n) { 
            if (this.numberFormat != null) return n.ToString(this.numberFormat); else return n.ToString(); 
        }

        // FormatSequence(elements, separator, formatItem, empty)
        // returns a string of the 'elements' formatted by 'formatElements', separated by 'separator'
        // if the elements are empty, returns 'empty'
        // if a 'formatElements' returns "", the separator is skipped for that element
        public delegate string FormatElementDelegate<T>(T ob);
        public delegate string FormatKeypairDelegate<T,U>(KeyValuePair<T,U> ob);
        public static string FormatSequence<T>(List<T> elements, string separator, FormatElementDelegate<T> FormatElement, string empty = "") {
            return elements.Aggregate(empty, (a, b) => { string bs = FormatElement(b); return (a == empty) ? bs : (bs == "") ? a : a + separator + bs; });
            //return elements.Aggregate(empty, (a, b) => (a == empty) ? FormatItem(b) : a + separator + FormatItem(b));
            //return (objects.Count == 0) ? empty : objects.Aggregate("", (a, b) => (a == "") ? FormatItem(b) : a + separator + FormatItem(b));
        }
        public static string FormatSequence<T>(List<T> elements, string separator, FormatElementDelegate<T> FormatElement, string empty, int maxNo) {
            string s = empty;
            for (int i = 0; i < elements.Count; i++) {
                if (i >= maxNo) { s = s + separator + ".."; break; }
                if (i == 0) s = FormatElement(elements[i]);
                else s = s + separator + FormatElement(elements[i]);
            }
            return s;
        }
        public static string FormatSequence<T,U>(SortedList<T,U> elements, string separator, FormatKeypairDelegate<T,U> FormatElement, string empty = "") {
            return elements.Aggregate(empty, (a, b) => { string bs = FormatElement(b); return (a == empty) ? bs : (bs == "") ? a : a + separator + bs; });
            //return elements.Aggregate(empty, (a, b) => (a == empty) ? FormatElement(b) : a + separator + FormatElement(b));
        }
        public static string FormatSequence<T>(T[] elements, string separator, FormatElementDelegate<T> FormatElement, string empty = "") {
            return elements.Aggregate(empty, (a, b) => { string bs = FormatElement(b); return (a == empty) ? bs : (bs == "") ? a : a + separator + bs; });
            //return elements.Aggregate(empty, (a, b) => (a == empty) ? FormatElement(b) : a + separator + FormatElement(b));
            //return (objects.Length == 0) ? empty : objects.Aggregate("", (a, b) => (a == "") ? FormatItem(b) : a + separator + FormatItem(b));
        }

    }

    // ABSTRACT SYNTAX TREES

    public abstract class Tree {
        protected void StackCheck(int stackLevel) { if (stackLevel > 600) throw new StackOverflowException("ERROR: Stack Overflow."); }
        public abstract string Format();
    }

    // EXPRESSION

    public abstract class Expression : Tree {
        public abstract void Scope(Scope scope);
        public abstract Value EvalReject(Env env, Netlist netlist, Style style, int s);
        public abstract Value EvalFlow(Env env, Style style, int s); // does the same as Eval but with restriction so needs no netlist. Used in building Flows, but it returns a Value not a Flow
        public abstract Flow BuildFlow(Env env, Style style, int s); // builds a Flow, may call EvalFlow to expand funtion invocations and if-then-else into Flows
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
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) {
            Value value = env.LookupValue(this.name);
            if (value is ConstantFlow) { throw new ConstantEvaluation((value as ConstantFlow).Format(style)); }
            return value;
        }
        public override Value EvalFlow(Env env, Style style, int s) {
            Value value = env.LookupValue(this.name);
            if (value is ConstantFlow) { throw new ConstantEvaluation((value as ConstantFlow).Format(style)); }
            return value;
        }
        public override Flow BuildFlow(Env env, Style style, int s) {
            Value value = env.LookupValue(this.name); // we must convert this Value into a Flow
            Flow flow = value.ToFlow();
            if (flow == null) throw new Error("Flow expression: Variable '" + this.Format() + "' should denote a flow");
            return flow;
        }
    }

    public class Constant : Expression { // useful only within flows
        public string name;
        public Constant(string name) { this.name = name; }
        public override string Format() { return this.name; }
        public override void Scope(Scope scope) { }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { throw new Error("Constants cannot be evaluated: " + Format()); }
        public override Value EvalFlow(Env env, Style style, int s) { throw new Error("Constants cannot be evaluated: " + Format()); }
        public override Flow BuildFlow(Env env, Style style, int s) { return new ConstantFlow(new Symbol(this.name)); }
    }

    public class BoolLiteral : Expression {
        public bool value;
        public BoolLiteral(bool value) { this.value = value; }
        public override string Format() { if (this.value) return "true"; else return "false"; }
        public override void Scope(Scope scope) { }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { return new BoolValue(this.value); }
        public override Value EvalFlow(Env env, Style style, int s) { return new BoolValue(this.value); }
        public override Flow BuildFlow(Env env, Style style, int s) { return new BoolFlow(this.value); }
    }

    public class NumberLiteral : Expression {
        public double value;
        public NumberLiteral(double value) { this.value = value; }
        public override string Format() { return this.value.ToString(); }
        public override void Scope(Scope scope) { }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { return new NumberValue(this.value); }
        public override Value EvalFlow(Env env, Style style, int s) { return new NumberValue(this.value); }
        public override Flow BuildFlow(Env env, Style style, int s) { return new NumberFlow(this.value); }
    }

    public class StringLiteral : Expression {
        public string value;
        public StringLiteral(string value) { this.value = value; }
        public override string Format() { return this.value; }
        public override void Scope(Scope scope) { }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { return new StringValue(this.value); }
        public override Value EvalFlow(Env env, Style style, int s) { return new StringValue(this.value); }
        public override Flow BuildFlow(Env env, Style style, int s) { return new StringFlow(this.value); }
    }

    public class ListLiteral : Expression {
        public Expressions elements;
        public ListLiteral(Expressions elements) { this.elements = elements; }
        public override string Format() { return "[" + elements.Format() + "]";}
        public override void Scope(Scope scope) {
            foreach (Expression element in elements.expressions) element.Scope(scope);
        }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Value> values = new List<Value>();
            foreach (Expression element in elements.expressions) {
                Value v = element.EvalReject(env, netlist, style, s + 1);
                if (v == Value.REJECT) return Value.REJECT;
                values.Add(v);
            }
            return new ListValue<Value>(values);
        }
        public override Value EvalFlow(Env env, Style style, int s) { StackCheck(s);
            List<Value> values = new List<Value>();
            foreach (Expression element in elements.expressions) values.Add(element.EvalFlow(env, style, s + 1));
            return new ListValue<Value>(values);
        }
        public override Flow BuildFlow(Env env, Style style, int s) {
            throw new Error("Flow expression: a list is not a flow: " + this.Format());
        }
    }

    public class ParameterInfo {
        public string parameter;
        public double drawn;
        public DistributionValue distribution;
        public Style style;
        public bool locked;
        public ParameterInfo(string parameter, double drawn, DistributionValue distribution, Style style) {
            this.parameter = parameter;
            this.drawn = drawn;
            this.distribution = distribution;
            this.style = style;
            this.locked = false;
        }
        public string ParameterLabel() {
            return parameter + " <- " + this.distribution.Format(style);
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
            return "λ(" + parameters.Format() + ") {" + body.Format() + "}";
        }
        public override void Scope(Scope scope) {
            body.Scope(scope.Extend(parameters.parameters));
        }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) {
            return new FunctionValue(null, parameters, body, env);
        }
        public override Value EvalFlow(Env env, Style style, int s) {
            return new FunctionValue(null, parameters, body, env);
        }
        public override Flow BuildFlow(Env env, Style style, int s) {
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
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) {
            return new NetworkValue(null, parameters, body, env);
        }
        public override Value EvalFlow(Env env, Style style, int s) {
            throw new Error("Flow expression: network abstractions is not a flow: " + this.Format());
        }
        public override Flow BuildFlow(Env env, Style style, int s) {
            throw new Error("Flow expression: network abstractions is not a flow: " + this.Format());
        }
    }

    public class RandomAbstraction : Expression {
        private string omegaName;
        private Expression body;
        public RandomAbstraction(string omegaName, Expression body) {
            this.omegaName = omegaName;
            this.body = body;
        }
        public override string Format() {
            return "rand (omega " + omegaName + ") {" + body.Format() + "}";
        }
        public override void Scope(Scope scope) {
            body.Scope(new ConsScope(omegaName, scope));
        }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            return new HiDistributionValue(null, null, 
                (OmegaValue omega, Style dynamicStyle) => {
                    Env closureEnv = new ValueEnv(omegaName, new Type("omega"), omega, env); // does not emit this bindings into the netlist
                    return body.EvalReject(closureEnv, netlist, dynamicStyle, s + 1);
                    }
            );
        }
        public override Value EvalFlow(Env env, Style style, int s) {
            throw new Error("Flow expression: random variable abstractions is not a flow: " + this.Format());
        }
        public override Flow BuildFlow(Env env, Style style, int s) {
            throw new Error("Flow expression: random variable abstractions is not a flow: " + this.Format());
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
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Env extEnv = this.statements.EvalReject(env, netlist, style, s + 1);
            if (extEnv == Env.REJECT) return Value.REJECT;
            return this.expression.EvalReject(extEnv, netlist, style, s+1);
        }
        public override Value EvalFlow(Env env, Style style, int s) {
            // this should never happen because BuildFlow of a BlockExpression will directly call BuildFlow of the value definition statements and of the final expression
            throw new Error("BlockExpression EvalFlow " + this.Format());
        }
        public override Flow BuildFlow(Env env, Style style, int s) { StackCheck(s);
            Env extEnv = env;
            foreach (Statement statement in statements.statements) {
                if (statement is ValueDefinition) extEnv = ((ValueDefinition)statement).BuildFlow(extEnv, style, s + 1);
                else throw new Error("Flow expression: function bodies can contain only value definitions (including flow definitions) and a final flow expression; functions with flow parameters to be invoked there can be defined externally: " + Format());
            }
            return this.expression.BuildFlow(extEnv, style, s + 1);
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
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value value = this.function.EvalReject(env, netlist, style, s+1);
            if (value == Value.REJECT) return Value.REJECT;
            if (value is FunctionValue) {
                FunctionValue closure = (FunctionValue)value;
                List<Value> arguments = this.arguments.EvalReject(env, netlist, style, s+1);
                if (arguments == Expressions.REJECT) return Value.REJECT;
                string invocation = "";
                if (false) { //### style.traceComputational) {
                    Style restyle = style.RestyleAsDataFormat("symbol");
                    invocation = closure.Format(restyle) + "(" + Style.FormatSequence(arguments, ", ", x => x.Format(restyle)) + ")";
                    netlist.Emit(new CommentEntry("BEGIN " + invocation));
                }
                Value result = closure.ApplyReject(arguments, netlist, style, s);
                if (result == Value.REJECT) return Value.REJECT;
                if (false) { //### style.traceComputational) 
                    netlist.Emit(new CommentEntry("END " + invocation));
                }
                return result;
            } else if (value is OperatorValue) {
                OperatorValue oper = (OperatorValue)value;
                if (oper.name == "if") { // it was surely parsed with 3 arguments
                    List<Expression> actuals = this.arguments.expressions;
                    Value cond = actuals[0].EvalReject(env, netlist, style, s+1);
                    if (cond == Value.REJECT) return Value.REJECT;
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].EvalReject(env, netlist, style, s+1); else return actuals[2].EvalReject(env, netlist, style, s+1);
                    else throw new Error("'if' predicate should be a bool: " + Format());
                } else if (oper.name == "observe") {
                    List<Expression> actuals = this.arguments.expressions;
                    if (actuals.Count != 1 && actuals.Count != 2) throw new Error("'observe' wrong number of arguments " + Format()); ;
                    Flow flow = actuals[0].BuildFlow(env, style, s + 1);
                    Value sample = env.LookupValue("vessel");
                    if (actuals.Count == 2) sample = actuals[1].EvalReject(env, netlist, style, s + 1);
                    if (sample == Value.REJECT) return Value.REJECT;
                    if (!(sample is SampleValue)) throw new Error("'observe' second argument should be a sample: " + Format());
                    return (sample as SampleValue).Observe(flow, netlist, style);
                } else {
                    List<Value> arguments = this.arguments.EvalReject(env, netlist, style, s+1);
                    if (arguments == Expressions.REJECT) return Value.REJECT;
                    return oper.Apply(arguments, netlist, style, s);
                }
            } else if (value is ListValue<Value>) {
                ListValue<Value> list = (ListValue<Value>)value;
                List<Value> arguments = this.arguments.EvalReject(env, netlist, style, s + 1);
                if (arguments == Expressions.REJECT) return Value.REJECT;
                if (arguments.Count == 0) return new NumberValue(list.elements.Count);
                else if (arguments.Count == 1) return list.Select(arguments[0], style);
                else if (arguments.Count == 2) return list.Sublist(arguments[0], arguments[1], style);
                else throw new Error("Wrong number of arguments to list selection: " + Format());
            } else if (value is OmegaValue) {
                OmegaValue omega = value as OmegaValue;
                List<Value> arguments = this.arguments.EvalReject(env, netlist, style, s + 1);
                if (arguments == Expressions.REJECT) return Value.REJECT;
                //if (arguments.Count == 0) return new NumberValue(omega.Access(new OmegaDimension())); else
                if (arguments.Count == 1) {
                    if (arguments[0] is NumberValue) {
                        return new NumberValue(omega.Access((int)(arguments[0] as NumberValue).value));  // omega contains the right subsamplespace to access
                    } else throw new Error("Bad arguments to omega: " + Format());
                } else throw new Error("Wrong number of arguments to omega: " + Format());
            } else if (value is LoDistributionValue) {
                LoDistributionValue dist = value as LoDistributionValue;
                List<Value> arguments = this.arguments.EvalReject(env, netlist, style, s + 1);
                if (arguments == Expressions.REJECT) return Value.REJECT;
                if (arguments.Count == 1) {
                    if (arguments[0] is OmegaValue omega) return dist.Generate(omega, style);    // will generate w.r.t the subsamplespace of dist
                    else throw new Error("Wrong argument to random variable: " + Format());
                } else throw new Error("Wrong number of arguments to random variable: " + Format());
            } else if (value is HiDistributionValue) {
                HiDistributionValue dist = value as HiDistributionValue;
                List<Value> arguments = this.arguments.EvalReject(env, netlist, style, s + 1);
                if (arguments == Expressions.REJECT) return Value.REJECT;
                if (arguments.Count == 1) {
                    if (arguments[0] is OmegaValue omega) return dist.Generate(omega, style);   // will generate w.r.t the subsamplespace of dist
                    else throw new Error("Wrong argument to random variable: " + Format());
                } else throw new Error("Wrong number of arguments to random variable: " + Format());
            } else throw new Error("Invocation of a non-function, non-list, or non-operator: " + Format());
        }
        public override Value EvalFlow(Env env, Style style, int s) { StackCheck(s);
            Value value = this.function.EvalFlow(env, style, s + 1);
            if (value is FunctionValue) {
                FunctionValue closure = (FunctionValue)value;
                List<Value> arguments = this.arguments.EvalFlow(env, style, s + 1);
                return closure.ApplyFlow(arguments, style, s);
            } else if (value is OperatorValue) {
                OperatorValue oper = (OperatorValue)value;
                if (oper.name == "if") { // it was surely parsed with 3 arguments
                    List<Expression> actuals = this.arguments.expressions;
                    Value cond = actuals[0].EvalFlow(env, style, s + 1);
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].EvalFlow(env, style, s + 1); else return actuals[2].EvalFlow(env, style, s + 1);
                    else throw new Error("Flow expression: 'if' predicate should be a bool: " + Format());
                } else {
                    List<Value> arguments = this.arguments.EvalFlow(env, style, s + 1);
                    return oper.ApplyFlow(arguments, style);
                }
            } else if (value is ListValue<Value>) {
                ListValue<Value> list = (ListValue<Value>)value;
                List<Value> arguments = this.arguments.EvalFlow(env, style, s + 1);
                if (arguments.Count == 1) return list.Select(arguments[0], style);
                else if (arguments.Count == 2) return list.Sublist(arguments[0], arguments[1], style);
                else throw new Error("Flow expression: Wrong number of parameters to list selection: " + Format());
            } else throw new Error("Flow expression: Invocation of a non-function, non-list, or non-operator: " + Format());
        }
        public override Flow BuildFlow(Env env, Style style, int s) { StackCheck(s);
            Value value = this.function.EvalFlow(env, style, s + 1);
            if (value is FunctionValue) {
                FunctionValue closure = (FunctionValue)value;
                List<Flow> arguments = this.arguments.BuildFlow(env, style, s + 1); 
                return closure.BuildFlow(arguments, style, s + 1);
            } else if (value is OperatorValue) {
                OperatorValue oper = (OperatorValue)value;
                if (oper.name == "if") { // it was surely parsed with 3 arguments
                    List<Expression> actuals = this.arguments.expressions;
                    Value cond = actuals[0].EvalFlow(env, style, s + 1); // this is a real boolean value, not a flow
                    if (cond is BoolValue) if (((BoolValue)cond).value) return actuals[1].BuildFlow(env, style, s + 1); else return actuals[2].BuildFlow(env, style, s + 1);
                    else throw new Error("Flow expression: 'if' predicate should be a bool: " + Format());
                } else {
                    List<Flow> arguments = this.arguments.BuildFlow(env, style, s + 1); // operator arguments are Flows that are composed with the operator
                    return oper.BuildFlow(arguments, style);
                }
            } else if (value is ListValue<Value>) {
                ListValue<Value> list = (ListValue<Value>)value;
                List<Value> arguments = this.arguments.EvalFlow(env, style, s + 1);
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
            return Style.FormatSequence(this.statements, Environment.NewLine, x => (x == null) ? "<null statement>" : x.Format());
            //string str = "";
            //foreach (Statement stat in this.statements) {
            //    string s = (stat == null) ? "<null statement>" : stat.Format();
            //    if (str == "") str = s;
            //    else str = str + Environment.NewLine + s;
            //}
            //return str;
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
        public Env EvalReject(Env env, Netlist netlist, Style style, int s) { // more complex version of Eval for recursive statement blocks, mutually recursive definitions must be contiguous
            return EvalInc(this.statements, 0, env, netlist, style, s);
        }
        public Env EvalInc(List<Statement> statements, int i, Env env, Netlist netlist, Style style, int s) { StackCheck(s); // incremental Eval
            if (i >= statements.Count) return env;
            Statement statement = statements[i];
            if (statement is FunctionDefinition || statement is NetworkDefinition)
                return EvalRec(statements, i, env, netlist, style, s); // switch to recursive eval
            else {
                Env incEnv = statement.EvalReject(env, netlist, style, s);
                if (incEnv == Env.REJECT) return Env.REJECT;
                return EvalInc(statements, i + 1, incEnv, netlist, style, s);
            }
        }
        public Env EvalRec(List<Statement> statements, int i, Env env, Netlist netlist, Style style, int s) { StackCheck(s); // recursive Eval
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
                    if (s == 0) netlist.Emit(new FunctionEntry(symbol, value));     // insert the closures in the netlist
                }
                if (statement is NetworkDefinition) {
                    Symbol symbol = recEnv.LookupSymbol(((NetworkDefinition)statement).Name());
                    NetworkValue value = ((NetworkDefinition)statement).NetworkClosure(symbol, recEnv);
                    recEnv.AssignValue(symbol, value);
                    if (s == 0) netlist.Emit(new NetworkEntry(symbol, value));     // insert the closures in the netlist
                }
                j = j + 1;
            }
            return EvalInc(statements, j, recEnv, netlist, style, s); // switch to incremental eval
        }
    }

    // STATEMENT

    public abstract class Statement : Tree {
        public abstract Scope Scope(Scope scope);
        public abstract Env EvalReject(Env env, Netlist netlist, Style style, int s);
    }

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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Symbol symbol = new Symbol(this.name);
            Value value = (type.Is("flow")) ? definee.BuildFlow(env, style, s + 1) : definee.EvalReject(env, netlist, style, s+1);     // evaluate
            if (value == Value.REJECT) return Env.REJECT;
            if (value is DistributionValue valueAs) valueAs.BindSymbol(symbol);
            return new ValueEnv(symbol, type, value, s==0 ? netlist : null, env);  // make new symbol, check that types match, emit also in the netlist, return extended env
        }
        public Env BuildFlow(Env env, Style style, int s) {   // special case: only value definitions among all statements support BuildFlow
            Flow flow = definee.BuildFlow(env, style, s + 1);                              // evaluate
            return new ValueEnv(this.name, new Type("flow"), flow, env);                   // checks that the ("flow") types match
        }
    }

    public class PatternDefinition : Statement {
        private Pattern pattern;
        private Expression definee;
        public PatternDefinition(Pattern pattern, Expression definee) {
            this.pattern = pattern;
            this.definee = definee;
        }
        public override string Format() {
            return pattern.Format() + " = " + definee.Format();
        }
        public override Scope Scope(Scope scope) {
            definee.Scope(scope);
            return scope.Extend(pattern);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value value = definee.EvalReject(env, netlist, style, s);
            if (value == Value.REJECT) return Env.REJECT;
            return env.ExtendValue<Value>(pattern, value, s == 0 ? netlist : null, pattern.Format(), style, s);
        }
        public Env BuildFlow(Env env, Style style) {   // special case: only value definitions among all statements support BuildFlow
            throw new Error("Flow expression: a list definition is not a flow definition: " + this.Format()); // would have to support ListFlow first
        }
    }

    public class ParameterDefinition : Statement {
        private string name;
        public Type type;
        private Expression definee;
        private Type numberType;
        public ParameterDefinition(string name, Type type, Expression definee) {
            this.name = name;
            this.type = type;
            this.numberType = new Type("number");
            this.definee = definee;
        }
        public override string Format() {
            return "parameter " + numberType.Format() + " " + name + " <- " + definee.Format();
        }
        public override Scope Scope(Scope scope) {
            definee.Scope(scope);
            return new ConsScope(this.name, scope);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Symbol symbol = new Symbol(this.name); 
            Value value = definee.EvalReject(env, netlist, style, s + 1);
            if (value == Value.REJECT) return Env.REJECT;
            if (value is DistributionValue distribution) {
                NumberValue drawn;
                double oracle = KControls.ParameterOracle(symbol.Format(style)); // returns NaN unless the value has been locked in the Gui
                if (!double.IsNaN(oracle)) drawn = new NumberValue(oracle);
                else {
                    Value v = distribution.Draw(style);
                    if (v is NumberValue vAs) drawn = vAs;
                    else throw new Error("A parameter must be drawn from a numerical random variable: " + this.Format());
                }
                Env extEnv = new ValueEnv(symbol, numberType, drawn, env);
                netlist.Emit(new ParameterEntry(symbol, numberType, drawn, distribution));
                return extEnv;
            }
            else throw new Error("A parameter must be drawn from a random variable: " + this.Format());
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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value volume = this.volume.EvalReject(env, netlist, style, s+1);
            if (volume == Value.REJECT) return Env.REJECT;
            Value temperature = this.temperature.EvalReject(env, netlist, style, s+1);
            if (temperature == Value.REJECT) return Env.REJECT;
            if ((!(volume is NumberValue)) || (!(temperature is NumberValue))) throw new Error("Bad arg types to sample " + this.name);
            double volumeValue = Protocol.NormalizeVolume(((NumberValue)volume).value, this.volumeUnit);
            double temperatureValue = Protocol.NormalizeTemperature(((NumberValue)temperature).value, this.temperatureUnit);
            if (volumeValue <= 0) throw new Error("Sample volume must be positive: " + this.name);
            if (temperatureValue < 0) throw new Error("Sample temperature must be non-negative: " + this.name);
            Symbol symbol = new Symbol(name);
            SampleValue sample = Protocol.Sample(symbol, volumeValue, temperatureValue);
            KDeviceHandler.Sample(sample, style);
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
            string s = "new species" + "{" + Style.FormatSequence(substances, ", ", x => x.Format()) + "}";
            if (statements.Count() > 0) s += " " + statements.Format();
            return s;
        }
        public override Scope Scope(Scope scope) {
            Scope extScope = scope;
            foreach (Substance substance in substances) {
                if (substance is SubstanceMolarmass asMolarmass) asMolarmass.Scope(scope);
                extScope = new ConsScope(substance.name, extScope);
            }
            statements.Scope(extScope);
            return extScope;
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Env extEnv = env;
            foreach (Substance substance in this.substances) {
                Symbol symbol = new Symbol(substance.name);                         // create a new symbol from name
                double molarmass;
                if (substance is SubstanceMolarmass asMolarmass) {
                    Value molarmassValue = asMolarmass.EvalReject(env, netlist, style, s+1);    // eval molarmass, returns a bool for absence of molarmass
                    if (molarmassValue == Value.REJECT) return Env.REJECT;
                    if (molarmassValue is NumberValue asNumber) molarmass = asNumber.value;
                    else throw new Error("Molar mass must be a number, for species: " + substance.name);
                    if (molarmass <= 0) throw new Error("Molar mass must be positive, for species: " + substance.name);
                } else { // substance is SubstanceConcentration
                    molarmass = -1.0; // molarmass not specified
                }
                SpeciesValue species = new SpeciesValue(symbol, molarmass);         // use the new symbol for the uninitialized species value
                extEnv = new ValueEnv(symbol, null, species, extEnv);               // extend environment
                netlist.Emit(new SpeciesEntry(species));                            // put the species in the netlist (its initial value goes into a sample)
            }
            Env ignoreEnv = this.statements.EvalReject(extEnv, netlist, style, s + 1);          // eval the statements in the new environment
            if (ignoreEnv == Env.REJECT) return Env.REJECT;
            return extEnv;                                                         // return the environment with the new species definitions (only)
        }
    }

    public abstract class Substance {
        public string name;
        public abstract string Format();
    }
    public class SubstanceConcentration : Substance {
        public SubstanceConcentration(string name) {
            this.name = name;
        }
        public override string Format() {
            return name;
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
        public void Scope(Scope scope) {
            molarmass.Scope(scope);
        }
        public Value EvalReject(Env env, Netlist netlist, Style style, int s) {
            return molarmass.EvalReject(env, netlist, style, s+1);
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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { // this and the related Emit are probably never executed because of the separate handing of recursive environments
            Symbol symbol = new Symbol(this.name);                              // create a new symbol from name
            FunctionValue value = this.FunctionClosure(symbol, env);
            Env extEnv = new ValueEnv(symbol, null, value, env);            // checks that the types match
            if (s == 0) netlist.Emit(new FunctionEntry(symbol, value));                     // embed the new symbol also in the netlist
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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { // this and the related Emit are probably never executed because of the separate handing of recursive environments
            Symbol symbol = new Symbol(this.name);                              // create a new symbol from name
            NetworkValue value = this.NetworkClosure(symbol, env);
            Env extEnv = new ValueEnv(symbol, null, value, env);            // checks that the types match
            if (s == 0) netlist.Emit(new NetworkEntry(symbol, value));                      // embed the new symbol also in the netlist
            return extEnv;                                                      // return the extended environment
        }
    }

    public class RandomDefinition : Statement {
        private string name;
        private string omegaName;
        private Expression body;
        public RandomDefinition(string name, string omegaName, Expression body) {
            this.name = name;
            this.omegaName = omegaName;
            this.body = body;
        }
        public string Name() { return this.name; }
        public override string Format() {
            return "new random " + name + "(omega " + omegaName + ") {" + body.Format() + "}";
        }
        public override Scope Scope(Scope scope) {
            body.Scope(new ConsScope(this.omegaName, scope));
            return new ConsScope(this.name, scope);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Symbol symbol = new Symbol(this.name);    
            Symbol omegaSymbol = new Symbol(this.omegaName);
            DistributionValue value = new HiDistributionValue(symbol, null, 
                (OmegaValue omega, Style dynamicStyle) => {
                    Env closureEnv = new ValueEnv(omegaName, new Type("omega"), omega, env); // does not emit this bindings into the netlist
                    return body.EvalReject(closureEnv, netlist, dynamicStyle, s + 1);
                });
            Env extEnv = new ValueEnv(symbol, new Type("random"), value, env); 
            if (s == 0) netlist.Emit(new RandomEntry(symbol, value)); 
            return extEnv;
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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value cond = this.ifExpr.EvalReject(env, netlist, style, s+1);
            if (cond == Value.REJECT) return Env.REJECT;
            if (cond is BoolValue) {
                if (((BoolValue)cond).value) { 
                    Env ignoreEnv = this.thenStatements.EvalReject(env, netlist, style, s + 1);
                    if (ignoreEnv == Env.REJECT) return Env.REJECT;
                } else { 
                    Env ignoreEnv = this.elseStatements.EvalReject(env, netlist, style, s + 1);
                    if (ignoreEnv == Env.REJECT) return Env.REJECT;
                }
                return env;
            } else throw new Error("Bad predicate type to 'if'");
        }
    }

    public class NetworkInstance : Statement {
        private Expression network;
        private Expressions arguments;
        public NetworkInstance(Expression network, Expressions arguments) {
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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value value = this.network.EvalReject(env, netlist, style, s+1);
            if (value == Value.REJECT) return Env.REJECT;
            List<Value> arguments = this.arguments.EvalReject(env, netlist, style, s + 1);
            if (arguments == Expressions.REJECT) return Env.REJECT;
            if (value is NetworkValue) {
                NetworkValue closure = (NetworkValue)value;
                string invocation = "";
                if (false) { //### (style.traceComputational) {
                    Style restyle = style.RestyleAsDataFormat("symbol");
                    invocation = closure.Format(restyle) + "(" + Style.FormatSequence(arguments, ", ", x => x.Format(restyle)) + ")";
                    netlist.Emit(new CommentEntry("BEGIN " + invocation));
                }
                Env ignoreEnv = closure.ApplyReject(arguments, netlist, style, s);
                if (ignoreEnv == Env.REJECT) return Env.REJECT;
                if (false) { //### style.traceComputational) {
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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Symbol> reactants = this.reactants.EvalReject(env, netlist, style, s + 1);
            if (reactants == Complex.REJECT) return Env.REJECT;
            List<Symbol> products = this.products.EvalReject(env, netlist, style, s + 1);
            if (products == Complex.REJECT) return Env.REJECT;
            RateValue rate; try { 
                rate = this.rate.EvalReject(env, netlist, style, s + 1);
                if (rate == RateValue.REJECT) return Env.REJECT;
            } catch (ConstantEvaluation e) { rate = ConvertToGeneralRate(e.Message, reactants, env, netlist, style, s); }
            ReactionValue reaction = new ReactionValue(reactants, products, rate);
            netlist.Emit(new ReactionEntry(reaction));
            return env;
        }
        // in case we attempt to use a constant inside {...} we try to convert it to a flow with mass action kinetics, as if it had appeared inside {{...}}
        private RateValue ConvertToGeneralRate(string msg, List<Symbol> reactants, Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            if (!(rate is MassActionRate)) throw new Error("ConvertToGeneralRate");
            MassActionRate rateExpr = rate as MassActionRate;
            string err = "Cannot evaluate a constant '" + msg + "' inside a mass action rate {...}; try using general reaction rates {{...}}";
            if (!(rateExpr.activationEnergy is NumberLiteral && (rateExpr.activationEnergy as NumberLiteral).value == 0.0)) throw new Error(err);
            RateValue rateValue = new GeneralRate(rateExpr.collisionFrequency).EvalReject(env, netlist, style, s + 1); // try evaluate the rate as a flow // does not REJECT beacause it is a flow
            Flow rateFunction = (rateValue as GeneralRateValue).rateFunction; // now build up the mass action kinetics
            if (!rateFunction.IsNumericConstantExpression()) throw new Error(err); // make sure this is only a combination of constants and numbers, not e.g. species
            foreach (Symbol reactant in reactants) { rateFunction = OpFlow.Op("*", rateFunction, new SpeciesFlow(reactant)); }
            return new GeneralRateValue(rateFunction);
        }
    }

    public abstract class Rate {
        protected void StackCheck(int stackLevel) { if (stackLevel > 600) throw new StackOverflowException("ERROR: Stack Overflow."); }
        public abstract string Format();
        public abstract void Scope(Scope scope);
        public abstract RateValue EvalReject(Env env, Netlist netlist, Style style, int s);
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
        public override RateValue EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);  // never REJECT
            Flow flow = rateFunction.BuildFlow(env, style, s + 1);  // whether this is a numeric flow is checked later
            if (!flow.HasDeterministicValue()) throw new Error("This flow-expression cannot appear in {{ ... }} rate: " + rateFunction.Format());
            return new GeneralRateValue(flow); 
        }
    }

    public class MassActionRate : Rate {
        public Expression collisionFrequency;
        public Expression activationEnergy;
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
        public override RateValue EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value cf = collisionFrequency.EvalReject(env, netlist, style, s+1);
            if (cf == Value.REJECT) return RateValue.REJECT;
            Value ae = activationEnergy.EvalReject(env, netlist, style, s+1);
            if (ae == Value.REJECT) return RateValue.REJECT;
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
        public Amount(Ids names, Expression initial, string dimension, Expression sample) {
            this.vars = new List<Variable> { };
            foreach (string name in names.ids) this.vars.Add(new Variable(name));
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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value initialValue = this.initial.EvalReject(env, netlist, style, s+1);
            if (initialValue == Value.REJECT) return Env.REJECT;
            if (!(initialValue is NumberValue)) throw new Error("Amount " + this.FormatVars() + " requires a number value for concentration");
            Value sampleValue = this.sample.EvalReject(env, netlist, style, s + 1);
            if (sampleValue == Value.REJECT) return Env.REJECT;
            if (!(sampleValue is SampleValue)) throw new Error("Amount " + this.FormatVars() + " requires a sample value");
            foreach (Variable var in this.vars) {
                Value speciesValue = var.EvalReject(env, netlist, style, s + 1);
                if (speciesValue == Value.REJECT) return Env.REJECT;
                if (!(speciesValue is SpeciesValue)) throw new Error("Amount " + this.FormatVars() + "has a non-species in the list of variables");
                Protocol.Amount((SampleValue)sampleValue, (SpeciesValue)speciesValue, (NumberValue)initialValue, this.dimension, style);
                KDeviceHandler.Amount((SampleValue)sampleValue, (SpeciesValue)speciesValue, (NumberValue)initialValue, this.dimension, style);
                netlist.Emit(new AmountEntry((SpeciesValue)speciesValue, (NumberValue)initialValue, this.dimension, (SampleValue)sampleValue));
            }
            return env;
        }
    }
       
    public class Mix : Statement {
        private string name;
        private Expressions expressions;
        public Mix(string name, Expressions expressions) {
            this.name = name;
            this.expressions = expressions;
        }
        public override string Format() {       
            return "mix " + name + " = " + expressions.Format();
        }
        public override Scope Scope(Scope scope) {
            expressions.Scope(scope);
            return new ConsScope(name, scope);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Value> values = expressions.EvalReject(env, netlist, style, s+1); // allow single parameter that is a list of samples to mix:
            if (values == Expressions.REJECT) return Env.REJECT;
            if (values.Count == 1 && values[0] is ListValue<Value>) values = (values[0] as ListValue<Value>).elements;
            List<SampleValue> samples = new List<SampleValue> { };
            foreach (Value value in values) {
                if (!(value is SampleValue)) throw new Error("mix '" + name + "' requires samples to mix");
                samples.Add((SampleValue)value);
            }
            if (samples.Count < 2) throw new Error("mix '" + name + "' requires at least two samples to mix or a list of them");
            Symbol symbol = new Symbol(name);
            SampleValue sample = Protocol.Mix(symbol, samples, netlist, style);
            KDeviceHandler.Mix(sample, samples, style);
            netlist.Emit(new MixEntry(sample, samples));
            return new ValueEnv(symbol, null, sample, env);
        }
    }
      
    public class Split : Statement {
        private IdSeq names;
        private Expression from;
        private Expressions proportions; // length zero for splitting in equal parts
        public Split(IdSeq names, Expression from, Expressions proportions) {
            this.names = names;
            this.from = from;
            this.proportions = proportions;
        }
        public override string Format() {       
            return "split " + names.Format() + " = " + from.Format() + ((proportions.expressions.Count == 0) ? "" : " by " + proportions.Format());
        }
        public override Scope Scope(Scope scope) {
            from.Scope(scope);
            proportions.Scope(scope);
            return names.Scope(scope);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value fromValue = this.from.EvalReject(env, netlist, style, s+1);
            if (fromValue == Value.REJECT) return Env.REJECT;
            if (!(fromValue is SampleValue)) throw new Error("split '" + names.Format() + "' requires a sample");
            SampleValue fromSample = (SampleValue)fromValue;
            List<Value> proportionValues = this.proportions.EvalReject(env, netlist, style, s+1);
            if (proportionValues == Expressions.REJECT) return Env.REJECT;
            List<NumberValue> proportionNumbers = new List<NumberValue> { };

            double sum = 0.0;
            foreach (Value proportionValue in proportionValues) {
                if (!(proportionValue is NumberValue)) throw new Error("split '" + names.Format() + "' requires numbers as proportions");
                NumberValue numberValue = (NumberValue)proportionValue;
                if ((numberValue.value <= 0) || (numberValue.value >= 1)) throw new Error("split '" + names.Format() + "' requires numbers strictly between 0 and 1 as proportions: " + numberValue.Format(style));
                proportionNumbers.Add(numberValue);
                sum += numberValue.value;
            }

            List<Symbol> symbols = new List<Symbol> { };
            foreach (string name in names.ids) symbols.Add(new Symbol(name));
            if (symbols.Count < 2) throw new Error("split '" + names.Format() + "' requires to split into at least two samples");
            if (symbols.Count != proportionNumbers.Count) {
                if (proportionNumbers.Count == 0) {
                    double equalProportion = 1.0 / symbols.Count;
                    foreach (Symbol symbol in symbols) proportionNumbers.Add(new NumberValue(equalProportion));
                    sum = 1.0;
                } else if (symbols.Count == 1 + proportionNumbers.Count) {
                    if (sum >= 1.0) throw new Error("split '" + names.Format() + "' proportions exceed 1");
                    proportionNumbers.Add(new NumberValue(1.0 - sum));
                    sum = 1.0;
                } else throw new Error("Split '" + names.Format() + "' different number of ids and proportions");
            }
            if (sum != 1.0) throw new Error("split '" + names.Format() + "' proportions do not sum up to 1: " + sum.ToString());

            List<SampleValue> samples = Protocol.Split(symbols, fromSample, proportionNumbers, netlist, style);
            KDeviceHandler.Split(samples, fromSample, style);
            netlist.Emit(new SplitEntry(samples, fromSample, proportionNumbers));
            Env extEnv = env;
            for (int i = symbols.Count - 1; i >= 0; i--)
                extEnv = new ValueEnv(symbols[i], null, samples[i], extEnv);
            return extEnv;
        }
    }
 
    public class Dispose : Statement {
        private Expressions samples;
        public Dispose(Expressions samples) {
            this.samples = samples;
        }
        public override string Format() {       
            return "dispose " + samples.Format();
        }
        public override Scope Scope(Scope scope) {
            samples.Scope(scope);
            return scope;
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Value> values = this.samples.EvalReject(env, netlist, style, s + 1); // allow single parameter that is a list of samples to dispose:
            if (values == Expressions.REJECT) return Env.REJECT;
            if (values.Count == 1 && values[0] is ListValue<Value>) values = (values[0] as ListValue<Value>).elements;
            List<SampleValue> dispSamples = new List<SampleValue> { };
            foreach (Value value in values) {
                if (!(value is SampleValue)) throw new Error("dispose requires sample");
                dispSamples.Add((SampleValue)value);
            }
            Protocol.Dispose(dispSamples, netlist, style);
            KDeviceHandler.Dispose(dispSamples, style);
            netlist.Emit(new DisposeEntry(dispSamples));
            return env;
        }
    }
   
    public class Equilibrate : Statement {
        private IdSeq names;
        private Expressions samples;
        private EndCondition endcondition;
        public Equilibrate(IdSeq names, Expressions samples, EndCondition endcondition) {
            this.names = names;
            this.samples = samples;
            this.endcondition = endcondition;
        }
        public override string Format() {       
            return "equilibrate" +  " " + names.Format() + " = " + samples.Format() + endcondition.Format();
        }
        public override Scope Scope(Scope scope) {
            samples.Scope(scope);
            endcondition.Scope(scope);
            return names.Scope(scope);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Value> inSampleValues = this.samples.EvalReject(env, netlist, style, s + 1); // allow single parameter that is a list of samples to equilibrate:
            if (inSampleValues == Expressions.REJECT) return Env.REJECT;
            if (inSampleValues.Count == 1 && inSampleValues[0] is ListValue<Value>) inSampleValues = (inSampleValues[0] as ListValue<Value>).elements;
            List<SampleValue> inSamples = new List<SampleValue> { };
            foreach (Value inSampleValue in inSampleValues) {
                if (!(inSampleValue is SampleValue)) throw new Error("equilibrate '" + names.Format() + "' requires samples");
                inSamples.Add((SampleValue)inSampleValue);
            }
            Noise noise = KControls.SelectNoiseSelectedItem;
            Value forTimeValue = endcondition.fortime.EvalReject(env, netlist, style, s+1);
            if (forTimeValue == Value.REJECT) return Env.REJECT;
            if (!(forTimeValue is NumberValue)) throw new Error("equilibrate '" + names.Format() + "' requires a number for duration");
            double forTime = ((NumberValue)forTimeValue).value;
            if (forTime < 0) throw new Error("equilibrate '" + names.Format() + "' requires a nonnegative number for duration");

            List<Symbol> outSymbols = new List<Symbol> { };
            foreach (string name in names.ids) outSymbols.Add(new Symbol(name));
            if (outSymbols.Count != inSamples.Count) throw new Error("equilibrate '" + names.Format() + "' different number of ids and samples");

            if (endcondition is EndConditionSimple) {
                Protocol.PauseEquilibrate(netlist, style); // Gui pause between successive equilibrate, if enabled
                List<KDeviceHandler.Place> goBacks = KDeviceHandler.StartEquilibrate(inSamples, forTime, style); // can be null
                List<SampleValue> outSamples = Protocol.EquilibrateList(outSymbols, inSamples, noise, forTime, netlist, style);
                if (goBacks != null) KDeviceHandler.EndEquilibrate(goBacks, outSamples, inSamples, forTime, style); 
                netlist.Emit(new EquilibrateEntry(outSamples, inSamples, forTime));
                Env extEnv = env;
                for (int i = outSymbols.Count - 1; i >= 0; i--)
                    extEnv = new ValueEnv(outSymbols[i], null, outSamples[i], extEnv);
                return extEnv;
            } else throw new Error("Equilibrate");
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

    public class Regulate : Statement {
        private IdSeq names;
        private Expressions expressions;
        private Expression temperature;
        private string temperatureUnit;
        public Regulate(IdSeq names, Expressions samples, Expression temperature, string temperatureUnit) {
            this.names = names;
            this.expressions = samples;
            this.temperature = temperature;
            this.temperatureUnit = temperatureUnit;
        }
        public override string Format() {
            return "regulate " + names.Format() + " = " + expressions.Format() + " to " + temperature.Format() + temperatureUnit;
        }
        public override Scope Scope(Scope scope) {
            expressions.Scope(scope);
            temperature.Scope(scope);
            return names.Scope(scope);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Value> values = expressions.EvalReject(env, netlist, style, s + 1); // allow single parameter that is a list of samples to regulate:
            if (values == Expressions.REJECT) return Env.REJECT;
            if (values.Count == 1 && values[0] is ListValue<Value>) values = (values[0] as ListValue<Value>).elements;
            List<SampleValue> inSamples = new List<SampleValue> { };
            foreach (Value value in values) {
                if (!(value is SampleValue)) throw new Error("regulate '" + names.Format() + "' requires samples to regulate");
                inSamples.Add((SampleValue)value);
            }
            Value temperature = this.temperature.EvalReject(env, netlist, style, s+1);
            if (temperature == Value.REJECT) return Env.REJECT;
            if (!(temperature is NumberValue)) throw new Error("Bad temperature to regulate '" + names.Format() + "'");
            double temperatureValue = Protocol.NormalizeTemperature(((NumberValue)temperature).value, this.temperatureUnit);
            if (temperatureValue < 0) throw new Error("temperature for 'regulate " + names.Format() + "' must be non-negative");

            List<Symbol> outSymbols = new List<Symbol> { };
            foreach (string name in names.ids) outSymbols.Add(new Symbol(name));
            if (outSymbols.Count != inSamples.Count) throw new Error("regulate '" + names.Format() + "' different number of ids and samples");

            List<SampleValue> outSamples = Protocol.Regulate(outSymbols, temperatureValue, inSamples, netlist, style);
            KDeviceHandler.Regulate(outSamples, inSamples, style);
            netlist.Emit(new RegulateEntry(outSamples, inSamples, temperatureValue));
            Env extEnv = env;
            for (int i = outSymbols.Count - 1; i >= 0; i--)
                extEnv = new ValueEnv(outSymbols[i], null, outSamples[i], extEnv);
            return extEnv;
        }
    }

    public class Concentrate : Statement {
        private IdSeq names;
        private Expressions expressions;
        private Expression volume;
        private string volumeUnit;
        public Concentrate(IdSeq names, Expressions samples, Expression volume, string volumeUnit) {
            this.names = names;
            this.expressions = samples;
            this.volume = volume;
            this.volumeUnit = volumeUnit;
        }
        public override string Format() {
            return "concentrate " + names.Format() + " = " + expressions.Format() + " to " + volume.Format() + volumeUnit;
        }
        public override Scope Scope(Scope scope) {
            expressions.Scope(scope);
            volume.Scope(scope);
            return names.Scope(scope);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Value> values = expressions.EvalReject(env, netlist, style, s + 1); // allow single parameter that is a list of samples to regulate:
            if (values == Expressions.REJECT) return Env.REJECT;
            if (values.Count == 1 && values[0] is ListValue<Value>) values = (values[0] as ListValue<Value>).elements;
            List<SampleValue> inSamples = new List<SampleValue> { };
            foreach (Value value in values) {
                if (!(value is SampleValue)) throw new Error("concentrate '" + names.Format() + "' requires samples to concentrate");
                inSamples.Add((SampleValue)value);
            }
            Value volume = this.volume.EvalReject(env, netlist, style, s+1);
            if (volume == Value.REJECT) return Env.REJECT;
            if (!(volume is NumberValue)) throw new Error("Bad volume to concentrate '" + names.Format() + "'");
            double volumeValue = Protocol.NormalizeVolume(((NumberValue)volume).value, this.volumeUnit);
            if (volumeValue <= 0) throw new Error("volume for 'concentrate " + names.Format() + "' must be positve");

            List<Symbol> outSymbols = new List<Symbol> { };
            foreach (string name in names.ids) outSymbols.Add(new Symbol(name));
            if (outSymbols.Count != inSamples.Count) throw new Error("regulate '" + names.Format() + "' different number of ids and samples");

            List<SampleValue> outSamples = Protocol.Concentrate(outSymbols, volumeValue, inSamples, netlist, style);
            KDeviceHandler.Concentrate(outSamples, inSamples, style);
            netlist.Emit(new ConcentrateEntry(outSamples, inSamples, volumeValue));
            Env extEnv = env;
            for (int i = outSymbols.Count - 1; i >= 0; i--)
                extEnv = new ValueEnv(outSymbols[i], null, outSamples[i], extEnv);
            return extEnv;
        }
    }

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
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            string asLabel = null;
            if (this.asExpr != null) {
                Value value = this.asExpr.EvalFlow(env, style, s + 1);
                if (value is StringValue) asLabel = ((StringValue)value).value; // the raw string contents, unquoted
                else asLabel = value.Format(style);
            }
            netlist.Emit(new ReportEntry(expression.BuildFlow(env, style, s + 1), asLabel));
            return env;
        }
    }
    
    public class DrawFromStatement : Statement {
        public Expression several; 
        public Expression from; 
        public DrawFromStatement(Expression several, Expression from) {
            this.several = several;
            this.from = from;
        }
        public override string Format() {
            return "draw " + this.several.Format() + " from " + this.from.Format();
        }
        public override Scope Scope(Scope scope) {
            this.several.Scope(scope);
            this.from.Scope(scope);
            return scope;
        }
        public static (List<DistributionValue> manyRand, List<FunctionValue> manyFun, bool single) ExtractLists(Value from, Func<string> format) {
            List<DistributionValue> manyRand = new List<DistributionValue>();
            List<FunctionValue> manyFun = new List<FunctionValue>();
            bool single = true;
            if (from is DistributionValue singleRand) {
                manyRand.Add(singleRand);
            } else if (from is FunctionValue singleFun) {
                manyFun.Add(singleFun);
            } else if (from is ListValue<Value> list) {
                single = false;
                foreach (var item in list.elements) {
                    if (item is DistributionValue oneRand) {
                        manyRand.Add(oneRand);
                        if (manyFun.Count != 0) throw new Error("draw: uniform list of random variables expected: " + format());
                    } else if (item is FunctionValue oneFun) {
                        manyFun.Add(oneFun);
                        if (manyRand.Count != 0) throw new Error("draw: uniform list of functions expected: " + format());
                    } else throw new Error("draw: functions or random variables expected: " + format());
                }
            } else throw new Error("draw: functions or random variables expected: " + format());
            return (manyRand, manyFun, single);
        }
        public override Env EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value several = this.several.EvalReject(env, netlist, style, s + 1);
            if (several == Value.REJECT) return Env.REJECT;
            Value from = this.from.EvalReject(env, netlist, style, s + 1);  
            if (from == Value.REJECT) return Env.REJECT;
            int count = (several is NumberValue severalAs) ? (int)severalAs.value : throw new Error("draw: number expected: " + this.Format());
            (List<DistributionValue> manyRand, List<FunctionValue> manyFun, bool single) = ExtractLists(from, () => { return this.Format(); });
            if (manyRand.Count != 0) DistributionValue.DensityPlot(count, manyRand, style);
            if (manyFun.Count != 0) FunctionValue.Plot(count, manyFun, netlist, style, s); //### CAN REJECT
            return env;
        }
    }

    public class DrawFromExpression : Expression {
        public Expression several; 
        public Expression from; 
        public DrawFromExpression(Expression several, Expression from) {
            this.several = several;
            this.from = from;
        }
        public override string Format() {
            return "draw " + this.several.Format() + " from " + this.from.Format();
        }
        public override void Scope(Scope scope) {
            this.several.Scope(scope);
            this.from.Scope(scope);
        }
        public override Value EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            Value several = this.several.EvalReject(env, netlist, style, s + 1);
            if (several == Value.REJECT) return Value.REJECT;
            Value from = this.from.EvalReject(env, netlist, style, s + 1);
            if (from == Value.REJECT) return Value.REJECT;
            int count = (several is NumberValue severalAs) ? (int)severalAs.value : throw new Error("draw: number expected: " + this.Format());
            (List<DistributionValue> manyRand, List<FunctionValue> manyFun, bool single) = DrawFromStatement.ExtractLists(from, () => { return this.Format(); });
            if (manyRand.Count != 0) return DistributionValue.Enumerate(count, manyRand, single, style);
            if (manyFun.Count != 0) return FunctionValue.Enumerate(count, manyFun, single, netlist, style, s);
            return new ListValue<Value>(new List<Value>());
        }
        public override Value EvalFlow(Env env, Style style, int s) {
            throw new Error("Cannot be a flow: " + Format());
        }
        public override Flow BuildFlow(Env env, Style style, int s) {
            throw new Error("Cannot be a flow: " + Format());
        }
    }


    // COMPLEX

    public abstract class Complex : Tree {
        public const List<Symbol> REJECT = null; // return REJECT=null from complex evaluation to mean "sample is rejected" in random variable sampling (instead of an exception, trapping which is way too expensive)
        public abstract void Scope(Scope scope);
        public abstract List<Symbol> EvalReject(Env env, Netlist netlist, Style style, int s);
    }

    public class Simplex : Complex {
        private Expression stoichiometry; // may be null if stoichiometry is 1
        private Variable species; // may be null is stoichiometry is 0 OR if species is #
        public Simplex(Expression stoichiometry, Variable species) {
            this.stoichiometry = stoichiometry;
            this.species = species;
        }
        public override string Format() {
            if (stoichiometry == null && species == null) return "Ø"; // '#'
            if (stoichiometry != null && species == null) return stoichiometry.Format() + " * Ø"; // " * #"
            if (stoichiometry == null && species != null) return species.Format();
            if (stoichiometry != null && species != null) return stoichiometry.Format() + " * " + species.Format();
            return "";
        }
        public override void Scope(Scope scope) {
            if (stoichiometry != null) stoichiometry.Scope(scope);
            if (species != null) species.Scope(scope);
        }
        public override List<Symbol> EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Symbol> list = new List<Symbol> { };
            int count = 1;
            Value stoichValue = null;
            if (stoichiometry != null) {
                Value v = stoichiometry.EvalReject(env, netlist, style, s + 1);
                if (v == Value.REJECT) return Complex.REJECT;
                stoichValue = v;
            }
            if (stoichValue != null) {
                if (!(stoichValue is NumberValue))
                    throw new Error("Stoichiometry value '" + stoichiometry.Format() + "' must denote number, not: " + stoichValue.Format(style));
                else if ((stoichValue as NumberValue).value % 1 != 0) // not an integer
                    throw new Error("Stoichiometry value '" + stoichiometry.Format() + "' must denote integer, not: " + stoichValue.Format(style));
                else count = (int)((stoichValue as NumberValue).value);
            }
            if (species != null) { 
                Value speciesValue = species.EvalReject(env, netlist, style, s+1);
                if (speciesValue == Value.REJECT) return Complex.REJECT;
                if (!(speciesValue is SpeciesValue))
                    throw new Error("Species variable '" + species.Format() + "' must denote species, not: " + speciesValue.Format(style));
                for (int i = 1; i <= count; i++) {
                    Value v = species.EvalReject(env, netlist, style, s + 1);
                    if (v == Value.REJECT) return Complex.REJECT;
                    list.Add(((SpeciesValue)v).symbol); 
                }
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
        public override List<Symbol> EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Symbol> list1 = complex1.EvalReject(env, netlist, style, s + 1);
            if (list1 == Complex.REJECT) return Complex.REJECT;
            List<Symbol> list2 = complex2.EvalReject(env, netlist, style, s + 1);
            if (list2 == Complex.REJECT) return Complex.REJECT;
            list1.AddRange(list2);
            return list1;
        }
    }

    // PARAMETERS

    public class Ids : Tree { // list of ids not separated by commas
        public List<string> ids;
        public Ids() {
            this.ids = new List<string> { };
        }
        public Ids(List<string> ids) {
            this.ids = ids;
        }
        public Ids Add(string id) {
            this.ids.Add(id);
            return this;
        }
        public override string Format() {
            return Style.FormatSequence(this.ids, " ", x => x);
        }
        private Scope ScopeRev(int i, List<string> list, Scope scope) {
            if (i >= list.Count) return scope; else return new ConsScope(list[i], ScopeRev(i + 1, list, scope));
        }
        public Scope Scope(Scope scope) {
            return ScopeRev(0, ids, scope);
        }
    }

    public class IdSeq : Tree { // list of ids separated by commas
        public List<string> ids;
        public IdSeq() {
            this.ids = new List<string> { };
        }
        public IdSeq(List<string> ids) {
            this.ids = ids;
        }
        public IdSeq Add(string id) {
            this.ids.Add(id);
            return this;
        }
        public override string Format() {
            return Style.FormatSequence(this.ids, ", ", x => x);
        }
        private Scope ScopeRev(int i, List<string> list, Scope scope) {
            if (i >= list.Count) return scope; else return new ConsScope(list[i], ScopeRev(i + 1, list, scope));
        }
        public Scope Scope(Scope scope) {
            return ScopeRev(0, ids, scope);
        }
    }

    public class Parameters : Tree {
        public List<Pattern> parameters;
        public Parameters() {
            this.parameters = new List<Pattern> { };
        }
        public Parameters Add(Pattern param) {
            this.parameters.Add(param);
            return this;
        }
        public override string Format() {
            return Style.FormatSequence(this.parameters, ", ", x => x.Format());
        }
    }
    public abstract class Pattern : Tree {
    }
    public class SinglePattern : Pattern {
        public Type type;
        public string name;
        public SinglePattern(Type type, string id) {
            this.type = type;
            this.name = id;
        }
        public override string Format() {
            return this.type.Format() + " " + this.name;
        }
    }
    public class ListPattern : Pattern {
        public Parameters list;
        public ListPattern(Parameters list) {
            this.list = list;
        }
        public override string Format() {
            return "[" + this.list.Format() + "]";
        }
    }
    public class HeadConsPattern : Pattern {
        public Parameters list;          // binding the fixed size part of the list
        public SinglePattern single;   // binding the rest of the list, of type list
        public HeadConsPattern(Parameters list, SinglePattern single) {
            this.list = list;
            this.single = single;
        }
        public override string Format() {
            return "[" + this.list.Format() + "] + " + this.single.Format();
        }
    }
    public class TailConsPattern : Pattern {
        public SinglePattern single;   // binding the rest of the list, of type list
        public Parameters list;          // binding the fixed size part of the list
        public TailConsPattern(SinglePattern single, Parameters list) {
            this.single = single;
            this.list = list;
        }
        public override string Format() {
            return this.single.Format() + " + [" + this.list.Format() + "]";
        }
    }


    // ARGUMENTS

    public class Expressions : Tree {
        public const List<Value> REJECT = null; // return REJECT=null from arguments evaluation to mean "sample is rejected" in random variable sampling (instead of an exception, trapping which is way too expensive)
        public List<Expression> expressions;
        public Expressions() {
            this.expressions = new List<Expression> { };
        }
        public Expressions Add(Expression expression) {
            this.expressions.Add(expression);
            return this;
        }
        public override string Format() {
            return Style.FormatSequence(this.expressions, ", ", x => x.Format());
        }
        public void Scope(Scope scope) {
            foreach (Expression expression in this.expressions) { expression.Scope(scope); }
        }
        public List<Value> EvalReject(Env env, Netlist netlist, Style style, int s) { StackCheck(s);
            List<Value> expressions = new List<Value>();
            foreach (Expression expression in this.expressions) {
                Value v = expression.EvalReject(env, netlist, style, s + 1);
                if (v == Value.REJECT) return Expressions.REJECT;
                expressions.Add(v); 
            }
            return expressions;
        }
        public List<Value> EvalFlow(Env env, Style style, int s) { StackCheck(s);
            List<Value> expressions = new List<Value>();
            foreach (Expression expression in this.expressions) { expressions.Add(expression.EvalFlow(env, style, s + 1)); }
            return expressions;
        }
        public List<Flow> BuildFlow(Env env, Style style, int s) { StackCheck(s);
            List<Flow> expressions = new List<Flow>();
            foreach (Expression expression in this.expressions) { expressions.Add(expression.BuildFlow(env, style, s + 1)); }
            return expressions;
        }
    }

}

