using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kaemika;

namespace KaemikaXM.Pages
{
    class RenamePage : ContentPage {

        public static ModelInfo renameModelInfo = null;

        public RenamePage() {

            var entry = new Xamarin.Forms.Entry { Text = renameModelInfo.title };

            var buttonCancel = new Button { Text = "Cancel" };
            buttonCancel.Clicked += async (object sender, EventArgs e) => {
                await MainTabbedPage.theModelListPageNavigation.PopAsync();
            };
            var buttonDone = new Button { Text = "Done" };
            buttonDone.Clicked += async (object sender, EventArgs e) => {
                renameModelInfo.title = entry.Text;
                MainTabbedPage.theModelEntryPage.modelInfo = renameModelInfo;
                MainTabbedPage.theModelEntryPage.Overwrite();
                MainTabbedPage.theModelListPage.RegenerateList();
                await MainTabbedPage.theModelListPageNavigation.PopAsync();
            };

            var layout =
                new StackLayout {
                    Padding = 10,
                    BackgroundColor = Color.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children = {
                        new Label{ Text = "Rename to" },
                        entry,
                        new StackLayout {
                            Orientation = StackOrientation.Horizontal,
                            Children = {
                                buttonCancel,
                                buttonDone,
                            }
                        }
                    }
                };

            Content = layout;
        }
    }
}
