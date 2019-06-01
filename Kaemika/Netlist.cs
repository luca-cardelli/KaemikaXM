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
                return type.Format() + " " + symbol.Format(style) + " = " + value.Format(style);
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
            if (style.dataFormat == "symbol") return value.symbol.Format(style);
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
            return "amount " +  species.Format(style) + " @ " + initial.Format(style) + " " + dimension + " in " + sample.symbol.Format(style);
        }
    }

    public abstract class OperationEntry : ProtocolEntry {
    }

    public class MixEntry : OperationEntry {
        public SampleValue outSample;
        public SampleValue inSample1;
        public SampleValue inSample2;
        public MixEntry(SampleValue outSample, SampleValue inSample1, SampleValue inSample2) {
            this.outSample = outSample;
            this.inSample1 = inSample1;
            this.inSample2 = inSample2;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return "mix " + outSample.symbol.Format(style);
            else if (style.dataFormat == "header") return "mix " + outSample.symbol.Format(style) + " := " + inSample1.symbol.Format(style) + " with " + inSample2.symbol.Format(style)
                     + Environment.NewLine + "   => " + outSample.Format(style);
            else if (style.dataFormat == "full") return "mix " + outSample.symbol.Format(style) + " := " + inSample1.symbol.Format(style) + " with " + inSample2.symbol.Format(style)
                     + Environment.NewLine + "   => " + outSample.Format(style);
            else return "unknown format: " + style.dataFormat;
        }
    }

    public class SplitEntry : OperationEntry {
        public SampleValue outSample1;
        public SampleValue outSample2;
        public SampleValue inSample;
        public NumberValue proportion;
        public SplitEntry(SampleValue outSample1, SampleValue outSample2, SampleValue inSample, NumberValue proportion) {
            this.outSample1 = outSample1;
            this.outSample2 = outSample2;
            this.inSample = inSample;
            this.proportion = proportion;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return "split " + outSample1.symbol.Format(style) + ", " + outSample2.symbol.Format(style);
            else if (style.dataFormat == "header") return "split " + outSample1.symbol.Format(style) + ", " + outSample2.symbol.Format(style) + " := " + inSample.symbol.Format(style) + " by " + proportion.Format(style)
                    + Environment.NewLine + "   => " + outSample1.Format(style) + ", " + Environment.NewLine + "   => " + outSample2.Format(style);
            else if (style.dataFormat == "full") return "split " + outSample1.symbol.Format(style) + ", " + outSample2.symbol.Format(style) + " := " + inSample.symbol.Format(style) + " by " + proportion.Format(style) 
                    + Environment.NewLine + "   => " + outSample1.Format(style) + ", " + Environment.NewLine + "   => " + outSample2.Format(style);
            else return "unknown format: " + style.dataFormat;
        }
    }

    public class EquilibrateEntry : OperationEntry {
        public SampleValue outSample;
        public SampleValue inSample;
        public NumberValue time;
        public EquilibrateEntry(SampleValue outValue, SampleValue inValue, NumberValue time) {
            this.outSample = outValue;
            this.inSample = inValue;
            this.time = time;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return "equilibrate " + outSample.symbol.Format(style);
            else if (style.dataFormat == "header") return "equilibrate " + outSample.symbol.Format(style) + " := " + inSample.symbol.Format(style) + " for " + time.Format(style)
                    + Environment.NewLine + "   => " + outSample.Format(style);
            else if (style.dataFormat == "full") return "equilibrate " + outSample.symbol.Format(style) + " := " + inSample.symbol.Format(style) + " for " + time.Format(style)
                    + Environment.NewLine + "   => " + outSample.Format(style);
            else return "unknown format: " + style.dataFormat;
        }
    }

    public class TransferEntry : OperationEntry {
        public SampleValue outSample;
        public SampleValue inSample;
        public TransferEntry(SampleValue outSample, SampleValue inSample) {
            this.outSample = outSample;
            this.inSample = inSample;
        }
        public override string Format(Style style) {
            if (style.dataFormat == "symbol") return "transfer " + outSample.symbol.Format(style);
            else if (style.dataFormat == "header") return "transfer " + outSample.symbol.Format(style) + " := " + inSample.symbol.Format(style)
                    + Environment.NewLine + "   => " + outSample.Format(style);
            else if (style.dataFormat == "full") return "transfer " + outSample.symbol.Format(style) + " := " + inSample.symbol.Format(style)
                    + Environment.NewLine + "   => " + outSample.Format(style);
            else return "unknown format: " + style.dataFormat;
        }
    }

    public class DisposeEntry : OperationEntry {
        public SampleValue inSample;
        public DisposeEntry(SampleValue value) {
            this.inSample = value;
        }
        public override string Format(Style style) {
            return "dispose " + inSample.symbol.Format(style);
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
    //        return "change molarity" + species.symbol.Format(style) + " @ " + number.Format(style) + " in " + sample.symbol.Format(style);
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
            this.stoichio = new Matrix(new double[sample.species.Count, this.reactions.Count]);
            for (int s = 0; s < sample.species.Count; s++) {
                for (int r = 0; r < reactions.Count; r++) {
                    stoichio[s, r] = reactions[r].NetStoichiometry(sample.species[s].symbol);
                    if (stoichio[s, r] != 0) this.trivial = false;
                }
            }
            this.driftFactor = null;
            if (precomputeLNA) {
                int speciesCount = sample.species.Count;
                driftFactor = new double[speciesCount, speciesCount, reactions.Count];
                for (int i = 0; i < speciesCount; i++)
                    for (int j = 0; j < speciesCount; j++)
                        for (int r = 0; r < reactions.Count; r++)
                            driftFactor[i, j, r] = stoichio[i, r] * stoichio[j, r];
            }
        }

        public string Format(Style style) {
            string str = "CRN species = {";
            string s1 = "";
            foreach (SpeciesValue sp in sample.species) { s1 += sp.Format(style) + ", "; }
            if (s1.Length > 0) s1 = s1.Substring(0, s1.Length - 2); // remove last comma
            str += s1 + "}, reactions = {";
            string s2 = "";
            foreach (ReactionValue re in this.reactions) { s2 += re.Format(style) + ", "; }
            if (s2.Length > 0) s2 = s2.Substring(0, s2.Length - 2); // remove last comma
            str += s2 + "}";
            return str;
        }

        public bool Trivial(Style style) {
            return trivial;
        }

        public string FormatAsODE(Style style, string prefixDiff = "∂", string suffixDiff = "") {
            string ODEs = "";
            foreach (SpeciesValue variable in sample.species) {
                string polynomial = "";
                foreach (ReactionValue reaction in this.reactions) {
                    string monomial = "";
                    int netStoichiometry = reaction.NetStoichiometry(variable.symbol);
                    if (netStoichiometry != 0) {
                        if (reaction.rate is MassActionRateValue) {
                            foreach (SpeciesValue sp in sample.species) {
                                int spStoichio = reaction.Stoichiometry(sp.symbol, reaction.reactants);
                                if (spStoichio > 0) {
                                    string factor = sp.Format(style);
                                    if (spStoichio != 1) factor = factor + "^" + spStoichio;
                                    monomial = (monomial == "") ? factor : monomial + "*" + factor;
                                }
                            }
                            double rate = ((MassActionRateValue)reaction.rate).Rate(this.temperature);
                            if ((rate != 1) && (monomial != "")) monomial = "*" + monomial;
                            if (rate != 1) monomial = style.FormatDouble(rate) + monomial;
                        } else if (reaction.rate is GeneralRateValue) {
                            Flow rate = (reaction.rate as GeneralRateValue).rateFunction;
                            monomial = rate.Format(style);
                        } else {
                            throw new Error("FormatAsODE");
                        }
                        if ((netStoichiometry == -1) && (monomial == "")) monomial = "-1";
                        else if ((netStoichiometry == -1) && (monomial != "")) monomial = "-" + monomial;
                        else if ((netStoichiometry == 1) && (monomial != "")) { }
                        else if (monomial == "") monomial = netStoichiometry.ToString();
                        else if (monomial != "") monomial = netStoichiometry.ToString() + "*" + monomial;
                    }
                    if (monomial != "") {
                        if (polynomial == "") polynomial = monomial;
                        else if (monomial.Substring(0,1) == "-") polynomial = polynomial + " - " + monomial.Substring(1);
                        else polynomial = polynomial + " + " + monomial;
                    }
                }
                if (polynomial != "")
                    ODEs = ODEs + prefixDiff + variable.Format(style) + suffixDiff + " = " + polynomial + Environment.NewLine;
            }
            return ODEs;
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
            State allState = new State(sample.species.Count, true).InitAll(state);
            Vector meanState = allState.MeanVector();                                             // first part of state is the means
            Matrix covarState = allState.CovarMatrix();                                           // second part of state is the covariances
            Vector action = Action(time, meanState, style);                                       // the mass action of all reactions in this state
            double[] actionA = action.ToArray();
            State result = new State(sample.species.Count, true).InitZero();

            // fill the first part of deriv - the means     
            result.AddMean(stoichio * action);                                                     // Mass Action equation

            // fill the second part of deriv - the covariances                                
            Matrix J = NordsieckState.Jacobian((t, x) => Flux(t, x, style), meanState, 0.0);       // The Jacobian of the flux in this state
            result.AddCovar((J * covarState) + (covarState * J.Transpose()) + Drift(actionA));      // LNA equation

            return result.ToArray();
        }

        // Gillespie (eq 21): Linear noise appoximation is valid over limited times 
        // Performance critical inner loop
        private static double[][] w = null;                           // if indexed like this, OSLO will not copy it again on new Matrix(w)
        private Matrix Drift(double[] actionA) {                      // pass an array to avoid expensive Vector accesses
            int speciesCount = sample.species.Count;
            int reactionsCount = reactions.Count;
            if (w == null || w.GetLength(0) != speciesCount) {
                w = new double[speciesCount][]; for (int i = 0; i < speciesCount; i++) w[i] = new double[speciesCount];
            }
            for (int i = 0; i < speciesCount; i++) Array.Clear(w[i], 0, speciesCount); // for (int j = 0; j < speciesCount; j++) w[i][j] = 0;

            if (driftFactor == null) {  // slower, less memory
                for (int i = 0; i < speciesCount; i++)
                    for (int j = 0; j < speciesCount; j++)
                        for (int r = 0; r < reactionsCount; r++)
                            w[i][j] += stoichio[i,r] * stoichio[j,r] * actionA[r];     // this line takes 48% of CPU time on LNA runs
            } else {  // faster, more memory (driftFactor matrix)
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
                else if (entry is SplitEntry) { sampleList.Add(((SplitEntry)entry).outSample1); sampleList.Add(((SplitEntry)entry).outSample2); }
                else if (entry is EquilibrateEntry) sampleList.Add(((EquilibrateEntry)entry).outSample);
                else if (entry is TransferEntry) sampleList.Add(((TransferEntry)entry).outSample);
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
                else if (entry is TransferEntry) operations.Add((OperationEntry)entry);
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
        //                        "Reaction '" + reaction.Format(style) + "' involves species '" + notCovered.Format(style) + "' in sample '" + sample.symbol.Format(style)
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
            // return the list of report in this netlist that involve any of the species in the species list
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
