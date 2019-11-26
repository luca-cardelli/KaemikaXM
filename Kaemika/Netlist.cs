using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Oslo;

namespace Kaemika
{
    public class AlphaMap {
        private Dictionary<int, string> map;
        public AlphaMap() {
            this.map = new Dictionary<int, string>();
        }
        public string Format() {
            string result = "map[";
            foreach (KeyValuePair<int, string> entry in this.map) {
                result = result + "(" + entry.Key + "," + entry.Value + ") ";
            }
            return result + "]";
        }
        public bool ContainsKey(int key) { return this.map.ContainsKey(key); }
        public bool ContainsValue(string value) { return this.map.ContainsValue(value); }
        public void Assign(int key, string value) { this.map[key] = value; }
        public string Extract(int key) { return this.map[key]; }
    }

    public class SwapMap {
        private Dictionary<string, string> map;
        public SwapMap(bool subsup = false) {
            this.map = new Dictionary<string, string>();
            if (subsup) {
                this.map["'"] = "_";
                this.map["⁺"] = "_p";
                this.map["⁻"] = "_n";
                this.map["⁼"] = "_e";
                this.map["⁰"] = "_0";
                this.map["¹"] = "_1";
                this.map["²"] = "_2";
                this.map["³"] = "_3";
                this.map["⁴"] = "_4";
                this.map["⁵"] = "_5";
                this.map["⁶"] = "_6";
                this.map["⁷"] = "_7";
                this.map["⁸"] = "_8";
                this.map["⁹"] = "_9";
                this.map["⁽"] = "_d";
                this.map["⁾"] = "_b";
                this.map["₊"] = "__p";
                this.map["₋"] = "__n";
                this.map["₌"] = "__e";
                this.map["₀"] = "__0";
                this.map["₁"] = "__1";
                this.map["₂"] = "__2";
                this.map["₃"] = "__3";
                this.map["₄"] = "__4";
                this.map["₅"] = "__5";
                this.map["₆"] = "__6";
                this.map["₇"] = "__7";
                this.map["₈"] = "__8";
                this.map["₉"] = "__9";
                this.map["₍"] = "__d";
                this.map["₎"] = "__b";
            }
        }
        public Dictionary<string, string> Pairs() { return this.map; }
        public bool ContainsKey(string key) { return this.map.ContainsKey(key); }
        public bool ContainsValue(string value) { return this.map.ContainsValue(value); }
        public SwapMap Assign(string key, string value) { this.map[key] = value; return this; }
        public string Extract(string key) { return (this.map.ContainsKey(key)) ? this.map[key] : null; }
    }

    public abstract class Entry {
        public abstract string Format(Style style);
    }

    public class CommentEntry : Entry {
        public string comment;
        public CommentEntry(string comment) {
            this.comment = comment;
        }
        public override string Format(Style style) {
            return comment;
        }
    }

    public class ValueEntry : Entry {
        public Symbol symbol;
        public Type type;
        public Value value;
        public DistributionValue distribution;
        public ValueEntry(Symbol symbol, Type type, Value value, DistributionValue distribution = null) {
            this.symbol = symbol;
            this.type = type;
            this.value = value;
            this.distribution = distribution;
        }
 
        public override string Format(Style style) {
            if (style.traceComputational) {
                if (value is ConstantFlow) {
                    return "constant " + value.Format(style);
                } else if (value is Flow) {
                    return type.Format() + " " + symbol.Format(style) + " = "
                        //// DEBUG
                        //+ Environment.NewLine + "[Raw         ] " + value.Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + "[Expand      ] " + (value as Flow).Expand(style).Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + "[E+Regroup   ] " + (value as Flow).Expand(style).ReGroup(style).Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + "[E+R+Simplify] " + (value as Flow).Expand(style).ReGroup(style).Simplify(style).Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + " == "
                        //// END
                        + (value as Flow).Normalize(style).TopFormat(style);
                } else return type.Format() + " " + symbol.Format(style) + " = " + value.Format(style);
            } else return "";
        }
    }

    public class FunctionEntry : Entry {
        public Symbol symbol;
        public FunctionValue value;
        public FunctionEntry(Symbol symbol, FunctionValue value) {
            this.symbol = symbol;
            this.value = value;
        }
        public override string Format(Style style) {
            if (style.traceComputational) {
                return "new function " + symbol.Format(style) + " = " + value.Format(style);
            } else return "";
        }
    }

    public class NetworkEntry : Entry {
        public Symbol symbol;
        public NetworkValue value;
        public NetworkEntry(Symbol symbol, NetworkValue value) {
            this.symbol = symbol;
            this.value = value;
        }
        public override string Format(Style style) {
            if (style.traceComputational) {
                return "new network " + symbol.Format(style) + " = " + value.Format(style);
            } else return "";
        }
    }

    public class SpeciesEntry : Entry {
        public SpeciesValue species;
        public SpeciesEntry(SpeciesValue species) {
            this.species = species;
        }
        public override string Format(Style style) {
            if (style.traceComputational) {
                return "new species " + species.symbol.Format(style);
            } else return "";
        }
    }

    public class ReactionEntry : Entry {
        public ReactionValue reaction;
        public ReactionEntry(ReactionValue reaction) {
            this.reaction = reaction;
        }
        public override string Format(Style style) {
            return reaction.Format(style);
        }
    }

