using System;
using System.Collections;

namespace CalithaGoldParser
{

	/// <summary>
	/// Type-safe list of Rule objects.
	/// </summary>
	public class RuleCollection : IEnumerable
	{
		private IList list;

		public RuleCollection()
		{
			list = new ArrayList();
		}

		public IEnumerator GetEnumerator()
		{
			return list.GetEnumerator();
		}

		public void Add(Rule rule)
		{
			list.Add(rule);
		}

		public Rule Get(int index)
		{
			return list[index] as Rule;
		}

		public Rule this[int index]
		{
			get {return Get(index);}
		}
	}


	/// <summary>
	/// The Rule consists of the symbols that can be reduced to another symbol.
	/// </summary>
	public class Rule
	{
		private int id;
		private SymbolNonterminal lhs;
		private Symbol[] rhs;

		/// <summary>
		/// Creates a new rule.
		/// </summary>
		/// <param name="id">Id of this rule.</param>
		/// <param name="lhs">Left hand side. The other symbols can be reduced to
		/// this symbol.</param>
		/// <param name="rhs">The right hand side. The symbols that can be reduced.</param>
		public Rule(int id, SymbolNonterminal lhs, Symbol[] rhs)
		{
			this.id = id;
			this.lhs = lhs;
			this.rhs = rhs;
		}

        /// <summary>
        /// String representation of the rule.
        /// </summary>
        /// <returns>The string.</returns>
        //public override String ToString()
        //{
        //    String str = lhs + " ::= ";
        //    for (int i = 0; i < rhs.Length; i++)
        //    {
        //        str += rhs[i] + " ";
        //    }
        //    return str.Substring(0, str.Length - 1);
        //}
        public override String ToString()  //###################### Modified for compatibility with CookV5 version of same functionality
        // The result of this function is used for matching productions as strings
        // If this gives more trouble, look at Parent.Text() in CookV5Parser, which has a parameter that defaults to false to always draw delimiters on terminals
        {
            String str = lhs+" ::= ";
			for (int i=0; i < rhs.Length; i++) {
                Symbol sym = rhs[i];
                if (sym is SymbolTerminal) {
                    string s = sym.ToString();
                    char c = s[0];
                    if (char.IsLetter(c) || s == "-") str += sym + " ";
                    else str += "'" + sym + "'" + " "; // quote the non-alphabetic terminals, but the terminal "-" is an exception for some reason
                } else str += sym + " ";
			}
            if (rhs.Length > 0)
                return str.Substring(0, str.Length - 1);
            else return str; // do not strip the only space left after "::="
        }

		/// <summary>
		/// Id of this rule.
		/// </summary>
		public int Id {get{return id;}}

		/// <summary>
		/// Left hand side. The other symbols can be reduced to
		/// this symbol.
		/// </summary>
		public SymbolNonterminal Lhs {get{return lhs;}}

		/// <summary>
		/// Right hand side. The symbols that can be reduced.
		/// </summary>
		public Symbol[] Rhs {get{return rhs;}}
	}
}
