// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using AppKit;

namespace KaemikaMAC
{
	public partial class PreferencesInfoController : NSViewController
	{
		public PreferencesInfoController (IntPtr handle) : base (handle) {
		}

		// This Controller file was created by selecting the Info Scene > Info in XCode
		// assigning it a new Custom Class "PreferencesInfoController"
		// and StoryBoard ID "PreferencesInfo"
		// and switching back to VisualStudio to create these .cs, .h and .m files
		// See https://docs.microsoft.com/en-us/xamarin/mac/user-interface/dialog > Writing Preferences to Preference Views
		// Then we can link Actions to the .h file in XCode
		// via the Assistant as described in https://docs.microsoft.com/en-us/xamarin/mac/get-started/hello-mac
		// (The Assistant is now opened from the "script lines" icon menu)

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			PrefsVersionNo.StringValue = "1.0.28";  // copy from Gui.KaemikaVersion
		}

		partial void PrefsPrivacyPolicy (Foundation.NSObject sender) {
			MacGui.macGui.macControls.PrivacyPolicyToClipboard();
		}
	}
}
