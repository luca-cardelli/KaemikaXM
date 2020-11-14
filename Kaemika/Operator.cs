using System;
using System.Collections.Generic;

namespace Kaemika {

    public class OperatorValue : Value {
        public string name;
        public OperatorValue(string name) {
            this.name = name;
        }
        public override string Format(Style style) {
            return name;
        }
        protected Flow Bad(Value arg1, Style style) { throw new Error("Not acceptable: '" + name + "' with arguments: " + arg1.Format(style)); }
        protected Flow Bad(Value arg1, Value arg2, Style style) { throw new Error("Not acceptable: '" + name + "' with arguments: " + arg1.Format(style) + "," + arg2.Format(style)); }
        protected Flow Bad(Value arg1, Value arg2, Value arg3, Style style) { throw new Error("Not acceptable: '" + name + "' with arguments: " + arg1.Format(style) + "," + arg2.Format(style) + "," + arg3.Format(style)); }
        protected Flow Bad(Expression arg1, Expression arg2, Expression arg3) { throw new Error("Not acceptable: '" + name + "' with arguments: " + arg1.Format() + "," + arg2.Format() + "," + arg3.Format()); }
        protected Flow Bad(List<Value> argN, Style style) { throw new Error("Not acceptable: '" + name + "' with arguments: " + Style.FormatSequence(argN, ", ", x => x.Format(style))); }
        protected Flow Bad(List<Expression> argN, Style style) { throw new Error("Not acceptable: '" + name + "' with arguments: " + Style.FormatSequence(argN, ", ", x => x.Format())); }

        // ==== OPERATOR DISPATCHING ==== //

