using System;
using System.Collections.Generic;
using Microsoft.Research.Oslo;

namespace Kaemika {

    public class StateMap { // associate a list of species to a state
        public Symbol sample; // the sample this stateMap belongs to
        public List<SpeciesValue> species;
        public Dictionary<Symbol, int> index; // reverse indexing of speciesList
        public State state; // indexed compatibly with speciesList. N.B.: a new state is allocated at each AddSpecies
        public StateMap(Symbol sample, List<SpeciesValue> species, State state) {
            this.sample = sample;
            this.species = species;
            RecomputeIndex();
            this.state = state;
        }
        private void RecomputeIndex() {
            this.index = new Dictionary<Symbol, int> { };
            for (int i = 0; i < this.species.Count; i++) { this.index[this.species[i].symbol] = i; }
        }
        public bool HasSpecies(Symbol species, out int index) {
            index = 0;
            foreach (SpeciesValue s in this.species) {
                if (s.symbol.SameSymbol(species)) return true;
                index++;
            }
            return false;
        }
        public int IndexOf(Symbol species) {
            int index = 0;
            foreach (SpeciesValue s in this.species) {
                if (s.symbol.SameSymbol(species)) return index;
                index++;
            }
            throw new Error("StateMap.IndexOf");
        }
        public double Molarity(Symbol species, Style style) { // checks if species exists
            if (this.HasSpecies(species, out int index)) return this.state.Mean(index);
            else throw new Error("Uninitialized species '" + species.Format(style) + "' in sample '" + this.sample.Format(style) + "'");
        }
        public double Mean(Symbol species) {
            return this.state.Mean(this.index[species]);
        }
        public double Covar(Symbol species1, Symbol species2) {
            return this.state.Covar(this.index[species1], this.index[species2]);
        }
        public void AddDimensionedSpecies(SpeciesValue species, double molarity, double molarityVariance, string dimension, double volume, Style style) {
            if (this.HasSpecies(species.symbol, out int index))
                throw new Error("Repeated amount of '" + species.Format(style) + "' in sample '" + this.sample.Format(style) + "' with value " + style.FormatDouble(molarity));
            else if (molarity < 0)
                throw new Error("Amount of '" + species.Format(style) + "' in sample '" + this.sample.Format(style) + "' must be non-negative: " + style.FormatDouble(molarity));
            else if (molarityVariance < 0)
                throw new Error("Variance of amount of '" + species.Format(style) + "' in sample '" + this.sample.Format(style) + "' must be non-negative: " + style.FormatDouble(molarityVariance));
            else {
                this.species.Add(species);
                RecomputeIndex();
                this.state = this.state.Extend(1);
                this.state.SumMean(this.state.size-1, NormalizeDimension(species, molarity, dimension, volume, style));
                if (this.state.lna)
                   this.state.SumCovar(this.state.size - 1, this.state.size - 1, NormalizeDimension(species, molarityVariance, dimension, volume, style)); // may normalize variance by volume
            }
        }
        public void SumMean(Symbol species, double x) {
            this.state.SumMean(index[species], x);
        }
        public void SumCovar(Symbol species1, Symbol species2, double x) {
            this.state.SumCovar(index[species1], index[species2], x);
        }
        public void Mix(StateMap other, double thisVolume, double otherVolume, Style style) { // mix another stateMap into this one
            int n = 0;
            foreach (SpeciesValue otherSpecies in other.species)
                if (!this.HasSpecies(otherSpecies.symbol, out int i)) {
                    this.species.Add(otherSpecies);
                    n++;
                }
            RecomputeIndex();
            if (n > 0) this.state = this.state.Extend(n);
            if (thisVolume > 0) {
                foreach (SpeciesValue otherSpecies in other.species) {
                    double ratio = otherVolume / thisVolume;
                    double otherMean = other.Mean(otherSpecies.symbol) * ratio;
                    SumMean(otherSpecies.symbol, otherMean);
                    if (this.state.lna && other.state.lna) {
                        double squareRatio = ratio * ratio; // square of volume ratio law,
                        foreach (SpeciesValue otherSpecies2 in other.species) {
                            double otherCovar = other.Covar(otherSpecies.symbol, otherSpecies2.symbol) * squareRatio; 
                            SumCovar(otherSpecies.symbol, otherSpecies2.symbol, otherCovar);
                        }
                    }
                }
            }
        }
        public void Split(StateMap other, Style style) {
            int n = 0;
            foreach (SpeciesValue otherSpecies in other.species) {
                    this.species.Add(otherSpecies);
                    n++;
                }
            RecomputeIndex();
            if (n > 0) this.state = this.state.Extend(n);
            foreach (SpeciesValue otherSpecies in other.species) {
                SumMean(otherSpecies.symbol, other.Mean(otherSpecies.symbol));
                if (this.state.lna && other.state.lna) {
                    foreach (SpeciesValue otherSpecies2 in other.species) {
                        double otherCovar = other.Covar(otherSpecies.symbol, otherSpecies2.symbol);
                        SumCovar(otherSpecies.symbol, otherSpecies2.symbol, otherCovar);
                    }
                }
            }
        }
        //public void Transfer(StateMap other, double thisVolume, double otherVolume, Style style) {
        //    // transfer from another stateMap into this (empty) one
        //    // if new volume is larger, then it is "dilution", and it the same as mixing with an empty sample
        //    // but if new volume is smaller, then it is "evaporation", which cannot be otherwise expressed
        //    int n = 0;
        //    foreach (SpeciesValue otherSpecies in other.species) {
        //            this.species.Add(otherSpecies);
        //            n++;
        //        }
        //    RecomputeIndex();
        //    if (n > 0) this.state = this.state.Extend(n);
        //    // if volumeScaling==0 then volume will be 0 so it does not matter what mean and covar are
        //    foreach (SpeciesValue otherSpecies in other.species) {
        //        SumMean(otherSpecies.symbol, (proportion == 0.0) ? 0.0 : other.Mean(otherSpecies.symbol)/proportion);
        //        double squareProportion = proportion * proportion; // square of volume ratio law,
        //        if (this.state.lna && other.state.lna) {
        //            foreach (SpeciesValue otherSpecies2 in other.species) {
        //                double otherCovar = (proportion == 0.0) ? 0.0 : other.Covar(otherSpecies.symbol, otherSpecies2.symbol) / squareProportion;
        //                SumCovar(otherSpecies.symbol, otherSpecies2.symbol, otherCovar);
        //            }
        //        }
        //    }
        //}
        public double NormalizeDimension(SpeciesValue species, double value, string dimension, double volume, Style style) {
            if (double.IsNaN(value)) return value;
            double normal;
            normal = Protocol.NormalizeMolarity(value, dimension);
            if (normal >= 0) return normal; // value had dimension M = mol/L
            normal = Protocol.NormalizeMole(value, dimension);
            if (normal >= 0) return normal / volume; // value had dimension mol, convert it to M = mol/L
            normal = Protocol.NormalizeWeight(value, dimension);
            if (normal >= 0) {
                if (species.HasMolarMass())
                    return (normal / species.MolarMass()) / volume;    // value had dimension g, convert it to M = (g/(g/M))/L
                throw new Error("Species '" + species.Format(style)
                    + "' was given no molar mass, hence its amount in sample '" + this.sample.Format(style)
                    + "' should have dimension 'M' (concentration) or 'mol' (mole), not '" + dimension + "'");
            }
            throw new Error("Invalid dimension '" + dimension + "'" + " or dimension value " + style.FormatDouble(value));
        }
    }

