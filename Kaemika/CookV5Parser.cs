using System;
using System.IO;

namespace Kaemika {

    public class CookV5Reduction : IReduction {
        private GOLD.Reduction reduction;
        private string production;
        public CookV5Reduction(GOLD.Reduction reduction) {
            this.reduction = reduction;
            this.production = reduction.Parent.Text(); // Parent is the lhs
        }
        public override string Production() {
            return this.production;
        }
        public override string Head() {
            return this.reduction.Parent.Text(false);
        }
        public override int Arms() {
            return this.reduction.Count();
        }
        public override bool IsTerminal(int n) {
            return this.reduction[n].Type() == GOLD.SymbolType.Nonterminal;
        }
        public override string Terminal(int n) {
            return (string)reduction[n].Data;
        }
        public override IReduction Nonterminal(int n) {
            return new CookV5Reduction((GOLD.Reduction)reduction[n].Data);
        }

    }

    public class CookV5Parser : IParser {
        private GOLD.Parser parser;

        public CookV5Parser(GOLD.Parser parser)
        { // initialization of the GOLD parser from Resource tables is framework-dependent
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

        public override bool Parse(string source, out IReduction root) {
            //This procedure starts the GOLD Parser Engine and handles each of the
            //messages it returns. Each time a reduction is made, you can create new
            //custom object and reassign the .CurrentReduction property. Otherwise, 
            //the system will use the Reduction object that was returned.
            //
            //The resulting tree will be a pure representation of the language 
            //and will be ready to implement.

            GOLD.ParseMessage response;
            bool done;                      //Controls when we leave the loop
            bool accepted = false;          //Was the parse successful?
            root = null;

            StringReader reader = new StringReader(source);
            parser.Open(reader);
            parser.TrimReductions = false;  //Please read about this feature before enabling  

            done = false;
            while (!done)
            {
                response = parser.Parse();

                switch (response)
                {
                    case GOLD.ParseMessage.LexicalError:
                        //Cannot recognize token
                        failLineNumber = parser.CurrentPosition().Line;
                        failColumnNumber = parser.CurrentPosition().Column;
                        failLength = parser.CurrentToken().Data.ToString().Length;
                        failMessage = "Lexical Error:" + Environment.NewLine +
                                      "Line " + (failLineNumber + 1) + ", Column " + (failColumnNumber + 1) + Environment.NewLine +
                                      "Read: " + parser.CurrentToken().Data;
                        done = true;
                        break;

                    case GOLD.ParseMessage.SyntaxError:
                        //Expecting a different token
                        failLineNumber = parser.CurrentPosition().Line;
                        failColumnNumber = parser.CurrentPosition().Column;
                        failLength = parser.CurrentToken().Data.ToString().Length;
                        failMessage = "Syntax Error:" + Environment.NewLine +
                                      "Line " + (failLineNumber + 1) + ", Column " + (failColumnNumber + 1) + Environment.NewLine +
                                      "Read: " + parser.CurrentToken().Data + Environment.NewLine +
                                      "Expecting one of: " + parser.ExpectedSymbols().Text();
                        done = true;
                        break;

                    case GOLD.ParseMessage.Reduction:
                        //For this project, we will let the parser build a tree of Reduction objects
                        // parser.CurrentReduction = CreateNewObject(parser.CurrentReduction);
                        break;

                    case GOLD.ParseMessage.Accept:
                        //Accepted!
                        root = new CookV5Reduction((GOLD.Reduction)parser.CurrentReduction);    //The root node!                                  
                        done = true;
                        accepted = true;
                        break;

                    case GOLD.ParseMessage.TokenRead:
                        //You don't have to do anything here.
                        break;

                    case GOLD.ParseMessage.InternalError:
                        //INTERNAL ERROR! Something is horribly wrong.
                        done = true;
                        break;

                    case GOLD.ParseMessage.NotLoadedError:
                        //This error occurs if the CGT was not loaded.                   
                        failLineNumber = 0;
                        failColumnNumber = 0;
                        failLength = 0;
                        failMessage = "Tables not loaded";
                        done = true;
                        break;

                    case GOLD.ParseMessage.GroupError:
                        //GROUP ERROR! Unexpected end of file
                        failLineNumber = 0;
                        failColumnNumber = 0;
                        failLength = 0;
                        failMessage = "COMMENT ERROR! Unexpected end of file";
                        done = true;
                        break;
                }
            } //while

            return accepted;
        }

        // Parser Position ---------------------------
        //             reduction[reduction.Count()-1].Position

    }

}
