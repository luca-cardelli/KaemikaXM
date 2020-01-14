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
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSButton buttonCompute { get; set; }

		[Outlet]
		public AppKit.NSButton buttonDevice { get; private set; }

		[Outlet]
		AppKit.NSButton buttonDeviceView { get; set; }

		[Outlet]
		AppKit.NSButton buttonFontBigger { get; set; }

		[Outlet]
		AppKit.NSButton buttonFontSmaller { get; set; }

		[Outlet]
		AppKit.NSButton buttonKeyboard { get; set; }

		[Outlet]
		public AppKit.NSButton buttonLegend { get; private set; }

		[Outlet]
		AppKit.NSButton buttonLoad { get; set; }

		[Outlet]
		AppKit.NSButton buttonNoise { get; set; }

		[Outlet]
		AppKit.NSButton buttonParameters { get; set; }

		[Outlet]
		public AppKit.NSButton buttonPlay { get; private set; }

		[Outlet]
		AppKit.NSButton buttonSave { get; set; }

		[Outlet]
		AppKit.NSButton buttonSettings { get; set; }

		[Outlet]
		AppKit.NSButton buttonShare { get; set; }

		[Outlet]
		public AppKit.NSButton buttonStop { get; private set; }

		[Outlet]
		AppKit.NSButton buttonTutorial { get; set; }

		[Outlet]
		public AppKit.NSTextField charTooltip { get; private set; }

		[Outlet]
		AppKit.NSButton checkboxPrecomputeDrift { get; set; }

		[Outlet]
		AppKit.NSBox computeFlyoutBox { get; set; }

		[Outlet]
		AppKit.NSGridView computeFlyoutMenu { get; set; }

		[Outlet]
		public AppKit.NSBox deviceBox { get; private set; }

		[Outlet]
		KaemikaMAC.ViewController.NSTextViewPlus inputTextView { get; set; }

		[Outlet]
		public KaemikaMAC.NSChartView kaemikaChart { get; private set; }

		[Outlet]
		public KaemikaMAC.NSDeviceView kaemikaDevice { get; private set; }

		[Outlet]
		AppKit.NSBox keyboardFlyoutBox { get; set; }

		[Outlet]
		AppKit.NSGridView keyboardFlyoutMenu { get; set; }

		[Outlet]
		AppKit.NSBox leftButtonPanel { get; set; }

		[Outlet]
		AppKit.NSButton leftPanelClicker { get; set; }

		[Outlet]
		AppKit.NSBox legendBox { get; set; }

		[Outlet]
		AppKit.NSBox legendBox2 { get; set; }

		[Outlet]
		AppKit.NSBox legendFlyoutBox { get; set; }

		[Outlet]
		AppKit.NSGridView legendFlyoutMenu { get; set; }

		[Outlet]
		AppKit.NSGridView legendGridView { get; set; }

		[Outlet]
		AppKit.NSScrollView legendScrollBox { get; set; }

		[Outlet]
		AppKit.NSScrollView legendScrollBox2 { get; set; }

		[Outlet]
		AppKit.NSStackView legendStackView { get; set; }

		[Outlet]
		AppKit.NSStackView legendStackView2 { get; set; }

		[Outlet]
		AppKit.NSBox noiseFlyoutBox { get; set; }

		[Outlet]
		AppKit.NSGridView noiseFlyoutMenu { get; set; }

		[Outlet]
		KaemikaMAC.ViewController.NSTextViewPlus outputTextView { get; set; }

		[Outlet]
		AppKit.NSBox panelSettings { get; set; }

		[Outlet]
		AppKit.NSBox parameterBox { get; set; }

		[Outlet]
		AppKit.NSGridView parametersFlyoutMenu { get; set; }

		[Outlet]
		AppKit.NSButton radioGearBDF { get; set; }

		[Outlet]
		AppKit.NSButton radioRK547M { get; set; }

		[Outlet]
		AppKit.NSBox rightButtonPanel { get; set; }

		[Outlet]
		AppKit.NSButton rightPanelClicker { get; set; }

		[Outlet]
		AppKit.NSBox settingsFlyoutBox { get; set; }

		[Outlet]
		AppKit.NSGridView settingsFlyoutMenu { get; set; }

		[Outlet]
		AppKit.NSBox shareFlyoutBox { get; set; }

		[Outlet]
		AppKit.NSGridView shareFlyoutMenu { get; set; }

		[Outlet]
		AppKit.NSImageView splashImage { get; set; }

		[Outlet]
		AppKit.NSBox splashImageBacking { get; set; }

		[Outlet]
		public AppKit.NSScrollView textInput { get; private set; }

		[Outlet]
		public AppKit.NSScrollView textOutput { get; private set; }

		[Outlet]
		AppKit.NSBox tutorialFlyoutBox { get; set; }

		[Outlet]
		AppKit.NSGridView tutorialFlyoutMenu { get; set; }

		[Action ("buttonDirectoryForModelFilesAction:")]
		partial void buttonDirectoryForModelFilesAction (Foundation.NSObject sender);

		[Action ("checkboxPrecomputeDriftAction:")]
		partial void checkboxPrecomputeDriftAction (Foundation.NSObject sender);

		[Action ("computeButton:")]
		partial void computeButton (Foundation.NSObject sender);

		[Action ("deviceButton:")]
		partial void deviceButton (Foundation.NSObject sender);

		[Action ("deviceViewButton:")]
		partial void deviceViewButton (Foundation.NSObject sender);

		[Action ("fontBiggerButton:")]
		partial void fontBiggerButton (Foundation.NSObject sender);

		[Action ("fontSmallerButton:")]
		partial void fontSmallerButton (Foundation.NSObject sender);

		[Action ("keyboardButton:")]
		partial void keyboardButton (Foundation.NSObject sender);

		[Action ("legendButton:")]
		partial void legendButton (Foundation.NSObject sender);

		[Action ("loadButton:")]
		partial void loadButton (Foundation.NSObject sender);

		[Action ("noiseButton:")]
		partial void noiseButton (Foundation.NSObject sender);

		[Action ("parametersButton:")]
		partial void parametersButton (Foundation.NSObject sender);

		[Action ("playButton:")]
		partial void playButton (Foundation.NSObject sender);

		[Action ("radioGearBDFAction:")]
		partial void radioGearBDFAction (Foundation.NSObject sender);

		[Action ("radioRK547MAction:")]
		partial void radioRK547MAction (Foundation.NSObject sender);

		[Action ("saveButton:")]
		partial void saveButton (Foundation.NSObject sender);

		[Action ("settingsButton:")]
		partial void settingsButton (Foundation.NSObject sender);

		[Action ("shareButton:")]
		partial void shareButton (Foundation.NSObject sender);

		[Action ("stopButton:")]
		partial void stopButton (Foundation.NSObject sender);

		[Action ("tutorialButton:")]
		partial void tutorialButton (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (buttonCompute != null) {
				buttonCompute.Dispose ();
				buttonCompute = null;
			}

			if (buttonDevice != null) {
				buttonDevice.Dispose ();
				buttonDevice = null;
			}

			if (buttonDeviceView != null) {
				buttonDeviceView.Dispose ();
				buttonDeviceView = null;
			}

			if (buttonFontBigger != null) {
				buttonFontBigger.Dispose ();
				buttonFontBigger = null;
			}

			if (buttonFontSmaller != null) {
				buttonFontSmaller.Dispose ();
				buttonFontSmaller = null;
			}

			if (buttonKeyboard != null) {
				buttonKeyboard.Dispose ();
				buttonKeyboard = null;
			}

			if (buttonLegend != null) {
				buttonLegend.Dispose ();
				buttonLegend = null;
			}

			if (buttonLoad != null) {
				buttonLoad.Dispose ();
				buttonLoad = null;
			}

			if (buttonNoise != null) {
				buttonNoise.Dispose ();
				buttonNoise = null;
			}

			if (buttonParameters != null) {
				buttonParameters.Dispose ();
				buttonParameters = null;
			}

			if (buttonPlay != null) {
				buttonPlay.Dispose ();
				buttonPlay = null;
			}

			if (buttonSave != null) {
				buttonSave.Dispose ();
				buttonSave = null;
			}

			if (buttonSettings != null) {
				buttonSettings.Dispose ();
				buttonSettings = null;
			}

			if (buttonShare != null) {
				buttonShare.Dispose ();
				buttonShare = null;
			}

			if (buttonStop != null) {
				buttonStop.Dispose ();
				buttonStop = null;
			}

			if (buttonTutorial != null) {
				buttonTutorial.Dispose ();
				buttonTutorial = null;
			}

			if (charTooltip != null) {
				charTooltip.Dispose ();
				charTooltip = null;
			}

			if (checkboxPrecomputeDrift != null) {
				checkboxPrecomputeDrift.Dispose ();
				checkboxPrecomputeDrift = null;
			}

			if (computeFlyoutBox != null) {
				computeFlyoutBox.Dispose ();
				computeFlyoutBox = null;
			}

			if (computeFlyoutMenu != null) {
				computeFlyoutMenu.Dispose ();
				computeFlyoutMenu = null;
			}

			if (deviceBox != null) {
				deviceBox.Dispose ();
				deviceBox = null;
			}

			if (inputTextView != null) {
				inputTextView.Dispose ();
				inputTextView = null;
			}

			if (kaemikaChart != null) {
				kaemikaChart.Dispose ();
				kaemikaChart = null;
			}

			if (kaemikaDevice != null) {
				kaemikaDevice.Dispose ();
				kaemikaDevice = null;
			}

			if (keyboardFlyoutBox != null) {
				keyboardFlyoutBox.Dispose ();
				keyboardFlyoutBox = null;
			}

			if (keyboardFlyoutMenu != null) {
				keyboardFlyoutMenu.Dispose ();
				keyboardFlyoutMenu = null;
			}

			if (leftButtonPanel != null) {
				leftButtonPanel.Dispose ();
				leftButtonPanel = null;
			}

			if (leftPanelClicker != null) {
				leftPanelClicker.Dispose ();
				leftPanelClicker = null;
			}

			if (legendBox != null) {
				legendBox.Dispose ();
				legendBox = null;
			}

			if (legendBox2 != null) {
				legendBox2.Dispose ();
				legendBox2 = null;
			}

			if (legendFlyoutBox != null) {
				legendFlyoutBox.Dispose ();
				legendFlyoutBox = null;
			}

			if (legendFlyoutMenu != null) {
				legendFlyoutMenu.Dispose ();
				legendFlyoutMenu = null;
			}

			if (parametersFlyoutMenu != null) {
				parametersFlyoutMenu.Dispose ();
				parametersFlyoutMenu = null;
			}

			if (legendGridView != null) {
				legendGridView.Dispose ();
				legendGridView = null;
			}

			if (legendScrollBox != null) {
				legendScrollBox.Dispose ();
				legendScrollBox = null;
			}

			if (legendScrollBox2 != null) {
				legendScrollBox2.Dispose ();
				legendScrollBox2 = null;
			}

			if (legendStackView != null) {
				legendStackView.Dispose ();
				legendStackView = null;
			}

			if (legendStackView2 != null) {
				legendStackView2.Dispose ();
				legendStackView2 = null;
			}

			if (noiseFlyoutBox != null) {
				noiseFlyoutBox.Dispose ();
				noiseFlyoutBox = null;
			}

			if (noiseFlyoutMenu != null) {
				noiseFlyoutMenu.Dispose ();
				noiseFlyoutMenu = null;
			}

			if (outputTextView != null) {
				outputTextView.Dispose ();
				outputTextView = null;
			}

			if (panelSettings != null) {
				panelSettings.Dispose ();
				panelSettings = null;
			}

			if (parameterBox != null) {
				parameterBox.Dispose ();
				parameterBox = null;
			}

			if (radioGearBDF != null) {
				radioGearBDF.Dispose ();
				radioGearBDF = null;
			}

			if (radioRK547M != null) {
				radioRK547M.Dispose ();
				radioRK547M = null;
			}

			if (rightButtonPanel != null) {
				rightButtonPanel.Dispose ();
				rightButtonPanel = null;
			}

			if (rightPanelClicker != null) {
				rightPanelClicker.Dispose ();
				rightPanelClicker = null;
			}

			if (settingsFlyoutBox != null) {
				settingsFlyoutBox.Dispose ();
				settingsFlyoutBox = null;
			}

			if (settingsFlyoutMenu != null) {
				settingsFlyoutMenu.Dispose ();
				settingsFlyoutMenu = null;
			}

			if (shareFlyoutBox != null) {
				shareFlyoutBox.Dispose ();
				shareFlyoutBox = null;
			}

			if (shareFlyoutMenu != null) {
				shareFlyoutMenu.Dispose ();
				shareFlyoutMenu = null;
			}

			if (splashImage != null) {
				splashImage.Dispose ();
				splashImage = null;
			}

			if (splashImageBacking != null) {
				splashImageBacking.Dispose ();
				splashImageBacking = null;
			}

			if (textInput != null) {
				textInput.Dispose ();
				textInput = null;
			}

			if (textOutput != null) {
				textOutput.Dispose ();
				textOutput = null;
			}

			if (tutorialFlyoutBox != null) {
				tutorialFlyoutBox.Dispose ();
				tutorialFlyoutBox = null;
			}

			if (tutorialFlyoutMenu != null) {
				tutorialFlyoutMenu.Dispose ();
				tutorialFlyoutMenu = null;
			}
		}
	}
}
