using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    public abstract class Hungarize {

        public static CRN MassCompileCRN(CRN crn, Style style){
            (Lst<Polynomize.ODE> odes, Lst<Polynomize.Equation> eqs) = Polynomize.FromCRN(crn);
            (Lst<Polynomize.PolyODE> polyOdes, Lst<Polynomize.Equation> polyEqs) = Polynomize.PolynomizeODEs(odes, eqs.Reverse(), style);
            Gui.Log("Polynomize:" + Environment.NewLine + Polynomize.PolyODE.Format(polyOdes, style)
                + "Initial:" + Environment.NewLine + Polynomize.Equation.Format(polyEqs, style));

            (Lst<Polynomize.PolyODE> posOdes, Lst<Polynomize.Equation> posEqs, Dictionary <Symbol, SpeciesFlow> dict, Lst<Positivize.Subst> substs) = Positivize.PositivizeODEs(polyOdes, polyEqs, style);
            Gui.Log("Positivize:" + Environment.NewLine + Polynomize.PolyODE.Format(posOdes, style)
                + "Initial:" + Environment.NewLine + Polynomize.Equation.Format(posEqs, style));

            Lst<ReactionValue> reactions = Hungarize.ToReactions(posOdes, style);
            Gui.Log("Hungarize:" + Environment.NewLine + reactions.FoldR((r, s) => { return r.Format(style) + Environment.NewLine + s; }, "")
                + "Initial:" + Environment.NewLine + Polynomize.Equation.Format(posEqs, style));

            CRN crnOut = Hungarize.ToCRN(posOdes, posEqs, style);
            Gui.Log(Environment.NewLine + crnOut.Format(style) + Environment.NewLine);

            return crnOut;
        }

        public static SampleValue MassCompileSample(Symbol outSymbol, SampleValue inSample, Netlist netlist, Style style) {
            //inSample.Consume(null, 0, null, netlist, style); // this prevents simulating a sample and its massaction version in succession
            List<ReactionValue> inReactions = inSample.RelevantReactions(netlist, style);
            CRN inCrn = new CRN(inSample, inReactions);
            Gui.Log(Environment.NewLine + inCrn.FormatNice(style));

            (Lst <Polynomize.ODE> odes, Lst<Polynomize.Equation> eqs) = Polynomize.FromCRN(inCrn);
            (Lst <Polynomize.PolyODE> polyOdes, Lst<Polynomize.Equation> polyEqs) = Polynomize.PolynomizeODEs(odes, eqs.Reverse(), style);
            Gui.Log("Polynomize:" + Environment.NewLine + Polynomize.PolyODE.Format(polyOdes, style)
                + "Initial:" + Environment.NewLine + Polynomize.Equation.Format(polyEqs, style));

            (Lst<Polynomize.PolyODE> posOdes, Lst<Polynomize.Equation> posEqs, Dictionary<Symbol, SpeciesFlow> dict, Lst<Positivize.Subst> substs) = Positivize.PositivizeODEs(polyOdes, polyEqs, style);
            Gui.Log("Positivize:" + Environment.NewLine + Polynomize.PolyODE.Format(posOdes, style)
                + "Initial:" + Environment.NewLine + Polynomize.Equation.Format(posEqs, style));

            Lst<ReactionValue> outReactions = Hungarize.ToReactions(posOdes, style);
            Gui.Log("Hungarize:" + Environment.NewLine + outReactions.FoldR((r, s) => { return r.Format(style) + Environment.NewLine + s; }, "")
                + "Initial:" + Environment.NewLine + Polynomize.Equation.Format(posEqs, style));

            SampleValue outSample = new SampleValue(outSymbol, new StateMap(outSymbol, new List<SpeciesValue> { }, new State(0, lna: inSample.stateMap.state.lna)), new NumberValue(inSample.Volume()), new NumberValue(inSample.Temperature()), produced: true);
            posOdes.Each(ode => {
                double init = Polynomize.Equation.Eval(ode.var, posEqs, style);
                if (init < 0) throw new Error("Negative initial value of Polynomized ODE for: " + Polynomize.Lookup(ode.var, eqs, style).Format(style) + " = " + init + Environment.NewLine + Polynomize.Equation.Format(eqs, style));
                outSample.stateMap.AddDimensionedSpecies(new SpeciesValue(ode.var.species, -1.0), init, "M", outSample.Volume(), style);
            });
            outReactions.Each(reaction => { netlist.Emit(new ReactionEntry(reaction)); });

            substs.Each(subst => { ReportEntry report = new ReportEntry(null, OpFlow.Op(subst.plus, "-", subst.minus), null, outSample); outSample.AddReport(report); });
            foreach (KeyValuePair<Symbol, SpeciesFlow> keypair in dict) { ReportEntry report = new ReportEntry(null, keypair.Value, null, outSample); outSample.AddReport(report); };

            return outSample;
        }

        public static CRN ToCRN(Lst<Polynomize.PolyODE> odes, Lst<Polynomize.Equation> eqs, Style style) {
            SampleValue sample = Exec.Vessel();
            odes.Each(ode => {
                double init = Polynomize.Equation.Eval(ode.var, eqs, style);
                if (init < 0) throw new Error("Negative initial value of Polynomized ODE for: " + Polynomize.Lookup(ode.var, eqs, style).Format(style) + " = " + init + Environment.NewLine + Polynomize.Equation.Format(eqs, style));
                sample.stateMap.AddDimensionedSpecies(new SpeciesValue(ode.var.species, -1.0), init, "M", sample.Volume(), style); 
            });
            List<ReactionValue> reactions = ToReactions(odes, style).ToList();
            return new CRN(sample, reactions);
        }

        public static Lst<ReactionValue> ToReactions(Lst<Polynomize.PolyODE> odes, Style style) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                return ToReactions(cons.head.var, cons.head.poly.monomials, ToReactions(cons.tail, style), style);
            } else return new Nil<ReactionValue>();
        }

        public static Lst<ReactionValue> ToReactions(SpeciesFlow variable, Lst<Monomial> monomials, Lst<ReactionValue> rest, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return ToReactions(variable, cons.head, ToReactions(variable, cons.tail, rest, style), style);
            } else return rest;
        }

        public static Lst<ReactionValue> ToReactions(SpeciesFlow variable, Monomial monomial, Lst<ReactionValue> rest, Style style) {
            if (monomial.coefficient == 0) {
                return rest;
            } else if (monomial.coefficient > 0) {
                return new Cons<ReactionValue>(
                    new ReactionValue(
                        Factor.ToList(monomial.factors),
                        Factor.ToList(Factor.Product(new Factor(variable, 1), monomial.factors)),
                        new MassActionNumericalRate(monomial.coefficient)),
                    rest);
            } else {
                return new Cons<ReactionValue>(
                    new ReactionValue(
                        Factor.ToList(monomial.factors),
                        Factor.ToList(Factor.Quotient(monomial.factors, new Factor(variable, 1), style)),
                        new MassActionNumericalRate(-monomial.coefficient)),
                rest);
            }
        }





        //public static Lst<ReactionDefinition> ToReactions(Lst<Positivize.ODE> odes, Style style) {
        //    if (odes is Cons<Positivize.ODE> cons) {
        //        return ToReactions(cons.head.var, cons.head.poly.monomials, ToReactions(cons.tail, style), style);
        //    } else return new Nil<ReactionDefinition>();
        //}

        //public static Lst<ReactionDefinition> ToReactions(SpeciesFlow variable, Lst<Monomial> monomials, Lst<ReactionDefinition> rest, Style style) {
        //    if (monomials is Cons<Monomial> cons) {
        //        return ToReactions(variable, cons.head, ToReactions(variable, cons.tail, rest, style), style);
        //    } else return rest;
        //}

        //public static Lst<ReactionDefinition> ToReactions(SpeciesFlow variable, Monomial monomial, Lst<ReactionDefinition> rest, Style style) {
        //    if (monomial.coefficient >= 0) {
        //        return new Cons<ReactionDefinition>(
        //            new ReactionDefinition(
        //                Factor.ToList(monomial.factors),
        //                Factor.ToList(Factor.Product(new Factor(variable, 1), monomial.factors)),
        //                new MassActionRate(new NumberLiteral(monomial.coefficient))),
        //            rest);
        //    } else {
        //        return new Cons<ReactionDefinition>(
        //            new ReactionDefinition(
        //                Factor.ToList(monomial.factors),
        //                Factor.ToList(Factor.Quotient(monomial.factors, new Factor(variable, 1), style)),
        //                new MassActionRate(new NumberLiteral(monomial.coefficient))),
        //        rest);
        //    }
        //}


    }
}