    public class ReportEntry : Entry {
        public Flow flow;
        public string asLabel; // can be null
        public ReportEntry(Flow flow, string asLabel) {
            this.flow = flow;
            this.asLabel = asLabel;
        }
        public override string Format(Style style) {
            if (style.traceComputational) {
                string s = "report " + flow.Format(style);
                if (asLabel != null) { s += " as '" + asLabel + "'"; }
                return s;
            } else return "";
        }
    }

    public abstract class ProtocolEntry : Entry {
    }

    public class SampleEntry : ProtocolEntry {
        public SampleValue value;
        public SampleEntry(SampleValue value) {
            this.value = value;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return value.FormatSymbol(style);
            else if (style.dataFormat == "header") return "sample " + value.FormatHeader(style);
            else if (style.dataFormat == "full") return (style.traceComputational ? "new " : "") + value.Format(style);
            else return "unknown format: " + style.dataFormat;
        }
    }
   
    public class AmountEntry : ProtocolEntry {
        public SpeciesValue species;
        public NumberValue initial;
        public string dimension;
        public SampleValue sample;
        public AmountEntry(SpeciesValue species, NumberValue initial, string dimension, SampleValue sample) {
            this.species = species;
            this.initial = initial;
            this.dimension = dimension;
            this.sample = sample;
        }
        public override string Format(Style style) {
            return "amount " +  species.Format(style) + " @ " + initial.Format(style) + " " + dimension + " in " + sample.FormatSymbol(style);
        }
    }

    public abstract class OperationEntry : ProtocolEntry {
    }

