using AppKit;
using Foundation;
using Kaemika;

namespace KaemikaMAC
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        public AppDelegate()
        {
        }

        partial void AppBarFileOpen (Foundation.NSObject sender){
            MacGui.macGui.macControls.Load();
        }

        partial void AppBarFileSaveAs (Foundation.NSObject sender){
             MacGui.macGui.macControls.Save();
       }

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            return true;
        }
    }
}
