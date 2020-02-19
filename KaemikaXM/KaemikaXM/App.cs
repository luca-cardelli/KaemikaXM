using System;
using System.IO;
using System.Collections.Generic;
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

        public App(CustomTextEditorDelegate customTextEditorDelegate) {
//          FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            KGui.Register(new XMGui(customTextEditorDelegate));
            DocListPage.docs = Tutorial.Groups();
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

        //public static void LandscapeOrientation() {
        //    theApp.MainPage = MainTabbedPage.theChartPageLandscape;
        //}

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
