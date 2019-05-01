﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Kaemika {
 
    public class TheParser {
        public static IParser parser; // place "the" parser here
    }

    public abstract class IParser {
        public abstract bool Parse(string source, out IReduction reduction);
        public abstract string FailMessage(); // line zero reported as 1, column zero reported as 1
        public abstract int FailLineNumber(); // first line is 0
        public abstract int FailColumnNumber(); // first column is 0
        public abstract int FailLength();
    }

    public abstract class IReduction {
        public abstract string Production(); // the production as a string in BNF
        public abstract string Head(); // the lhs
        public abstract int Arms(); // number of rhs children  
        public abstract bool IsTerminal(int i); // whether the i-th rhs child is a terminal 
        public abstract string Terminal(int i); // the i-th rhs child, assuming it is a terminal
        public abstract IReduction Nonterminal(int i); // the i-th rhs child, assuming it is a nonterminal
        public void DrawReductionTree(GuiInterface gui) {
            StringBuilder tree = new StringBuilder();
            tree.AppendLine("+-" + this.Head());
            this.DrawReduction(tree, 1);
            gui.OutputSetText(tree.ToString());
        }
        private void DrawReduction(StringBuilder tree, int indent) {
            int n;
            string indentText = "";
            for (n = 1; n <= indent; n++) { indentText += "| "; }
            for (n = 0; n < this.Arms(); n++) {
                if (this.IsTerminal(n)) {
                    tree.AppendLine(indentText + "+-" + this.Terminal(n));
                } else {
                    IReduction arm = this.Nonterminal(n);
                    tree.AppendLine(indentText + "+-" + arm.Head());
                    arm.DrawReduction(tree, indent + 1);
                }
            }
        }
    }

    public class Parser {
        // STATEMENTS ---------------------------------------------------------------------------    

        public static Statements ParseTop(IReduction reduction){
            if (reduction.Production()              == "<Top> ::= <Statements>") {
                return ParseStatements(reduction.Nonterminal(0));
            //} else if (reduction.Production()           == "<Top> ::= kaemika '[' <Directives> ']' <Statements>") {
            //    Statements top = new Statements();
            //    top.Append(ParseDirectives(reduction.Nonterminal(2)));
            //    top.Append(ParseStatements(reduction.Nonterminal(4)).statements);
            //    return top;
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
 
        //public static List<Statement> ParseDirectives(Token reduction) {
        //    if (reduction.Production() == "<Directives> ::= <Directives> ',' <Directive>") {
        //        List<Statement> directives = ParseDirectives(reduction.Nonterminal(0));
        //        directives.Add(ParseDirective(reduction.Nonterminal(2)));
        //        return directives;
        //    } else if (reduction.Production() == "<Directives> ::= <Directive>") {
        //        return new List<Statement> { ParseDirective(reduction.Nonterminal(0)) };
        //    } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        //}

        //public static Directive ParseDirective(Token reduction) {
        //    if (reduction.Production()               == "<Directive> ::= Id '=' <Expression>") {
        //        return new Directive(reduction.Terminal(0), ParseExpression(reduction.Nonterminal(2)));
        //    } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        //}

        public static Statements ParseStatements(IReduction reduction) {
            if (reduction.Production()               == "<Statements> ::= <Statements> <Statement>") {
                Statements ss = ParseStatements(reduction.Nonterminal(0));
                return ss.Append(ParseStatement(reduction.Nonterminal(1)));
            } else if (reduction.Production()        == "<Statements> ::= ") {
                return new Statements();
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static List<Statement> ParseStatement(IReduction reduction) {
            if ((reduction.Production() == "<Statement> ::= <Net Instance>")) {
                return ParseStatement(reduction.Nonterminal(0));
            } else if ((reduction.Production() == "<Net Instance> ::= Id '(' ')'")) {
                return new List<Statement> { new NetworkInstance(new Variable(reduction.Terminal(0)), new Arguments()) };
            } else if ((reduction.Production() == "<Net Instance> ::= Id '(' <Args> ')'")) {
                return new List<Statement> { new NetworkInstance(new Variable(reduction.Terminal(0)), ParseArguments(reduction.Nonterminal(2))) };
            } else if (reduction.Production() == "<Statement> ::= bool Id '=' <Expression>") {
                return new List<Statement> { new ValueDefinition(reduction.Terminal(1), new Type("bool"), ParseExpression(reduction.Nonterminal(3))) };
            } else if (reduction.Production() == "<Statement> ::= number Id '=' <Expression>") {
                return new List<Statement> { new ValueDefinition(reduction.Terminal(1), new Type("number"), ParseExpression(reduction.Nonterminal(3))) };
            } else if (reduction.Production() == "<Statement> ::= string Id '=' <Expression>") {
                return new List<Statement> { new ValueDefinition(reduction.Terminal(1), new Type("string"), ParseExpression(reduction.Nonterminal(3))) };
            } else if (reduction.Production() == "<Statement> ::= flow Id '=' <Expression>") {
                return new List<Statement> { new ValueDefinition(reduction.Terminal(1), new Type("flow"), ParseExpression(reduction.Nonterminal(3))) };
            } else if (reduction.Production() == "<Statement> ::= sample <Sample>") {
                return new List<Statement> { ParseSample(reduction.Nonterminal(1)) };
            } else if (reduction.Production() == "<Statement> ::= species <Species>") {
                return new List<Statement> { ParseSpecies(reduction.Nonterminal(1)) };
            } else if ((reduction.Production() == "<Statement> ::= function <Function>")) {
                return new List<Statement> { ParseFunction(reduction.Nonterminal(1)) };
            } else if ((reduction.Production() == "<Statement> ::= network <Network>")) {
                return new List<Statement> { ParseNetwork(reduction.Nonterminal(1)) };
            } else if ((reduction.Production() == "<Statement> ::= amount <Ids> '@' <Expression> <Quantity> <Allocation>")) {
                return new List<Statement> { new Amount(ParseIds(reduction.Nonterminal(1)), ParseExpression(reduction.Nonterminal(3)), ParseQuantity(reduction.Nonterminal(4)), ParseAllocation(reduction.Nonterminal(5))) };
            } else if ((reduction.Production() == "<Statement> ::= mix Id ':=' <Expression> with <Expression>")) {
                return new List<Statement> { new Mix(reduction.Terminal(1), ParseExpression(reduction.Nonterminal(3)), ParseExpression(reduction.Nonterminal(5))) };
            } else if ((reduction.Production() == "<Statement> ::= mix Id with <Expression>")) {
                string id = reduction.Terminal(1);
                return new List<Statement> { new Mix(id, new Variable(id), ParseExpression(reduction.Nonterminal(3))) };
            } else if ((reduction.Production()               == "<Statement> ::= split Id ',' Id ':=' <Expression> by <Expression>")) {
                return new List<Statement> { new Split(reduction.Terminal(1), reduction.Terminal(3), ParseExpression(reduction.Nonterminal(5)), ParseExpression(reduction.Nonterminal(7))) };
            } else if ((reduction.Production()               == "<Statement> ::= dispose <Expression>")) {
                return new List<Statement> { new Dispose(ParseExpression(reduction.Nonterminal(1))) };
            } else if ((reduction.Production()               == "<Statement> ::= equilibrate Id ':=' <Expression> for <Expression>")) {
                return new List<Statement> { new Equilibrate(reduction.Terminal(1), ParseExpression(reduction.Nonterminal(3)), ParseExpression(reduction.Nonterminal(5))) };
            } else if ((reduction.Production()               == "<Statement> ::= equilibrate Id for <Expression>")) {
                string id = reduction.Terminal(1);
                return new List<Statement> { new Equilibrate(id, new Variable(id), ParseExpression(reduction.Nonterminal(3))) };
            } else if ((reduction.Production()               == "<Statement> ::= equilibrate for <Expression>")) {
                return new List<Statement> { new Equilibrate("vessel", new Variable("vessel"), ParseExpression(reduction.Nonterminal(2))) };
            } else if ((reduction.Production()               == "<Statement> ::= transfer <EmptySample> ':=' <Expression>")) {
                ParseEmptySample(reduction.Nonterminal(1), out string name, out Expression volume, out string volumeUnit, out Expression temperature, out string temperatureUnit);
                Expression sample = ParseExpression(reduction.Nonterminal(3));
                return new List<Statement> { new TransferSample(name, volume, volumeUnit, temperature, temperatureUnit, sample) };
            //} else if ((reduction.Production()               == "<Statement> ::= change <Expression> '@' <Expression> <Quantity> <Allocation>")) {
            //    return new List<Statement> { new ChangeSpecies(ParseExpression(reduction.Nonterminal(1)), ParseExpression(reduction.Nonterminal(3)), ParseQuantity(reduction.Nonterminal(4)), ParseAllocation(reduction.Nonterminal(5))) };
            } else if ((reduction.Production()               == "<Statement> ::= report <Reports>")) {
                return ParseReports(reduction.Nonterminal(1));
            } else if ((reduction.Production()               == "<Statement> ::= <Reaction>")) {
                return ParseReaction(reduction.Nonterminal(0)).Select(r => { return (Statement)r; }).ToList(); // gosh! convert List<ReactionDefinition> to List<Statement>
            } else if ((reduction.Production()               == "<Statement> ::= if <Expression> then <Statements> <Else>")) {
                return new List<Statement> { new IfThenElse(ParseExpression(reduction.Nonterminal(1)), ParseStatements(reduction.Nonterminal(3)), ParseElse(reduction.Nonterminal(4))) };
            } else if ((reduction.Production()               == "<Statement> ::= ';'")) {
                return new List<Statement> { };
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Statement ParseSample(IReduction reduction) {
            if (reduction.Production()                   == "<Sample> ::= Id '=' <Expression>") {
                return new ValueDefinition(reduction.Terminal(0), new Type("sample"), ParseExpression(reduction.Nonterminal(2)));
            } else if (reduction.Production()            == "<Sample> ::= <EmptySample>") {
                ParseEmptySample(reduction.Nonterminal(0), out string name, out Expression volume, out string volumeUnit, out Expression temperature, out string temperatureUnit);
                return new SampleDefinition(name, volume, volumeUnit, temperature, temperatureUnit);
            } else if (reduction.Production()            == "<Sample> ::= Id") {
                return new SampleDefinition(reduction.Terminal(0), new NumberLiteral(1.0), "mL", new NumberLiteral(20.0), "Celsius");
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static void ParseEmptySample(IReduction reduction, out string name, out Expression volume, out string volumeUnit, out Expression temperature, out string temperatureUnit) {
            if (reduction.Production() == "<EmptySample> ::= Id '{' <Expression> <Volume> ',' <Expression> <Temperature> '}'") {
                name = reduction.Terminal(0);
                volume = ParseExpression(reduction.Nonterminal(2));
                volumeUnit = ParseVolume(reduction.Nonterminal(3));
                temperature = ParseExpression(reduction.Nonterminal(5));
                temperatureUnit = ParseTemperature(reduction.Nonterminal(6));
            } else { name = null; volume = null; volumeUnit = null; temperature = null; temperatureUnit = null; Gui.Log("UNKNOWN Production " + reduction.Production()); }
        }
        public static string ParseVolume(IReduction reduction) {
            if (reduction.Production()                   == "<Volume> ::= Id") {
                return reduction.Terminal(0);
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static string ParseTemperature(IReduction reduction) {
            if (reduction.Production()                   == "<Temperature> ::= Id") {
                return reduction.Terminal(0);
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static string ParseQuantity(IReduction reduction) {
            if (reduction.Production() == "<Quantity> ::= Id") {
                return reduction.Terminal(0);
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Statement ParseSpecies(IReduction reduction) {
            if (reduction.Production()                   == "<Species> ::= Id '=' <Expression>") {
                return new ValueDefinition(reduction.Terminal(0), new Type("species"), ParseExpression(reduction.Nonterminal(2)));
            } else if (reduction.Production()            == "<Species> ::= '{' <Substances> '}'") {
                return new SpeciesDefinition(ParseSubstances(reduction.Nonterminal(1)), new Statements());
            } else if (reduction.Production()            == "<Species> ::= <Substances> '@' <Expression> <Quantity> <Allocation>") {
                List<Substance> substances = ParseSubstances(reduction.Nonterminal(0));
                List<string> ids = new List<string> { }; foreach (Substance substance in substances) { ids.Add(substance.name); };
                Expression initial = ParseExpression(reduction.Nonterminal(2));
                string quantity = ParseQuantity(reduction.Nonterminal(3));
                Expression allocation = ParseAllocation(reduction.Nonterminal(4));
                return new SpeciesDefinition(substances, new Statements().Add(new Amount(ids, initial, quantity, allocation)));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static List<Substance> ParseSubstances(IReduction reduction) {
            if (reduction.Production() == "<Substances> ::= <Substances> ',' <Substance>") {
                List<Substance> list = ParseSubstances(reduction.Nonterminal(0));
                list.Add(ParseSubstance(reduction.Nonterminal(2)));
                return list;
            } else if (reduction.Production()            == "<Substances> ::= <Substance>") {
                return new List<Substance> { ParseSubstance(reduction.Nonterminal(0)) };
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }  

        public static Substance ParseSubstance(IReduction reduction) {
            if (reduction.Production() == "<Substance> ::= Id '#' <Expression>") {
                return new SubstanceMolarmass(reduction.Terminal(0), ParseExpression(reduction.Nonterminal(2)));
            } else if (reduction.Production()            == "<Substance> ::= Id") {
                return new SubstanceConcentration(reduction.Terminal(0));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }  

        public static Expression ParseAllocation(IReduction reduction) {
            if (reduction.Production() == "<Allocation> ::= in <Expression>") {
                return ParseExpression(reduction.Nonterminal(1));
            } else if (reduction.Production()            == "<Allocation> ::= ") {
                return new Variable("vessel");
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
     
        public static Statement ParseFunction(IReduction reduction) {
            if (reduction.Production()                   == "<Function> ::= Id '=' <Expression>") {
                return new ValueDefinition(reduction.Terminal(0), new Type("function"), ParseExpression(reduction.Nonterminal(2)));
            } else if (reduction.Production()            == "<Function> ::= Id <Fun>") {
                return ParseFunctionDef(reduction.Terminal(0), reduction.Nonterminal(1));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
        public static Statement ParseFunctionDef(string funName, IReduction reduction) {
            if (reduction.Production()                   == "<Fun> ::= <Headers> '{' <Statements> return <Expression> '}'") {
                List<Parameters> headers = ParseHeaders(reduction.Nonterminal(0));
                return new FunctionDefinition(funName, headers[0],
                    CurriedFunctionAbstraction(headers, 1,
                        new BlockExpression(ParseStatements(reduction.Nonterminal(2)), ParseExpression(reduction.Nonterminal(4)))));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Statement ParseNetwork(IReduction reduction) {
            if (reduction.Production()                   == "<Network> ::= Id '=' <Expression>") {
                return new ValueDefinition(reduction.Terminal(0), new Type("network"), ParseExpression(reduction.Nonterminal(2)));
            } else if (reduction.Production()                   == "<Network> ::= Id <Net>") {
                return ParseNetworkDef(reduction.Terminal(0), reduction.Nonterminal(1));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
        public static Statement ParseNetworkDef(string netName, IReduction reduction) {
            if (reduction.Production()                   == "<Net> ::= <Header> '{' <Statements> '}'") {
                return new NetworkDefinition(netName, ParseHeader(reduction.Nonterminal(0)), ParseStatements(reduction.Nonterminal(2)));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
        
        public static List<Parameters> ParseHeaders(IReduction reduction) {
            if (reduction.Production()                   == "<Headers> ::= <Headers> <Header>") {
                List<Parameters> headers = ParseHeaders(reduction.Nonterminal(0));
                headers.Add(ParseHeader(reduction.Nonterminal(1)));
                return headers;
            } else if (reduction.Production()           == "<Headers> ::= <Header>") {
                return new List<Parameters>() { ParseHeader(reduction.Nonterminal(0)) };
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Parameters ParseHeader(IReduction reduction) {
            if (reduction.Production()                   == "<Header> ::= '(' ')'") {
                return new Parameters();
            } else if (reduction.Production()            == "<Header> ::= '(' <Params> ')'") {
                return ParseParams(reduction.Nonterminal(1));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Parameters ParseParams(IReduction reduction) {
            if (reduction.Production()                   == "<Params> ::= <Params> ',' <Param>") {
                Parameters parameters = ParseParams(reduction.Nonterminal(0));
                ParseParam(parameters, reduction.Nonterminal(2));
                return parameters;
            } else if (reduction.Production()           == "<Params> ::= <Param>") {
                Parameters parameters = new Parameters();
                ParseParam(parameters, reduction.Nonterminal(0));
                return parameters;
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
        public static void ParseParam(Parameters parameters, IReduction reduction) {
            if ((reduction.Production()               == "<Param> ::= bool <Ids>") || 
                (reduction.Production()               == "<Param> ::= number <Ids>") ||
                (reduction.Production()               == "<Param> ::= string <Ids>") ||
                (reduction.Production()               == "<Param> ::= flow <Ids>") ||
                (reduction.Production()               == "<Param> ::= species <Ids>") ||
                (reduction.Production()               == "<Param> ::= sample <Ids>") ||
                (reduction.Production()               == "<Param> ::= function <Ids>") ||
                (reduction.Production()               == "<Param> ::= network <Ids>")) {
                    string type = reduction.Terminal(0);
                List<string> ids = ParseIds(reduction.Nonterminal(1));
                foreach (string id in ids) { parameters.Add(new Parameter(new Type(type), id)); }
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); }
        }

        public static List<string> ParseIds(IReduction reduction) {
            if (reduction.Production()                   == "<Ids> ::= <Ids> Id") {
                List<string> ids = ParseIds(reduction.Nonterminal(0));
                ids.Add(reduction.Terminal(1));
                return ids;
            } else if (reduction.Production()            == "<Ids> ::= Id") {
                return new List<string> { reduction.Terminal(0) };
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Arguments ParseArguments(IReduction reduction) {
            if (reduction.Production()                   == "<Args> ::= <Args> ',' <Expression>") {
                return ParseArguments(reduction.Nonterminal(0)).Add(ParseExpression(reduction.Nonterminal(2)));
            } else if (reduction.Production()            == "<Args> ::= <Expression>") {
                return new Arguments().Add(ParseExpression(reduction.Nonterminal(0)));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Statements ParseElse(IReduction reduction) {
            if (reduction.Production()                   == "<Else> ::= elseif <Expression> then <Statements> <Else>") {
                return new Statements().Add(new IfThenElse(ParseExpression(reduction.Nonterminal(1)), ParseStatements(reduction.Nonterminal(3)), ParseElse(reduction.Nonterminal(4))));
            } else if (reduction.Production()            == "<Else> ::= else <Statements> end") {
                return ParseStatements(reduction.Nonterminal(1));
            } else if (reduction.Production()            == "<Else> ::= end") {
                return new Statements();
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static List<Statement> ParseReports(IReduction reduction) {
            if (reduction.Production()                   == "<Reports> ::= <Reports> ',' <Report>") {
                List<Statement> reports = ParseReports(reduction.Nonterminal(0));
                reports.Add(ParseReport(reduction.Nonterminal(2)));
                return reports;
            } else if (reduction.Production()            == "<Reports> ::= <Report>") {
                List<Statement> reports = new List<Statement>();
                reports.Add(ParseReport(reduction.Nonterminal(0)));
                return reports;
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Report ParseReport(IReduction reduction) {
            if (reduction.Production()                   == "<Report> ::= <Expression> as '[' <Args> ']'") {
                return new Report(ParseExpression(reduction.Nonterminal(0)), ParseArguments(reduction.Nonterminal(3)).arguments);
            } else if (reduction.Production()            == "<Report> ::= <Expression> as <Expression>") {
                return new Report(ParseExpression(reduction.Nonterminal(0)), new List<Expression> { ParseExpression(reduction.Nonterminal(2)) });
            } else if (reduction.Production()            == "<Report> ::= <Expression>") {
                return new Report(ParseExpression(reduction.Nonterminal(0)), null);
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        // REACTIONS ---------------------------------------------------------------------------    

        public static List<ReactionDefinition> ParseReaction(IReduction reduction) {
            if (reduction.Production()                          == "<Reaction> ::= <Transition>") {
                return ParseTransition(reduction.Nonterminal(0));
            } else if (reduction.Production()                   == "<Reaction> ::= <Complex> '>>' <Transition>") {
                Complex complex = ParseComplex(reduction.Nonterminal(0));
                List<ReactionDefinition> reactions = ParseTransition(reduction.Nonterminal(2));
                // return reactions.Select(r => { return new ReactionDefinition(r.reactants.Add(complex.complex), r.products.Add(complex.complex), r.rate); }).ToList();
                return reactions.Select(r => { return new ReactionDefinition(new SumComplex(complex, r.reactants), new SumComplex(complex, r.products), r.rate); }).ToList();
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static List<ReactionDefinition> ParseTransition(IReduction reduction) {
            if (reduction.Production()                       == "<Transition> ::= <Complex> '->' <Complex>") {
                return new List<ReactionDefinition> { new ReactionDefinition(ParseComplex(reduction.Nonterminal(0)), ParseComplex(reduction.Nonterminal(2)), new MassActionRate()) };
            } else if (reduction.Production()                == "<Transition> ::= <Complex> '->' '{' <Rate> '}' <Complex>") {
                return new List<ReactionDefinition> { new ReactionDefinition(ParseComplex(reduction.Nonterminal(0)), ParseComplex(reduction.Nonterminal(5)), ParseRate(reduction.Nonterminal(3))) };
            } else if (reduction.Production()                == "<Transition> ::= <Complex> '->' <Complex> '{' <Rate> '}'") {
                return new List<ReactionDefinition> { new ReactionDefinition(ParseComplex(reduction.Nonterminal(0)), ParseComplex(reduction.Nonterminal(2)), ParseRate(reduction.Nonterminal(4))) };
            } else if (reduction.Production()                == "<Transition> ::= <Complex> '<->' <Complex>") {
                Complex lhs = ParseComplex(reduction.Nonterminal(0));  Complex rhs = ParseComplex(reduction.Nonterminal(2));
                return new List<ReactionDefinition> { new ReactionDefinition(lhs, rhs, new MassActionRate()), new ReactionDefinition(rhs, lhs, new MassActionRate()) };
            } else if (reduction.Production()                == "<Transition> ::= <Complex> '{' <Rate> '}' '<->' '{' <Rate> '}' <Complex>") {
                Complex lhs = ParseComplex(reduction.Nonterminal(0)); Rate lhsRate = ParseRate(reduction.Nonterminal(2));
                Rate rhsRate = ParseRate(reduction.Nonterminal(6)); Complex rhs = ParseComplex(reduction.Nonterminal(8));
                return new List<ReactionDefinition> { new ReactionDefinition(lhs, rhs, rhsRate), new ReactionDefinition(rhs, lhs, lhsRate) };
            } else if (reduction.Production()                == "<Transition> ::= <Complex> '<->' <Complex> '{' <Rate> '}' '{' <Rate> '}'") {
                Complex lhs = ParseComplex(reduction.Nonterminal(0)); Complex rhs = ParseComplex(reduction.Nonterminal(2));
                Rate lhsRate = ParseRate(reduction.Nonterminal(4)); Rate rhsRate = ParseRate(reduction.Nonterminal(7)); 
                return new List<ReactionDefinition> { new ReactionDefinition(lhs, rhs, rhsRate), new ReactionDefinition(rhs, lhs, lhsRate) };
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Rate ParseRate(IReduction reduction) {
            if (reduction.Production()                          == "<Rate> ::= <Expression>") {
                return new MassActionRate(ParseExpression(reduction.Nonterminal(0)));
            } else if (reduction.Production()                == "<Rate> ::= <Expression> ',' <Expression>") {
                return new MassActionRate(ParseExpression(reduction.Nonterminal(0)), ParseExpression(reduction.Nonterminal(2)));
            } else if (reduction.Production()                == "<Rate> ::= '{' <Expression> '}'") {
                return new GeneralRate(ParseExpression(reduction.Nonterminal(1)));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Complex ParseComplex(IReduction reduction) {
            if (reduction.Production()                   == "<Complex> ::= <Complex> '+' <Simplex>") {
                return new SumComplex(ParseComplex(reduction.Nonterminal(0)), ParseSimplex(reduction.Nonterminal(2)));
            } else if (reduction.Production()            == "<Complex> ::= <Simplex>") {
                return ParseSimplex(reduction.Nonterminal(0));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Simplex ParseSimplex(IReduction reduction) {
            if (reduction.Production()                   == "<Simplex> ::= Integer Id") {
                int n = int.Parse(reduction.Terminal(0));
                Variable species = new Variable(reduction.Terminal(1));
                return new Simplex(new NumberLiteral(n), species);
            } else if (reduction.Production()            == "<Simplex> ::= Id '*' Id") {
                return new Simplex(new Variable(reduction.Terminal(0)), new Variable(reduction.Terminal(2)));
            } else if (reduction.Production()            == "<Simplex> ::= Id") {
                return new Simplex(null, new Variable(reduction.Terminal(0)));
            } else if (reduction.Production()            == "<Simplex> ::= '#'") {
                return new Simplex(null, null);
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        // EXPRESSIONS ---------------------------------------------------------------------------    

        public static Expression ParseExpression(IReduction reduction) {
            if ((reduction.Production() == "<Expression> ::= <Or Exp>") ||
                (reduction.Production() == "<Or Exp> ::= <And Exp>") ||
                (reduction.Production() == "<And Exp> ::= <Not Exp>") ||
                (reduction.Production() == "<Not Exp> ::= <Comp Exp>") ||
                (reduction.Production() == "<Comp Exp> ::= <Sum Exp>") ||
                (reduction.Production() == "<Sum Exp> ::= <Mult Exp>") ||
                (reduction.Production() == "<Mult Exp> ::= <Neg Exp>") ||
                (reduction.Production() == "<Neg Exp> ::= <Powr Exp>") ||
                (reduction.Production() == "<Powr Exp> ::= <Base Exp>")) {
                    return ParseExpression(reduction.Nonterminal(0));
            } else if ((reduction.Production() == "<Neg Exp> ::= - <Powr Exp>") ||    // somehow it must be "-", not "'-'"
                       (reduction.Production() == "<Not Exp> ::= not <Comp Exp>")) {
                return new FunctionInstance(new Variable(reduction.Terminal(0)), new Arguments().Add(ParseExpression(reduction.Nonterminal(1))));
            } else if ((reduction.Production() == "<Sum Exp> ::= <Sum Exp> '+' <Mult Exp>") ||
                (reduction.Production() == "<Sum Exp> ::= <Sum Exp> - <Mult Exp>") ||  // somehow it must be "-", not "'-'"
                (reduction.Production() == "<Mult Exp> ::= <Mult Exp> '*' <Neg Exp>") ||
                (reduction.Production() == "<Mult Exp> ::= <Mult Exp> '/' <Neg Exp>") ||
                (reduction.Production() == "<Or Exp> ::= <Or Exp> or <And Exp>") ||
                (reduction.Production() == "<And Exp> ::= <And Exp> and <Not Exp>") ||
                (reduction.Production() == "<Comp Exp> ::= <Sum Exp> '=' <Sum Exp>") ||
                (reduction.Production() == "<Comp Exp> ::= <Sum Exp> '<>' <Sum Exp>") ||
                (reduction.Production() == "<Comp Exp> ::= <Sum Exp> '<=' <Sum Exp>") ||
                (reduction.Production() == "<Comp Exp> ::= <Sum Exp> '>=' <Sum Exp>") ||
                (reduction.Production() == "<Comp Exp> ::= <Sum Exp> '<' <Sum Exp>") ||
                (reduction.Production() == "<Comp Exp> ::= <Sum Exp> '>' <Sum Exp>") ||
                (reduction.Production() == "<Powr Exp> ::= <Powr Exp> '^' <Base Exp>")) {
                return new FunctionInstance(new Variable(reduction.Terminal(1)), new Arguments().Add(ParseExpression(reduction.Nonterminal(0))).Add(ParseExpression(reduction.Nonterminal(2))));
            } else  if (reduction.Production()       == "<Base Exp> ::= if <Expression> then <Expression> <Else Exp>") {
                return new FunctionInstance(new Variable("if"), new Arguments().Add(ParseExpression(reduction.Nonterminal(1))).Add(ParseExpression(reduction.Nonterminal(3))).Add(ParseElseExpression(reduction.Nonterminal(4))));
            } else  if (reduction.Production()       == "<Base Exp> ::= <Fun Instance>") {
                return ParseExpressionInstance(reduction.Nonterminal(0));
            } else if ((reduction.Production() == "<Base Exp> ::= true") ||
                       (reduction.Production() == "<Base Exp> ::= false")) {
                return new BoolLiteral(bool.Parse(reduction.Terminal(0)));
            } else  if (reduction.Production()       == "<Base Exp> ::= Integer") {
                return new NumberLiteral(int.Parse(reduction.Terminal(0)));
            } else  if (reduction.Production()       == "<Base Exp> ::= Float") {
                return new NumberLiteral(double.Parse(reduction.Terminal(0)));
            } else  if (reduction.Production()       == "<Base Exp> ::= QuotedString") {
                return new StringLiteral(ParseString(reduction.Terminal(0)));
            } else  if (reduction.Production()       == "<Base Exp> ::= fun <Fun>") {
                return ParseFunctionAbstraction(reduction.Nonterminal(1));
            } else  if (reduction.Production()       == "<Base Exp> ::= net <Net>") {
                return ParseNetworkAbstraction(reduction.Nonterminal(1));
            } else  if (reduction.Production()       == "<Base Exp> ::= '(' <Expression> ')'") {
                return ParseExpression(reduction.Nonterminal(1));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static string ParseString(string s) {
            // remove endquotes and replace any \x by x, except replace \n by linefeed
            string e = ""; int i = 1;
            while (i < s.Length-1) {
                if (s[i] == '\\') {
                    if (i+1 < s.Length-1) {
                        i++;
                        if (s[i] == 'n') { e += Environment.NewLine; }
                        else { e += s[i]; }
                    } else e += s[i];
                } else e += s[i];
                i++;
            }
            return e;
        }

        public static string FormatString(string s) {
            // add endquotes and replace any " and \ by \" and \\. Also replace linefeed by "\\n"
            string e = "\""; int i = 0;
            while (i < s.Length) {
                if (s[i] == '\\' || s[i] == '"') { e += '\\'; e += s[i]; }
                else if (s[i] == Environment.NewLine[0]) { e += "\\n"; }
                else if (s[i] == '\r' || s[i] == '\n') { }
                else { e += s[i]; }
                i++;
            }
            return e + "\"";
        }

        public static Expression ParseFunctionAbstraction(IReduction reduction) {
            if (reduction.Production()               == "<Fun> ::= <Headers> '{' <Statements> return <Expression> '}'") {
                List<Parameters> headers = ParseHeaders(reduction.Nonterminal(0));
                return new FunctionAbstraction(headers[0],
                    CurriedFunctionAbstraction(headers, 1,
                        new BlockExpression(ParseStatements(reduction.Nonterminal(2)), ParseExpression(reduction.Nonterminal(4)))));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
        public static BlockExpression CurriedFunctionAbstraction(List<Parameters> headers, int next, BlockExpression body) {
            if (next >= headers.Count) return body;
            else return new BlockExpression(new Statements(), new FunctionAbstraction(headers[next], CurriedFunctionAbstraction(headers, next + 1, body)));
        }

        public static Expression ParseNetworkAbstraction(IReduction reduction) {
            if (reduction.Production()               == "<Net> ::= <Header> '{' <Statements> '}'") {
                return new NetworkAbstraction(ParseHeader(reduction.Nonterminal(0)), ParseStatements(reduction.Nonterminal(2)));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

        public static Expression ParseExpressionInstance(IReduction reduction) {
            if (reduction.Production()               == "<Fun Instance> ::= Id") {
                return new Variable(reduction.Terminal(0));
            } else  if (reduction.Production()       == "<Fun Instance> ::= <Fun Instance> '(' ')'") {
                return new FunctionInstance(ParseExpressionInstance(reduction.Nonterminal(0)), new Arguments());
            } else  if (reduction.Production()       == "<Fun Instance> ::= <Fun Instance> '(' <Args> ')'") {
                return new FunctionInstance(ParseExpressionInstance(reduction.Nonterminal(0)), ParseArguments(reduction.Nonterminal(2)));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }
        
        public static Expression ParseElseExpression(IReduction reduction) {
            if (reduction.Production()                   == "<Else Exp> ::= elseif <Expression> then <Expression> <Else Exp>") {
                return new FunctionInstance(new Variable("if"), new Arguments().Add(ParseExpression(reduction.Nonterminal(1))).Add(ParseExpression(reduction.Nonterminal(3))).Add(ParseElseExpression(reduction.Nonterminal(4))));
            } else if (reduction.Production()            == "<Else Exp> ::= else <Expression> end") {
                return ParseExpression(reduction.Nonterminal(1));
            } else { Gui.Log("UNKNOWN Production " + reduction.Production()); return null; }
        }

    };

}