using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace KaemikaXM.Pages {

    // uses ModelInfo to store data
    // uses ModelInfoGroup for grouped lists

    public class ModelInfoGroup : ObservableCollection<ModelInfo> {
        public string GroupHeading { get; private set; }
        public ModelInfoGroup(string groupHeading) {
            GroupHeading = groupHeading;
        }
    }

    public class DocListPage : KaemikaPage {

        public static List<ModelInfoGroup> docs;
        private ListView listView;

        public DocListPage() {
            Title = "Tutorial";
            Icon = "icons8usermanual100.png";

            ToolbarItem kaemikaLogo = new ToolbarItem("Logo", "kaemikaLogo.png", async () => { });
            kaemikaLogo.IsEnabled = true;
            ToolbarItems.Add(kaemikaLogo);
            ToolbarItem toolbarSpacer = new ToolbarItem("", null, async () => { });
            toolbarSpacer.IsEnabled = false;
            ToolbarItems.Add(toolbarSpacer);

            listView = CreateGroupedListView();

            AbsoluteLayout layout = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(listView, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(listView, AbsoluteLayoutFlags.All);

            layout.Children.Add(listView);
            Content = layout;
        }

        public override void OnSwitchedTo() {
            MainTabbedPage.OnAnySwitchedTo(this);
            listView.ItemsSource = docs;
        }

        public ListView CreateGroupedListView () {
            var listView = new ListView {
                Margin = 20,
                IsGroupingEnabled = true,
                SeparatorVisibility = SeparatorVisibility.None,
                GroupDisplayBinding = new Binding("GroupHeading"),  // ModelInfoGroup property
                GroupShortNameBinding = new Binding("GroupHeading")  // ModelInfoGroup property
            };

            var template = new DataTemplate(typeof(TextCell));
            template.SetBinding(TextCell.TextProperty, "title");        // ModelInfo property
            listView.ItemTemplate = template;

            listView.ItemSelected += async (object sender, SelectedItemChangedEventArgs e) => {
                if (e.SelectedItem != null) {
                    ModelInfo modelInfo = e.SelectedItem as ModelInfo;
                    MainTabbedPage.theModelEntryPage.SetModel(modelInfo, editable:false);
                    MainTabbedPage.SwitchToTab(MainTabbedPage.theModelEntryPageNavigation);
                }           
                listView.SelectedItem = null; // Deselect item (the "ItemSelected" event does not fire if item is alrady selected; the "ItemTapped" event fires even if the item is selected)
            };

            return listView;
        }
    }
}

    //// HOW TO MAKE GROUPED LISTS
    //// https://docs.microsoft.com/en-us/dotnet/api/xamarin.forms.listview.groupdisplaybinding?view=xamarin-forms#Xamarin_Forms_ListView_GroupDisplayBinding
    ////This example shows an alphabetized list of people, grouped by first initial with the display binding set.
    //public class TestGroupedViews {

    //    class Person {                                      // the equivalent of ModelInfo
    //        public string FullName { get; set; }            // bindable property
    //        public string Address { get; set; }             // bindable property
    //}

    //    // this is the trick: must define a Group that is a collection with Add method, and has bindable properties
    //    class Group : ObservableCollection<Person> {
    //        public string FirstInitial { get; private set; }   // bindable group property
    //        public Group(string firstInitial) {
    //            FirstInitial = firstInitial;
    //        }
    //    }

    //    public ListView CreateListView() {
    //        var listView = new ListView {
    //            IsGroupingEnabled = true,
    //            GroupDisplayBinding = new Binding("FirstInitial"),
    //            GroupShortNameBinding = new Binding("FirstInitial")
    //        };

    //        var template = new DataTemplate(typeof(TextCell));
    //        template.SetBinding(TextCell.TextProperty, "FullName");
    //        template.SetBinding(TextCell.DetailProperty, "Address");
    //        listView.ItemTemplate = template;

    //        //listView.ItemsSource = new[] {
    //        //    new Group ("C") {
    //        //        new Person { FullName = "Caprice Nave" }
    //        //    },
    //        //    new Group ("J") {
    //        //        new Person { FullName = "James Smith", Address = "404 Nowhere Street" },
    //        //        new Person { FullName = "John Doe", Address = "404 Nowhere Ave" }
    //        //    }
    //        //};

    //        // OR MORE PROGRAMMATICALLY:

    //        var group1 = new Group("A");
    //        group1.Add(new Person { FullName = "Caprice Nave" });
    //        var group2 = new Group("J");
    //        group2.Add(new Person { FullName = "James Smith", Address = "404 Nowhere Street" });
    //        group2.Add(new Person { FullName = "John Doe", Address = "404 Nowhere Ave" });
    //        var groups = new List<Group>();
    //        groups.Add(group1);
    //        groups.Add(group2);
    //        listView.ItemsSource = groups;

    //        return listView;
    //    }
    //}