    public class State {
        public int size;       // number of species
        public bool lna;       // whether covariances are included
        private double[] state; // of length size if not lna, of length size+size*size if lna; only allocated if inited=true
        private bool inited;    // whether state is allocated
        public State(int size, bool lna) {
            this.size = size;
            this.lna = lna;
            this.state = new double[0];
            this.inited = false;
        }
        public State Clone() {
            double[] clone = new double[state.Length];
            for (int i = 0; i < state.Length; i++) clone[i] = state[i];
            return new State(this.size, this.lna).InitAll(clone);
        }
        public State Positive() {
            for (int i = 0; i < size; i++) {
                if (state[i] < 0) state[i] = 0; // the ODE solver screwed up
            }
            return this;
        }
        public bool NaN() {
            for (int i = 0; i < size; i++) {
                if (double.IsNaN(state[i])) return true;
            }
            return false;
        }
        public State InitZero() {
            if (this.inited) throw new Error("InitZero: already inited");
            if (!lna) this.state = new double[size];
            else this.state = new double[size + size * size];
            // for (int i = 0; i < this.state.Length; i++) this.state[i] = 0.0;
            this.inited = true;
            return this;
        }
        public State InitMeans(double[] init) {
            if (this.inited) throw new Error("InitMeans: already inited");
            if (init.Length != size) throw new Error("InitMeans: wrong size");
            if (!lna) this.state = init;
            else {
                this.state = new double[size + size * size];
                for (int i = 0; i < size; i++) this.state[i] = init[i];
                // for (int i = size; i < this.state.Length; i++) this.state[i] = 0.0;
            }
            this.inited = true;
            return this;
        }
        public State InitAll(double[] init) {
            if (this.inited) throw new Error("InitAll: already inited");
            if (((!lna) && init.Length != size) || (lna && init.Length != size + size * size)) throw new Error("InitAll: wrong size");
            this.state = init;
            this.inited = true;
            return this;
        }
        //public State Add(double molarity) {
        //    State newState = new State(this.size + 1, lna: this.lna).InitZero();
        //    // copy the old means:
        //    for (int i = 0; i < this.size; i++) newState.SumMean(i, this.Mean(i));
        //    // the mean of the new state component is set to molarity:
        //    newState.SumMean(this.size, molarity);
        //    if (this.lna) {
        //        // copy the old covariances:
        //        for (int i = 0; i < this.size; i++)
        //            for (int j = 0; j < this.size; j++)
        //                newState.MixCovar(i, j, this.Covar(i, j));
        //        // the covariance of the new state component with all the other components remains zero
        //    }
        //    return newState;
        //}
        public State Extend(int n) {
            State newState = new State(this.size + n, lna: this.lna).InitZero();
            // copy the old means:
            for (int i = 0; i < this.size; i++) newState.SumMean(i, this.Mean(i));
            // the mean of the new state components remains zero
            if (this.lna) {
                // copy the old covariances:
                for (int i = 0; i < this.size; i++)
                    for (int j = 0; j < this.size; j++)
                        newState.SumCovar(i, j, this.Covar(i, j));
                // the covariance of the new state component with all the other components remains zero
            }
            return newState;
        }
        public double[] ToArray() {  // danger! does not copy the state
            return this.state;
        }
        public double Mean(int i) {
            return this.state[i];
        }
        public Vector MeanVector() {
            double[] m = new double[size];
            for (int i = 0; i < size; i++) m[i] = this.state[i];
            return new Vector(m);
        }
        public void SetMean(int i, double x) {
            this.state[i] = x;
        }
        public void SumMean(int i, double x) {
            this.state[i] += x;
        }
        public void SumMean(Vector x) {
            if (x.Length != size) throw new Error("SumMean: wrong size");
            for (int i = 0; i < size; i++) this.state[i] += x[i];
        }
        public double Covar(int i, int j) {
            return this.state[size + (i * size) + j];
        }
        public Matrix CovarMatrix() {
            if (!this.lna) throw new Error("Covars: not lna state");
            double[,] m = new double[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    m[i, j] = this.state[size + (i * size) + j];
            return new Matrix(m);
        }
        public void SetCovar(int i, int j, double x) {
            this.state[size + (i * size) + j] = x;
        }
        public void SumCovar(int i, int j, double x) {
            this.state[size + (i * size) + j] += x;
        }
        public void SumCovar(Matrix x) {
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    this.state[size + (i * size) + j] += x[i, j];
        }
        public string FormatSpecies(List<SpeciesValue> species, Style style) {
            string s = "";
            for (int i = 0; i < this.size; i++) {
                s += species[i].Format(style) + "=" + Mean(i).ToString() + ", ";
            }
            if (this.lna) {
                for (int i = 0; i < this.size; i++)
                    for (int j = 0; j < this.size; j++) {
                        s += "(" + species[i].Format(style) + "," + species[j].Format(style) + ")=" + Covar(i, j).ToString() + ", ";
                    }
            }
            if (s.Length > 0) s = s.Substring(0, s.Length - 2);
            return s;
        }
        //public string FormatReports(List<ReportEntry> reports, SampleValue sample, Func<double, Vector, Vector> flux, double time, Noise noise, string[] series, string[] seriesLNA, Style style) {
        //    string s = "";
        //    for (int i = 0; i < reports.Count; i++) {
        //        if (series[i] != null) { // if a series was actually generated from this report
        //            // generate deterministic series
        //            if ((noise == Noise.None && reports[i].flow.HasDeterministicValue()) ||
        //                (noise != Noise.None && reports[i].flow.HasStochasticMean())) {
        //                double mean = reports[i].flow.ObserveMean(sample, time, this, flux, style);
        //                s += KChartHandler.ChartAddPointAsString(series[i], time, mean, 0.0, Noise.None) + ", ";
        //            }
        //            // generate LNA-dependent series
        //            if (noise != Noise.None && reports[i].flow.HasStochasticVariance() && !reports[i].flow.HasNullVariance()) {
        //                double mean = reports[i].flow.ObserveMean(sample, time, this, flux, style);
        //                double variance = reports[i].flow.ObserveVariance(sample, time, this, style);
        //                s += KChartHandler.ChartAddPointAsString(seriesLNA[i], time, mean, variance, noise) + ", ";
        //            }
        //        }
        //    }
        //    if (s.Length > 0) s = s.Substring(0, s.Length - 2);
        //    return s;
        //}
    }

}
