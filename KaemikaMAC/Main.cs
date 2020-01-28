using AppKit;
using Kaemika;

namespace KaemikaMAC
{
    static class MainClass
    {

        public static GuiToMac guiToMac;     // the raw application form; refer as MainClass.form // initialized by ViewController.ViewDidLoad
        public static MacToGui macToGui;     // implementing GuiInterface; refer as MainClass.gui // initialized by ViewController.ViewDidLoad

        static void Main(string[] args) {

            Gui.platform = Kaemika.Platform.macOS;

            NSApplication.Init();
            NSApplication.Main(args);
        }
    }
}
