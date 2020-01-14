using AppKit;
using Kaemika;

namespace KaemikaMAC
{
    static class MainClass
    {

        public static ViewController form;     // the raw application form; refer as MainClass.form // initialized by ViewController.ViewDidLoad
        public static GUI_Mac gui;             // implementing GuiInterface; refer as MainClass.gui // initialized by ViewController.ViewDidLoad

        static void Main(string[] args) {

            Gui.platform = Kaemika.Platform.macOS;

            NSApplication.Init();
            NSApplication.Main(args);
        }
    }
}
