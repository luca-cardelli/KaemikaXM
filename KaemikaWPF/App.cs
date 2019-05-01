using System;
using System.IO;
using System.Windows.Forms;
using CalithaGoldParser;
using Kaemika;

namespace KaemikaWPF
{
    public static class App
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        // // Need to get rid of GOLD Engine assmembly linking because it seems to interfere with building the Android version, since it requires VisualBasic runtime
        //public static GOLD.Parser CookV5GoldParser() {
        //    GOLD.Parser parser = new GOLD.Parser();
        //    try {
        //        //if (parser.LoadTables(Application.StartupPath + "\\kaemika.egt")) setupDone = true;
        //        if (parser.LoadTables(new System.IO.BinaryReader(new System.IO.MemoryStream(Properties.Resources.kaemika)))) return parser;
        //        else throw new Error("Parser EGT failed to load");
        //    } catch (GOLD.ParserException ex) {
        //        throw new Error("Parser loading failed: " + ex.Message);
        //    }
        //}

       public static LALRParser GoldParser() {
            try {
                Stream stream = new MemoryStream(Properties.Resources.kaemikaCGT);
                //numberFormatInfo = new NumberFormatInfo();
                //numberFormatInfo.NumberDecimalSeparator = ".";
                CGTReader reader = new CGTReader(stream);
                LALRParser parser = reader.CreateNewParser();
                parser.TrimReductions = false;
                parser.StoreTokens = LALRParser.StoreTokensMode.NoUserObject;
                return parser;
            } catch (Exception ex) {
                throw new Error("Parser loading failed: " + ex.Message);
            }
        }

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            GUI_Windows gui = new GUI_Windows(new GUI());
            Gui.gui = gui;

            // Choose a Parser, they all implement the IParser interface
            // TheParser.parser = new CookV5Parser(CookV5GoldParser());  // Linked dll, official V5 engine, written in Visual Basic (requires VB runtime which may be non-portable)
            TheParser.parser = new CalithaParser(GoldParser());  // Compiled project, pre-V5 engine, written in C#. NOTE!!! Rule.ToString has been modified for compatibility.

//###MSAGL            ////================== MSAGL TEST

            ////create a viewer object 
            ////Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            //////create a graph object 
            //Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph");
            ////create the graph content 
            //graph.AddEdge("A", "B");
            //graph.AddEdge("B", "C");
            //graph.AddEdge("A", "C").Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
            //graph.FindNode("A").Attr.FillColor = Microsoft.Msagl.Drawing.Color.Magenta;
            //graph.FindNode("B").Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;
            //Microsoft.Msagl.Drawing.Node c = graph.FindNode("C");
            //c.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PaleGreen;
            //c.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Diamond;
            ////bind the graph to the viewer 
            //gui.form.gViewer1.Graph = graph;


            //================== MSAGL TEST

            // gui.TxtTargetAppendText("Kaemika starts in " + Application.StartupPath + Environment.NewLine);
            Application.Run(gui.form);
        }

    }
}
