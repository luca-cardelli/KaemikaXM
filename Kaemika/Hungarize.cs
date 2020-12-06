using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    public abstract class Hungarize {

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
            Gui.Log("Hungarize:" + Environment.NewLine + outReactions.FoldR((r, s) => { return r.FormatNormal(style) + Environment.NewLine + s; }, "")
                + "Initial:" + Environment.NewLine + Polynomize.Equation.Format(posEqs, style));

            SampleValue outSample = new SampleValue(outSymbol, new StateMap(outSymbol, new List<SpeciesValue> { }, new State(0, lna: inSample.stateMap.state.lna)), new NumberValue(inSample.Volume()), new NumberValue(inSample.Temperature()), produced: true);
            netlist.Emit(new SampleEntry(outSample));
            posOdes.Each(ode => {
                Flow initFlow = Polynomize.Equation.ToFlow(ode.var, posEqs, style).Normalize(style);
                double init; if (initFlow is NumberFlow num) init = num.value; else throw new Error("Cannot generate a simulatable sample because initial values contain constants (but the symbolic version has been generated assuming constants are nonnegative).");
                if (init < 0) throw new Error("Negative initial value of Polynomized ODE for: " + Polynomize.Lookup(ode.var, eqs, style).Format(style) + " = " + init + Environment.NewLine + Polynomize.Equation.Format(eqs, style));
                outSample.stateMap.AddDimensionedSpecies(new SpeciesValue(ode.var.species, -1.0), init, 0.0, "M", outSample.Volume(), style);
            });
            outReactions.Each(reaction => { netlist.Emit(new ReactionEntry(reaction)); });

            substs.Each(subst => { ReportEntry report = new ReportEntry(null, OpFlow.Op(subst.plus, "-", subst.minus), null, outSample); outSample.AddReport(report); });
            foreach (KeyValuePair<Symbol, SpeciesFlow> keypair in dict) { ReportEntry report = new ReportEntry(null, keypair.Value, null, outSample); outSample.AddReport(report); };

            return outSample;
        }

        public static Lst<ReactionValue> ToReactions(Lst<Polynomize.PolyODE> odes, Style style) {
            if (odes is Cons<Polynomize.PolyODE> cons) {
                Lst<ReactionValue> reactions = ToReactions(cons.head.var, cons.head.poly.monomials, ToReactions(cons.tail, style), style);
                if (cons.head.split == Polynomize.Split.Pos) {
                    SpeciesFlow pos = cons.head.var;
                    SpeciesFlow neg = (cons.tail is Cons<Polynomize.PolyODE> tailCons && tailCons.head.split == Polynomize.Split.Neg) ? tailCons.head.var : throw new Error("ToReactions annihilation");
                    ReactionValue annihil = new ReactionValue(new List<Symbol> { pos.species, neg.species }, new List<Symbol> { }, new MassActionNumericalRate(1.0));
                    //ReactionValue annihil = new ReactionValue(new List<Symbol> { pos.species, pos.species, neg.species, neg.species }, new List<Symbol> { }, new MassActionNumericalRate(1.0));
                    return new Cons<ReactionValue>(annihil, reactions);
                } else return reactions;
            } else return new Nil<ReactionValue>();
        }

        public static Lst<ReactionValue> ToReactions(SpeciesFlow variable, Lst<Monomial> monomials, Lst<ReactionValue> rest, Style style) {
            if (monomials is Cons<Monomial> cons) {
                return ToReactions(variable, cons.head, ToReactions(variable, cons.tail, rest, style), style);
            } else return rest;
        }

        public static Lst<ReactionValue> ToReactions(SpeciesFlow variable, Monomial monomial, Lst<ReactionValue> rest, Style style) {
            if (monomial.IsZero()) {
                return rest;
            } else {
                int decide = Polynomial.DecideNonnegative(monomial.coefficient, style);
                if (decide == 1) { // positive monomials
                    RateValue rate;
                    if (monomial.coefficient is NumberFlow num) rate = new MassActionNumericalRate(num.value);
                    else rate = new MassActionFlowRate(monomial.coefficient);
                    return new Cons<ReactionValue>(
                        new ReactionValue(
                            Factor.ToList(monomial.factors),
                            Factor.ToList(Factor.Product(new Factor(variable, 1), monomial.factors)),
                            rate),
                        rest);
                } else if (decide == -1) { // negative monomials
                    RateValue rate;
                    if (monomial.coefficient is NumberFlow num) rate = new MassActionNumericalRate(-num.value);
                    else rate = new MassActionFlowRate(OpFlow.Op("-", monomial.coefficient).Normalize(style));
                    return new Cons<ReactionValue>(
                        new ReactionValue(
                            Factor.ToList(monomial.factors),
                            Factor.ToList(Factor.Quotient(monomial.factors, new Factor(variable, 1), style)),
                            rate),
                    rest);
                } else {
                    throw new Error("MassActionCompiler: aborted because it cannot determine the sign of this expression: " + monomial.coefficient.Format(style));
                }
            }
        }
    }
}
