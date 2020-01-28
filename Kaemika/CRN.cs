using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Research.Oslo;
using SkiaSharp;

namespace Kaemika {

    public class CRN {
        private SampleValue sample;
        private List<ReactionValue> reactions;
        private double temperature;
        private Matrix stoichio;        // for each species s and reaction r the net stoichiometry of s in r
        private double[,,] driftFactor; // LNA precomputation
        private bool trivial;           // try to identify trivial stoichiometry (no change to any species), but not guaranteed

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
                if (Exec.lastExecution != null) Gui.toGui.OutputAppendText(Exec.lastExecution.PartialElapsedTime("After precomputeLNA"));
            }
        }

        public string Format(Style style) {
            return "CRN species = {" + Style.FormatSequence(sample.stateMap.species, ", ", x => x.Format(style)) + "}, reactions = {" + Style.FormatSequence(this.reactions, ", ", x => x.Format(style)) + "}";
        }

        public string FormatNice(Style style) {
            return
                (sample.symbol.IsVesselVariant() ? "" : Environment.NewLine + "Sample " + sample.FormatSymbol(style) + Environment.NewLine)
                + sample.FormatContent(style, true, false, false) + Environment.NewLine + Environment.NewLine
                + Style.FormatSequence(this.reactions, Environment.NewLine, x => x.Format(style)) + Environment.NewLine + Environment.NewLine
                + FormatAsODE(style, "∂ ", "", false) + Environment.NewLine;
        }

        public SKSize Measure(Colorer colorer, float pointSize, Style style) {
            var sizeX = 0.0f;
            var sizeY = 0.0f;
            using (var paint = colorer.TextPaint(colorer.font, pointSize, SKColors.Black)) {
                foreach (SpeciesValue sp in sample.stateMap.species) {
                    var r = colorer.MeasureText(sp.Format(style) + " = " + Gui.FormatUnit(sample.stateMap.Mean(sp.symbol), " ", "M", style.numberFormat), paint);
                    sizeX = Math.Max(sizeX, r.Width);
                    sizeY += pointSize;
                }
                sizeY += pointSize;
                foreach (var reaction in this.reactions) {
                    var r = colorer.MeasureText(reaction.TopFormat(style), paint);
                    sizeX = Math.Max(sizeX, r.Width);
                    sizeY += pointSize;
                }
            }
            return new SKSize(sizeX, sizeY);
        }
        public void Draw(Painter painter, SKPoint origin, SKSize size, float pointSize, Style style) {
            using (var paint = painter.TextPaint(painter.font, pointSize, SKColors.Black)) {
                var Y = origin.Y + pointSize;
                foreach (SpeciesValue sp in sample.stateMap.species) {
                    painter.DrawText(sp.Format(style) + " = " + Gui.FormatUnit(sample.stateMap.Mean(sp.symbol), " ", "M", style.numberFormat), new SKPoint(origin.X, Y), paint);
                    Y += pointSize;
                }
                Y += pointSize;
                foreach (var reaction in this.reactions) {
                    painter.DrawText(reaction.TopFormat(style), new SKPoint(origin.X, Y), paint);
                    Y += pointSize;
                }
            }
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

        public string FormatAsODE(Style style, string prefixDiff = "∂", string suffixDiff = "", bool withCovariances = true) {
            (SpeciesValue[] vars, Flow[] flows) = MeanFlow();
            string ODEs = "";
            for (int speciesIx = 0; speciesIx < flows.Length; speciesIx++) {
                ODEs = ODEs + flows[speciesIx].FormatAsODE(vars[speciesIx], style, prefixDiff, suffixDiff) + Environment.NewLine;
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

            bool includeLNA = withCovariances && KControls.SelectNoiseSelectedItem != Noise.None;
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

        public (SpeciesValue[] vars, Flow[] flows) MeanFlow() {
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

        public Dictionary<SpeciesValue, Flow> MeanFlowDictionary() {
            var dictionary = new Dictionary<SpeciesValue, Flow>();
            (SpeciesValue[] vars, Flow[] flows) = MeanFlow();
            for (int i = 0; i < vars.Count(); i++) dictionary[vars[i]] = flows[i];
            return dictionary;
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

}
