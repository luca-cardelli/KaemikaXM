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

        public static GOLD.Parser CookV5GoldParser() {
            GOLD.Parser parser = new GOLD.Parser();
            try {
                //if (parser.LoadTables(Application.StartupPath + "\\kaemika.egt")) setupDone = true;
                if (parser.LoadTables(new System.IO.BinaryReader(new System.IO.MemoryStream(Properties.Resources.kaemika)))) return parser;
                else throw new Error("Parser EGT failed to load");
            } catch (GOLD.ParserException ex) {
                throw new Error("Parser loading failed: " + ex.Message);
            }
        }
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

            //### Choose a Parser, they all implement the IParser interface
            // TheParser.parser = new CookV5Parser(CookV5GoldParser());  // Linked dll, official V5 engine, written in Visual Basic (requires VB runtime which may be non-portable)
            TheParser.parser = new CalithaParser(GoldParser());  // Compiled project, pre-V5 engine, written in C#. NOTE!!! Rule.ToString has been modified for compatibility.

            // gui.TxtTargetAppendText("Kaemika starts in " + Application.StartupPath + Environment.NewLine);
            Application.Run(gui.form);
        }

    }
}
