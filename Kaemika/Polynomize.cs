using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    public abstract class Polynomize {

        // ===== ODEs =====

        public class ODE {
            public SpeciesFlow var;
            public Flow flow;
            public ODE(SpeciesFlow var, Flow flow) {
                this.var = var;
                this.flow = flow;
            }
            public string Format(Style style) {
                return "∂" + var.Format(style) + " = " + flow.Normalize(style).TopFormat(style);
            }
        }

        public static (Lst<ODE> odes, Lst<Equation> eqs) FromCRN(CRN crn) {
            (SpeciesValue[] vars, Flow[] flows) = crn.MeanFlow();
            Lst<ODE> odes = new Nil<ODE>();
            for (int i = vars.Length-1; i >= 0; i--) {
                odes = new Cons<ODE>(new ODE(new SpeciesFlow(vars[i].symbol), flows[i]), odes);
            }
            List<SpeciesValue> species = crn.sample.stateMap.species;
            Lst<Equation> eqs = new Nil<Equation>();
            for (int i = species.Count - 1; i >= 0; i--) {
                eqs = new Cons<Equation>(new Equation(new SpeciesFlow(species[i].symbol), "id", new Lst<Monomial>[1] { Monomial.Singleton(new Monomial(crn.sample.stateMap.state.Mean(i))) }), eqs);
            }
            return (odes, eqs);
        }

        public static Polynomial Lookup(SpeciesFlow var, Lst<PolyODE>odes, Style style) {
            if (odes is Cons<PolyODE> cons) {
                if (cons.head.var.species.SameSymbol(var.species)) return cons.head.poly;
                else return Lookup(var, cons.tail, style);
            } else throw new Error("ODE Lookup not found: " + var.Format(style));
        }

        public static Equation Lookup(SpeciesFlow var, Lst<Equation>eqs, Style style) {
            if (eqs is Cons<Equation> cons) {
                if (cons.head.var.species.SameSymbol(var.species)) return cons.head;
                else return Lookup(var, cons.tail, style);
            } else throw new Error("Equation Lookup not found: " + var.Format(style));
        }

        public static string Format(Lst<ODE> odes, Style style) {
            if (odes is Cons<ODE> cons) {
                return cons.head.Format(style) + Environment.NewLine + Format(cons.tail, style);
            } else return "";
        }

        // ===== PolyODEs =====

        public enum Split { No, Pos, Neg };

        public class PolyODE {
            public SpeciesFlow var;
            public Polynomial poly;
            public Split split; // whether this ODE was split by Positivize
            public PolyODE(SpeciesFlow var, Polynomial poly, Split split) {
                this.var = var;
                this.poly = poly;
                this.split = split;
            }
            public PolyODE(SpeciesFlow var, Lst<Monomial> monomials, Split split) {
                this.var = var;
                this.poly = new Polynomial(monomials);
                this.split = split;
            }
            public PolyODE(SpeciesFlow var, Monomial monomial, Split split) {
                this.var = var;
                this.poly = new Polynomial(Monomial.Singleton(monomial));
                this.split = split;
            }
            public bool Hungarian(SpeciesFlow differentiationVariable, Style style) {
                return this.poly.Hungarian(differentiationVariable, style);
            }
            public string Format(Style style) {
                return "∂" + var.Format(style) + " = " + poly.Format(style);
            }
            public static string Format(Lst<PolyODE> odes, Style style) {
                if (odes is Cons<PolyODE> cons) {
                    return cons.head.Format(style) + Environment.NewLine + Format(cons.tail, style);
                } else return "";
            }
            public static Lst<PolyODE> nil = new Nil<PolyODE>();
            public static Lst<SpeciesFlow> Variables(Lst<PolyODE> odes) {
                return odes.Map<SpeciesFlow>(ode => { return ode.var; });
            }
        }

        public static Lst<PolyODE> FromODEs(Lst<ODE> odes, Style style) {
            if (odes is Cons<ODE> cons) {
                return new Cons<PolyODE>(new PolyODE(cons.head.var, Polynomial.ToPolynomial(cons.head.flow, style), Split.No), FromODEs(cons.tail, style));
            } else return PolyODE.nil;
        }

        // ===== Equations =====

        public class Equation {
            public SpeciesFlow var;
            public string op;            // may be a non-polynomial operator
            public Lst<Monomial>[] args; // but its arguments are polynomial
            public Split splitOp;        // op modifier used later by Positivize: can be "No" (= op(args)) "Pos" (= pos(op(args))) or "Neg" (= pos(-op(args)))
            public Equation(SpeciesFlow var, string op, Lst<Monomial>[] args, Split splitOp = Split.No) {
                this.var = var;
                this.op = op;
                this.args = args;
                this.splitOp = splitOp;
            }
            public string Format(Style style) {
                string s = "";
                if (op == "id") 
                    s += Monomial.Format(args[0], style);
                else if (op == "time") s += op;
                else if (op == "poly 1/[]") s += "1/[" + Monomial.Format(args[0], style) + "]";
                else if (op == "poly []^(1/[])") s += "[" + Monomial.Format(args[0], style) + "]^1/[" + Monomial.Format(args[1], style) + "]";
                else {
                    string sargs = "";
                    for (int i = 0; i < args.Length; i++) { sargs += Monomial.Format(args[i], style) + ((i == args.Length - 1) ? "" : ","); }
                    s += op + "(" + sargs + ")";
                }
                if (splitOp == Split.Pos) s = "pos(" + s + ")";
                if (splitOp == Split.Neg) s = "pos(-" + s + ")";
                return var.Format(style) + " = " + s;
            }
            public static string Format(Lst<Equation> eqs, Style style) {
                if (eqs is Cons<Equation> cons) {
                    return cons.head.Format(style) + Environment.NewLine + Format(cons.tail, style);
                } else return "";
            }
            public static Lst<Equation> nil = new Nil<Equation>();
            public static Flow ToFlow(SpeciesFlow var, Lst<Equation> eqs, Style style) {
                return Lookup(var, eqs, style).ToFlow(eqs, style);
            }
            public Flow ToFlow(Lst<Equation> eqs, Style style) {
                // this is a bit redundant because eventually we call Flow.Normalize, but Normalize does not handle pos, etc.
                Flow result;
                if (op == "id") result = Monomial.ToFlow(args[0], eqs, style);
                else if (op == "time") result = Flow.zero;
                else if (op == "poly 1/[]") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num) result = new NumberFlow(1 / num.value);
                    else result = OpFlow.Op(Flow.one, "/", arg0);
                } else if (op == "poly []^(1/[])") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    Flow arg1 = Monomial.ToFlow(args[1], eqs, style);
                    if (arg0 is NumberFlow num0 && arg1 is NumberFlow num1) result = new NumberFlow(Math.Pow(num0.value, 1 / num1.value));
                    else result = OpFlow.Op(arg0, "^", OpFlow.Op(Flow.one, "/", arg1));
                } else if (op == "exp") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Exp(num0.value)); else result = OpFlow.Op("exp", arg0);
                } else if (op == "log") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Log(num0.value)); else result = OpFlow.Op("log", arg0);
                } else if (op == "sin") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Sin(num0.value)); else result = OpFlow.Op("sin", arg0);
                } else if (op == "cos") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Cos(num0.value)); else result = OpFlow.Op("cos", arg0);
                } else if (op == "tan") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Tan(num0.value)); else result = OpFlow.Op("tan", arg0);
                } else if (op == "sinh") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Sin(num0.value)); else result = OpFlow.Op("sinh", arg0);
                } else if (op == "cosh") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Cos(num0.value)); else result = OpFlow.Op("cosh", arg0);
                } else if (op == "tanh") {
                    Flow arg0 = Monomial.ToFlow(args[0], eqs, style);
                    if (arg0 is NumberFlow num0) result = new NumberFlow(Math.Tan(num0.value)); else result = OpFlow.Op("tanh", arg0);
                } else throw new Error("Polynomize.Equation.Eval op: " + op);
                if (splitOp == Split.Pos) {
                    if (result is NumberFlow num) result = (num.value < 0.0) ? Flow.zero : result;  // result = pos(result)
                    else result = OpFlow.Op("pos", result);  
                }
                if (splitOp == Split.Neg) {
                    if (result is NumberFlow num) result = (-num.value < 0.0) ? Flow.zero : new NumberFlow(-num.value); // result = pos(-result)
                    else result = OpFlow.Op("pos", OpFlow.Op("-", result));  
                }
                return result;
            }
        }
        
        public static (SpeciesFlow newVar, Lst<Equation> newEqs) AddNewEquation(string op, Lst<Monomial>[] args, Lst<Equation> eqs, SpeciesFlow parentVar, Style style) {
            SpeciesFlow newVar = new SpeciesFlow(new Symbol(parentVar.species.Raw()));
            return (newVar, new Cons<Equation>(new Equation(newVar, op, args), eqs));
        }

        // ===== Rational numbers =====

        public static bool IsInt(double r) {
            int n = (int)r;
            return r == n;
        }

        // https://stackoverflow.com/questions/4266741/check-if-a-number-is-rational-in-python-for-a-given-fp-accuracy

        // Return the fraction with the lowest denominator that differs from x by no more than e. (Knuth)
        public static (int num, int den) ApproximateRational(double x, double e) {
            (int num, int den) = SimplestRationalInInterval(x - e, x + e);
            //Gui.Log("ApproximateRational(" + x.ToString() + ", " + e.ToString() + ") = " + num.ToString() + "/" + den.ToString());
            return (num, den);
        }
        // Return the fraction with the lowest denominator in [x,y].
        public static (int num, int den) SimplestRationalInInterval(double x, double y) {
            if (x == y) { //The algorithm will not terminate if x and y are equal.
                throw new Error("Equal arguments.");
            } else if (x < 0 && y < 0) { // Handle negative arguments by solving positive case and negating.
                (int nnum, int nden) = SimplestRationalInInterval(-y, -x);
                return (-nnum, nden);
            } else if (x <= 0 || y <= 0) { // One argument is 0, or arguments are on opposite sides of 0, so the simplest fraction in interval is 0 exactly.
                return (0, 1);
            } else { // Remainder and Coefficient of continued fractions for x and y.
                (double xr, double xc) = Modf(1 / x);
                (double yr, double yc) = Modf(1 / y);
                if (xc < yc)
                    return (1, (int)(xc) + 1);
                else if (yc < xc)
                    return (1, (int)(yc) + 1);
                else { // return 1 / ((int)(xc) + SimplestFractionInInterval(xr, yr));
                    (int inum, int iden) = SimplestRationalInInterval(xr, yr);
                    return (iden, (int)(xc) * iden + inum);
                }
            }
        }
        // Break a double into integer and fractional part
        public static (double fpart, double ipart) Modf(double x) {
            double ipart = Math.Floor(x);
            double fpart = x - ipart;
            return (fpart, ipart);
        }

        // ===== POLYNOMIZE ALGORITHM =====

        // Equations rhs are polynomials except for the outermost operators,
        // and contain only the special cases 1/e, e^n (n in Nat), e^(1/n) (n in Nat) instead of the general / and ^ operators
        // and moreover these special cases appear only as outermost operators in the rhs

        // Polynomize(O) = Refold(Unfold(O))   -- returns <O',E> where O' are polynomial ODEs and E equations are used for initial values of newly introduced variables

        // Unfold(O) = Unfold(O,{},{})         -- O are the input odes, E are the accumulated output equations, Q are the accumulated ouput odes
        //                                     -- returns <E',O'>: the equations E' to process by Refold, and O' = O enriched with new odes for the variables in E'

        // Unfold({}, E, Q) = <reverse(E), Q>  -- reverse because they have been build backwards and Refold needs to process them in forward order
        // Unfold({∂v = e}+O, E, Q) = <e',E'> = ExpUnfold(e,E); Unfold(O, E', {∂v = e'}+Q)

        // ExpUnfold(x, E) = <x, E>
        // ExpUnfold(c, E) = <c, E>
        // ExpUnfold(time, E) = new v; <v, {v = time}+E>
        // ExpUnfold(e0+e1, E) = <ExpUnfold(e0, E) + ExpUnfold(e1, E), E>
        // ExpUnfold(e0-e1, E) = <ExpUnfold(e0, E) - ExpUnfold(e1, E), E>
        // ExpUnfold(-e0, E) = <-ExpUnfold(e0, E), E>
        // ExpUnfold(e0*e1, E) = <ExpUnfold(e0, E) * ExpUnfold(e1, E), E>
        // ExpUnfold(e0/e1, E) = <e1',E'> = ExpUnfold(e1,E); new v; ExpUnfold(e0*v, {v = 1/e1'}+E')
        // ExpUnfold(e0^r1, E) = ExpUnfold(e0^(q0/q1), E)  if r1=q0/q1 is rational (use Knuth's algorithm to find an approximate rational)
        // ExpUnfold(e0^(q0/q1), E) = ExpUnfold(1/e0^(-q0/q1), E)   if q0/q1 is negative
        // ExpUnfold(e0^(q0/q1), E) = <e0',E'> = ExpUnfold(e0,E); new v; <v^q0, {v = e0'^(1/q1)}+E'>
        // ExpUnfold(sqrt(e0), E) = ExpUnfold(e0^0.5, E)
        // ExpUnfold(exp(e0), E) = <e0',E'> = ExpUnfold(e0,E); new v; <v, {v = exp(e0')}+E'>
        // ExpUnfold(log(e0), E) = <e0',E'> = ExpUnfold(e0,E); new v; <v, {v = log(e0')}+E'>
        // ExpUnfold(sin(e0), E) = <e0',E'> = ExpUnfold(e0,E); new v; <v, {v = sin(e0')}+E'>
        // ExpUnfold(cos(e0), E) = <e0',E'> = ExpUnfold(e0,E); new v; <v, {v = cos(e0')}+E'>
        // ExpUnfold(tan(e0), E) = <e0',E'> = ExpUnfold(e0,E); new v; <v, {v = tan(e0')}+E'>

        // Refold(E, O) = Refold(E, O, {})   -- E is the eqs to process (they can grow), O is the accumulated output odes, F is the eqs that have been processed
        //                                   -- returns <O',F'> the new odes and the new equations (used for odes initial values)

        // Refold({}, O, F) = <reverse(O), reverse(F)> // reverse just for cleanliness
        // Refold({v = time}+E, O, F) = Refold(E, {∂v = 1} + O,                                               {v = 0}+F)
        // Refold({v = 1/e0}+E, O, F) = Refold(E, {∂v = -v^2 * Diff(e0,O)}+O,                                 {v = 1/e0}+F)
        // Refold({v = e0^(1/1)}+E, O, F) = Refold(E, O,                                                      {v = e0}+F)
        // Refold({v = e0^(1/q0)}+E, O, F) = new w; Refold(E, {∂v = 1/q0 * v * w * Diff(e0,O)}+O,             {w = 1/e0}+{v = e0^(1/q0)}+F)     q0 > 1
        // Refold({v = exp(e0)}+E, O, F) = Refold(E, {∂v = v * Diff(e0,O)}+O,                                 {v = exp(e0)}+F)
        // Refold({v = log(e0)}+E, O, F) = new w; Refold(E+{w = 1/e0}, {∂v = w * Diff(e0,O)}+O,               {v = log(e0)}+F)
        // Refold({v = sin(e0)}+E, O, F) = new w; Refold(E, {∂w = -v * Diff(e0,O)}+{∂v = w * Diff(e0,O)}+O,   {w = cos(e0)}+{v = sin(e0)}+F)
        // Refold({v = cos(e0)}+E, O, F) = new w; Refold(E, {∂w = v * Diff(e0,O)}+{∂v = -w * Diff(e0,O)}+O,   {w = sin(e0)}+{v = cos(e0)}+F)
        // Refold({v = tan(e0)}+E, O, F) = Refold(E, {∂v = (1 + v^2) * Diff(e0,O)}+O,                         {v = tan(e0)}+F)

        // Diff(e0, O)  computes the derivative of a polynomial e0; the Diff of variables are replaced by the rhs's of odes from O

        // Note: polynomialize ∂x = x^(1/n)   (the algorithm is wrong in the appendix of the Liu paper)
        // let v = x^(1/n), hence ∂x = v
        // differentiate  ∂v  =  1/n * x^(1/n - 1) * ∂x  =  1/n * x^(1/n) * x^-1 * ∂x  =  1/n * v^2 * x^-1
        // let w = x^-1, hence ∂v = 1/n * v^2 * w
        // differentiate  ∂w  =  -1 * x^-2 * ∂x  =  -1 * x^-1 * x^-1 * ∂x  =  -w^2 * v
        // Hence:
        //      ∂x = v
        //      ∂v = 1/n * v^2 * w         Note the inductive step above: ∂v = 1/n * v * w * ∂x,  where ∂x = v in this case
        //      ∂w = -w^2 * v


        // ===== POLYNOMIZE =====

        // PolynomizeODEs: Input is elementary-function ODEs
        // Ouput is polynomial-function ODEs, and equations for initial conditions of newly introduced variables
        // The input equations contain the intial values of the ODE variables; the output equations extend them with the new variables

        public static (Lst<PolyODE> odes, Lst<Equation> eqs) PolynomizeODEs(Lst<ODE> odes, Lst<Equation> eqs, Style style) {
            //Gui.Log("Input: " + Environment.NewLine + Format(odes, style));
            (Lst<Equation> polyEqs, Lst<PolyODE> polyODEs) = Unfold(odes, eqs, style);
            //Gui.Log("After Unfold: " + Environment.NewLine + Format(polyODEs, style) + Format(eqs, style));
            (Lst<PolyODE> finalODEs, Lst<Equation> initEqs) = Refold(polyEqs, polyODEs, style);
            //Gui.Log("After Refold: " + Environment.NewLine + Format(finalODEs,style) + Format(initEqs, style));
            return (finalODEs, initEqs);
        }

        // Unfold: Input is elementary-function odes
        // Ouput1 is a set of polynomial-function ODEs (with variables referring to the equations in Output2)
        // Output2 is a set of equations, whose rhs are polynomial except for the outermost operator 

        public static (Lst<Equation> polyEqs, Lst<PolyODE> polyODEs) Unfold(Lst<ODE> odes, Lst<Equation> eqs, Style style) {
            return Unfold(odes, eqs, new Nil<PolyODE>(), style);
        }

        public static (Lst<Equation> eqs, Lst<PolyODE> polyODEs) Unfold(Lst<ODE> odes, Lst<Equation> eqs, Lst<PolyODE> polyODEs, Style style) {
            if (odes is Cons<ODE> cons) {
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(cons.head.flow, eqs, cons.head.var, style);
                return Unfold(cons.tail, eqs0, new Cons<PolyODE>(new PolyODE(cons.head.var, new Polynomial(exp0), Split.No), polyODEs), style);
            } else return (eqs.Reverse(), polyODEs);
        }

        // ExpUnfold: Input is an elementary-function expression
        // Output is a polynomial-function expression
        // Also adds new equations to (the initially empty) eqs
        // all expressions in eqs are polynomial except for the outermost operator

        public static (Lst<Monomial> poly, Lst<Equation> eqs) ExpUnfold(Flow exp, Lst<Equation> eqs, SpeciesFlow parentVar, Style style) { // 
            string bad = "Polynomize: invalid input: " + exp.Format(style);
            if (exp is SpeciesFlow var) {
                return (Monomial.Singleton(new Monomial(new Factor(var))), eqs);
            } else if (exp is NumberFlow num) {
                return (Monomial.Singleton(new Monomial(num.value)), eqs);
            } else if (exp is ConstantFlow) {
                return (Monomial.Singleton(new Monomial(exp, style)), eqs);
            } else if (exp is OpFlow opTime && opTime.op == "time") {
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("time", new Lst<Monomial>[0], eqs, new SpeciesFlow(new Symbol("time")), style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opAdd && opAdd.op == "+") {
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opAdd.args[0], eqs, parentVar, style);
                (Lst<Monomial> exp1, Lst<Equation> eqs1) = ExpUnfold(opAdd.args[1], eqs0, parentVar, style);
                return (Monomial.Sum(exp0, exp1, style), eqs1);
            } else if (exp is OpFlow opSub && opSub.op == "-" && opSub.arity == 2) {
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opSub.args[0], eqs, parentVar, style);
                (Lst<Monomial> exp1, Lst<Equation> eqs1) = ExpUnfold(opSub.args[1], eqs0, parentVar, style);
                return (Monomial.Sum(exp0, Monomial.Negate(exp1, style), style), eqs1);
            } else if (exp is OpFlow opMinus && opMinus.op == "-" && opMinus.arity == 1) {
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opMinus.args[0], eqs, parentVar, style);
                return (Monomial.Negate(exp0, style), eqs0);
            } else if (exp is OpFlow opMul && opMul.op == "*") {
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opMul.args[0], eqs, parentVar, style);
                (Lst<Monomial> exp1, Lst<Equation> eqs1) = ExpUnfold(opMul.args[1], eqs0, parentVar, style);
                return (Monomial.Product(exp0, exp1, style), eqs1);
            } else if (exp is OpFlow opDiv && opDiv.op == "/") {
                //Gui.Log("Unfold / IN: " + exp.Format(style) + Environment.NewLine + "inEqs" + Environment.NewLine + Format(eqs, style));
                (Lst<Monomial> exp1, Lst<Equation> eqs1) = ExpUnfold(opDiv.args[1], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("poly 1/[]", new Lst<Monomial>[1] { exp1 }, eqs1, parentVar, style);
                //Gui.Log("Unfold / OUT: " + exp.Format(style) + Environment.NewLine + "outEqs" + Environment.NewLine + Format(newEqs, style));
                return ExpUnfold(OpFlow.Op(opDiv.args[0], "*", newVar), newEqs, parentVar, style);
            } else if (exp is OpFlow opPow && opPow.op == "^") {
                //Gui.Log("Unfold ^ IN: " + exp.Format(style) + Environment.NewLine + "inEqs" + Environment.NewLine + Equation.Format(eqs, style));
                // convert real exponents to rationals:
                int n0; int n1;
                if (opPow.args[1] is NumberFlow powNum) (n0, n1) = ApproximateRational(powNum.value, 0.01); else throw new Error(bad);
                // convert negative rationals to fractions of positive rationals, which are handled by the "/" case:
                if (n0 < 0 && n1 < 0) { n0 = -n0; n1 = -n1; }
                if (n0 < 0) return ExpUnfold(new OpFlow("/", true, new NumberFlow(1.0), OpFlow.Op(opPow.args[0], "^", new NumberFlow(-n0 / n1))), eqs, parentVar, style); // simplify "^" but do not simplify "/"
                if (n1 < 0) return ExpUnfold(new OpFlow("/", true, new NumberFlow(1.0), OpFlow.Op(opPow.args[0], "^", new NumberFlow(n0 / -n1))), eqs, parentVar, style); // simplify "^" but do not simplify "/"
                // optimize trivial exponents
                if (n0 == 0.0) return ExpUnfold(new NumberFlow(1.0), eqs, parentVar, style);
                if (n0 == 1.0 && n1 == 1.0) return ExpUnfold(opPow.args[0], eqs, parentVar, style);
                if (n1 == 1.0) {
                    (Lst<Monomial> expP0, Lst<Equation> eqsP0) = ExpUnfold(opPow.args[0], eqs, parentVar, style);
                    return (Monomial.Power(expP0, n0, style), eqsP0);
                }
                // otherwise handle exp0^(n0/n1):
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opPow.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("poly []^(1/[])", new Lst<Monomial>[2] { exp0, Monomial.Singleton(new Monomial(n1)) }, eqs0, parentVar, style);
                //Gui.Log("Unfold ^ OUT: " + exp.Format(style) + Environment.NewLine + "outEqs" + Environment.NewLine + Equation.Format(newEqs, style));
                return (Monomial.Singleton(new Monomial(new Factor(newVar, n0))), newEqs);
            } else if (exp is OpFlow opSqrt && opSqrt.op == "sqrt") {
                return ExpUnfold(OpFlow.Op(opSqrt.args[0], "^", new NumberFlow(0.5)), eqs, parentVar, style);
            } else if (exp is OpFlow opExp && opExp.op == "exp") { 
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opExp.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("exp", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opLog && opLog.op == "log") {
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opLog.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("log", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opSin && opSin.op == "sin") { 
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opSin.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("sin", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opCos && opCos.op == "cos") { 
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opCos.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("cos", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opTan && opTan.op == "tan") {
                // return ExpUnfold(OpFlow.Op(OpFlow.Op("sin", opTan.args[0]), "/", OpFlow.Op("cos", opTan.args[0])), eqs, parentVar, style); // ok but introduces many more variables
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opTan.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("tan", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opSinh && opSinh.op == "sinh") { 
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opSinh.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("sinh", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opCosh && opCosh.op == "cosh") { 
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opCosh.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("cosh", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else if (exp is OpFlow opTanh && opTanh.op == "tanh") {
                (Lst<Monomial> exp0, Lst<Equation> eqs0) = ExpUnfold(opTanh.args[0], eqs, parentVar, style);
                (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("tanh", new Lst<Monomial>[1] { exp0 }, eqs0, parentVar, style);
                return (Monomial.Singleton(new Monomial(new Factor(newVar))), newEqs);
            } else throw new Error(bad);
        }

        // Refold: Input1 is a set of polynomial-function ODEs (with variables referring to the equations in Input2)
        // Input2 is a set of equations, whose rhs are polynomial except for the outermost operator 
        // New polynomial odes are added to Input1, so that in the end it is independent of the equations and entirely polynomial

        public static (Lst<PolyODE> polyODE, Lst<Equation> initEqs) Refold(Lst<Equation> inEqs, Lst<PolyODE> odes, Style style) {
            (Lst<PolyODE> polyODE, Lst<Equation> initEqs) = Refold(inEqs, odes, new Nil<Equation>(), style);
            return (polyODE.Reverse(), initEqs.Reverse());
        }

        public static (Lst<PolyODE> polyODEs, Lst<Equation> initEqs) Refold(Lst<Equation> inEqs, Lst<PolyODE> odes, Lst<Equation> outEqs, Style style) {
            if (inEqs is Cons<Equation> cons) {
                if (cons.head.op == "id") { // simple variable initialization
                    return Refold(cons.tail, odes, new Cons<Equation>(cons.head, outEqs), style);
                } else if (cons.head.op == "time") {
                    Equation eq = cons.head;
                    return Refold(cons.tail,
                        new Cons<PolyODE>(new PolyODE(eq.var, new Monomial(1.0), Split.No), odes), 
                        new Cons<Equation>(new Equation(eq.var, "id", new Lst<Monomial>[1] { Monomial.Singleton(new Monomial(0.0)) }), outEqs), style); 
                } else if (cons.head.op == "poly 1/[]") { // generated by Unfold: special case of "/" where [] is a polynomial
                    //Gui.Log("Refold /: " + Environment.NewLine + Format(odes, style) + "inEqs" + Environment.NewLine + Format(inEqs, style) + "outEqs" + Environment.NewLine + Format(outEqs, style));
                    Equation eq = cons.head;
                    return Refold(cons.tail,
                        new Cons<PolyODE>(new PolyODE(eq.var, 
                            Monomial.Product(new Monomial(Flow.minusOne, new Factor(eq.var, 2), style),
                                PolyDiff(cons.head.args[0], odes, style), style), Split.No),
                            odes),
                        new Cons<Equation>(eq, outEqs), style);
                } else if (cons.head.op == "poly []^(1/[])") { // generated by Unfold: special case of "^" where the first [] is a polynomial and the second [] is an integer
                    //Gui.Log("Refold ^: " + cons.head.Format(style) + Environment.NewLine + "inEqs" + Environment.NewLine + "odes" + Environment.NewLine + PolyODE.Format(odes, style) + Equation.Format(inEqs, style) + "outEqs" + Environment.NewLine + Equation.Format(outEqs, style));
                    Equation eq = cons.head;
                    Flow n1 = (cons.head.args[1] as Cons<Monomial>).head.coefficient; // an integer
                    if (!(n1 is NumberFlow)) throw new Error("Could not reduce to an integer: " + n1.Format(style) + ". Are there unknown constants in that expression?");
                    int n1int = (int)((n1 as NumberFlow).value);
                    Flow n1inverse = OpFlow.Op(Flow.one, "/", n1);
                    if (n1int == 1) { // the minimum an integer denominator can be
                        return Refold(cons.tail, odes, new Cons<Equation>(eq, outEqs), style);
                    } else {
                        (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("poly 1/[]", new Lst<Monomial>[1] { cons.head.args[0] }, cons.tail, eq.var, style);
                        return Refold(newEqs,
                            new Cons<PolyODE>(new PolyODE(eq.var,
                                Monomial.Product(new Monomial(n1inverse, style),
                                    Monomial.Product(Monomial.Product(new Factor(eq.var), new Factor(newVar), style),
                                        PolyDiff(cons.head.args[0], odes, style), style), style), Split.No),
                                odes),
                            new Cons<Equation>(eq, outEqs), style);
                    }
                } else if (cons.head.op == "exp") {
                    Equation eq = cons.head;
                    return Refold(cons.tail,
                        new Cons<PolyODE>(new PolyODE(eq.var,
                            Monomial.Product(new Monomial(new Factor(eq.var)),
                                PolyDiff(cons.head.args[0], odes, style), style), Split.No), 
                           odes),
                        new Cons<Equation>(eq, outEqs), style);
                } else if (cons.head.op == "log") {
                    Equation eq = cons.head;
                    (SpeciesFlow newVar, Lst<Equation> newEqs) = AddNewEquation("poly 1/[]", new Lst<Monomial>[1] { cons.head.args[0] }, cons.tail, eq.var, style);
                    return Refold(newEqs,
                        new Cons<PolyODE>(new PolyODE(eq.var,
                            Monomial.Product(new Monomial(new Factor(newVar)),
                                PolyDiff(cons.head.args[0], odes, style), style), Split.No), 
                            odes),
                        new Cons<Equation>(eq, outEqs), style);
                } else if (cons.head.op == "sin") {
                    //Gui.Log("Refold sin: " + Environment.NewLine + Format(odes, style) + "inEqs" + Environment.NewLine + Format(inEqs, style) + "outEqs" + Environment.NewLine + Format(outEqs, style));
                    Equation eq = cons.head;
                    Lst<Equation> outEqs0 = new Cons<Equation>(eq, outEqs);
                    (SpeciesFlow newVar, Lst<Equation> outEqs1) = AddNewEquation("cos", new Lst<Monomial>[1] { cons.head.args[0] }, outEqs0, new SpeciesFlow(new Symbol(eq.var.species.Raw())), style);
                    Lst<Monomial> e0diff = PolyDiff(cons.head.args[0], odes, style);
                    Lst<PolyODE> odes0 = new Cons<PolyODE>(new PolyODE(eq.var, Monomial.Product(new Monomial(new Factor(newVar)), e0diff, style), Split.No), odes);
                    Lst<PolyODE> odes1 = new Cons<PolyODE>(new PolyODE(newVar, Monomial.Product(new Monomial(-1.0, new Factor(eq.var)), e0diff, style), Split.No), odes0);
                    return Refold(cons.tail, odes1, outEqs1, style);
                } else if (cons.head.op == "cos") {
                    //Gui.Log("Refold cos: " + Environment.NewLine + Format(odes, style) + "inEqs" + Environment.NewLine + Format(inEqs, style) + "outEqs" + Environment.NewLine + Format(outEqs, style));
                    Equation eq = cons.head;
                    Lst<Equation> outEqs0 = new Cons<Equation>(eq, outEqs);
                    (SpeciesFlow newVar, Lst<Equation> outEqs1) = AddNewEquation("sin", new Lst<Monomial>[1] { cons.head.args[0] }, outEqs0, new SpeciesFlow(new Symbol(eq.var.species.Raw())), style);
                    Lst<Monomial> e0diff = PolyDiff(cons.head.args[0], odes, style);
                    Lst<PolyODE> odes0 = new Cons<PolyODE>(new PolyODE(eq.var, Monomial.Product(new Monomial(-1.0, new Factor(newVar)), e0diff, style), Split.No), odes);
                    Lst<PolyODE> odes1 = new Cons<PolyODE>(new PolyODE(newVar, Monomial.Product(new Monomial(new Factor(eq.var)), e0diff, style), Split.No), odes0);
                    return Refold(cons.tail, odes1, outEqs1, style);
                } else if (cons.head.op == "tan") {
                    Equation eq = cons.head;
                    return Refold(cons.tail,
                        new Cons<PolyODE>(new PolyODE(eq.var,
                            Monomial.Product(Monomial.Sum(new Monomial(1.0), Monomial.Singleton(new Monomial(new Factor(eq.var, 2))), style),
                                PolyDiff(cons.head.args[0], odes, style), style), Split.No),
                           odes),
                        new Cons<Equation>(eq, outEqs), style);
                } else if (cons.head.op == "sinh") {
                    Equation eq = cons.head;
                    Lst<Equation> outEqs0 = new Cons<Equation>(eq, outEqs);
                    (SpeciesFlow newVar, Lst<Equation> outEqs1) = AddNewEquation("cosh", new Lst<Monomial>[1] { cons.head.args[0] }, outEqs0, new SpeciesFlow(new Symbol(eq.var.species.Raw())), style);
                    Lst<Monomial> e0diff = PolyDiff(cons.head.args[0], odes, style);
                    Lst<PolyODE> odes0 = new Cons<PolyODE>(new PolyODE(eq.var, Monomial.Product(new Monomial(new Factor(newVar)), e0diff, style), Split.No), odes);
                    Lst<PolyODE> odes1 = new Cons<PolyODE>(new PolyODE(newVar, Monomial.Product(new Monomial(new Factor(eq.var)), e0diff, style), Split.No), odes0);
                    return Refold(cons.tail, odes1, outEqs1, style);
                } else if (cons.head.op == "cosh") {
                    Equation eq = cons.head;
                    Lst<Equation> outEqs0 = new Cons<Equation>(eq, outEqs);
                    (SpeciesFlow newVar, Lst<Equation> outEqs1) = AddNewEquation("sinh", new Lst<Monomial>[1] { cons.head.args[0] }, outEqs0, new SpeciesFlow(new Symbol(eq.var.species.Raw())), style);
                    Lst<Monomial> e0diff = PolyDiff(cons.head.args[0], odes, style);
                    Lst<PolyODE> odes0 = new Cons<PolyODE>(new PolyODE(eq.var, Monomial.Product(new Monomial(new Factor(newVar)), e0diff, style), Split.No), odes);
                    Lst<PolyODE> odes1 = new Cons<PolyODE>(new PolyODE(newVar, Monomial.Product(new Monomial(new Factor(eq.var)), e0diff, style), Split.No), odes0);
                    return Refold(cons.tail, odes1, outEqs1, style);
                } else if (cons.head.op == "tanh") {
                    Equation eq = cons.head;
                    return Refold(cons.tail,
                        new Cons<PolyODE>(new PolyODE(eq.var,
                            Monomial.Product(Monomial.Sum(new Monomial(1.0), Monomial.Singleton(new Monomial(-1.0, new Factor(eq.var, 2))), style),
                                PolyDiff(cons.head.args[0], odes, style), style), Split.No),
                           odes),
                        new Cons<Equation>(eq, outEqs), style);
                } else throw new Error("Polynomize Refold: bad equation: " + cons.head.Format(style));
            } else return (odes, outEqs);
        }

        // PolyDiff: Compute the derivative of a polynomial-function Input1
        // where the (polynomial) derivative of a variable is obtained by looking up the rhs of the ode for that variable in Input2

        public static Lst<Monomial> PolyDiff(Lst<Monomial> monomials, Lst<PolyODE> odes, Style style) {
            //Gui.Log("IN  PolyDiff(Ms[" + Monomial.Format(monomials, style) + "])");
            Lst<Monomial> result;
            if (monomials is Cons<Monomial> cons)  {
                result = Monomial.Sum(PolyDiff(cons.head, odes, style), PolyDiff(cons.tail, odes, style), style); // ∂(f+g) = ∂f+∂g
            } else result = Monomial.Singleton(new Monomial(Flow.zero, style));
            //Gui.Log("OUT PolyDiff(Ms[" + Monomial.Format(monomials, style) + "]) = Ms[" + Monomial.Format(result, style) + "]");
            return result;
        }

        public static Lst<Monomial> PolyDiff(Monomial monomial, Lst<PolyODE> odes, Style style) {
            if (monomial.factors is Nil<Factor>) return Monomial.Singleton(new Monomial(Flow.zero, style));  // ∂(n) = 0
            else return Monomial.Product(monomial.coefficient, PolyDiff(monomial.factors, odes, style), style); // ∂(n*f) = n*∂f
        }

        public static Lst<Monomial> PolyDiff(Lst<Factor> factors, Lst<PolyODE> odes, Style style) {
            //Gui.Log("IN  PolyDiff(Fs[" + Factor.Format(factors, style) + "])");
            Lst<Monomial> result;
            if (factors is Cons<Factor> cons) {  // ∂(f*g) = ∂f * g + f * ∂g
                result = Monomial.Sum(
                    Monomial.Product(PolyDiff(cons.head, odes, style), cons.tail, style),
                    Monomial.Product(cons.head, PolyDiff(cons.tail, odes, style), style), style);
            } else result = Monomial.Singleton(new Monomial(0.0));
            //Gui.Log("OUT PolyDiff(Fs[" + Factor.Format(factors, style) + "]) = Ms[" + Monomial.Format(result, style) + "]");
            return result;
        }

        public static Lst<Monomial> PolyDiff(Factor factor, Lst<PolyODE> odes, Style style) {
            if (factor.power == 1) return PolyDiff(factor.variable, odes, style); // ∂f = its value on the rhs of f in odes
            else return Monomial.Product(  // ∂(f^n) = n*(f^(n-1)) * ∂f // special case if exponent is a number
                Monomial.Product(new NumberFlow(factor.power), Monomial.Singleton(new Monomial(new Factor(factor.variable, factor.power - 1))), style),
                PolyDiff(factor.variable, odes, style), style);
        }

        public static Lst<Monomial> PolyDiff(SpeciesFlow species, Lst<PolyODE> odes, Style style) {
            return Lookup(species, odes, style).monomials;
        }
    }
}
