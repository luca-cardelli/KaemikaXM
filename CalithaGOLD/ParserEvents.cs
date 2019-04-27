using System;
using CalithaGoldParser.lalr;

namespace CalithaGoldParser
{

	/// <summary>
	/// Event arguments for the TokenRead event.
	/// </summary>
	public class TokenReadEventArgs : EventArgs
	{
		private TerminalToken token;
		private bool contin;

		public TokenReadEventArgs(TerminalToken token)
		{
			this.token = token;
			contin = true;
		}

		/// <summary>
		/// The terminal token that will be processed by the LALR parser.
		/// </summary>
		public TerminalToken Token {get{return token;}}

		/// <summary>
		/// Determines if the parse process should continue
		/// after this event. True by default.
		/// </summary>
		public bool Continue
		{
			get {return contin;} 
			set {contin = value;}
		}

	}

	/// <summary>
	/// Event arguments for the Shift event.
	/// </summary>
	public class ShiftEventArgs : EventArgs
	{
		private TerminalToken token;
		private State newState;

		public ShiftEventArgs(TerminalToken token, State newState)
		{
			this.token = token;
			this.newState = newState;
		}

		/// <summary>
		/// The terminal token that is shifted onto the stack.
		/// </summary>
		public TerminalToken Token{get{return token;}}

		/// <summary>
		/// The state that the parser is in after the shift.
		/// </summary>
		public State NewState{get{return newState;}}
	}

	/// <summary>
	/// Event arguments for the Reduce event.
	/// </summary>
	public class ReduceEventArgs : EventArgs
	{
		private Rule rule;
		private NonterminalToken token;
		private State newState;
		private bool contin;

		public ReduceEventArgs(Rule rule, NonterminalToken token, State newState)
		{
			this.rule = rule;
			this.token = token;
			this.newState = newState;
			this.contin = true;
		}

		/// <summary>
		/// The rule that was used to reduce tokens.
		/// </summary>
		public Rule Rule{get{return rule;}}

		/// <summary>
		/// The nonterminal token that consists of nonterminal or terminal
		/// tokens that has been reduced by the rule.
		/// </summary>
		public NonterminalToken Token{get{return token;}}

		/// <summary>
		/// The state after the reduction.
		/// </summary>
		public State NewState{get{return newState;}}

		/// <summary>
		/// Determines if the parse process should continue
		/// after this event. True by default.
		/// </summary>
		public bool Continue
		{
			get {return contin;} 
			set {contin = value;}
		}
	}

	/// <summary>
	/// Event arguments after a goto event.
	/// </summary>
	public class GotoEventArgs : EventArgs
	{
		private SymbolNonterminal symbol;
		private State newState;

		public GotoEventArgs(SymbolNonterminal symbol, State newState)
		{
			this.symbol = symbol;
			this.newState = newState;
		}

		/// <summary>
		/// The symbol that causes the goto event.
		/// </summary>
		public SymbolNonterminal Symbol{get{return symbol;}}

		/// <summary>
		/// The state after the goto event.
		/// </summary>
		public State NewState{get{return newState;}}
	}

	/// <summary>
	/// Event argument for an Accept event.
	/// </summary>
	public class AcceptEventArgs : EventArgs
	{
		private NonterminalToken token;

		public AcceptEventArgs(NonterminalToken token)
		{
			this.token = token;
		}

		/// <summary>
		/// The fully reduced nonterminal token that consists of
		/// all the other reduced tokens.
		/// </summary>
		public NonterminalToken Token{get{return token;}}
	}

	/// <summary>
	/// Event arguments for a token read error.
	/// </summary>
	public class TokenErrorEventArgs : EventArgs
	{
		private TerminalToken token;
		private bool contin;

		public TokenErrorEventArgs(TerminalToken token)
		{
			this.token = token;
			this.contin = false;
		}

		/// <summary>
		/// The error token that also consists of the character that causes the
		/// token read error.
		/// </summary>
		public TerminalToken Token {get{return token;}}

		/// <summary>
		/// The continue property can be set during the token error event,
		/// to continue the parsing process. The current token will be ignored.
		/// Default value is false.
		/// </summary>
		public bool Continue
		{
			get{return contin;}
			set{this.contin = value;}
		}
	}

    public enum ContinueMode {Stop, Insert, Skip}


	/// <summary>
	/// Event arguments for the Parse Error event.
	/// </summary>
	public class ParseErrorEventArgs : EventArgs
	{
		private TerminalToken unexpectedToken;
		private SymbolCollection expectedTokens;
		private ContinueMode contin;
		private TerminalToken nextToken;

		public ParseErrorEventArgs(TerminalToken unexpectedToken,
			                       SymbolCollection expectedTokens)
		{
			this.unexpectedToken = unexpectedToken;
			this.expectedTokens = expectedTokens;
			this.contin = ContinueMode.Stop;
			this.nextToken = null;
		}

		/// <summary>
		/// The token that caused this parser error.
		/// </summary>
		public TerminalToken UnexpectedToken { get{return unexpectedToken;}}

		/// <summary>
		/// The symbols that were expected by the parser.
		/// </summary>
		public SymbolCollection ExpectedTokens{get{return expectedTokens;}}

		/// <summary>
		/// The continue property can be set during the parse error event.
		/// It can be set to the following:
		/// (1) Stop to not try to parse the rest of the input.
		/// (2) Insert will pretend that the next token is the one set in
		///     NextToken after which the current "bad" token will be parsed again.
		/// (3) Skip will just ignore the current bad token and proceed to parse
		///     the input as if nothing happened.
		/// The default value is Stop.
		/// </summary>
		public ContinueMode Continue
		{
			get{return contin;}
			set{this.contin = value;}
		}

		/// <summary>
		/// If the continue property is set to true, then NextToken will be the
		/// next token to be used as input to the parser (it will become the lookahead token).
		/// The default value is null, which means that the next token will be read from the
		/// normal input stream.
		/// stream.
		/// </summary>
		public TerminalToken NextToken
		{
			get{return nextToken;}
			set{this.nextToken = value;}
		}

	}

	/// <summary>
	/// Event argument for a CommentRead event.
	/// </summary>
	public class CommentReadEventArgs : EventArgs
	{
		private string comment;
		private string content;
		private bool lineComment;

        /// <summary>
        /// Creates a new arguments object for a CommentRead event.
        /// </summary>
        /// <param name="comment">The comment including comment characters</param>
        /// <param name="content">The content of the comment</param>
        /// <param name="lineComment">True for a line comment, otherwise a 
        ///                           block comment.</param>
		public CommentReadEventArgs(string comment,
			                        string content,
			                        bool lineComment)
		{
			this.comment = comment;
			this.content = content;
			this.lineComment = lineComment;
		}

		/// <summary>
		/// The comment that has been read, including comment characters.
		/// </summary>
		public string Comment{get{return comment;}}

		/// <summary>
		/// The content of the comment.
		/// </summary>
		public string Content{get{return content;}}

		/// <summary>
		/// Determines if it is a line or block comment.
		/// </summary>
		public bool LineComment{get{return lineComment;}}
	
	}


}
