using System;
using System.Collections.Generic;


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
                this.map["'"] = "_q";
                this.map["⁺"] = "__p";
                this.map["⁻"] = "__n";
                this.map["⁼"] = "__e";
                this.map["⁰"] = "__0";
                this.map["¹"] = "__1";
                this.map["²"] = "__2";
                this.map["³"] = "__3";
                this.map["⁴"] = "__4";
                this.map["⁵"] = "__5";
                this.map["⁶"] = "__6";
                this.map["⁷"] = "__7";
                this.map["⁸"] = "__8";
                this.map["⁹"] = "__9";
                this.map["⁽"] = "__d";
                this.map["⁾"] = "__b";
                this.map["₊"] = "_p";
                this.map["₋"] = "_n";
                this.map["₌"] = "_e";
                this.map["₀"] = "_0";
                this.map["₁"] = "_1";
                this.map["₂"] = "_2";
                this.map["₃"] = "_3";
                this.map["₄"] = "_4";
                this.map["₅"] = "_5";
                this.map["₆"] = "_6";
                this.map["₇"] = "_7";
                this.map["₈"] = "_8";
                this.map["₉"] = "_9";
                this.map["₍"] = "_d";
                this.map["₎"] = "_b";
            }
        }
        public Dictionary<string, string> Pairs() { return this.map; }
        public bool ContainsKey(string key) { return this.map.ContainsKey(key); }
        public bool ContainsValue(string value) { return this.map.ContainsValue(value); }
        public SwapMap Assign(string key, string value) { this.map[key] = value; return this; }
        public string Extract(string key) { return (this.map.ContainsKey(key)) ? this.map[key] : null; }
        public string Map(string key) { return (this.map.ContainsKey(key)) ? this.map[key] : key; }
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
        public ValueEntry(Symbol symbol, Type type, Value value) {
            this.symbol = symbol;
            this.type = type;
            this.value = value;
        }
 
        public override string Format(Style style) {
            if (style.traceFull) {
                if (value is ConstantFlow) {
                    return "constant " + value.Format(style);
                } else if (value is Flow) {
                    return Types.Format(type) + " " + symbol.Format(style) + " = "
                        //// DEBUG
                        //+ Environment.NewLine + "[Raw         ] " + value.Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + "[Expand      ] " + (value as Flow).Expand(style).Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + "[E+Regroup   ] " + (value as Flow).Expand(style).ReGroup(style).Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + "[E+R+Simplify] " + (value as Flow).Expand(style).ReGroup(style).Simplify(style).Format(style.RestyleAsDataFormat("operator"))
                        //+ Environment.NewLine + " == "
                        //// END
                        + (value as Flow).Normalize(style).TopFormat(style);
                } else return Types.Format(type) + " " + symbol.Format(style) + " = " + value.Format(style);
            } else return "";
        }
    }

    public class ParameterEntry : ValueEntry {
        public DistributionValue distribution;
        public ParameterEntry(Symbol symbol, Type type, NumberValue value, DistributionValue distribution) : base(symbol,type,value) {
            this.distribution = distribution;
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
            if (style.traceFull) {
                return "new function " + symbol.Format(style); //### + " = " + value.Format(style);
            } else return "";
        }
    }
    
    public class RandomEntry : Entry {
        public Symbol symbol;
        public DistributionValue value;
        public RandomEntry(Symbol symbol, DistributionValue value) {
            this.symbol = symbol;
            this.value = value;
        }
        public override string Format(Style style) {
            if (style.traceFull) {
                return "new random " + symbol.Format(style); //### + " = " + value.Format(style);
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
            if (style.traceFull) {
                return "new network " + symbol.Format(style); //### + " = " + value.Format(style);
            } else return "";
        }
    }

    public class SpeciesEntry : Entry {
        public SpeciesValue species;
        public SpeciesEntry(SpeciesValue species) {
            this.species = species;
        }
        public override string Format(Style style) {
            if (style.traceFull) {
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
        public Symbol timecourse; // can be null
        public Flow flow;
        public string asLabel; // can be null
        public SampleValue sample;
        public ReportEntry(Symbol timecourse, Flow flow, string asLabel, SampleValue sample) {
            this.timecourse = timecourse;
            this.flow = flow;
            this.asLabel = asLabel;
            this.sample = sample;
        }
        public override string Format(Style style) {
            //if (style.traceFull) {
                string s = "report ";
                if (timecourse != null) s += timecourse.Format(style) + " = ";
                s += flow.Format(style);
                if (asLabel != null) { s += " as '" + asLabel + "'"; }
                s += " in " + sample.FormatSymbol(style);
                return s;
            //} else return "";
        }
    }

    public class TriggerEntry : Entry {
        public SpeciesValue target;
        public Flow condition;
        public Flow assignment;
        public Flow assignmentVariance; // can be null
        public string dimension;
        public SampleValue sample;
        public TriggerEntry(SpeciesValue target, Flow condition, Flow assignment, Flow assignmentVariance, string dimension, SampleValue sample) {
            this.target = target;
            this.condition = condition;
            this.assignment = assignment;
            this.assignmentVariance = assignmentVariance;
            this.dimension = dimension;
            this.sample = sample;
        }
        public override string Format(Style style) {
            string s = "trigger ";
            s += target.Format(style) + " @ ";
            s += assignment.TopFormat(style) + " ";
            s += (assignmentVariance == null) ? "" : " ± " + assignmentVariance.Format(style) + " ";
            s += dimension + " when ";
            s += condition.TopFormat(style);
            s += sample.symbol.IsVesselVariant() ? "" : " in " + sample.FormatSymbol(style);
            return s;
        }
    }

    //public class ReportEntryWithCRNFlows : ReportEntry {
    //    public Dictionary<SpeciesValue, Flow> dictionary;
    //    public ReportEntryWithCRNFlows(Flow flow, string asLabel, Dictionary<SpeciesValue, Flow> dictionary) : base(flow, asLabel) {
    //        this.dictionary = dictionary;
    //    }
    //}

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
            else if (style.dataFormat == "full") return (style.traceFull ? "new " : "") + value.Format(style);
            else return "unknown format: " + style.dataFormat;
        }
    }
   
    public class AmountEntry : ProtocolEntry {
        public SpeciesValue species;
        public NumberValue initial;
        public NumberValue initialVariance;
        public string dimension;
        public SampleValue sample;
        public AmountEntry(SpeciesValue species, NumberValue initial, NumberValue initialVariance, string dimension, SampleValue sample) {
            this.species = species;
            this.initial = initial;
            this.initialVariance = initialVariance;
            this.dimension = dimension;
            this.sample = sample;
        }
        public override string Format(Style style) {
            return "amount " +  species.Format(style) + " @ " + initial.Format(style) + ((initialVariance is NumberValue num && num.value == 0.0) ? "" : " ± " + initialVariance.Format(style)) + " " + dimension + " in " + sample.FormatSymbol(style);
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
        public bool EmitChem(Style style, int s) {
            return true;
        }
        public bool EmitComp(Style style, int s) {
            return s == 0; // top level definitions only
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

        public string AllComments() {
            string comments = "";
            foreach (Entry entry in this.entries) {
                if (entry is CommentEntry) comments += ((CommentEntry)entry).comment + Environment.NewLine;
            }
            return comments;
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


        //public List<ReportEntry> Reports(List<SpeciesValue> species) {
        //    // return the list of reports in this netlist that involve any of the species in the species list
        //    // but include only those reports that are fully covered by those species, just ignore the others
        //    // also ignore the reports that have already be dealt with by previous equilibrate
        //    List<ReportEntry> reportList = new List<ReportEntry> { };
        //    foreach (Entry entry in this.entries) {
        //        if (entry is EquilibrateEntry) {
        //            reportList = new List<ReportEntry> { }; // reset and continue to find the latest reports
        //        } else if (entry is ReportEntry) {
        //            ReportEntry reportEntry = (ReportEntry)entry;
        //            if (reportEntry.flow.CoveredBy(species, out Symbol notCovered)) reportList.Add(reportEntry);
        //        }
        //    }
        //    // if there are no reports, then report all the species
        //    if (reportList.Count == 0) { 
        //        foreach (SpeciesValue s in species) reportList.Add(new ReportEntry(null, new SpeciesFlow(s.symbol), null));
        //    }
        //    return reportList;
        //}

        public List<ParameterEntry> Parameters() {
            List<ParameterEntry> parameterList = new List<ParameterEntry> { };
            foreach (Entry entry in this.entries) {
                if (entry is ParameterEntry parameterEntry) {
                    parameterList.Add(parameterEntry);
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
