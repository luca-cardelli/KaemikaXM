using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    public abstract class Positivize {

        // ===== Equations ===== (evaluated version of Polynomize.Equations)

        public class Equation {
            public SpeciesFlow var;
            public double value;
            public Equation(SpeciesFlow var, double value) {
                this.var = var;
                this.value = value;
            }
            public string Format(Style style) {
                return var.Format(style) + " = " + style.FormatDouble(this.value);
            }
            public static string Format(Lst<Equation> eqs, Style style) {
                if (eqs is Cons<Equation> cons) {
                    return cons.head.Format(style) + Environment.NewLine + Format(cons.tail, style);
                } else return "";
            }
        }

        // ===== Separate =====

        public static (Lst<Monomial> positive, Lst<Monomial> negative) Separate(Lst<Monomial> monomials) {
            if (monomials is Cons<Monomial> cons) {
                (Lst<Monomial> positive, Lst<Monomial> negative) = Separate(cons.tail);
                if (cons.head.coefficient >= 0) return (Monomial.Cons(cons.head, positive), negative);
                else return (positive, Monomial.Cons(new Monomial(-cons.head.coefficient, cons.head.factors), negative));
            } else return (Monomial.nil, Monomial.nil);
        }

        // ===== Substitute =====

        public class Subst {
            public SpeciesFlow var;
            public SpeciesFlow plus;
            public SpeciesFlow minus;
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
                if (cons.head.Hungarian(cons.head.var) && Polynomize.Equation.Eval(cons.head.var, eqs, style) >= 0) return NecessarySubsts(cons.tail, eqs, style);
                else return new Cons<Subst>(new Subst(cons.head.var, style), NecessarySubsts(cons.tail, eqs, style));
            } else return Subst.nil;
        }

        // substitute x with (x+ - x-) in a flow that is already in polynomial form as a result of Polynomize

        public static Lst<Polynomize.PolyODE> Substitute(Lst<Polynomize.PolyODE> odes, Lst<Subst> substs, Style style) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                Polynomize.PolyODE ode = cons.head;
                Subst subst = Lookup(ode.var, substs);
                if (subst == null) {
                    return new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(ode.var, Substitute(ode.poly, substs, style)),
                        Substitute(cons.tail, substs, style));
                } else {
                    throw new Error("Polynomize.Substitute"); // this should never happen because we split ODEs in PositivizeODEs
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
                return Monomial.Sum(Substitute(cons.head, substs, style), Substitute(cons.tail, substs, style));
            } else return Monomial.nil;
        }

        public static Lst<Monomial> Substitute(Monomial monomial, Lst<Subst> substs, Style style) {
            return Monomial.Product(new Monomial(monomial.coefficient), Substitute(monomial.factors, substs, style), style);
        }

        public static Lst<Monomial> Substitute(Lst<Factor> factors, Lst<Subst> substs, Style style) {
            if (factors is Cons<Factor> cons) {
                return Monomial.Product(Substitute(cons.head, substs, style), Substitute(cons.tail, substs, style), style);
            } else return Monomial.Singleton(new Monomial(1.0));
        }

        public static Lst<Monomial> Substitute(Factor factor, Lst<Subst> substs, Style style) {
            Subst subst = Lookup(factor.variable, substs);
            if (subst == null) return Monomial.Singleton(new Monomial(factor));
            else return Monomial.Power(Monomial.Cons(new Monomial(new Factor(subst.plus)), Monomial.Singleton(new Monomial(-1.0, new Factor(subst.minus)))), factor.power, style);
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
                        new Cons<Polynomize.Equation>(new Polynomize.Equation(subst.plus, eq.op, args, splitOp: "+"),
                            new Cons<Polynomize.Equation>(new Polynomize.Equation(subst.minus, eq.op, args, splitOp: "-"),
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
            return (Rename(dict, odes), Rename(dict, eqs), dict);
        }

        public static Lst<Polynomize.PolyODE> Rename(Dictionary<Symbol, SpeciesFlow> dict, Lst<Polynomize.PolyODE> odes) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                return new Cons<Polynomize.PolyODE>(Rename(dict, cons.head), Rename(dict, cons.tail));
            } else return Polynomize.PolyODE.nil;
        }

        public static Polynomize.PolyODE Rename(Dictionary<Symbol, SpeciesFlow> dict, Polynomize.PolyODE ode) {
            return new Polynomize.PolyODE(
                dict.ContainsKey(ode.var.species) ? dict[ode.var.species] : ode.var,
                new Polynomial(Rename(dict, ode.poly.monomials))); ;
        }

        public static Lst<Monomial> Rename(Dictionary<Symbol, SpeciesFlow> dict, Lst<Monomial> monomials) {
            if (monomials is Cons<Monomial> cons) {
                return Monomial.Cons(Rename(dict, cons.head), Rename(dict, cons.tail));
            } else return Monomial.nil;
        }

        public static Monomial Rename(Dictionary<Symbol, SpeciesFlow> dict, Monomial monomial) {
            return new Monomial(monomial.coefficient, Rename(dict, monomial.factors));
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

        public static Lst<Polynomize.Equation> Rename(Dictionary<Symbol, SpeciesFlow> dict, Lst<Polynomize.Equation> eqs) {
            if (eqs is Cons<Polynomize.Equation> cons) {
                return new Cons<Polynomize.Equation>(Rename(dict, cons.head), Rename(dict, cons.tail));
            } else return Polynomize.Equation.nil;
        }

        public static Polynomize.Equation Rename(Dictionary<Symbol, SpeciesFlow> dict, Polynomize.Equation eq) {
            Lst<Monomial>[] args = new Lst<Monomial>[eq.args.Length];
            for (int i = 0; i < eq.args.Length; i++) args[i] = Rename(dict, eq.args[i]);
            if (dict.ContainsKey(eq.var.species) && eq.splitOp != "") throw new Error("Positivize.Rename");
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
            Lst<Subst> substs = Subst.nil;
            while (true) {
                Lst<Subst> newSubsts = NecessarySubsts(resOdes, resEqs, style);
                //Gui.Log("PolyODEs:" + Environment.NewLine + Polynomize.Format(unsplit.Append(split), style));
                //Gui.Log("NecessarySubsts:" + Environment.NewLine + Format(substs, style));
                if (newSubsts is Nil<Subst>) { // we converged
                    Lst<SpeciesFlow> unsplitVars = UnsplitVars(odes, substs);
                    (Lst<Polynomize.PolyODE> renOdes, Lst<Polynomize.Equation> renEqs, Dictionary<Symbol, SpeciesFlow> dict) = Rename(unsplitVars, resOdes, resEqs, style);
                    return (renOdes, renEqs, dict, substs);
                    //return (resOdes, resEqs);
                }
                substs = newSubsts.Append(substs);
                resOdes = PositivizeODEs(odes, substs, style);
                resEqs = Substitute(eqs, substs, style);
            }
        }

        public static Lst<Polynomize.PolyODE> PositivizeODEs(Lst<Polynomize.PolyODE> odes, Lst<Subst> substs, Style style) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                Lst<Polynomize.PolyODE> tail = PositivizeODEs(cons.tail, substs, style);
                Polynomize.PolyODE ode = cons.head;
                Polynomial poly = Substitute(ode.poly, substs, style);
                Subst subst = Lookup(ode.var, substs);
                if (subst == null) { // we do not split this ODE
                    return new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(ode.var, poly), tail);
                } else { // we split this ODE
                    (Lst<Monomial> positive, Lst<Monomial> negative) = Separate(poly.monomials);
                    Monomial damp = new Monomial(-1.0, Factor.Cons(new Factor(subst.plus, 2), Factor.Singleton(new Factor(subst.minus, 2))));
                    return new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.plus, Monomial.Cons(damp, positive)),
                           new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.minus, Monomial.Cons(damp, negative)),
                           tail));
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

        //public static (Lst<Polynomize.PolyODE> posOdes, Lst<Polynomize.Equation> posEqs) PositivizeODEs(Lst<Polynomize.PolyODE> odes, Lst<Polynomize.Equation> eqs, Style style) {
        //    // We could split all variables to be sure, but we try to be clever:
        //    // Split only those variables that have non-Hungarian monomials.
        //    // However, substitution of the split variables may introduce new non-Hungarian monomials, so we need to iterate until closure.
        //    // Moreover, some non-split variables may get initialized to negative values (consider #->x{{log(x)}} with x0=0.9, whose polynomization is Hungarian but log(x0)<0), 
        //    // so they must be split too even if their monomials are Hungarian
        //    Lst<Polynomize.PolyODE> unsplit = odes;
        //    Lst<Polynomize.PolyODE> split = Polynomize.PolyODE.nil;
        //    Lst<Polynomize.Equation> posEqs = eqs;
        //    while (true) {
        //        Lst<Subst> newSubsts = NecessarySubsts(unsplit, posEqs, style);
        //        //Gui.Log("PolyODEs:" + Environment.NewLine + Polynomize.Format(unsplit.Append(split), style));
        //        //Gui.Log("NecessarySubsts:" + Environment.NewLine + Format(substs, style));
        //        if (newSubsts is Nil<Subst>) { // we converged
        //            Lst<SpeciesFlow> unsplitVars = Polynomize.PolyODE.Variables(unsplit);
        //            (Lst<Polynomize.PolyODE> renOdes, Lst<Polynomize.Equation> renEqs) = Rename(unsplitVars, unsplit.Append(split), posEqs, style);
        //            return (renOdes, renEqs);
        //        }
        //        (Lst<Polynomize.PolyODE> unsplit1, Lst<Polynomize.PolyODE> split1) = PositivizeODEs(unsplit, newSubsts, style);
        //        split = Substitute(split, newSubsts, style).Append(split1); // newSubsts should not contain any of the ODE head variables of split
        //        unsplit = unsplit1;
        //        posEqs = Substitute(posEqs, newSubsts, style);
        //    }
        //}

        //public static (Lst<Polynomize.PolyODE> unsplit, Lst<Polynomize.PolyODE> split) PositivizeODEs(Lst<Polynomize.PolyODE> odes, Lst<Subst> substs, Style style) {
        //    if (odes is Cons<Polynomize.PolyODE> cons) {
        //        (Lst<Polynomize.PolyODE> unsplitTail, Lst<Polynomize.PolyODE> splitTail) = PositivizeODEs(cons.tail, substs, style);
        //        Polynomize.PolyODE ode = cons.head;
        //        Polynomial poly = Substitute(ode.poly, substs, style);
        //        Subst subst = Lookup(ode.var, substs);
        //        if (subst == null) { // we do not split this ODE
        //            return
        //                (new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(ode.var, poly), unsplitTail),
        //                 splitTail);
        //        } else { // we split this ODE
        //            (Polynomial positive, Polynomial negative) = Separate(poly);
        //            return
        //                (unsplitTail,
        //                 new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.plus, positive),
        //                    new Cons<Polynomize.PolyODE>(new Polynomize.PolyODE(subst.minus, negative),
        //                        splitTail)));
        //        }
        //    } else return (Polynomize.PolyODE.nil, Polynomize.PolyODE.nil);
        //}

    }
}
