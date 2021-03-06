﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Xamarin.Forms;
using Kaemika;

namespace KaemikaXM.Pages {

    public class MyModelListCell : TextCell {
        public MyModelListCell() {
            TextColor = MainTabbedPage.barColor;    // COLOR OF MENU ITEMS IN MY NETWORKS PAGE
            // DetailColor = Color.Brown;

            var renameAction = new MenuItem { Text = "Rename" };
            renameAction.SetBinding(MenuItem.CommandParameterProperty, new Binding("."));
            renameAction.Clicked += async (object sender, EventArgs e) => {
                var mi = ((MenuItem)sender);
                var modelInfo = (ModelInfo)(mi.CommandParameter);
                RenamePage.renameModelInfo = modelInfo;
                await MainTabbedPage.theModelListPageNavigation.PushAsync(new RenamePage());
            };
            ContextActions.Add(renameAction);

            var deleteAction = new MenuItem { Text = "Delete", IsDestructive = true }; // red background
            deleteAction.SetBinding(MenuItem.CommandParameterProperty, new Binding("."));
            deleteAction.Clicked += async (object sender, EventArgs e) => {
                var mi = ((MenuItem)sender);
                var modelInfo = (ModelInfo)(mi.CommandParameter);
                var ok = await MainTabbedPage.theModelEntryPage.DisplayAlert("Confirm delete", modelInfo.title, "Ok", "Cancel");
                if (ok) {
                    if (File.Exists(modelInfo.filename)) {
                            File.Delete(modelInfo.filename);
                            MainTabbedPage.theModelEntryPage.SetModel(new ModelInfo(), editable:true);
                        }
                        MainTabbedPage.theModelListPage.RegenerateList();
                }
            };
            ContextActions.Add(deleteAction);
        }
    }

public class ModelListPage : KaemikaPage {

    public async Task NavigationPushModalAsync(Page page) {
            await Navigation.PushModalAsync(page);
    }

        public static string sample =
@"species a,b @ 1 M
species c @ 0 M

a + b -> 2c

equilibrate for 10
";
        //@"species H₂O @ 1 M
        //species H₂, O₂ @ 0 M

        //2H₂O -> 2H₂ + O₂

        //equilibrate for 10
        //";

        private ListView listView;
        public ModelListPage() {
            Title = "My Networks";
            IconImageSource = "icons8openedfolder96.png";

            // in iOS>Resource the images of the TitleBar buttons must be size 40, otherwise they will scale but still take the horizontal space of the original

            listView = new ListView { Margin = 20, };
            listView.ItemTemplate = new DataTemplate(typeof(MyModelListCell));                   // use MyModelListCell here to activate context menus on (ModelInfo) list items
            listView.ItemTemplate.SetBinding(MyModelListCell.TextProperty, "title");             // binds property in ModelInfo
            listView.ItemTemplate.SetBinding(MyModelListCell.DetailProperty, "datestring");      // binds property in ModelInfo

            listView.ItemSelected += async (object sender, SelectedItemChangedEventArgs e) => {
                if (e.SelectedItem != null) {
                    MainTabbedPage.theModelEntryPage.SetModel(e.SelectedItem as ModelInfo, editable:true);
                    MainTabbedPage.SwitchToTab(MainTabbedPage.theModelEntryPageNavigation);
                }
                listView.SelectedItem = null; // Deselect item.
            };
            listView.BackgroundColor = MainTabbedPage.almostWhite;  // BACKGROUND COLOR OF MENU ITEMS

            ToolbarItems.Add(
                new ToolbarItem("Add", "icons8plus32.png",  
                    async () => {
                        MainTabbedPage.theModelEntryPage.SetModel(new ModelInfo(sample), editable:true);
                        MainTabbedPage.SwitchToTab(MainTabbedPage.theModelEntryPageNavigation);
                    }));

            AbsoluteLayout layout = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(listView, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(listView, AbsoluteLayoutFlags.All);

            layout.Children.Add(listView);
            Content = layout;
        }

        public void RegenerateList() {
            var models = new List<ModelInfo>();
            var files = Directory.EnumerateFiles(App.FolderPath, "*" + App.modelExtension);
            try {
                foreach (var filename in files) {
                    string text = File.ReadAllText(filename);
                    StringReader reader = new StringReader(text);
                    models.Add(new ModelInfo {
                        filename = filename,
                        title = reader.ReadLine(),
                        text = reader.ReadToEnd(),
                        modified = false,
                        date = File.GetCreationTime(filename)
                    });
                }
            } catch { }
            DisplayList(models);
        }

        public void DisplayList(List<ModelInfo> models) {
            // https://github.com/xamarin/xamarin-forms-samples/blob/master/UserInterface/ListView/BuiltInCells/builtInCellsListView/builtInCellsListView/Views/ListViewCode.cs
            var source = new ObservableCollection<ModelInfo>();
            listView.ItemsSource = source;
            foreach (ModelInfo model in models.OrderByDescending(d => d.date)) source.Add(model);
        }

        public override void OnSwitchedTo() {
            MainTabbedPage.OnAnySwitchedTo(this);
            RegenerateList();
        }

    }
}
