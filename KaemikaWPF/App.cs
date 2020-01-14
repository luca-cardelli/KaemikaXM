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

        public static GuiToWin fromGui;      // implementing FromGui, including the raw application form; refer as: App.fromGui
        public static WinToGui toGui;        // implementing ToGui; refer as: App.toGui

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Gui.platform = Kaemika.Platform.Windows;
            App.fromGui = new GuiToWin();    // of type GuiToWin : Form, will contain a clicker: FromGui
            App.toGui = new WinToGui();      // of type WinToGui : ToGui
            Gui.gui = App.toGui;             // of type ToGui

            Application.Run(App.fromGui); 
        }

    }
}
