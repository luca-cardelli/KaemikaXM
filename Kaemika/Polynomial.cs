using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    public class Factor {
        public SpeciesFlow variable { get; }
        public int power { get; } // >= 0

        public Factor(SpeciesFlow variable) {
            this.variable = variable;
            this.power = 1;
        }
        public Factor(SpeciesFlow variable, int power) {
            this.variable = variable;
            this.power = power;
        }
        public bool IsOne() { return power == 0; }
        public static Lst<Factor> nil = new Nil<Factor>();
        public static Lst<Factor> Cons(Factor factor, Lst<Factor> factors) {
            if (factor.IsOne()) return factors;
            else return new Cons<Factor>(factor, factors);
        }
        public static Lst<Factor> Singleton(Factor f) { return Cons(f, nil); }
        public string Format(Style style) {
            return this.variable.Format(style) + ((power == 1) ? "" : "^" + this.power.ToString());
        }
        public static string Format(Lst<Factor> factors, Style style) {
            if (factors is Nil<Factor> cons) return "1";
            else return FormatLst(factors, style);
        }
        public static string FormatLst(Lst<Factor> factors, Style style) {
            if (factors is Cons<Factor> cons) {
                return cons.head.Format(style) + ((cons.tail is Nil<Factor>) ? "" : "·" + FormatLst(cons.tail, style));
            } else return "";
        }
        public Flow ToFlow() {
            if (power == 0) return new NumberFlow(1.0);
            if (power == 1) return variable;
            return OpFlow.Op(variable, "^", new NumberFlow(power));
        }
        public static Flow ToFlow(Lst<Factor> factors) {
            if (factors is Cons<Factor> cons) {
                return cons.tail is Nil<Factor> ? cons.head.ToFlow() : OpFlow.Op(cons.head.ToFlow(), "*", ToFlow(cons.tail));
            } else return new NumberFlow(1.0);
        }

        public bool SameFactor(Factor other) {
            return this.variable.SameSpecies(other.variable) && this.power == other.power;
        }

        public static bool HasFactorVariable(SpeciesFlow variable, Lst<Factor> factors) {
            if (factors is Cons<Factor> cons) {
                if (cons.head.variable.SameSpecies(variable)) return true;
                else return HasFactorVariable(variable, cons.tail);
            } else return false;
        }

        public static bool HasFactor(Factor factor, Lst<Factor> factors) {
            if (factors is Cons<Factor> cons) {
                if (cons.head.SameFactor(factor)) return true;
                else return HasFactor(factor, cons.tail);
            } else return false;
        }

        public static bool Included(Lst<Factor> factors1, Lst<Factor> factors2) {
            if (factors1 is Cons<Factor> cons) {
                if (HasFactor(cons.head, factors2)) return Included(cons.tail, factors2);
                else return false;
            } else return true;
        }
        public static bool SameFactors(Lst<Factor> factors1, Lst<Factor> factors2) {
            return Included(factors1, factors2) && Included(factors2, factors1);
        }

        public static Lst<Factor> Product(Factor factor, Lst<Factor> factors) {
            if (factor.IsOne()) return factors;
            if (factors is Cons<Factor> cons) {
                if (cons.head.variable.SameSpecies(factor.variable)) {
                    int sumPower = cons.head.power + factor.power;
                    if (sumPower == 0) return cons.tail;
                    else return Factor.Cons(new Factor(cons.head.variable, sumPower), cons.tail);
                } else return Factor.Cons(cons.head, Product(factor, cons.tail));
            } else return Factor.Singleton(factor);
        }
        public static Lst<Factor> Product(Lst<Factor> factors1, Lst<Factor> factors2) {
            if (factors1 is Cons<Factor> cons) {
                return Product(cons.head, Product(cons.tail, factors2));
            } else return factors2;
        }

        public static Lst<Factor> Quotient(Lst<Factor> factors, Factor factor, Style style) {
            if (factor.IsOne()) return factors;
            if (factors is Cons<Factor> cons) {
                if (cons.head.variable.SameSpecies(factor.variable)) {
                    int difPower = cons.head.power - factor.power;
                    if (difPower < 0) throw new Error("Factor.Quotient: " + Format(factors, style) + "/" + factor.Format(style));
                    if (difPower == 0) return cons.tail;
                    else return Factor.Cons(new Factor(cons.head.variable, difPower), cons.tail);
                } else return Factor.Cons(cons.head, Quotient(cons.tail, factor, style));
            } else throw new Error("Factor.Quotient: " + Format(factors, style) + "/" + factor.Format(style));
        }

        public static Lst<Factor> Power(Lst<Factor> factors, int power) {
            if (factors is Cons<Factor> cons) {
                return Factor.Cons(new Factor(cons.head.variable, cons.head.power * power), Power(cons.tail, power));
            } else return Factor.nil;
        }

        public Flow ToFlow(Lst<Polynomize.Equation> eqs, Style style) {
            return OpFlow.Op(Polynomize.Lookup(this.variable, eqs, style).ToFlow(eqs, style), "^", new NumberFlow(this.power));
        }
        public static Flow ToFlow(Lst<Factor> factors, Lst<Polynomize.Equation> eqs, Style style) {
            if (factors is Cons<Factor> cons) {
                return OpFlow.Op(cons.head.ToFlow(eqs, style), "*", ToFlow(cons.tail, eqs, style));
            } else return Flow.one;
        }

        public List<Symbol> ToList() { return ToList(new List<Symbol>()); }
        private List<Symbol> ToList(List<Symbol> rest) {
            for (int i = 0; i < this.power; i++) rest.Insert(0, this.variable.species);
            return rest;
        }

        public static List<Symbol> ToList(Lst<Factor> factors) { return ToList(factors, new List<Symbol>()); }
        private static List<Symbol> ToList(Lst<Factor> factors, List<Symbol> rest) {
            if (factors is Cons<Factor> cons)
                return cons.head.ToList(ToList(cons.tail, rest));
            else return rest;
        }
    }

    public class Monomial {
        public Flow coefficient { get; } // this is kept in normal form by Flow.Normalize
        public Lst<Factor> factors { get; }

        public Monomial(double coefficient) {
            this.coefficient = new NumberFlow(coefficient);
            this.factors = Factor.nil;
        }
        public Monomial(Flow coefficient, Style style) {
            this.coefficient = coefficient.Normalize(style);
            this.factors = Factor.nil;
        }
        public Monomial(Factor factor) {
            this.coefficient = Flow.one;
            this.factors = Factor.Singleton(factor);
        }
        public Monomial(Lst<Factor> factors) {
            this.coefficient = Flow.one;
            this.factors = factors;
        }
        public Monomial(Flow coefficient, Factor factor, Style style) {
            this.coefficient = coefficient.Normalize(style);
            this.factors = this.coefficient.IsNumber(0.0) ? Factor.nil : Factor.Singleton(factor);
        }
        public Monomial(double coefficient, Factor factor) {
            this.coefficient = new NumberFlow(coefficient);
            this.factors = this.coefficient.IsNumber(0.0) ? Factor.nil : Factor.Singleton(factor);
        }
        public Monomial(Flow coefficient, Lst<Factor> factors, Style style) {
            this.coefficient = coefficient.Normalize(style);
            this.factors = this.coefficient.IsNumber(0.0) ? Factor.nil : factors;
        }
        public bool IsZero() { return coefficient.IsNumber(0.0); } // coefficient is in normal form
        public static Lst<Monomial> nil = new Nil<Monomial>();
        public static Lst<Monomial> Cons(Monomial monomial, Lst<Monomial> monomials) {
            if (monomial.IsZero()) return monomials;
            else return new Cons<Monomial>(monomial, monomials);
        }
        public static Lst<Monomial> Singleton(Monomial m) { return Cons(m, nil); }
        public string Format(Style style) {
            string coeff = this.coefficient.IsNumber(1.0) ? "" : this.coefficient.IsNumber(-1.0) ? "-" : this.coefficient.Format(style);
            string factors = Factor.Format(this.factors, style);
            if (coeff == "" && factors == "1") return "1";
            if (coeff == "-" && factors == "1") return "-1";
            if (factors == "1") return coeff;
            if (coeff == "") return factors;
            if (coeff == "-") return "-" + factors;
            return coeff + "·" + factors;
        }
        public static string Format(Lst<Monomial> monomials, Style style) {
            if (monomials is Nil<Monomial>) return "0";
            else return FormatLst(monomials, style);
        }
        public static string FormatLst(Lst<Monomial> monomials, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return cons.head.Format(style) + ((cons.tail is Nil<Monomial>) ? "" : " + " + FormatLst(cons.tail, style));
            } else return "";
        }

        public bool Hungarian(SpeciesFlow differentiationVariable, Style style) {
            int decide = Polynomial.DecideNonnegative(this.coefficient, style);
            if (decide == 1) return true;
            else if (decide == -1) return Factor.HasFactorVariable(differentiationVariable, this.factors);
            else throw new Error("MassActionCompiler: aborted because it cannot determine the sign of this expression: " + this.coefficient.Format(style));
        }

        public static bool Hungarian(SpeciesFlow differentiationVariable, Lst<Monomial> monomials, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return cons.head.Hungarian(differentiationVariable, style) && Hungarian(differentiationVariable, cons.tail, style);
            } else return true;
        }

        public Monomial Product(Flow coefficient, Style style) {
            return new Monomial(OpFlow.Op(this.coefficient, "*", coefficient), this.factors, style);
        }
        public Monomial Product(Factor factor, Style style) {
            return new Monomial(this.coefficient, Factor.Product(factor, this.factors), style);
        }
        public Monomial Product(Monomial other, Style style) {
            return new Monomial(OpFlow.Op(this.coefficient, "*", other.coefficient), Factor.Product(this.factors, other.factors), style);
        }
        
        public Monomial Power(int power, Style style) {
            return new Monomial(OpFlow.Op(this.coefficient, "^", new NumberFlow(power)), Factor.Power(this.factors, power), style);
        }

        public bool SameFactors(Monomial monomial) {
            return Factor.SameFactors(factors, monomial.factors);
        }

        public static Lst<Monomial> Sum(Monomial monomial, Lst<Monomial> monomials, Style style) {
            if (monomial.IsZero()) return monomials;
            if (monomials is Cons<Monomial> cons) {
                if (cons.head.SameFactors(monomial)) {
                    Flow sumCoeff = OpFlow.Op(cons.head.coefficient, "+", monomial.coefficient).Normalize(style);
                    if (sumCoeff.IsNumber(0.0)) return cons.tail;
                    else return Monomial.Cons(new Monomial(sumCoeff, monomial.factors, style), cons.tail);
                } else return Monomial.Cons(cons.head, Sum(monomial, cons.tail, style));
            } else return Monomial.Singleton(monomial);
        }
        public static Lst<Monomial> Sum(Lst<Monomial> monomials1, Lst<Monomial> monomials2, Style style) {
            //Gui.Log("IN  Sum(Ms[" + Format(monomials1, Style.nil) + "], Ms[" + Format(monomials2, Style.nil) + "])");
            Lst<Monomial> result = null;
            if (monomials1 is Cons<Monomial> cons) {
                result = Sum(cons.head, Sum(cons.tail, monomials2, style), style);
            } else result = monomials2;
            //Gui.Log("OUT Sum(Ms[" + Format(monomials1, Style.nil) + "], Ms[" + Format(monomials2, Style.nil) + "]) = Ms[" + Format(result, Style.nil) + "]");
            return result;
        }

        public static Lst<Monomial> Negate(Lst<Monomial> monomials, Style style) { 
            if (monomials is Cons<Monomial> cons) {
                return Monomial.Cons(cons.head.Product(Flow.minusOne, style), Negate(cons.tail, style));
            } else return Monomial.nil;
        }

        public static Lst<Monomial> Product(Flow coefficient, Lst<Monomial> monomials, Style style)  {
            return Product(Monomial.Singleton(new Monomial(coefficient, style)), monomials, style);
        }
        public static Lst<Monomial> Product(Factor factor1, Factor factor2, Style style)  {
            return Product(factor1, Monomial.Singleton(new Monomial(factor2)), style);
        }
        public static Lst<Monomial> Product(Factor factor, Lst<Monomial> monomials, Style style)  {
            return Product(Monomial.Singleton(new Monomial(factor)), monomials, style);
        }
        public static Lst<Monomial> Product(Lst<Monomial> monomials, Lst<Factor> factors, Style style)  {
            return Product(monomials, Monomial.Singleton(new Monomial(factors)), style);
        }
        public static Lst<Monomial> Product(Monomial monomial, Lst<Monomial> monomials, Style style)  {
            //Gui.Log("IN  Product([" + monomial.Format(style) + "], [" + Format(monomials, style) + "])");
            Lst<Monomial> result;
            if (monomials is Cons<Monomial> cons) {
               result = Sum(new Monomial(OpFlow.Op(cons.head.coefficient, "*", monomial.coefficient), Factor.Product(cons.head.factors, monomial.factors), style), Product(monomial, cons.tail, style), style);
            } else result = Monomial.nil;
            //Gui.Log("OUT Product([" + monomial.Format(style) + "], [" + Format(monomials, style) + "]) = [" + Format(result, style) + "]");
            return result;
        }
        public static Lst<Monomial> Product(Lst<Monomial> monomials1, Lst<Monomial> monomials2, Style style) {
            //Gui.Log("IN  Product([" + Format(monomials1, style) + "], [" + Format(monomials2, style) + "])");
            Lst<Monomial> result;
            if (monomials1 is Cons<Monomial> cons) {
                result = Sum(Product(cons.head, monomials2, style), Product(cons.tail, monomials2, style), style);
            } else result = Monomial.nil;
            //Gui.Log("OUT Product([" + Format(monomials1, style) + "], [" + Format(monomials2, style) + "]) = [" + Format(result, style) + "]");
            return result;
        }

        public static Lst<Monomial> Power(Lst<Monomial> monomials, int power, Style style) {
            if (power == 0) return Singleton(new Monomial(Flow.one, style));
            else if (power == 1) return monomials;
            else return Product(monomials, Power(monomials, power - 1, style), style);
        }

        public Flow ToFlow(Lst<Polynomize.Equation> eqs, Style style) {
            return OpFlow.Op(this.coefficient, "*", Factor.ToFlow(this.factors, eqs, style));
        }

        public static Flow ToFlow(Lst<Monomial> monomials, Lst<Polynomize.Equation> eqs, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return OpFlow.Op(cons.head.ToFlow(eqs, style), "+", ToFlow(cons.tail, eqs, style));
            } else return Flow.zero;
        }
    }

    public class Polynomial {
        public Lst<Monomial> monomials { get; }

        public Polynomial(Monomial monomial) {
            this.monomials = Monomial.Singleton(monomial);
        }
        public Polynomial(Lst<Monomial> monomials) {
            this.monomials = monomials;
        }
        public string Format(Style style) {
            return Monomial.Format(this.monomials, style);
        }

        public static bool IsInt(double r) {
            int n = (int)r;
            return r == n;
        }

        public bool Hungarian(SpeciesFlow differentiationVariable, Style style) {
            return Monomial.Hungarian(differentiationVariable, this.monomials, style);
        }

        public static Polynomial ToPolynomial(Flow flow, Style style) {
            return new Polynomial(ToMonomials(flow, style));
        }

        public static Lst<Monomial> ToMonomials(Flow flow, Style style) {
            if (flow is NumberFlow num) return Monomial.Singleton(new Monomial(new NumberFlow(num.value), style));
            else if (flow is SpeciesFlow species) return Monomial.Singleton(new Monomial(new Factor(species)));
            else if (flow is OpFlow op) {
                if (op.arity == 1) {
                    if (op.op == "-") return ToMonomials(op.args[0], style).Map((Monomial m) => m.Product(Flow.minusOne, style));
                } else if (op.arity == 2) {
                    if (op.op == "+") return Monomial.Sum(ToMonomials(op.args[0], style), ToMonomials(op.args[1], style), style);
                    else if (op.op == "-") return Monomial.Sum(ToMonomials(op.args[0], style), ToMonomials(op.args[1], style).Map((Monomial m) => m.Product(Flow.minusOne, style)), style);
                    else if (op.op == "*") return Monomial.Product(ToMonomials(op.args[0], style), ToMonomials(op.args[1], style), style);
                    else if (op.op == "^") 
                        if (op.args[1] is NumberFlow exp && IsInt(exp.value) && (int)exp.value >= 0) 
                            return Monomial.Power(ToMonomials(op.args[0], style), (int)exp.value, style);
                        else throw new Error("Polynomial.ToMonomials: " + flow.Format(style));
                    else throw new Error("Polynomial.ToMonomials: " + flow.Format(style));
                }
            } throw new Error("Polynomial.ToMonomials: " + flow.Format(style));
        }

        // Whether a *normalized* flow denotes a number that is nonnegative provided that constants within it are nonnegative
        // return 1 : nonnegative is true
        // return 0 : can't tell if nonnegative
        // retunr -1: nonnegative is false (negative is true)
        public static int DecideNonnegative(Flow flow, Style style) { 
            if (flow is NumberFlow num) return (num.value >= 0) ? 1 : -1;
            else if (flow is ConstantFlow) return 1;
            else if (flow is OpFlow op) {
                if (op.op == "+" && op.arity == 2) {
                    int d0 = DecideNonnegative(op.args[0], style);
                    int d1 = DecideNonnegative(op.args[1], style);
                    return (d0 == 0 || d1 == 0) ? 0 : (d0 == 1 && d1 == 1) ? 1 : (d0 == -1 && d1 == -1) ? -1 : 0;
                } else if (op.op == "-" && op.arity == 2) {
                    int d0 = DecideNonnegative(op.args[0], style);
                    int d1 = DecideNonnegative(op.args[1], style);
                    return (d0 == 0 || d1 == 0) ? 0 : (d0 == 1 && d1 == -1) ? 1 : (d0 == -1 && d1 == 1) ? -1 : 0;
                } else if (op.op == "-" && op.arity == 1) {
                    return -1 * DecideNonnegative(op.args[0], style);
                } else if ((op.op == "*" || op.op == "/") && op.arity == 2) {
                    int d0 = DecideNonnegative(op.args[0], style);
                    int d1 = DecideNonnegative(op.args[1], style);
                    return (d0 == 0 || d1 == 0) ? 0 : (d0 == d1) ? 1 : -1;
                }
                else if (op.op == "^" && op.arity == 2) return DecideNonnegative(op.args[0], style);
                else if (op.op == "sqrt" && op.arity == 1) return 1;
                else if (op.op == "exp" && op.arity == 1) return 1;
                else if (op.op == "pos" && op.arity == 1) return 1;
                else return 0;
            } else return 0;
        }
    }

}
