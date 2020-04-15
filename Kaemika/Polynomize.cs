using System;
using System.Collections.Generic;
using System.Text;

namespace Kaemika {

    abstract class Polynomialize {

        public class ODE {
            public SpeciesFlow var;
            public Flow flow;
            public ODE(SpeciesFlow var, Flow flow) {
                this.var = var;
                this.flow = flow;
            }
            public string Format(Style style) {
                return "∂" + var.Format(style) + " = " + flow.TopFormat(style);
            }
        }

        public class ODEs {
            public List<ODE> odes;
            public ODEs(List<ODE> odes) {
                this.odes = odes;
            }
            public void Add(ODE ode) {
                odes.Add(ode);
            }
            public string Format(Style style) {
                string s = "";
                foreach (ODE ode in odes) {
                    s += ode.Format(style) + Environment.NewLine;
                }
                return s;
            }
        }

        public class Equation {
            public SpeciesFlow var;
            public Flow flow;
            public Equation(SpeciesFlow var, Flow flow) {
                this.var = var;
                this.flow = flow;
            }
        }

        public class Equations {
            public List<Equation> eqs;
            public Equations(List<Equation> eqs) {
                this.eqs = eqs;
            }
            public void Add(Equation equation) {
                eqs.Add(equation);
            }
        }

        // Input is an elementary-function expression
        // Output is a polynomial-function expression
        // Also adds new equations to (the initially empty) eqs
        // all expressions in eqs are polynomial except for the outermost operator
        public static Flow VT(Flow exp, Equations eqs) { // 
            if (exp is SpeciesFlow || exp is BoolFlow || exp is NumberFlow || exp is StringFlow || exp is ConstantFlow) {
                return exp;
            } else if (exp is OpFlow opAdd && opAdd.op == "+") {
                Flow exp0 = VT(opAdd.args[0], eqs);
                Flow exp1 = VT(opAdd.args[1], eqs);
                return OpFlow.Op("+", exp0, exp1);
            } else if (exp is OpFlow opSub && opSub.op == "-" && opSub.arity == 2) {
                Flow exp0 = VT(opSub.args[0], eqs);
                Flow exp1 = VT(opSub.args[1], eqs);
                return OpFlow.Op("-", exp0, exp1);
            } else if (exp is OpFlow opMinus && opMinus.op == "-" && opMinus.arity == 1) {
                Flow exp0 = VT(opMinus.args[0], eqs);
                return OpFlow.Op("-", exp0);
            } else if (exp is OpFlow opMul && opMul.op == "*") {
                Flow exp0 = VT(opMul.args[0], eqs);
                Flow exp1 = VT(opMul.args[1], eqs);
                return OpFlow.Op("*", exp0, exp1);
            } else if (exp is OpFlow opDiv && opDiv.op == "/") {
                SpeciesFlow newVar = new SpeciesFlow(new Symbol("x"));
                Flow exp1 = VT(opDiv.args[1], eqs);
                eqs.Add(new Equation(newVar, OpFlow.Op("/", new NumberFlow(1.0), exp1)));
                return OpFlow.Op("*", opDiv.args[0], newVar);
                //##### Exp(e0/e1)
            } else if (exp is OpFlow opExp && opExp.op == "exp") { 
                SpeciesFlow newVar = new SpeciesFlow(new Symbol("x"));
                Flow exp0 = VT(opExp.args[0], eqs);
                eqs.Add(new Equation(newVar, OpFlow.Op("exp", exp0)));
                return newVar;
            //##### others
            } else throw new Error("VT");
        }

        public static void U(ODEs odes, Equations eqs) {
            int i = 0;
            while (i < eqs.eqs.Count) {
                Equation eq = eqs.eqs[i];
                if (eq.flow is OpFlow opDiv && opDiv.op == "/") {
                    odes.Add(new ODE(eq.var,
                        OpFlow.Op("*",
                            OpFlow.Op("-", OpFlow.Op("^", eq.var, new NumberFlow(2.0))),
                            Diff(opDiv.args[1], odes))));
                } else if (eq.flow is OpFlow opExp && opExp.op == "exp") {
                    odes.Add(new ODE(eq.var,
                        Diff(opExp.args[0], odes)));
                }
                i++;
            }
        }

        public static ODEs Polynomize(ODEs odes) {
            ODEs polyODEs = new ODEs(new List<ODE> { });
            Equations eqs = new Equations(new List<Equation> { });
            foreach (ODE ode in odes.odes) {
                polyODEs.Add(new ODE(ode.var, VT(ode.flow, eqs)));
            }
            U(polyODEs, eqs);
            return polyODEs;
        }

        public static Flow Diff(Flow flow, ODEs odes) {
            return OpFlow.Op("∂", flow);  //#####
        }

        public static ODEs FromCRN(CRN crn) {
            (SpeciesValue[] vars, Flow[] flows) = crn.MeanFlow();
            ODEs odes = new ODEs(new List<ODE>());
            for (int i = 0; i < vars.Length; i++) {
                odes.Add(new ODE(new SpeciesFlow(vars[i].symbol), flows[i]));
            }
            return odes;
        }

    }
}
