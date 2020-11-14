using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Kaemika;

namespace KaemikaXM.Pages {

    public class MyDocListCell : TextCell {
        public MyDocListCell() {
            TextColor = MainTabbedPage.barColor;    // COLOR OF MENU ITEMS IN TUTORIAL PAGE
            // DetailColor = Color.Brown;
        }
    }

    public class MyDocListHeaderCell : TextCell {
        public MyDocListHeaderCell() {
            TextColor = Color.Red;                 // COLOR OF MENU GROUPS IN TUTORIAL PAGE
            // DetailColor = Color.Brown;
        }
    }

    public class DocListPage : KaemikaPage {

        public static List<ModelInfoGroup> docs;
        private ListView listView;
        private ToolbarItem kaemikaLogo;
        private bool kaemikaLogoToggle = true;

        public DocListPage() {
            Title = "Tutorial";
            IconImageSource = "icons8usermanual100.png";

            // in iOS>Resource the images of the TitleBar buttons must be size 40, otherwise they will scale but still take the horizontal space of the original

            kaemikaLogo = new ToolbarItem("Logo", "kaemikaLogoWhite.png", async () => {
                if (kaemikaLogoToggle) { kaemikaLogo.IconImageSource = null; kaemikaLogo.Text = Gui.KaemikaVersion; } else kaemikaLogo.IconImageSource = "kaemikaLogoWhite.png";
                kaemikaLogoToggle = !kaemikaLogoToggle;
            });
            kaemikaLogo.IsEnabled = true;
            ToolbarItems.Add(kaemikaLogo);
            //ToolbarItem toolbarSpacer = new ToolbarItem("", null, async () => { });
            //toolbarSpacer.IsEnabled = false;
            //ToolbarItems.Add(toolbarSpacer);

            listView = CreateGroupedListView();
            listView.BackgroundColor = MainTabbedPage.almostWhite;  // BACKGROUND COLOR OF MENU ITEMS

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
                GroupShortNameBinding = new Binding("GroupHeading"),  // ModelInfoGroup property
            };

            var template = new DataTemplate(typeof(MyDocListCell));
            template.SetBinding(MyDocListCell.TextProperty, "title");        // ModelInfo property
            // template.SetBinding(MyDocListCell.DetailProperty, "datestring");
            listView.ItemTemplate = template;

            var headerTemplate = new DataTemplate(typeof(MyDocListHeaderCell));
            headerTemplate.SetBinding(MyDocListHeaderCell.TextProperty, "GroupHeading"); // ModelInfoGroup property
            listView.GroupHeaderTemplate = headerTemplate;

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
