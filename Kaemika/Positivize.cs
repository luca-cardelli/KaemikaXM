using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    public abstract class Positivize {

        // ===== Separate =====

        public static (Lst<Monomial> positive, Lst<Monomial> negative) Separate(Lst<Monomial> monomials, Style style) {
            if (monomials is Cons<Monomial> cons) {
                (Lst<Monomial> positive, Lst<Monomial> negative) = Separate(cons.tail, style);
                int decide = Polynomial.DecideNonnegative(cons.head.coefficient, style);
                if (decide == 1) return (Monomial.Cons(cons.head, positive), negative);
                else if (decide == -1) return (positive, Monomial.Cons(new Monomial(OpFlow.Op("-", cons.head.coefficient), cons.head.factors, style), negative));
                else throw new Error("MassActionCompiler: aborted because it cannot determine the sign of this expression: " + cons.head.coefficient.Format(style));
            } else return (Monomial.nil, Monomial.nil);
        }

        // ===== Substitute =====

        public class Subst {
            public SpeciesFlow var;   // variable to substitute with (plus - minus)
            public SpeciesFlow plus;  // plus variant of variable
            public SpeciesFlow minus; // minus variant of variable
            public Subst(SpeciesFlow var, Style style) {
                this.var = var;
                this.plus = new SpeciesFlow(new Symbol(var.species.Format(style) + "⁺"));
                this.minus = new SpeciesFlow(new Symbol(var.species.Format(style) + "⁻"));
            }
            public string Format(Style style) {
                return var.Format(style) + " -> " + plus.Format(style) + " - " + minus.Format(style);
            }
            public static string Format(Lst<Subst> substs, Style style) {
                if (substs is Cons<Subst> cons) return cons.head.Format(style) + Environment.NewLine + Format(cons.tail, style);
                else return "";
            }
            public static Lst<Subst> nil = new Nil<Subst>();
        }

        public static Subst Lookup(SpeciesFlow var, Lst<Subst> substs) { // returns null for not found
            if (substs is Cons<Subst> cons) {
                if (var.SameSpecies(cons.head.var)) return cons.head;
                else return Lookup(var, cons.tail);
            } else return null;
        }

        public static Lst<Subst> NecessarySubsts(Lst<Polynomize.PolyODE> odes, Lst<Polynomize.Equation> eqs, Style style) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                if (cons.head.split != Polynomize.Split.No || // ignore already split odes
                    (cons.head.Hungarian(cons.head.var, style) && // if the polynomial rhs is all Hungarian
                     Polynomial.DecideNonnegative(Polynomize.Equation.ToFlow(cons.head.var, eqs, style).Normalize(style),style) == 1)) // and we are sure the initial value is nonnegative
                    return NecessarySubsts(cons.tail, eqs, style); // then we do not need to split
                else return new Cons<Subst>(new Subst(cons.head.var, style), NecessarySubsts(cons.tail, eqs, style)); // else we split
            } else return Subst.nil;
        }

        // substitute x with (x+ - x-) in a flow that is already in polynomial form as a result of Polynomize
        public static Lst<Polynomize.PolyODE> Substitute(Lst<Polynomize.PolyODE> odes, Lst<Subst> substs, Style style) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                Polynomize.PolyODE ode = cons.head;
                Subst subst = Lookup(ode.var, substs);
                if (subst == null) {
                    return new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(ode.var, Substitute(ode.poly, substs, style), ode.split),
                        Substitute(cons.tail, substs, style));
                } else { // this should never happen because we split ODEs in PositivizeODEs
                    throw new Error("Polynomize.Substitute"); 
                    //return
                    //     new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.plus, Substitute(ode.poly, substs, style)),
                    //        new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.minus, Substitute(ode.poly, substs, style)),
                    //            Substitute(cons.tail, substs, style)));
                }
            } else return Polynomize.PolyODE.nil;
        }

        public static Polynomial Substitute(Polynomial polynomial, Lst<Subst> substs, Style style) {
            return new Polynomial(Substitute(polynomial.monomials, substs, style));
        }

        public static Lst<Monomial> Substitute(Lst<Monomial> monomials, Lst<Subst> substs, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return Monomial.Sum(Substitute(cons.head, substs, style), Substitute(cons.tail, substs, style), style);
            } else return Monomial.nil;
        }

        public static Lst<Monomial> Substitute(Monomial monomial, Lst<Subst> substs, Style style) {
            return Monomial.Product(new Monomial(monomial.coefficient, style), Substitute(monomial.factors, substs, style), style);
        }

        public static Lst<Monomial> Substitute(Lst<Factor> factors, Lst<Subst> substs, Style style) {
            if (factors is Cons<Factor> cons) {
                return Monomial.Product(Substitute(cons.head, substs, style), Substitute(cons.tail, substs, style), style);
            } else return Monomial.Singleton(new Monomial(1.0));
        }

        public static Lst<Monomial> Substitute(Factor factor, Lst<Subst> substs, Style style) {
            Subst subst = Lookup(factor.variable, substs);
            if (subst == null) return Monomial.Singleton(new Monomial(factor));
            else return Monomial.Power(Monomial.Cons(new Monomial(new Factor(subst.plus)), Monomial.Singleton(new Monomial(Flow.minusOne, new Factor(subst.minus), style))), factor.power, style);
        }

        public static Lst<Polynomize.Equation> Substitute(Lst<Polynomize.Equation> eqs, Lst<Subst> substs, Style style) {
            if (eqs is Cons<Polynomize.Equation> cons) {
                Polynomize.Equation eq = cons.head;
                Lst<Monomial>[] args = new Lst<Monomial>[eq.args.Length];
                for (int i = 0; i < eq.args.Length; i++) args[i] = Substitute(eq.args[i], substs, style);
                Subst subst = Lookup(eq.var, substs);
                if (subst == null) {
                    return 
                        new Cons<Polynomize.Equation>(new Polynomize.Equation(eq.var, eq.op, args, eq.splitOp), 
                            Substitute(cons.tail, substs, style));
                } else {
                    return
                        new Cons<Polynomize.Equation>(new Polynomize.Equation(subst.plus, eq.op, args, splitOp: Polynomize.Split.Pos),
                            new Cons<Polynomize.Equation>(new Polynomize.Equation(subst.minus, eq.op, args, splitOp: Polynomize.Split.Neg),
                                Substitute(cons.tail, substs, style)));
                }
            } else return Polynomize.Equation.nil;
        }

        // ===== Rename =====

        // rename variables x (a subset of the ode variables) to x⁰ in odes
        public static (Lst<Polynomize.PolyODE> renOdes, Lst<Polynomize.Equation> renEqs, Dictionary<Symbol, SpeciesFlow> dict) Rename(Lst<SpeciesFlow> vars, Lst<Polynomize.PolyODE> odes, Lst<Polynomize.Equation> eqs, Style style)  {
            Dictionary<Symbol, SpeciesFlow> dict = new Dictionary<Symbol, SpeciesFlow>();
            if (vars is Nil<SpeciesFlow>) return (odes, eqs, dict);
            vars.Each(var => { dict.Add(var.species, new SpeciesFlow(new Symbol(var.species.Format(style) + "⁰"))); });
            return (Rename(dict, odes, style), Rename(dict, eqs, style), dict);
        }

        public static Lst<Polynomize.PolyODE> Rename(Dictionary<Symbol, SpeciesFlow> dict, Lst<Polynomize.PolyODE> odes, Style style) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                return new Cons<Polynomize.PolyODE>(Rename(dict, cons.head, style), Rename(dict, cons.tail, style));
            } else return Polynomize.PolyODE.nil;
        }

        public static Polynomize.PolyODE Rename(Dictionary<Symbol, SpeciesFlow> dict, Polynomize.PolyODE ode, Style style) {
            return new Polynomize.PolyODE(
                dict.ContainsKey(ode.var.species) ? dict[ode.var.species] : ode.var,
                new Polynomial(Rename(dict, ode.poly.monomials, style)), ode.split);
        }

        public static Lst<Monomial> Rename(Dictionary<Symbol, SpeciesFlow> dict, Lst<Monomial> monomials, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return Monomial.Cons(Rename(dict, cons.head, style), Rename(dict, cons.tail, style));
            } else return Monomial.nil;
        }

        public static Monomial Rename(Dictionary<Symbol, SpeciesFlow> dict, Monomial monomial, Style style) {
            return new Monomial(monomial.coefficient, Rename(dict, monomial.factors), style);
        }

        public static Lst<Factor> Rename(Dictionary<Symbol, SpeciesFlow> dict, Lst<Factor> factors) {
            if (factors is Cons<Factor> cons) {
                return Factor.Cons(Rename(dict, cons.head), Rename(dict, cons.tail));
            } else return Factor.nil;
        }

        public static Factor Rename(Dictionary<Symbol, SpeciesFlow> dict, Factor factor) {
            return new Factor(
                dict.ContainsKey(factor.variable.species) ? dict[factor.variable.species] : factor.variable,
                factor.power);
        }

        public static Lst<Polynomize.Equation> Rename(Dictionary<Symbol, SpeciesFlow> dict, Lst<Polynomize.Equation> eqs, Style style) {
            if (eqs is Cons<Polynomize.Equation> cons) {
                return new Cons<Polynomize.Equation>(Rename(dict, cons.head, style), Rename(dict, cons.tail, style));
            } else return Polynomize.Equation.nil;
        }

        public static Polynomize.Equation Rename(Dictionary<Symbol, SpeciesFlow> dict, Polynomize.Equation eq, Style style) {
            Lst<Monomial>[] args = new Lst<Monomial>[eq.args.Length];
            for (int i = 0; i < eq.args.Length; i++) args[i] = Rename(dict, eq.args[i], style);
            if (dict.ContainsKey(eq.var.species) && eq.splitOp != Polynomize.Split.No) throw new Error("Positivize.Rename");
            SpeciesFlow newVar = dict.ContainsKey(eq.var.species) ? dict[eq.var.species] : eq.var;
            return new Polynomize.Equation(newVar, eq.op, args, eq.splitOp);
        }

        // ===== Positivize =====

        public static (Lst<Polynomize.PolyODE> posOdes, Lst<Polynomize.Equation> posEqs, Dictionary<Symbol, SpeciesFlow> dict, Lst<Subst> substs) PositivizeODEs(Lst<Polynomize.PolyODE> odes, Lst<Polynomize.Equation> eqs, Style style) {
            // We could split all variables to be sure, but we try to be clever:
            // Split only those variables that have non-Hungarian monomials.
            // However, substitution of the split variables may introduce new non-Hungarian monomials, so we need to iterate until closure.
            // Moreover, some non-split variables may get initialized to negative values (consider #->x{{log(x)}} with x0=0.9, whose polynomization is Hungarian but log(x0)<0), 
            // so they must be split too even if their monomials are Hungarian
            Lst<Polynomize.PolyODE> resOdes = odes;
            Lst<Polynomize.Equation> resEqs = eqs;
            Lst<Subst> accumulatedSubsts = Subst.nil;
            while (true) {
                Lst<Subst> newSubsts = NecessarySubsts(resOdes, resEqs, style); // it will ignore already split odes
                //Gui.Log("PolyODEs:" + Environment.NewLine + Polynomize.Format(unsplit.Append(split), style));
                //Gui.Log("NecessarySubsts:" + Environment.NewLine + Format(substs, style));
                if (newSubsts is Nil<Subst>) { // we converged
                    Lst<SpeciesFlow> unsplitVars = UnsplitVars(odes, accumulatedSubsts);
                    (Lst<Polynomize.PolyODE> renOdes, Lst<Polynomize.Equation> renEqs, Dictionary<Symbol, SpeciesFlow> dict) = Rename(unsplitVars, resOdes, resEqs, style);
                    return (renOdes, renEqs, dict, accumulatedSubsts);
                }
                accumulatedSubsts = newSubsts.Append(accumulatedSubsts);
                resOdes = PositivizeODEs(odes, accumulatedSubsts, style);
                resEqs = Substitute(eqs, accumulatedSubsts, style);
            }
        }

        public static Lst<Polynomize.PolyODE> PositivizeODEs(Lst<Polynomize.PolyODE> odes, Lst<Subst> substs, Style style) {
            //const int annihilationOrder = 1; // generation of annihilation reactions has been moved to Hungarize, otherwise two reactions are generated instead of one from the annihilation monomial
            if (odes is Cons<Polynomize.PolyODE> cons) {
                Lst<Polynomize.PolyODE> tail = PositivizeODEs(cons.tail, substs, style);
                Polynomize.PolyODE ode = cons.head;
                Polynomial poly = Substitute(ode.poly, substs, style);
                Subst subst = Lookup(ode.var, substs);
                if (subst == null) { // we do not split this ODE
                    return new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(ode.var, poly, split:Polynomize.Split.No), tail);
                } else { // we split this ODE
                    (Lst<Monomial> positive, Lst<Monomial> negative) = Separate(poly.monomials, style);
                    return new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.plus, positive, split: Polynomize.Split.Pos),
                           new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.minus, negative, split: Polynomize.Split.Neg),
                           tail));
                    //Monomial damp = new Monomial(Flow.minusOne, Factor.Cons(new Factor(subst.plus, annihilationOrder), Factor.Singleton(new Factor(subst.minus, annihilationOrder))), style); // annihilation monomial
                    //return new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.plus, Monomial.Cons(damp, positive), split:Polynomize.Split.Pos),
                    //       new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.minus, Monomial.Cons(damp, negative), split:Polynomize.Split.Neg),
                    //       tail));
                }
            } else return Polynomize.PolyODE.nil;
        }

        public static Lst<SpeciesFlow> UnsplitVars(Lst<Polynomize.PolyODE> odes, Lst<Subst> substs) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                Lst<SpeciesFlow> tail = UnsplitVars(cons.tail, substs); 
                Subst subst = Lookup(cons.head.var, substs);
                if (subst == null) return new Cons<SpeciesFlow>(cons.head.var, tail);
                else return tail;
            } else return new Nil<SpeciesFlow>();
        }
    }
}
