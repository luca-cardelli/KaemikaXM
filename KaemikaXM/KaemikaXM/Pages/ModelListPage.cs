using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Xamarin.Forms;

namespace KaemikaXM.Pages {

    public class MyCell : TextCell {
        public MyCell() {
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

    public class ModelInfo {
        public string title { get; set; }        // keep this as a Property for Xamarin Binding
        public string datestring { get; set; }    // keep this as a Poperty for Xamarin Binding

        public string filename; // this will be a randomly generated filename that is never shown
        public string text; // make sure to initialize to "", not null
        public bool modified;
        public DateTime date;
        public ModelInfo() {
            title = "Untitled";
            text = "";
            modified = false;
            filename = "";
            date = DateTime.Now;
            datestring = DateTime.Now.ToString();
        }
        public ModelInfo Copy() {
            ModelInfo copy = new ModelInfo();
            copy.title = this.title + " (copy)";
            copy.text = this.text;
            return copy;
        }
    }

public class ModelListPage : KaemikaPage {

    public async Task NavigationPushModalAsync(Page page) {
            await Navigation.PushModalAsync(page);
    }

    private ListView listView;
        public ModelListPage() {
            Title = "My Networks";
            Icon = "tab_feed.png";

            listView = new ListView { Margin = 20, };
            listView.ItemTemplate = new DataTemplate(typeof(MyCell));                   // use MyCell here to activate context menus on (ModelInfo) list items
            listView.ItemTemplate.SetBinding(MyCell.TextProperty, "title");             // binds property in ModelInfo
            listView.ItemTemplate.SetBinding(MyCell.DetailProperty, "datestring");      // binds property in ModelInfo

            listView.ItemSelected += async (object sender, SelectedItemChangedEventArgs e) => {
                if (e.SelectedItem != null) {
                    MainTabbedPage.theModelEntryPage.SetModel(e.SelectedItem as ModelInfo, editable:true);
                    MainTabbedPage.SwitchToTab(MainTabbedPage.theModelEntryPageNavigation);
                }
                listView.SelectedItem = null; // Deselect item.
            };

            ToolbarItems.Add(
                new ToolbarItem("Add", "icons8plus32.png",  
                    async () => {
                        MainTabbedPage.theModelEntryPage.SetModel(new ModelInfo(), editable:true);
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
