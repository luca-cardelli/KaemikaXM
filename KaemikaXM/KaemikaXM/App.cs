using System;
using System.IO;
using System.Collections.Generic;
using CalithaGoldParser;
using Xamarin.Forms;
using Kaemika;
using KaemikaXM.Pages;

namespace KaemikaXM
{
    public partial class App : Application
    {
        public static string FolderPath { get; private set; }

        public static string modelExtension = ".kae.txt";

        public interface IResourceContainer {
            string GetString(string key);
        }

        public static LALRParser GoldParser(Stream cgtStream) {
            try {
                //numberFormatInfo = new NumberFormatInfo();
                //numberFormatInfo.NumberDecimalSeparator = ".";
                CGTReader reader = new CGTReader(cgtStream);
                LALRParser parser = reader.CreateNewParser();
                parser.TrimReductions = false;
                parser.StoreTokens = LALRParser.StoreTokensMode.NoUserObject;
                return parser;
            } catch (Exception ex) {
                throw new Error("Parser loading failed: " + ex.Message);
            }
        }

        public App(Stream cgtStream, List<ModelInfoGroup> docs) {
//            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            Gui.gui = new GUI_Xamarin();
            TheParser.parser = new CalithaParser(GoldParser(cgtStream));
            DocListPage.docs = docs;
            MainTabbedPage.theMainTabbedPage = new MainTabbedPage();
            MainTabbedPage.theMainTabbedPage.CurrentPageChanged += MainTabbedPage.currentPageChangedDelegate;

            theApp = this;
            MainPage = MainTabbedPage.theMainTabbedPage;
            MainTabbedPage.SwitchToTab(MainTabbedPage.theDocListPageNavigation);
        }

        private static Application theApp;

        public static void PortraitOrientation() {
            theApp.MainPage = MainTabbedPage.theMainTabbedPage;
        }

        public static void LandscapeOrientation() {
            theApp.MainPage = MainTabbedPage.theChartPageLandscape;
        }

        protected override void OnStart() {
            // Handle when your app starts
        }

        protected override void OnSleep() {
            // Handle when your app sleeps
            if (MainTabbedPage.theMainTabbedPage.CurrentPage == MainTabbedPage.theModelEntryPageNavigation)
                MainTabbedPage.theModelEntryPage.SaveEditor();
        }

        protected override void OnResume() {
            // Handle when your app resumes
        }
    }
}
