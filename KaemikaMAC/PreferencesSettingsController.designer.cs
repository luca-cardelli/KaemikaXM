// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace KaemikaMAC
{
	[Register ("PreferencesSettingsController")]
	partial class PreferencesSettingsController
	{
		[Outlet]
		AppKit.NSButton PrefsPrecomputeDriftOutlet { get; set; }

		[Outlet]
		AppKit.NSButton PrefsScoreBachOutlet { get; set; }

		[Outlet]
		AppKit.NSButtonCell PrefsScoreMozartOutlet { get; set; }

		[Outlet]
		AppKit.NSButtonCell PrefsSolverGearBDFOutlet { get; set; }

		[Outlet]
		AppKit.NSButton PrefsSolverRK547MOutlet { get; set; }

		[Action ("PrefsPrecomputeDrift:")]
		partial void PrefsPrecomputeDrift (Foundation.NSObject sender);

		[Action ("PrefsScoreBach:")]
		partial void PrefsScoreBach (Foundation.NSObject sender);

		[Action ("PrefsScoreMozart:")]
		partial void PrefsScoreMozart (Foundation.NSObject sender);

		[Action ("PrefsSolverGearBDF:")]
		partial void PrefsSolverGearBDF (Foundation.NSObject sender);

		[Action ("PrefsSolverRK547M:")]
		partial void PrefsSolverRK547M (Foundation.NSObject sender);

		[Action ("PrefsUserSettings:")]
		partial void PrefsUserSettings (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (PrefsPrecomputeDriftOutlet != null) {
				PrefsPrecomputeDriftOutlet.Dispose ();
				PrefsPrecomputeDriftOutlet = null;
			}

			if (PrefsScoreMozartOutlet != null) {
				PrefsScoreMozartOutlet.Dispose ();
				PrefsScoreMozartOutlet = null;
			}

			if (PrefsScoreBachOutlet != null) {
				PrefsScoreBachOutlet.Dispose ();
				PrefsScoreBachOutlet = null;
			}

			if (PrefsSolverRK547MOutlet != null) {
				PrefsSolverRK547MOutlet.Dispose ();
				PrefsSolverRK547MOutlet = null;
			}

			if (PrefsSolverGearBDFOutlet != null) {
				PrefsSolverGearBDFOutlet.Dispose ();
				PrefsSolverGearBDFOutlet = null;
			}
		}
	}
}
