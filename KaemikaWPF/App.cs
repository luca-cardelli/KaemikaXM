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

        public static GuiToWin guiToWin;      // implementing GuiToWin : Form,   including winControls : WinControls : GuiControls
        public static WinToGui winToGui;      // implementing ToGui

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Gui.platform = Kaemika.Platform.Windows;
            App.guiToWin = new GuiToWin();               // of type GuiToWin : Form, contains a winControls: WinControls
            App.winToGui = new WinToGui();               // of type WinToGui : ToGui
            Gui.toGui = App.winToGui;                    // of type ToGui, so that higher levels can call the platform Gui
            //Gui.guiControls = App.guiToWin.winControls;       // of type GuiControls

            Application.Run(App.guiToWin); 
        }

    }
}