        public Env Execute(Expressions expressions, Env env, Netlist netlist, Style style, int s) {
            List<Expression> arguments = expressions.expressions;
            if (this is ExecutorBinary ex2) {
                if (arguments.Count != 2) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                Value arg2 = arguments[1].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT || arg2 == Value.REJECT) return Env.REJECT;
                ex2.Execute2(arg1, arg2, style);
                return env;
            } else if (this is ExecutorBinaryExt exExt2) {
                if (arguments.Count != 2) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                Value arg2 = arguments[1].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT || arg2 == Value.REJECT) return Env.REJECT;
                exExt2.Execute2(arg1, arg2, netlist, style, s);
                return env;
            } else throw new Error("Execute");
        }

        public Value Apply(Expressions expressions, bool infix, Env env, Netlist netlist, Style style, int s) {
            // arity/infix is assigned by the parser, and infix is supplied here from FunctionInstance->Apply
            // for infix operators known to the parser, arity = arguments.Count should be correct, but other function applications will not determine arity
            // the tricky case are "-" and observe, which are treated as Ennary operators, with arguments.Count = 1 or 2
            List<Expression> arguments = expressions.expressions;
            if (this is OperatorUnary op1) {
                if (arguments.Count != 1) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT) return Value.REJECT;
                return op1.Apply1(arg1, infix, style);
            } else if (this is OperatorBinary op2) {
                if (arguments.Count != 2) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                Value arg2 = arguments[1].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT || arg2 == Value.REJECT) return Value.REJECT;
                return op2.Apply2(arg1, arg2, infix, style);
            } else if (this is OperatorTernary op3) {
                if (arguments.Count != 3) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                Value arg2 = arguments[1].EvalReject(env, netlist, style, s + 1);
                Value arg3 = arguments[2].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT || arg2 == Value.REJECT || arg3 == Value.REJECT) return Value.REJECT;
                return op3.Apply3(arg1, arg2, arg3, infix, style);
            } else if (this is OperatorEnnary opN) {
                List<Value> argN = new List<Value>();
                for (int i = 0; i < arguments.Count; i++) {
                    Value argI = arguments[i].EvalReject(env, netlist, style, s + 1);
                    if (argI == Value.REJECT) return Value.REJECT;
                    argN.Add(argI);
                }
                return opN.ApplyN(argN, infix, style);
            } else if (this is OperatorUnaryExt opExt1) {
                if (arguments.Count != 1) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT) return Value.REJECT;
                return opExt1.Apply1(arg1, infix, env, netlist, style, s);
            } else if (this is OperatorBinaryExt opExt2) {
                if (arguments.Count != 2) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                Value arg2 = arguments[1].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT || arg2 == Value.REJECT) return Value.REJECT;
                return opExt2.Apply2(arg1, arg2, infix, env, netlist, style, s);
            } else if (this is OperatorTernaryExt opExt3) {
                if (arguments.Count != 3) Bad(arguments, style);
                Value arg1 = arguments[0].EvalReject(env, netlist, style, s + 1);
                Value arg2 = arguments[1].EvalReject(env, netlist, style, s + 1);
                Value arg3 = arguments[2].EvalReject(env, netlist, style, s + 1);
                if (arg1 == Value.REJECT || arg2 == Value.REJECT || arg3 == Value.REJECT) return Value.REJECT;
                return opExt3.Apply3(arg1, arg2, arg3, infix, env, netlist, style, s);
            } else if (this is OperatorEnnaryExt opExtN) {
                List<Value> argN = expressions.EvalReject(env, netlist, style, s + 1);
                return opExtN.ApplyN(argN, infix, env, netlist, style, s);
            } else if (this is OperatorPseudoTernary opPara3) {
                if (arguments.Count != 3) Bad(arguments, style);
                return opPara3.ApplyPseudo3(arguments[0], arguments[1], arguments[2], infix, env, netlist, style, s);
            } else throw new Error("Apply");
        }

        public Flow OpFlow1(Value arg1, bool infix, Style style) {
            Flow flow1 = arg1.ToFlow();
            if (flow1 != null) return new OpFlow(name, infix, flow1); else return Bad(arg1, style);
        }
        public Flow OpFlow2(Value arg1, Value arg2, bool infix, Style style) {
            Flow flow1 = arg1.ToFlow(); Flow flow2 = arg2.ToFlow();
            if (flow1 != null && flow2 != null) return new OpFlow(name, infix, flow1, flow2); else return Bad(arg1, arg2, style);
        }
        public Flow OpFlow3(Value arg1, Value arg2, Value arg3, bool infix, Style style) {
            Flow flow1 = arg1.ToFlow(); Flow flow2 = arg2.ToFlow(); Flow flow3 = arg3.ToFlow();
            if (flow1 != null && flow2 != null && flow3 != null) return new OpFlow(name, infix, flow1, flow2, flow3); else return Bad(arg1, arg2, arg3, style);
        }
        public Flow Coerce1(Value arg1, bool infix, Style style) {
            if (arg1 is Flow || arg1 is SpeciesValue) 
                return OpFlow1(arg1, infix, style);
            else return Bad(arg1, style); // we coerce to flows only if at least one of the arguments is a flow or species
        }
        public Flow Coerce2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is Flow || arg1 is SpeciesValue || arg2 is Flow || arg2 is SpeciesValue) 
                return OpFlow2(arg1, arg2, infix, style);
            else return Bad(arg1, arg2, style); // we coerce to flows only if at least one of the arguments is a flow or species
        }
        public Flow Coerce3(Value arg1, Value arg2, Value arg3, bool infix, Style style) {
            if (arg1 is Flow || arg1 is SpeciesValue || arg2 is Flow || arg2 is SpeciesValue || arg3 is Flow || arg3 is SpeciesValue) {
                return OpFlow3(arg1, arg2, arg3, infix, style);
            } else return Bad(arg1, arg2, arg3, style); // we coerce to flows only if at least one of the arguments is a flow or species
        }
    }

    // ==== EXECUTOR CLASSES ==== //

    public class NetworkOperatorValue : OperatorValue {
        public NetworkOperatorValue(string name) : base(name) { }
    }

    public class ExecutorBinary : NetworkOperatorValue {
        public ExecutorBinary(string name) : base(name) { }
        public virtual void Execute2(Value arg1, Value arg2, Style style) { throw new Error("OperatorBinary"); }
    }

    public class ExecutorBinaryExt : NetworkOperatorValue {
        public ExecutorBinaryExt(string name) : base(name) { }
        public virtual void Execute2(Value arg1, Value arg2, Netlist netlist, Style style, int s) { throw new Error("OperatorBinary"); }
    }

    // ==== OPERATOR CLASSES ==== //

    public class FunctionOperatorValue : OperatorValue {
        public FunctionOperatorValue(string name) : base(name) { }
    }

    public class OperatorUnary : FunctionOperatorValue {
        public OperatorUnary(string name) : base(name) { }
        public virtual Value Apply1(Value arg1, bool infix, Style style) { throw new Error("OperatorUnary"); }
    }

    public class OperatorBinary : FunctionOperatorValue {
        public OperatorBinary(string name) : base(name) { }
        public virtual Value Apply2(Value arg1, Value arg2, bool infix, Style style) { throw new Error("OperatorBinary"); }
    }

    public class OperatorTernary : FunctionOperatorValue {
        public OperatorTernary(string name) : base(name) { }
        public virtual Value Apply3(Value arg1, Value arg2, Value arg3, bool infix, Style style) { throw new Error("OperatorTernary"); }
    }

    public class OperatorEnnary : FunctionOperatorValue {
        public OperatorEnnary(string name) : base(name) { }
        public virtual Value ApplyN(List<Value> argN, bool infix, Style style) { throw new Error("OperatorEnnary"); }
    }

    public class OperatorUnaryExt : FunctionOperatorValue {
        public OperatorUnaryExt(string name) : base(name) { }
        public virtual Value Apply1(Value arg1, bool infix, Env env, Netlist netlist, Style style, int s) { throw new Error("OperatorUnaryExt"); }
    }

    public class OperatorBinaryExt : FunctionOperatorValue {
        public OperatorBinaryExt(string name) : base(name) { }
        public virtual Value Apply2(Value arg1, Value arg2, bool infix, Env env, Netlist netlist, Style style, int s) { throw new Error("OperatorBinaryExt"); }
    }

    public class OperatorTernaryExt : FunctionOperatorValue {
        public OperatorTernaryExt(string name) : base(name) { }
        public virtual Value Apply3(Value arg1, Value arg2, Value arg3, bool infix, Env env, Netlist netlist, Style style, int s) { throw new Error("OperatorTernaryExt"); }
    }

    public class OperatorEnnaryExt : FunctionOperatorValue {
        public OperatorEnnaryExt(string name) : base(name) { }
        public virtual Value ApplyN(List<Value> argN, bool infix, Env env, Netlist netlist, Style style, int s) { throw new Error("OperatorEnnaryExt"); }
    }

    public class OperatorPseudoTernary : FunctionOperatorValue {
        public OperatorPseudoTernary(string name) : base(name) { }
        public virtual Value ApplyPseudo3(Expression arg1, Expression arg2, Expression arg3, bool infix, Env env, Netlist netlist, Style style, int s) { throw new Error("OperatorPseudoTernary"); }
    }

    // ==== EXECUTORS ==== //

    public class Op_Each : ExecutorBinaryExt {
        public Op_Each() : base("each") { }
        public override void Execute2(Value arg1, Value arg2, Netlist netlist, Style style, int s) {
            if (arg1 is NetworkValue as1 && arg2 is ListValue<Value> as2) 
                ListValue<Value>.Each((Value e) => { Env extEnv = as1.ApplyReject(e, netlist, style, s); }, as2); else Bad(arg1, arg2, style);
        }
    }

    // ==== OPERATORS ==== //

    public class Op_Not : OperatorUnary {
        public Op_Not() : base("not") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is BoolValue as1) return new BoolValue(!as1.value); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Abs : OperatorUnary {
        public Op_Abs() : base("abs") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Abs(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Arccos : OperatorUnary {
        public Op_Arccos() : base("arccos") { }
        public override Value Apply1(Value arg1, bool infix, Style style) { 
            if (arg1 is NumberValue as1) return new NumberValue(Math.Acos(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Arcsin : OperatorUnary {
        public Op_Arcsin() : base("arcsin") { }
        public override Value Apply1(Value arg1, bool infix, Style style) { 
            if (arg1 is NumberValue as1) return new NumberValue(Math.Asin(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Arctan : OperatorUnary {
        public Op_Arctan() : base("arctan") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Atan(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Ceiling : OperatorUnary {
        public Op_Ceiling() : base("ceiling") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Ceiling(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Cos : OperatorUnary {
        public Op_Cos() : base("cos") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Cos(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Cosh : OperatorUnary {
        public Op_Cosh() : base("cosh") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Cosh(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Exp : OperatorUnary {
        public Op_Exp() : base("exp") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Exp(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Floor : OperatorUnary {
        public Op_Floor() : base("floor") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Floor(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Int : OperatorUnary {
        public Op_Int() : base("int") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) { double num1 = as1.value; return new NumberValue(Math.Round(num1)); } else return Coerce1(arg1, infix, style);          // convert number to integer number by rounding
        }
    }

    public class Op_Log : OperatorUnary {
        public Op_Log() : base("log") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Log(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Pos : OperatorUnary {
        public Op_Pos() : base("pos") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) { double num1 = as1.value; return new NumberValue((num1 > 0) ? num1 : 0); } else return Coerce1(arg1, infix, style);     // convert number to positive number by returning 0 if negative        }
        }
    }

    public class Op_Sign : OperatorUnary {
        public Op_Sign() : base("sign") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Sign(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Sin : OperatorUnary {
        public Op_Sin() : base("sin") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Sin(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Sinh : OperatorUnary {
        public Op_Sinh() : base("sinh") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Sinh(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Sqrt : OperatorUnary {
        public Op_Sqrt() : base("sqrt") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Sqrt(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Tan : OperatorUnary {
        public Op_Tan() : base("tan") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Tan(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Tanh : OperatorUnary {
        public Op_Tanh() : base("tanh") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return new NumberValue(Math.Tanh(as1.value)); else return Coerce1(arg1, infix, style);
        }
    }

    public class Op_Reverse : OperatorUnary {
        public Op_Reverse() : base("reverse") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is ListValue<Value> as1) return ListValue<Value>.Reverse(as1); else return Bad(arg1, style);
        }
    }

    public class Op_Transpose : OperatorUnary {
        public Op_Transpose() : base("transpose") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is ListValue<Value> as1) return ListValue<Value>.Transpose(as1); else return Bad(arg1, style);
        }
    }

    public class Op_Diff : OperatorUnary {
        public Op_Diff() : base("∂") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            return OpFlow1(arg1, infix, style);
        }
    }

    //public class Op_DiffAlso : OperatorUnary {
    //    public Op_DiffAlso() : base("diff") { }
    //    public override Value Apply1(Value arg1, bool infix, Style style) {
    //        return OpFlow1(arg1, infix, style);
    //    }
    //}

    public class Op_Sdiff : OperatorUnary { 
        public Op_Sdiff() : base("sdiff") { }
        public override Value Apply1(Value arg1, bool infix, Style style) { // a Flow will never contain sdiff: it is expanded out here
            Flow flow1 = arg1.ToFlow();
            if (flow1 != null) return flow1.Differentiate(null, style); else return Bad(arg1, style);
        }
    }

    public class Op_Var : OperatorUnary {
        public Op_Var() : base("var") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            return OpFlow1(arg1, infix, style);
        }
    }

    public class Op_Poisson : OperatorUnary {
        public Op_Poisson() : base("poisson") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            return OpFlow1(arg1, infix, style);
        }
    }

    public class Op_MassCompile : OperatorUnaryExt {
        public Op_MassCompile() : base("massaction") { }
        public override Value Apply1(Value arg1, bool infix, Env env, Netlist netlist, Style style, int s) {
            if (arg1 is SampleValue as1) return Hungarize.MassCompileSample(new Symbol(as1.symbol.Format(style)), as1, netlist, style); else return Bad(arg1, style);
        }
    }

    public class Op_Drawsample : OperatorUnary {
        public Op_Drawsample() : base("<-") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is DistributionValue as1) return as1.Draw(style); else return Bad(arg1, style);
        }
    }

    public class Op_Exponential : OperatorUnary {
        public Op_Exponential() : base("exponential") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return LoDistributionValue.Exponential(as1.value, style); else return Bad(arg1, style);
        }
    }

    public class Op_Bernoulli : OperatorUnary {
        public Op_Bernoulli() : base("bernoulli") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is NumberValue as1) return LoDistributionValue.Bernoulli(as1.value, style); else return Bad(arg1, style);
        }
    }

    public class Op_Basename : OperatorUnary {
        public Op_Basename() : base("basename") { }
        public override Value Apply1(Value arg1, bool infix, Style style) {
            if (arg1 is SpeciesValue as1) return new StringValue(as1.symbol.Raw()); else return Bad(arg1, style);
        }
    }

    public class Op_Or : OperatorBinary {
        public Op_Or() : base("or") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is BoolValue as1 && arg2 is BoolValue as2) return new BoolValue(as1.value || as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_And : OperatorBinary {
        public Op_And() : base("and") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is BoolValue as1 && arg2 is BoolValue as2) return new BoolValue(as1.value && as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Plus : OperatorBinary {
        public Op_Plus() : base("+") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is NumberValue asn1 && arg2 is NumberValue asn2) return new NumberValue(asn1.value + asn2.value);
            else if (arg1 is StringValue ass1 && arg2 is StringValue ass2) return new StringValue(ass1.value + ass2.value);
            else return Coerce2(arg1, arg2, infix, style); 
        }
    }

    public class Op_Mult : OperatorBinary {
        public Op_Mult() : base("*") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new NumberValue(as1.value * as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Div : OperatorBinary {
        public Op_Div() : base("/") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new NumberValue(as1.value / as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Conc : OperatorBinary {
        public Op_Conc() : base("++") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is ListValue<Value> as1 && arg2 is ListValue<Value> as2) return (as1.Append(as2)); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Pow : OperatorBinary {
        public Op_Pow() : base("^") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new NumberValue(Math.Pow(as1.value, as2.value)); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Arctan2 : OperatorBinary {
        public Op_Arctan2() : base("arctan2") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new NumberValue(Math.Atan2(as1.value, as2.value)); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Cov : OperatorBinary {
        public Op_Cov() : base("cov") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            return OpFlow2(arg1, arg2, infix, style);
        }
    }

    public class Op_Gauss : OperatorBinary {
        public Op_Gauss() : base("gauss") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            return OpFlow2(arg1, arg2, infix, style);
        }
    }

    public class Op_Eq : OperatorBinary {
        public Op_Eq() : base("=") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            bool eq = arg1.EqualValue(arg2, style, out bool hasFlows);
            if (hasFlows) return Coerce2(arg1, arg2, infix, style);
            else return new BoolValue(eq);
        }
    }

    public class Op_Neq : OperatorBinary {
        public Op_Neq() : base("<>") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            bool eq = arg1.EqualValue(arg2, style, out bool hasFlows);
            if (hasFlows) return Coerce2(arg1, arg2, infix, style);
            else return new BoolValue(!eq);
        }
    }

    public class Op_LessEq : OperatorBinary {
        public Op_LessEq() : base("<=") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new BoolValue(as1.value <= as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Less : OperatorBinary {
        public Op_Less() : base("<") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new BoolValue(as1.value < as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_GreatEq : OperatorBinary {
        public Op_GreatEq() : base(">=") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new BoolValue(as1.value >= as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Great : OperatorBinary {
        public Op_Great() : base(">") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new BoolValue(as1.value > as2.value); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Min : OperatorBinary {
        public Op_Min() : base("min") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new NumberValue(Math.Min(as1.value, as2.value)); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Max : OperatorBinary {
        public Op_Max() : base("max") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix,Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return new NumberValue(Math.Max(as1.value, as2.value)); else return Coerce2(arg1, arg2, infix, style);
        }
    }

    public class Op_Map : OperatorBinaryExt {
        public Op_Map() : base("map") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Env env, Netlist netlist, Style style, int s) {
            if (arg1 is FunctionValue as1 && arg2 is ListValue<Value> as2) return ListValue<Value>.Map((Value e) => as1.ApplyReject(e, netlist, style, s), as2); else return Bad(arg1, arg2, style);
        }
    }

    public class Op_Filter : OperatorBinaryExt {
        public Op_Filter() : base("filter") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Env env, Netlist netlist, Style style, int s) {
            if (arg1 is FunctionValue as1 && arg2 is ListValue<Value> as2) return ListValue<Value>.Filter((Value e) => as1.ApplyReject(e, netlist, style, s), as2); else return Bad(arg1, arg2, style);
        }
    }

    public class Op_Sort : OperatorBinaryExt {
        public Op_Sort() : base("sort") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Env env, Netlist netlist, Style style, int s) {
            if (arg1 is FunctionValue as1 && arg2 is ListValue<Value> as2) return ListValue<Value>.Sort((Value e1, Value e2) => as1.ApplyReject(e1, e2, netlist, style, s), as2, style); else return Bad(arg1, arg2, style);
        }
    }

    public class Op_Cond : OperatorTernary {
        public Op_Cond() : base("cond") { }
        public override Value Apply3(Value arg1, Value arg2, Value arg3, bool infix, Style style) {
            return Coerce3(arg1, arg2, arg3, infix, style);
        }
    }

    public class Op_ConditionOn : OperatorBinary {
        public Op_ConditionOn() : base("|") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is DistributionValue as1 && arg2 is DistributionValue as2) return as1.ConditionOn(as2, style); else return Bad(arg1, arg2, style);
        }
    }

    public class Op_Uniform : OperatorBinary {
        public Op_Uniform() : base("uniform") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return LoDistributionValue.Uniform(as1.value, as2.value, style); else return Bad(arg1, arg2, style);
        }
    }

    public class Op_Normal : OperatorBinary {
        public Op_Normal() : base("normal") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return LoDistributionValue.Normal(as1.value, as2.value, style); else return Bad(arg1, arg2, style);
        }
    }

    public class Op_Parabolic : OperatorBinary {
        public Op_Parabolic() : base("parabolic") { }
        public override Value Apply2(Value arg1, Value arg2, bool infix, Style style) {
            if (arg1 is NumberValue as1 && arg2 is NumberValue as2) return LoDistributionValue.Parabolic(as1.value, as2.value, style); else return Bad(arg1, arg2, style);
        }
    }

    public class Op_Argmin : OperatorTernaryExt {
        public Op_Argmin() : base("argmin") { }
        public override Value Apply3(Value arg1, Value arg2, Value arg3, bool infix, Env env, Netlist netlist, Style style, int s) {
            return Protocol.Argmin(arg1, arg2, arg3, netlist, style, s); // BFGF
        }
    }

    public class Op_Foldl : OperatorTernaryExt {
        public Op_Foldl() : base("foldl") { }
        public override Value Apply3(Value arg1, Value arg2, Value arg3, bool infix, Env env, Netlist netlist, Style style, int s) {
            if (arg1 is FunctionValue as1 && arg3 is ListValue<Value> as3) return ListValue<Value>.FoldL((Value e1, Value e2) => as1.ApplyReject(e1, e2, netlist, style, s), arg2, as3); else return Bad(arg1, arg2, arg3, style);
        }
    }

    public class Op_Foldr : OperatorTernaryExt {
        public Op_Foldr() : base("foldr") { }
        public override Value Apply3(Value arg1, Value arg2, Value arg3, bool infix, Env env, Netlist netlist, Style style, int s) {
            if (arg1 is FunctionValue as1 && arg3 is ListValue<Value> as3) return ListValue<Value>.FoldR((Value e1, Value e2) => as1.ApplyReject(e1, e2, netlist, style, s), arg2, as3); else return Bad(arg1, arg2, arg3, style);
        }
    }

    public class Op_Minus : OperatorEnnary { // both prefix and infix
        public Op_Minus() : base("-") { }
        public override Value ApplyN(List<Value> argN, bool infix, Style style) {
            if (argN.Count == 1) {
                if (argN[0] is NumberValue as1) return new NumberValue(-as1.value); else return Coerce1(argN[0], infix, style);
            } else if (argN.Count == 2) {
                if (argN[0] is NumberValue as1 && argN[1] is NumberValue as2) return new NumberValue(as1.value - as2.value); else return Coerce2(argN[0], argN[1], infix, style);
            } else return Bad(argN, style);
        }
    }

    public class Op_Observe : OperatorEnnaryExt { // both one and two arguments
        public Op_Observe() : base("observe") { }
        public override Value ApplyN(List<Value> argN, bool infix, Env env, Netlist netlist, Style style, int s) {
            Flow flow = (argN.Count >= 1) ? argN[0].ToFlow() : null;
            Value sample = (argN.Count == 1) ? env.LookupValue("vessel") : (argN.Count == 2) ? argN[1] : Bad(argN, style);
            if (flow != null && sample is SampleValue asSample)
                return asSample.Observe(flow, netlist, style);
            else return Bad(argN, style);
        }
    }

    public class Op_If : OperatorPseudoTernary { // not evaluating some arguments
        public Op_If() : base("if") { }
        public override Value ApplyPseudo3(Expression arg1, Expression arg2, Expression arg3, bool infix, Env env, Netlist netlist, Style style, int s) {
            Value cond = arg1.EvalReject(env, netlist, style, s + 1);
            if (cond == Value.REJECT) return Value.REJECT;
            if (cond is BoolValue asb) if (asb.value) return arg2.EvalReject(env, netlist, style, s + 1); else return arg3.EvalReject(env, netlist, style, s + 1); else return Bad(arg1, arg2, arg3);
        }
    }

}
