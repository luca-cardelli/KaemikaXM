using System;
using CalithaGoldParser;

namespace Kaemika {

    public class CalithaReduction : IReduction {
        private NonterminalToken token;
        private string production;
        public CalithaReduction (NonterminalToken token) {
            this.token = token;
            this.production = token.Rule.ToString();
        }
        public override string Production() {
            return this.production;
        }
        public override string Head() {
            return token.Rule.Lhs.ToString();
        }
        public override int Arms() {
            return token.Rule.Rhs.Length;
        }
        public override bool IsTerminal(int n) {
            return token.Tokens[n] is TerminalToken;
        }
        public override string Terminal(int n) {
            return ((TerminalToken)token.Tokens[n]).Text;
        }
        public override IReduction Nonterminal(int n) {
            return new CalithaReduction((NonterminalToken)token.Tokens[n]);
        }

    }

    public class CalithaParser : IParser {
        private LALRParser parser;

        public CalithaParser(LALRParser parser) { // initialization of the GOLD parser from Resource tables is framework-dependent
            // this is if we want to generate objects during parsing via callbacks: see TextCalculator.CalcParserV1
            //parser.OnReduce += new LALRParser.ReduceHandler(ReduceEvent);
            //parser.OnTokenRead += new LALRParser.TokenReadHandler(TokenReadEvent);
            //parser.OnAccept += new LALRParser.AcceptHandler(AcceptEvent);
            parser.OnTokenError += new LALRParser.TokenErrorHandler(TokenErrorEvent);
            parser.OnParseError += new LALRParser.ParseErrorHandler(ParseErrorEvent);
            this.parser = parser;
        }

        private string failMessage;
        private int failLineNumber;
        private int failColumnNumber;
        private int failLength;
        public override string FailMessage() { return failMessage; }
        public override int FailLineNumber() { return failLineNumber; }
        public override int FailColumnNumber() { return failColumnNumber; }
        public override int FailLength() { return failLength; }

        public override bool Parse(string source, out IReduction reduction) {
            NonterminalToken token = parser.Parse(source);
            if (token == null) {
                reduction = null;
                return false;
            } else {
                reduction = new CalithaReduction(token);
                return true;
            }
        }

        private void TokenErrorEvent(LALRParser parser, TokenErrorEventArgs args) {
            failLineNumber = args.Token.Location.LineNr;
            failColumnNumber = args.Token.Location.ColumnNr;
            failLength = args.Token.Text.Length;
            failMessage = "Lexical Error:" + Environment.NewLine +
                          "Line " + (failLineNumber + 1) + ", Column " + (failColumnNumber + 1) + Environment.NewLine +
                          "Read: " + args.Token.ToString();
        }

        private void ParseErrorEvent(LALRParser parser, ParseErrorEventArgs args) {
            failLineNumber = args.UnexpectedToken.Location.LineNr;
            failColumnNumber = args.UnexpectedToken.Location.ColumnNr;
            failMessage = "Parse error caused by token: '" + args.UnexpectedToken.ToString()+"'";
            failMessage = "Syntax Error:" + Environment.NewLine +
                          "Line " + (failLineNumber + 1) + ", Column " + (failColumnNumber + 1) + Environment.NewLine +
                          "Read: " + args.UnexpectedToken.ToString() + Environment.NewLine +
                          "Expecting one of: " + args.ExpectedTokens.ToString();
        }

    };

}