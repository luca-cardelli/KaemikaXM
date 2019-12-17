using System;
using System.Windows.Forms;
using Kaemika;

namespace KaemikaWPF
{
    public static class App
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //GUI_Windows gui = new GUI_Windows(new GUI());
            GUI_Windows2 gui = new GUI_Windows2(new GUI2());
            Gui.gui = gui;

            Application.Run(gui.GUI_Form()); 
        }

    }
}
