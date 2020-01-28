using System;
using System.Collections.Generic;

namespace Kaemika {

    public class Symbol {
        private string name;
        private int variant;    // global variant of symbol name, making it unique
        public Symbol(string name) {
            this.name = name;
            this.variant = Exec.NewUID();
        }
        public string Raw() { return name; }
        public bool IsVesselVariant() { return name == "vessel"; }
        public bool SameSymbol(Symbol otherSymbol) {
            return this.variant == otherSymbol.variant;
        }
        public string Replace(string name, string content, string replacement) {
            if (name.Contains(content) && name.Contains(replacement)) throw new Error("Cannot replace '" + content + "' with '" + replacement + "' in '" + name + "'");
            else return name.Replace(content, replacement);
        }
        public string Replace(string name, SwapMap swap) {
            foreach (var keypair in swap.Pairs()) name = Replace(name, keypair.Key, keypair.Value);
            return name;
        }
        public string Format(Style style) {
            string varchar = style.Varchar();
            if (varchar == null) return this.name;                                           // don't show the variant
            else {
                string sname = Replace(this.name, style.Swap());
                AlphaMap map = style.Map();
                if (map == null) return sname + varchar + this.variant.ToString();           // show the variant, don't remap it
                else {                                                                       // remap the variant
                    if (!map.ContainsKey(this.variant)) {           // never encountered this variant before: it is name unique?
                        int variantNo = -1;
                        string variantName = "";
                        do {
                            variantNo += 1;
                            variantName = (variantNo == 0) ? sname : sname + varchar + variantNo.ToString();
                        } while (map.ContainsValue(variantName));
                        map.Assign(this.variant, variantName);            // assign a unique name to this variant
                    }
                    return map.Extract(this.variant);
                }
            }
        }
        public bool Precedes(Symbol other) { // lexicographic order
            int comp = String.Compare(this.name, other.name);
            if (comp != 0) return comp < 0;
            return this.variant < other.variant;
        }
    }
    public class SymbolComparer : EqualityComparer<Symbol> {
        public override bool Equals(Symbol a, Symbol b) { return a.SameSymbol(b); }
        public override int GetHashCode(Symbol a) { return a.Raw().GetHashCode(); }
        public static SymbolComparer comparer = new SymbolComparer();
    }

    public abstract class Scope {
        public abstract bool Lookup(string var); // return true if var is defined
        public abstract string Format();
        public Scope Extend(Pattern pattern) {
            Scope scope = this;
            if (pattern is SinglePattern) { 
                scope = new ConsScope((pattern as SinglePattern).name, scope);
            } else if (pattern is ListPattern) {
                scope = scope.Extend((pattern as ListPattern).list.parameters);
            } else if (pattern is HeadConsPattern) {
                scope = scope.Extend((pattern as HeadConsPattern).list.parameters);
                scope = new ConsScope((pattern as HeadConsPattern).single.name, scope);
            } else if (pattern is TailConsPattern) {
                scope = new ConsScope((pattern as TailConsPattern).single.name, scope);
                scope = scope.Extend((pattern as TailConsPattern).list.parameters);
            } else throw new Error("Pattern");
            return scope;
        }
        public Scope Extend(List<Pattern> patterns) {
            Scope scope = this;
            foreach (Pattern pattern in patterns) { //  (a,b,c)+this = c,b,a,this
                scope = scope.Extend(pattern);
            }
            return scope;
        }
    }
    public class NullScope : Scope {
        public override bool Lookup(string var) {
            return false;
        }
        public override string Format() {
            return "";
        }
        private Scope builtIn = null;
        private Scope CopyBuiltIn(Env builtInEnv) {
            if (builtInEnv is NullEnv) return new NullScope();
            else {
                ValueEnv consEnv = (ValueEnv)builtInEnv;
                return new ConsScope(consEnv.symbol.Raw(), CopyBuiltIn(consEnv.next));
            }
        }
        public Scope BuiltIn(SampleValue vessel) { //we park this method inside NullScope for convenience
            if (builtIn == null) builtIn = CopyBuiltIn(new NullEnv().BuiltIn(vessel));
            return builtIn;
        }
    }
    public class ConsScope : Scope {
        public string name;
        public Scope next;
        public ConsScope(string name, Scope next) {
            this.name = name;
            this.next = next;
        }
        public override bool Lookup(string name) {
            if (name == this.name) return true;
            else return next.Lookup(name);
        }
        public override string Format() {
            string first = next.Format();
            string last = this.name;
            if (first == "") return last; else return first + Environment.NewLine + last;
        }
    }

}
