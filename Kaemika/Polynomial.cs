using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    //public abstract class Coefficient {
    //    public abstract string Format(Style style);
    //}

    //public class NumberCoefficient : Coefficient {
    //    double num;
    //    public NumberCoefficient(double num) {
    //        this.num = num;
    //    }
    //    public override string Format(Style style) {
    //        return style.FormatDouble(this.num);
    //    }
    //}

    //public class ConstantCoefficient : Coefficient {
    //    Symbol constant;
    //    public ConstantCoefficient(Symbol constant) {
    //        this.constant = constant;
    //    }
    //    public override string Format(Style style) {
    //        return this.constant.Format(style);
    //    }
    //}

    //public class SumCoefficient : Coefficient {
    //    Coefficient arg0;
    //    Coefficient rht;
    //    public SumCoefficient(Coefficient lft, Coefficient rht) {
    //        this.lft = lft;
    //        this.rht = rht;
    //    }
    //    public override string Format(Style style) {
    //        return lft.Format(style) + "+" + rht.Format(style);
    //    }
    //}

    //public class MinusCoefficient : Coefficient {
    //}

    //public class ProductCoefficient : Coefficient {
    //}


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

        public double Eval(Lst<Polynomize.Equation> eqs, Style style) {
            return Math.Pow(Polynomize.Lookup(this.variable, eqs, style).Eval(eqs, style), this.power);
        }
        public static double Eval(Lst<Factor> factors, Lst<Polynomize.Equation> eqs, Style style) {
            if (factors is Cons<Factor> cons) {
                return cons.head.Eval(eqs, style) * Eval(cons.tail, eqs, style);
            } else return 1.0;
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

        //public static Complex ToComplex(Lst<Factor> factors) {
        //    if (factors is Cons<Factor> cons) {
        //        return new SumComplex(new Simplex(new NumberLiteral(this.power), new Variable(variable.???)));
        //    } else return new Simplex(null, null);
        //}
    }

    public class Monomial {
        public double coefficient { get; }
        public Lst<Factor> factors { get; }

        public Monomial(double coefficient) {
            this.coefficient = coefficient;
            this.factors = Factor.nil;
        }
        public Monomial(Factor factor) {
            this.coefficient = 1.0;
            this.factors = Factor.Singleton(factor);
        }
        public Monomial(Lst<Factor> factors) {
            this.coefficient = 1.0;
            this.factors = factors;
        }
        public Monomial(double coefficient, Factor factor) {
            this.coefficient = coefficient;
            this.factors = (coefficient == 0.0) ? Factor.nil : Factor.Singleton(factor);
        }
        public Monomial(double coefficient, Lst<Factor> factors) {
            this.coefficient = coefficient;
            this.factors = (coefficient == 0.0) ? Factor.nil : factors;
        }
        public bool IsZero() { return coefficient == 0.0; }
        public static Lst<Monomial> nil = new Nil<Monomial>();
        public static Lst<Monomial> Cons(Monomial monomial, Lst<Monomial> monomials) {
            if (monomial.IsZero()) return monomials;
            else return new Cons<Monomial>(monomial, monomials);
        }
        public static Lst<Monomial> Singleton(Monomial m) { return Cons(m, nil); }
        public string Format(Style style) {
            string coeff = (this.coefficient == 1.0) ? "" : (this.coefficient == -1.0) ? "-" : this.coefficient.ToString();
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
        public Flow ToFlow() {
            if (factors is Nil<Factor>) return new NumberFlow(coefficient);
            if (coefficient is 1.0) return Factor.ToFlow(factors);
            return OpFlow.Op(new NumberFlow(coefficient), "*", Factor.ToFlow(factors));
        }
        public static Flow ToFlow(Lst<Monomial> monomials) {
            if (monomials is Cons<Monomial> cons) {
                return cons.tail is Nil<Monomial> ? cons.head.ToFlow() : OpFlow.Op(cons.head.ToFlow(), "+", ToFlow(cons.tail));
            } else return new NumberFlow(0.0);
        }

        public bool Hungarian(SpeciesFlow differentiationVariable) {
            if (this.coefficient >= 0) return true;
            else return Factor.HasFactorVariable(differentiationVariable, this.factors);
        }

        public static bool Hungarian(SpeciesFlow differentiationVariable, Lst<Monomial> monomials) {
            if (monomials is Cons<Monomial> cons) {
                return cons.head.Hungarian(differentiationVariable) && Hungarian(differentiationVariable, cons.tail);
            } else return true;
        }

        public Monomial Product(double coefficient) {
            double productCoeff = this.coefficient * coefficient;
            if (productCoeff == 0.0) return new Monomial(0.0, Factor.nil);
            else return new Monomial(productCoeff, this.factors);
        }
        public Monomial Product(Factor factor) {
            return new Monomial(this.coefficient, Factor.Product(factor, this.factors));
        }
        public Monomial Product(Monomial other) {
            return new Monomial(this.coefficient * other.coefficient, Factor.Product(this.factors, other.factors));
        }
        
        public Monomial Power(int power) {
            return new Monomial(Math.Pow(this.coefficient, (double)power), Factor.Power(this.factors, power));
        }

        public bool SameFactors(Monomial monomial) {
            return Factor.SameFactors(factors, monomial.factors);
        }

        public static Lst<Monomial> Sum(Monomial monomial, Lst<Monomial> monomials) {
            if (monomial.IsZero()) return monomials;
            if (monomials is Cons<Monomial> cons) {
                if (cons.head.SameFactors(monomial)) {
                    double sumCoeff = cons.head.coefficient + monomial.coefficient;
                    if (sumCoeff == 0.0) return cons.tail;
                    else return Monomial.Cons(new Monomial(sumCoeff, monomial.factors), cons.tail);
                } else return Monomial.Cons(cons.head, Sum(monomial, cons.tail));
            } else return Monomial.Singleton(monomial);
        }
        public static Lst<Monomial> Sum(Lst<Monomial> monomials1, Lst<Monomial> monomials2) {
            //Gui.Log("IN  Sum(Ms[" + Format(monomials1, Style.nil) + "], Ms[" + Format(monomials2, Style.nil) + "])");
            Lst<Monomial> result = null;
            if (monomials1 is Cons<Monomial> cons) {
                result = Sum(cons.head, Sum(cons.tail, monomials2));
            } else result = monomials2;
            //Gui.Log("OUT Sum(Ms[" + Format(monomials1, Style.nil) + "], Ms[" + Format(monomials2, Style.nil) + "]) = Ms[" + Format(result, Style.nil) + "]");
            return result;
        }

        public static Lst<Monomial> Negate(Lst<Monomial> monomials) { 
            if (monomials is Cons<Monomial> cons) {
                return Monomial.Cons(cons.head.Product(-1.0), Negate(cons.tail));
            } else return Monomial.nil;
        }

        public static Lst<Monomial> Product(double coefficient, Lst<Monomial> monomials, Style style)  {
            return Product(Monomial.Singleton(new Monomial(coefficient)), monomials, style);
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
               result = Sum(new Monomial(cons.head.coefficient * monomial.coefficient, Factor.Product(cons.head.factors, monomial.factors)), Product(monomial, cons.tail, style));
            } else result = Monomial.nil;
            //Gui.Log("OUT Product([" + monomial.Format(style) + "], [" + Format(monomials, style) + "]) = [" + Format(result, style) + "]");
            return result;
        }
        public static Lst<Monomial> Product(Lst<Monomial> monomials1, Lst<Monomial> monomials2, Style style) {
            //Gui.Log("IN  Product([" + Format(monomials1, style) + "], [" + Format(monomials2, style) + "])");
            Lst<Monomial> result;
            if (monomials1 is Cons<Monomial> cons) {
                result = Sum(Product(cons.head, monomials2, style), Product(cons.tail, monomials2, style));
            } else result = Monomial.nil;
            //Gui.Log("OUT Product([" + Format(monomials1, style) + "], [" + Format(monomials2, style) + "]) = [" + Format(result, style) + "]");
            return result;
        }

        public static Lst<Monomial> Power(Lst<Monomial> monomials, int power, Style style) {
            if (power == 0) return Singleton(new Monomial(1.0));
            else if (power == 1) return monomials;
            else return Product(monomials, Power(monomials, power - 1, style), style);
        }

        public double Eval(Lst<Polynomize.Equation> eqs, Style style) {
            return this.coefficient * Factor.Eval(this.factors, eqs, style);
        }

        public static double Eval(Lst<Monomial> monomials, Lst<Polynomize.Equation> eqs, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return cons.head.Eval(eqs, style) + Eval(cons.tail, eqs, style);
            } else return 0.0;
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
        public Flow ToFlow() {
            return Monomial.ToFlow(this.monomials);
        }

        public static bool IsInt(double r) {
            int n = (int)r;
            return r == n;
        }

        public bool Hungarian(SpeciesFlow differentiationVariable) {
            return Monomial.Hungarian(differentiationVariable, this.monomials);
        }

        public static Polynomial ToPolynomial(Flow flow, Style style) {
            return new Polynomial(ToMonomials(flow, style));
        }

        public static Lst<Monomial> ToMonomials(Flow flow, Style style) {
            if (flow is NumberFlow num) return Monomial.Singleton(new Monomial(num.value));
            else if (flow is SpeciesFlow species) return Monomial.Singleton(new Monomial(new Factor(species)));
            else if (flow is OpFlow op) {
                if (op.arity == 1) {
                    if (op.op == "-") return ToMonomials(op.args[0], style).Map((Monomial m) => m.Product(-1.0));
                } else if (op.arity == 2) {
                    if (op.op == "+") return Monomial.Sum(ToMonomials(op.args[0], style), ToMonomials(op.args[1], style));
                    else if (op.op == "-") return Monomial.Sum(ToMonomials(op.args[0], style), ToMonomials(op.args[1], style).Map((Monomial m) => m.Product(-1.0)));
                    else if (op.op == "*") return Monomial.Product(ToMonomials(op.args[0], style), ToMonomials(op.args[1], style), style);
                    else if (op.op == "^") 
                        if (op.args[1] is NumberFlow exp && IsInt(exp.value) && (int)exp.value >= 0) 
                            return Monomial.Power(ToMonomials(op.args[0], style), (int)exp.value, style);
                        else throw new Error("Polynomial.ToMonomials: " + flow.Format(style));
                    else throw new Error("Polynomial.ToMonomials: " + flow.Format(style));
                }
            } throw new Error("Polynomial.ToMonomials: " + flow.Format(style));
        }
    }


}