    public class MixEntry : OperationEntry {
        public SampleValue outSample;
        public List<SampleValue> inSamples;
        public MixEntry(SampleValue outSample, List<SampleValue> inSamples) {
            this.outSample = outSample;
            this.inSamples = inSamples;
        }
        //public override string Format(Style style) {
        //    if (style.dataFormat == "symbol") return "mix " + outSample.FormatSymbol(style);
        //    else if (style.dataFormat == "header" || style.dataFormat == "full") return "mix " + outSample.FormatSymbol(style) + " = " + inSample1.FormatSymbol(style) + " with " + inSample2.FormatSymbol(style)
        //             + Environment.NewLine + "   => " + outSample.Format(style);
        //    else return "unknown format: " + style.dataFormat;
        //}
        public override string Format(Style style) {
            string s = "mix ";
            if (style.dataFormat == "symbol") 
                s += outSample.FormatSymbol(style);
            else if (style.dataFormat == "header" || style.dataFormat == "full") {
                s += outSample.FormatSymbol(style) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)) + Environment.NewLine + "   => " + outSample.Format(style);
            } else s += "unknown format: " + style.dataFormat;
            return s;
        }
    }

    public class SplitEntry : OperationEntry {
        public List<SampleValue> outSamples;
        public SampleValue inSample;
        public List<NumberValue> proportions;
        public SplitEntry(List<SampleValue> outSamples, SampleValue inSample, List<NumberValue> proportions) {
            this.outSamples = outSamples;
            this.inSample = inSample;
            this.proportions = proportions;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") {
                return "split " + Style.FormatSequence(outSamples, ", ", x => x.FormatSymbol(style));
            } else if (style.dataFormat == "header" || style.dataFormat == "full") {
                return "split " + Style.FormatSequence(outSamples, ", ", x => x.FormatSymbol(style)) + " = " + inSample.FormatSymbol(style) + " by " + Style.FormatSequence(proportions, ", ", x => x.Format(style))
                    + Style.FormatSequence(outSamples, ", ", x => Environment.NewLine + "   => " + x.Format(style));
            } else return "unknown format: " + style.dataFormat;
        }
    }

    public class EquilibrateEntry : OperationEntry {
        public List<SampleValue> outSamples;
        public List<SampleValue> inSamples;
        public double fortime;
        public EquilibrateEntry(List<SampleValue> outSamples, List<SampleValue> inSamples, double fortime) {
            this.outSamples = outSamples;
            this.inSamples = inSamples;
            this.fortime = fortime;
        }
        public override string Format(Style style) {
            string s = "";
            for (int i = 0; i < outSamples.Count; i++) {
                if (style.dataFormat == "symbol") 
                    s += "equilibrate " + outSamples[i].FormatSymbol(style);
                else if (style.dataFormat == "header" || style.dataFormat == "full") 
                    s += "equilibrate " + outSamples[i].FormatSymbol(style) + " = " + inSamples[i].FormatSymbol(style) + " for " + style.FormatDouble(fortime)
                        + Environment.NewLine + "   => " + outSamples[i].Format(style);
                else s += "unknown format: " + style.dataFormat;
                s += Environment.NewLine;
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 1);
            return s;
        }
    }

    public class RegulateEntry : OperationEntry {
        public List<SampleValue> outSamples;
        public List<SampleValue> inSamples;
        public double temperature;
        public RegulateEntry(List<SampleValue> outSamples, List<SampleValue> inSamples, double temperature) {
            this.outSamples = outSamples;
            this.inSamples = inSamples;
            this.temperature = temperature;
        }
        public override string Format(Style style) {
            string s = "";
            for (int i = 0; i < outSamples.Count; i++) {
                if (style.dataFormat == "symbol") 
                    s += "regulate " + outSamples[i].FormatSymbol(style);
                else if (style.dataFormat == "header" || style.dataFormat == "full") 
                    s += "regulate " + outSamples[i].FormatSymbol(style) + " = " + inSamples[i].FormatSymbol(style) + " to " + style.FormatDouble(temperature)
                        + Environment.NewLine + "   => " + outSamples[i].Format(style);
                else s += "unknown format: " + style.dataFormat;
                s += Environment.NewLine;
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 1);
            return s;
        }
    }

    public class ConcentrateEntry : OperationEntry {
        public List<SampleValue> outSamples;
        public List<SampleValue> inSamples;
        public double volume;
        public ConcentrateEntry(List<SampleValue> outSamples, List<SampleValue> inSamples, double volume) {
            this.outSamples = outSamples;
            this.inSamples = inSamples;
            this.volume = volume;
        }
        public override string Format(Style style) {
            string s = "";
            for (int i = 0; i < outSamples.Count; i++) {
                if (style.dataFormat == "symbol") 
                    s += "concentrate " + outSamples[i].FormatSymbol(style);
                else if (style.dataFormat == "header" || style.dataFormat == "full") 
                    s += "concentrate " + outSamples[i].FormatSymbol(style) + " = " + inSamples[i].FormatSymbol(style) + " to " + style.FormatDouble(volume)
                        + Environment.NewLine + "   => " + outSamples[i].Format(style);
                else s += "unknown format: " + style.dataFormat;
                s += Environment.NewLine;
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 1);
            return s;
        }
    }

    public class DisposeEntry : OperationEntry {
        public List<SampleValue> inSamples;
        public DisposeEntry(List<SampleValue> inSamples) {
            this.inSamples = inSamples;
        }
        public override string Format(Style style) {
            return "dispose " + Style.FormatSequence(inSamples, ", ", x => x.Format(style));
        }
    }

    //public class ChangeSpeciesEntry : OperationEntry {
    //    public SpeciesValue species;
    //    public NumberValue number;
    //    public SampleValue sample;
    //    public ChangeSpeciesEntry(SpeciesValue species, NumberValue number, SampleValue sample) {
    //        this.species = species;
    //        this.number = number;
    //        this.sample = sample;
    //    }
    //    public override string Format(Style style) {
    //        return "change molarity" + species.FormatSymbol(style) + " @ " + number.Format(style) + " in " + sample.FormatSymbol(style);
    //    }
    //}

    public class CRN {
        private SampleValue sample;
        private List<ReactionValue> reactions;
        private double temperature;
        private Matrix stoichio;   // for each species s and reaction r the net stoichiometry of s in r

        //private Matrix[] stoichioR; // for each reaction r, an rx1 matrix (vector) that for each species s ...
        //private Matrix[] stoichio_stoichioT;
        //private double[,,] precomp;

        private double[,,] driftFactor; // LNA precomputation
        private bool trivial;       // try to identify trivial stoichiometry (no change to any species), but not guaranteed

        public CRN(SampleValue sample, List<ReactionValue> reactions, bool precomputeLNA = false) {
            this.sample = sample;
            this.temperature = sample.Temperature();
            this.reactions = reactions;
            this.trivial = true;
            List<SpeciesValue> species = sample.stateMap.species;
            this.stoichio = new Matrix(new double[species.Count, this.reactions.Count]);
            for (int s = 0; s < species.Count; s++) {
                for (int r = 0; r < reactions.Count; r++) {
                    stoichio[s, r] = reactions[r].NetStoichiometry(species[s].symbol);
                    if (stoichio[s, r] != 0) this.trivial = false;
                }
            }
            this.driftFactor = null;
            if (precomputeLNA) {
                driftFactor = new double[species.Count, species.Count, reactions.Count];
                for (int i = 0; i < species.Count; i++)
                    for (int j = 0; j < species.Count; j++)
                        for (int r = 0; r < reactions.Count; r++)
                            driftFactor[i, j, r] = stoichio[i, r] * stoichio[j, r];
                // if (Exec.lastExecution != null) Gui.gui.OutputAppendText(Exec.lastExecution.PartialElapsedTime("After precomputeLNA"));
            }
        }

        public string Format(Style style) {
            return "CRN species = {" + Style.FormatSequence(sample.stateMap.species, ", ", x => x.Format(style)) + "}, reactions = {" + Style.FormatSequence(this.reactions, ", ", x => x.Format(style)) + "}";
        }

        public string FormatStoichiometry(Style style) {
            string str = "CRN species = [";
            string s1 = "";
            foreach (SpeciesValue sp in sample.stateMap.species) { s1 += sp.Format(style) + " "; }

            str += s1 + "]" + Environment.NewLine + "stoichiometry = [" + Environment.NewLine;
            string s2 = "";
            for (int r = 0; r <= reactions.Count -1; r++) {
                for (int s = 0; s <= sample.Count() - 1; s++) {
                    s2 += stoichio[s, r].ToString() + " ";
                }
                s2 += Environment.NewLine;
            }

            str += s2 + "]" + Environment.NewLine + "reaction rates = [" + Environment.NewLine;
            string s3 = "";
            foreach (ReactionValue re in this.reactions) {
                string rt = re.rate.Format(style);
                if (rt == "") rt = "{1}";
                s3 += rt + Environment.NewLine; }
            str += s3 + "]";
            return str;
        }

        public bool Trivial(Style style) {
            return trivial;
        }

        public string FormatAsODE(Style style, string prefixDiff = "∂", string suffixDiff = "") {
            (SpeciesValue[] vars, Flow[] flows) = FluxFlows();
            string ODEs = "";
            for (int speciesIx = 0; speciesIx < flows.Length; speciesIx++) {
                ODEs = ODEs + prefixDiff + vars[speciesIx].Format(style) + suffixDiff + " = " + flows[speciesIx].Normalize(style).TopFormat(style) + Environment.NewLine;
            }

            //ODEs += Environment.NewLine;
            //Flow[,] jacobian = Jacobian(style);
            //for (int speciesI = 0; speciesI < flows.Length; speciesI++) {
            //    SpeciesValue variableI = sample.stateMap.species[speciesI];
            //    for (int speciesK = 0; speciesK < flows.Length; speciesK++) {
            //        SpeciesValue variableK = sample.stateMap.species[speciesK];
            //        ODEs = ODEs + "∂(" + prefixDiff + variableI.Format(style) + suffixDiff + ")/" + "∂" + variableK.Format(style) + " = " 
            //            + jacobian[speciesI, speciesK].TopFormat(style) + " >> " 
            //            + jacobian[speciesI, speciesK].Normalize(style).TopFormat(style) + Environment.NewLine;
            //    }
            //}

            //ODEs += Environment.NewLine;
            //Flow[,] drift = Drift();
            //for (int speciesI = 0; speciesI < flows.Length; speciesI++) {
            //    SpeciesValue variableI = sample.stateMap.species[speciesI];
            //    for (int speciesJ = 0; speciesJ < flows.Length; speciesJ++) {
            //        SpeciesValue variableJ = sample.stateMap.species[speciesJ];
            //        ODEs = ODEs + "W(" + variableI.Format(style) + "," + variableJ.Format(style) + ") = " + drift[speciesI, speciesJ].Normalize(style).TopFormat(style) + Environment.NewLine;
            //    }
            //}

            bool includeLNA = Gui.gui.NoiseSeries() != Noise.None;
            SpeciesFlow[,] covars = null; Flow[,] covarFlows = null;
            if (includeLNA) (covars, covarFlows) = CovarFlow(style);
            if (style.exportTarget == ExportTarget.WolframNotebook) {
                ODEs += Environment.NewLine + "Steady state equations for online Wolfram Notebook equation solver";
                ODEs += Environment.NewLine + "(if LNA is activated, then 'x_y' = 'cov(x,y)'):" + Environment.NewLine;
                ODEs += Environment.NewLine + "Solve[{";
                for (int speciesIx = 0; speciesIx < flows.Length; speciesIx++) {
                    Flow rhs = flows[speciesIx].Normalize(style);
                    if (!rhs.IsNumber(0.0)) ODEs += Environment.NewLine + "0 == " + rhs.TopFormat(style) + ",";
                }
                if (includeLNA) {
                    ODEs += Environment.NewLine;
                    for (int speciesI = 0; speciesI < covars.GetLength(0); speciesI++) {
                        for (int speciesJ = 0; speciesJ < covars.GetLength(1); speciesJ++) {
                            Flow rhs = covarFlows[speciesI, speciesJ].Normalize(style);
                            if (!rhs.IsNumber(0.0)) ODEs += Environment.NewLine + "0 == " + rhs.TopFormat(style) + ",";
                        }
                    }
                    for (int speciesIx = 0; speciesIx < flows.Length; speciesIx++) {
                        ODEs += Environment.NewLine + "Fano_" + vars[speciesIx].Format(style) + " == " + covars[speciesIx,speciesIx].Format(style) + "/" + vars[speciesIx].Format(style) + ",";
                    }
                }
                if (ODEs.Length > 0) ODEs = ODEs.Substring(0, ODEs.Length - 1); // remove last comma
                ODEs += Environment.NewLine + "},{" + Environment.NewLine;
                for (int speciesIx = 0; speciesIx < flows.Length; speciesIx++) {
                    ODEs += vars[speciesIx].Format(style) + ",";
                }
                if (includeLNA) {
                    ODEs += Environment.NewLine;
                    for (int speciesI = 0; speciesI < covars.GetLength(0); speciesI++) {
                        for (int speciesJ = 0; speciesJ < covars.GetLength(1); speciesJ++) {
                            ODEs += covars[speciesI, speciesJ].Format(style) + ",";
                        }
                    }
                    for (int speciesIx = 0; speciesIx < flows.Length; speciesIx++) {
                        ODEs += "Fano_" + vars[speciesIx].Format(style) + ",";
                    }
                }
                if (ODEs.Length > 0) ODEs = ODEs.Substring(0, ODEs.Length - 1); // remove last comma
                ODEs += Environment.NewLine + "}]" + Environment.NewLine;
            } else if (includeLNA) {
                ODEs += Environment.NewLine;
                for (int speciesI = 0; speciesI < covars.GetLength(0); speciesI++) {
                    for (int speciesJ = 0; speciesJ < covars.GetLength(1); speciesJ++) {
                        ODEs += prefixDiff + covars[speciesI, speciesJ].Format(style) + suffixDiff + " = "
                            + covarFlows[speciesI, speciesJ].Normalize(style).TopFormat(style) 
                            + Environment.NewLine;
                    }
                }
            }
            return ODEs;
        }

        public Flow RateFunction(ReactionValue reaction) {
            Flow monomial = NumberFlow.numberFlowOne;
            if (reaction.rate is MassActionRateValue) {
                double rate = ((MassActionRateValue)reaction.rate).Rate(this.temperature);
                foreach (SpeciesValue sp in sample.stateMap.species) {
                    int spStoichio = reaction.Stoichiometry(sp.symbol, reaction.reactants);
                    monomial = OpFlow.Op("*", monomial, OpFlow.Op("^", new SpeciesFlow(sp.symbol), new NumberFlow(spStoichio)));
                }
                monomial = OpFlow.Op("*", new NumberFlow(rate), monomial);
            } else if (reaction.rate is GeneralRateValue) {
                monomial = (reaction.rate as GeneralRateValue).rateFunction;
            } else throw new Error("RateFunction");
            return monomial;
        }

        public (SpeciesValue[] vars, Flow[] flows) FluxFlows() {
            SpeciesValue[] vars = new SpeciesValue[sample.Count()];
            Flow[] flows = new Flow[sample.Count()];
            for (int speciesIx = 0; speciesIx < flows.Length; speciesIx++) {
                vars[speciesIx] = sample.stateMap.species[speciesIx];
                Flow polynomial = NumberFlow.numberFlowZero;
                foreach (ReactionValue reaction in this.reactions) {
                    int netStoichiometry = reaction.NetStoichiometry(vars[speciesIx].symbol);
                    Flow monomial = (netStoichiometry == 0) ? NumberFlow.numberFlowZero : OpFlow.Op("*", new NumberFlow(netStoichiometry), RateFunction(reaction));
                    polynomial = OpFlow.Op("+", polynomial, monomial);
                }
                flows[speciesIx] = polynomial;
            }
            return (vars, flows);
        }

        public Flow[,] Jacobian(Style style) {
            int speciesNo = sample.Count();
            Flow[,] jacobian = new Flow[speciesNo, speciesNo];
            for (int speciesI = 0; speciesI < speciesNo; speciesI++) {
                SpeciesValue variableI = sample.stateMap.species[speciesI];
                for (int speciesK = 0; speciesK < speciesNo; speciesK++) {
                    SpeciesValue variableK = sample.stateMap.species[speciesK];
                    Flow polynomial = NumberFlow.numberFlowZero;
                    foreach (ReactionValue reaction in this.reactions) {
                        int netStoichiometryI = reaction.NetStoichiometry(variableI.symbol);
                        if ((reaction.rate is MassActionRateValue)) {
                            int stoichiometryK = reaction.Stoichiometry(variableK.symbol, reaction.reactants);
                            Flow monomial = (netStoichiometryI == 0 || stoichiometryK == 0) ? NumberFlow.numberFlowZero :
                                OpFlow.Op("*", OpFlow.Op("*", new NumberFlow(netStoichiometryI), new NumberFlow(stoichiometryK)),
                                   OpFlow.Op("/", RateFunction(reaction), new SpeciesFlow(variableK.symbol)));
                            polynomial = OpFlow.Op("+", polynomial, monomial);
                        } else if ((reaction.rate is GeneralRateValue)) {
                            Flow monomial = (netStoichiometryI == 0) ? NumberFlow.numberFlowZero :
                                OpFlow.Op("*", new NumberFlow(netStoichiometryI), 
                                    RateFunction(reaction).Differentiate(variableK.symbol, style));
                            polynomial = OpFlow.Op("+", polynomial, monomial);
                        } else { 
                            throw new Error("Jacobian");
                            //throw new Error("Symbolic Jacobian requires mass action rate functions");
                        }
                    }
                    // Gui.Log("d " + variableI.Format(style) + "/d" + variableK.Format(style) + " = " + polynomial.Format(style));
                    jacobian[speciesI, speciesK] = polynomial;
                }
            }
            return jacobian;
        }

        public Flow[,] Drift() {
            int speciesNo = sample.Count();
            int reactionsNo = reactions.Count();
            Flow[,] drift = new Flow[speciesNo, speciesNo];
            for (int speciesI = 0; speciesI < speciesNo; speciesI++) { // rows
                for (int speciesJ = 0; speciesJ < speciesNo; speciesJ++) { // columns
                    drift[speciesI, speciesJ] = NumberFlow.numberFlowZero;
                    for (int reactionR = 0; reactionR < reactionsNo; reactionR++) { // reactions
                        drift[speciesI, speciesJ] = OpFlow.Op("+", drift[speciesI, speciesJ],
                            OpFlow.Op("*", new NumberFlow(stoichio[speciesI, reactionR] * stoichio[speciesJ, reactionR]),
                            RateFunction(reactions[reactionR])));
                    }
                }
            }
            return drift;
        }

        public (SpeciesFlow[,], Flow[,]) CovarFlow(Style style) {
            int speciesNo = sample.Count();
            SpeciesFlow[,] covar = new SpeciesFlow[speciesNo, speciesNo]; // fill it with fresh covariance variables
            for (int speciesI = 0; speciesI < speciesNo; speciesI++) { // rows
                SpeciesValue variableI = sample.stateMap.species[speciesI];
                for (int speciesJ = 0; speciesJ < speciesNo; speciesJ++) { // columns
                    SpeciesValue variableJ = sample.stateMap.species[speciesJ];
                    if (style.exportTarget == ExportTarget.WolframNotebook) covar[speciesI, speciesJ] = new SpeciesFlow(new Symbol(variableI.Format(style) + "_" + variableJ.Format(style)));
                    else covar[speciesI, speciesJ] = (speciesI == speciesJ) ? new SpeciesFlow(new Symbol("var(" + variableI.Format(style) + ")")) : new SpeciesFlow(new Symbol("cov(" + variableI.Format(style) + "," + variableJ.Format(style) + ")"));
                }
            }
            Flow[,] covarFlow = new Flow[speciesNo, speciesNo];
            Flow[,] jacobian = Jacobian(style);
            Flow[,] drift = Drift();
            for (int speciesI = 0; speciesI < speciesNo; speciesI++) { // rows
                for (int speciesJ = 0; speciesJ < speciesNo; speciesJ++) { // columns
                    covarFlow[speciesI, speciesJ] = NumberFlow.numberFlowZero;
                    for (int speciesK = 0; speciesK < speciesNo; speciesK++) { // dot product index
                        covarFlow[speciesI, speciesJ] = OpFlow.Op("+", covarFlow[speciesI, speciesJ], OpFlow.Op("*", jacobian[speciesI, speciesK], covar[speciesK, speciesJ]));
                        covarFlow[speciesI, speciesJ] = OpFlow.Op("+", covarFlow[speciesI, speciesJ], OpFlow.Op("*", jacobian[speciesJ, speciesK], covar[speciesI, speciesK])); // jacobian transposed
                    }
                    covarFlow[speciesI, speciesJ] = OpFlow.Op("+", covarFlow[speciesI, speciesJ], drift[speciesI, speciesJ]);
                }
            }
            return (covar, covarFlow);
        }

        public Vector Action(double time, Vector state, Style style) {          // the mass action of all reactions in this state
            Vector action = new Vector(new double[reactions.Count]);
            for (int r = 0; r < reactions.Count; r++) action[r] = reactions[r].Action(sample, time, state, sample.Temperature(), style);
            return action;
        }

        public double[] Flux(double time, double[] state, Style style) {
            return stoichio * Action(time, state, style);                      // this line takes 64% of CPU for non-LNA runs, with 38% for Action
        }

        public double[] LNAFlux(double time, double[] state, Style style) {  
            State allState = new State(sample.Count(), lna: true).InitAll(state);
            Vector meanState = allState.MeanVector();                                             // first part of state is the means
            Matrix covarState = allState.CovarMatrix();                                           // second part of state is the covariances
            Vector action = Action(time, meanState, style);                                       // the mass action of all reactions in this state
            double[] actionA = action.ToArray();
            State result = new State(sample.Count(), lna: true).InitZero();

            // fill the first part of result - the means     
            result.SumMean(stoichio * action);                                                     // Mass Action equation

            // fill the second part of result - the covariances                                
            Matrix J = NordsieckState.Jacobian((t, x) => Flux(t, x, style), meanState, 0.0);       // The Jacobian of the flux in this state
            result.SumCovar((J * covarState) + (covarState * J.Transpose()) + Drift(actionA));     // LNA equation

            return result.ToArray();
        }

        // Gillespie (eq 21): Linear noise appoximation is valid over limited times 
        // Performance critical inner loop
        private static double[][] w = null;                           // if indexed like this, OSLO will not copy it again on new Matrix(w)
        private Matrix Drift(double[] actionA) {                      // pass an array to avoid expensive Vector accesses
            int speciesCount = sample.Count();
            int reactionsCount = reactions.Count;
            if (w == null || w.GetLength(0) != speciesCount) {
                w = new double[speciesCount][]; for (int i = 0; i < speciesCount; i++) w[i] = new double[speciesCount];
            }
            for (int i = 0; i < speciesCount; i++) Array.Clear(w[i], 0, speciesCount); // for (int j = 0; j < speciesCount; j++) w[i][j] = 0;

            if (driftFactor == null) {  // slower, less memory
                //for (int i = 0; i < speciesCount; i++)
                //    for (int j = 0; j < speciesCount; j++)
                //        for (int r = 0; r < reactionsCount; r++)
                //            w[i][j] += stoichio[i, r] * stoichio[j, r] * actionA[r];     // this line takes 48% of CPU time on LNA runs
                for (int i = 0; i < speciesCount; i++)
                    for (int j = 0; j <=i; j++) {
                        for (int r = 0; r < reactionsCount; r++) {
                            w[i][j] += stoichio[i, r] * stoichio[j, r] * actionA[r];     // this line was taking 48% of CPU time on LNA runs, but this matrix is symmetrical!
                        }
                        w[j][i] = w[i][j];
                    }
            }
            else {  // faster, more memory (driftFactor matrix)
                for (int i = 0; i < speciesCount; i++)
                    for (int j = 0; j < speciesCount; j++)
                        for (int r = 0; r < reactionsCount; r++)
                            w[i][j] += driftFactor[i, j, r] * actionA[r];               // this line takes 20% of CPU time on LNA runs
            }
            return new Matrix(w, speciesCount, speciesCount);          // by providing the bounds, OSLO will not check them nor copy w again



            //this.stoichioR = new Matrix[reactions.Count];
            //for (int r = 0; r < reactions.Count; r++) {
            //    stoichioR[r] = new Matrix(new double[sample.species.Count,1]);
            //    for (int s = 0; s < sample.species.Count; s++) {
            //        stoichioR[r][s,0] = stoichio[s, r];
            //    }
            //}
            //this.stoichio_stoichioT = new Matrix[reactions.Count];
            //for (int r = 0; r < reactions.Count; r++) {
            //    stoichio_stoichioT[r] = stoichioR[r] * stoichioR[r].Transpose();
            //}
            //this.precomp = new double[reactions.Count, sample.species.Count, sample.species.Count];
            //for (int r = 0; r < reactions.Count; r++) {
            //    Matrix m = stoichio_stoichioT[r];
            //    for (int i = 0; i < sample.species.Count; i++)
            //        for (int j = 0; j < sample.species.Count; j++)
            //            precomp[r,i,j] = m[i,j];
            //}

            //int speciesCount = sample.species.Count;
            //int reactionsCount = reactions.Count;
            //double[][] w = new double[speciesCount][];                 // if indexed like this, OSLO will not copy it again on new
            //if (driftFactor == null) {  // slower, less memory
            //    for (int i = 0; i < speciesCount; i++) {
            //        w[i] = new double[speciesCount];
            //        for (int j = 0; j < speciesCount; j++)
            //            for (int r = 0; r < reactionsCount; r++)
            //                w[i][j] += stoichio[i,r] * stoichio[j,r] * actionA[r];     // this line takes 44% of CPU time on LNA runs
            //    }
            //} else {  // faster, more memory (driftFactor matrix)
            //    for (int i = 0; i < speciesCount; i++) {
            //        w[i] = new double[speciesCount];
            //        for (int j = 0; j < speciesCount; j++)
            //            for (int r = 0; r < reactionsCount; r++)
            //                w[i][j] += driftFactor[i, j, r] * actionA[r];               // this line takes 20% of CPU time on LNA runs
            //    }
            //}
            //return new Matrix(w, speciesCount, speciesCount);          // by providing the bounds, OSLO will not check them nor copy w again

            //if (driftFactor == null) {  // slower, less memory
            //    int speciesCount = sample.species.Count;
            //    Matrix w = new Matrix(new double[speciesCount, speciesCount]);
            //    for (int i = 0; i < speciesCount; i++) {
            //        for (int j = 0; j < speciesCount; j++) {
            //            for (int r = 0; r < reactions.Count; r++)
            //                w[i,j] += stoichio[i,r] * stoichio[j,r] * actionA[r];     // this line takes 58% of CPU time on LNA runs
            //        }
            //    }
            //    return w;
            //} else {  // faster, more memory (driftFactor matrix)
            //    int speciesCount = sample.species.Count;
            //    double[][] w = new double[speciesCount][];                 // if indexed like this, OSLO will not copy it again on new
            //    for (int i = 0; i < speciesCount; i++) {
            //        w[i] = new double[speciesCount];
            //        for (int j = 0; j < speciesCount; j++)
            //            for (int r = 0; r < reactions.Count; r++) {
            //                w[i][j] += driftFactor[i, j, r] * actionA[r];    // this line takes 26% of CPU time on LNA runs, and little GC
            //        }
            //    }
            //    return new Matrix(w, speciesCount, speciesCount);          // by providing the bounds, OSLO will not check them nor copy w again
            //}
        }

        //private Matrix Drift(Vector action) {
        //    int speciesCount = sample.species.Count;
        //    Matrix w = new Matrix(new double[speciesCount, speciesCount]);
        //    for (int r = 0; r < reactions.Count; r++)
        //        w += stoichioR[r] * (stoichioR[r].Transpose() * action[r]);          // this line takes 55% of CPU time on LNA runs
        //    return w;
        //}
        //private Matrix Drift(Vector action) {
        //    int speciesCount = sample.species.Count;
        //    Matrix w = new Matrix(new double[speciesCount, speciesCount]);
        //    for (int r = 0; r < reactions.Count; r++)
        //        w += (stoichioR[r] * stoichioR[r].Transpose()) * action[r];          // this line takes 62% of CPU time on LNA runs
        //    return w;
        //}
        //private Matrix Drift(Vector action) {
        //    int speciesCount = sample.species.Count;
        //    Matrix w = new Matrix(new double[speciesCount, speciesCount]);              // this line takes 50.88% of CPU time on LNA runs
        //    for (int r = 0; r < reactions.Count; r++)
        //        w += stoichio_stoichioT[r] * action[r];          
        //    return w;
        //}
        //private Matrix Drift(Vector action) {
        //    int speciesCount = sample.species.Count;
        //    Matrix w = new Matrix(new double[speciesCount, speciesCount]);
        //    for (int i = 0; i < speciesCount; i++) {
        //        for (int j = 0; j < speciesCount; j++) {
        //            for (int r = 0; r < reactions.Count; r++)
        //                w[i,j] += stoichio_stoichioT[r][i,j] * action[r];    // this line takes 62% of CPU time on LNA runs, mostly overhead in accessing Oslo.Matix elements
        //        }
        //    }
        //    return w;
        //}

        //private Matrix Drift(Vector action) {
        //    int speciesCount = sample.species.Count;
        //    Matrix w = new Matrix(new double[speciesCount, speciesCount]);
        //    for (int r = 0; r < reactions.Count; r++) { 
        //        double action_r = action[r];
        //        for (int i = 0; i < speciesCount; i++)
        //           for (int j = 0; j < speciesCount; j++) {
        //                w[i,j] += driftFactor[r,i,j] * action_r;     // this line takes 45% of CPU time on LNA runs, and a lot less GC. Still significant Oslo.Matrix element access times
        //            }
        //    }
        //    return w;
        //}


        public string FormatScalar(string name, double X) {
            return name + " " + X.ToString();
        }
        public string FormatVector(string name, Vector V) {
            string s = name + "[";
            for (int i = 0; i < V.Length; i++) {
               s += V[i].ToString() + ", ";
            }
            return s + "]";
        }
        public string FormatMatrix(string name, Matrix M) {
            string s = name + "[" + Environment.NewLine;
            for (int i = 0; i < M.RowDimension; i++) {
                for (int j = 0; j < M.ColumnDimension; j++) {
                    s += M[i, j].ToString() + ", ";
                }
                s += Environment.NewLine;
            }
            return s + "]";
        }
    }

    public class Netlist {
        private List<Entry> entries;
        public bool autoContinue;
        public Netlist(bool autoContinue) {
            this.entries = new List<Entry> { };
            this.autoContinue = autoContinue;
        }
        public void Emit(Entry entry) {
            this.entries.Add(entry);
        }

        public string Format(Style style) {
            string format = "";
            foreach (Entry entry in this.entries) {
                string e = entry.Format(style);
                if (e != "") e += Environment.NewLine;
                format += e;
            }
            return format;
        }

        public List<Symbol> AllSpecies() {
            List<Symbol> speciesList = new List<Symbol> { };
            foreach (Entry entry in this.entries) {
                if (entry is SpeciesEntry) speciesList.Add(((SpeciesEntry)entry).species.symbol);
            }
            return speciesList;
        }

        public List<SampleValue> SourceSamples() {
            List<SampleValue> sampleList = new List<SampleValue> { };
            foreach (Entry entry in this.entries) {
                if (entry is SampleEntry) sampleList.Add(((SampleEntry)entry).value);
               else { } // ignore
            }
            return sampleList;
        }

        public List<SampleValue> AllSamples() {
            List<SampleValue> sampleList = new List<SampleValue> { };
            foreach (Entry entry in this.entries) {
                if (entry is SampleEntry) sampleList.Add(((SampleEntry)entry).value);
                else if (entry is MixEntry) sampleList.Add(((MixEntry)entry).outSample);
                else if (entry is SplitEntry) foreach (SampleValue sampleValue in ((SplitEntry)entry).outSamples) sampleList.Add(sampleValue);
                else if (entry is EquilibrateEntry) foreach (SampleValue sampleValue in ((EquilibrateEntry)entry).outSamples) sampleList.Add(sampleValue);
                else if (entry is RegulateEntry) foreach (SampleValue sampleValue in ((RegulateEntry)entry).outSamples) sampleList.Add(sampleValue);
                else if (entry is ConcentrateEntry) foreach (SampleValue sampleValue in ((ConcentrateEntry)entry).outSamples) sampleList.Add(sampleValue);
                else if (entry is DisposeEntry) { }
                else { } // ignore
            }
            return sampleList;
        }

        public List<OperationEntry> AllOperations() {
            List<OperationEntry> operations = new List<OperationEntry> { };
            foreach (Entry entry in this.entries) {
                if (entry is MixEntry) operations.Add((OperationEntry)entry);
                else if (entry is SplitEntry) operations.Add((OperationEntry)entry);
                else if (entry is EquilibrateEntry) operations.Add((OperationEntry)entry);
                else if (entry is RegulateEntry) operations.Add((OperationEntry)entry);
                else if (entry is ConcentrateEntry) operations.Add((OperationEntry)entry);
                else if (entry is DisposeEntry) operations.Add((OperationEntry)entry);
                else { } // ignore
            }
            return operations;
        }

        public List<ReactionValue> AllReactions() {
            List<ReactionValue> reactionList = new List<ReactionValue> { };
            foreach (Entry entry in this.entries) {
                if (entry is ReactionEntry) reactionList.Add(((ReactionEntry)entry).reaction);
            }
            return reactionList;
        }

        //public List<ReactionValue> RelevantReactions(SampleValue sample, List<SpeciesValue> species, Style style) { 
        //    // return the list of reactions in this netlist that involve any of the species in the species list
        //    // check that those reactions use only the species in the list, or give error
        //    List<ReactionValue> reactionList = new List<ReactionValue> { };
        //    foreach (Entry entry in this.entries) {
        //        if (entry is ReactionEntry) {
        //            ReactionValue reaction = ((ReactionEntry)entry).reaction;
        //            if (reaction.Involves(species)) {
        //                if (!reaction.CoveredBy(species, out Symbol notCovered)) {
        //                    throw new Error(
        //                    // Gui.Log("WARNING " +
        //                        "Reaction '" + reaction.Format(style) + "' involves species '" + notCovered.Format(style) + "' in sample '" + sample.FormatSymbol(style)
        //                        +"', but that species is uninitialized in that sample"); }
        //                else reactionList.Add(reaction);
        //            }
        //        }
        //    }
        //    //string s = "Relevant Reactions for sample " + sample.Format(style) + ":" + Environment.NewLine;       
        //    //foreach (ReactionValue r in reactionList) s += r.Format(style) + Environment.NewLine;
        //    //Gui.Log(s);
        //    return reactionList;
        //}

        public List<ReportEntry> Reports(List<SpeciesValue> species) {
            // return the list of reports in this netlist that involve any of the species in the species list
            // but include only those reports that are fully covered by those species, just ignore the others
            List<ReportEntry> reportList = new List<ReportEntry> { };
            foreach (Entry entry in this.entries) {
                if (entry is ReportEntry) {
                    ReportEntry reportEntry = (ReportEntry)entry;
                    if (reportEntry.flow.CoveredBy(species, out Symbol notCovered)) reportList.Add(reportEntry);
                }
            }
            // if there are no reports, then report all the species
            if (reportList.Count == 0) { 
                foreach (SpeciesValue s in species) reportList.Add(new ReportEntry(new SpeciesFlow(s.symbol), null));
            }
            return reportList;
        }

        public List<DistributionValue> Parameters() {
            List<DistributionValue> parameterList = new List<DistributionValue> { };
            foreach (Entry entry in this.entries) {
                if (entry is ValueEntry && (entry as ValueEntry).distribution != null) {
                    parameterList.Add((entry as ValueEntry).distribution);
                }
            }
            return parameterList;
        }

        public List<ProtocolEntry> Protocols() {
            // return the list of ProtocolEntry and CommentEntry in this netlist
            List<ProtocolEntry> reportList = new List<ProtocolEntry> { };
            foreach (Entry entry in this.entries) {
                if (entry is ProtocolEntry) reportList.Add(entry as ProtocolEntry);
                else { } // ignore
            }
            return reportList;
        }


    }

}
