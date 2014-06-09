using Sando.Core.Logging.Events;
using Sando.Core.QueryRefomers;
using Sando.DependencyInjection;
using Sando.Recommender;
using Sando.UI.ViewModel;
using Sando.Core.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Sando.UI.Actions;
using FocusTestVC;
using Sando.ExtensionContracts.SearchContracts;
using ABB.SrcML.VisualStudio.SrcMLService;

namespace Sando.UI.View
{
    /// <summary>
    /// Interaction logic for SearchView.xaml. 
    /// Since autocomplete textbox doesn't support data binding.
    /// We have to implement ISearchResultListener interface here.
    /// TODO:Replace the implementation of ISearchResultListener to SearchViewModel
    /// </summary>
    public partial class SearchView : UserControl, ISearchResultListener
    {

        private SearchViewModel _searchViewModel;
        private QueryRecommender _recommender;

        public SearchView()
        {
            InitializeComponent();

            this._recommender = new QueryRecommender();
            ServiceLocator.RegisterInstance<QueryRecommender>(this._recommender);

            SearchManagerFactory.GetUserInterfaceSearchManager().AddListener(this);

            this.DataContextChanged += SearchView_DataContextChanged;

        }

        private void SearchView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SearchViewModel vm = this.DataContext as SearchViewModel;
            if (null != vm)
            {
                this._searchViewModel = vm;
                
            }
        }

        private void IndexingList_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = true;
        }

        private void IndexingList_MouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = true;
        }

        private void BrowserButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                //if (MonitoredFiles.Count > 0)
                //    dialog.SelectedPath = MonitoredFiles.First().Id;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (System.Windows.Forms.DialogResult.OK.Equals(result))
                {
                    //MonitoredFiles.Add(new CheckedListItem(dialog.SelectedPath));
                    if (null != this._searchViewModel)
                    {
                        this._searchViewModel.SetIndexFolderPath(dialog.SelectedPath);
                    }
                }
            }
            
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = false;
        }

        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.SearchBox != null)
            {
                var textBox = this.SearchBox.Template.FindName("Text", this.SearchBox) as TextBox;
                if (textBox != null)
                {
                    TextBoxFocusHelper.RegisterFocus(textBox);
                    //textBox.KeyDown += HandleTextBoxKeyDown;
                }

                var listBox = this.SearchBox.Template.FindName("Selector", this.SearchBox) as ListBox;
                if (listBox != null)
                {
                    listBox.SelectionChanged += SearchBoxListBox_SelectionChanged;
                }
            }
        }

        private void SearchBoxListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var listBox = sender as ListBox;
                if (listBox != null)
                {
                    listBox.ScrollIntoView(listBox.SelectedItem);
                    LogEvents.SelectingRecommendationItem(this, listBox.SelectedIndex + 1);
                }
            }
            catch (Exception ee)
            {
                LogEvents.UIGenericError(this, ee);
            }
        }

        private void SearchBox_Populating(object sender, PopulatingEventArgs e)
        {
            e.Cancel = true;
            
            var recommendationWorker = new BackgroundWorker();
            var queryString = this.SearchBox.Text;
            recommendationWorker.DoWork += (sender1, args) =>
            {
                var result = this._recommender.GenerateRecommendations(queryString);

                Dispatcher.Invoke(new Action(delegate(){

                    this.SearchBox.ItemsSource = result;

                    this.SearchBox.PopulateComplete();

                }), null);

                
            };
            recommendationWorker.RunWorkerAsync();

        }

        private void SearchBox_OnKeyUpHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (null != this.SearchBox.Text)
                {

                    //Clear the old recommendation.
                    this.SearchBox.ItemsSource = null;
                    this.UpdateRecommendedQueries(Enumerable.Empty<String>().AsQueryable());

                    this.SearchButton.Command.Execute(this.SearchButton.CommandParameter);

                }
            }
        }

        //Since the autocomplete textbox doesn't support data binding, 
        //we have to implement ths recommendataion updated functions here.
        #region Update Recommendation

        public void UpdateRecommendedQueries(IQueryable<string> queries)
        {
            queries = SortRecommendedQueriesInUI(ControlRecommendedQueriesCount(queries));
            if (Thread.CurrentThread == Dispatcher.Thread)
            {
                InternalUpdateRecommendedQueries(queries);
            }
            else
            {
                Dispatcher.Invoke((Action)(() =>
                    InternalUpdateRecommendedQueries(queries)));
            }
        }

        private IQueryable<string> SortRecommendedQueriesInUI(IEnumerable<string> queries)
        {
            return queries.OrderBy(q => q.Split().Count()).AsQueryable();
        }

        private static IEnumerable<string> ControlRecommendedQueriesCount(IEnumerable<string> queries)
        {
            return queries.TrimIfOverlyLong(QuerySuggestionConfigurations.
                MAXIMUM_RECOMMENDED_QUERIES_IN_USER_INTERFACE).AsQueryable();
        }

        private void InternalUpdateRecommendedQueries(IEnumerable<string> quries)
        {
            quries = quries.ToList();
            this.RecommendedQueryTextBlock.Inlines.Clear();
            this.RecommendedQueryTextBlock.Inlines.Add(quries.Any() ? "Search instead for: " : "");
            var toRemoveList = new List<string>();
            toRemoveList.AddRange(this.SearchBox.Text.Split());
            int index = 0;
            foreach (string query in quries)
            {
                var hyperlink = new SandoQueryHyperLink(new Run(RemoveDuplicateTerms(query,
                    toRemoveList)), query, index++);
                hyperlink.Click += RecommendedQueryOnClick;
                this.RecommendedQueryTextBlock.Inlines.Add(hyperlink);
                this.RecommendedQueryTextBlock.Inlines.Add("  ");
            }
        }

        private string RemoveDuplicateTerms(string query, List<string> toRemoveList)
        {
            var addedTerms = query.Split().Except(toRemoveList,
                new StringEqualityComparer()).ToArray();
            toRemoveList.AddRange(addedTerms);
            return addedTerms.Any() ? addedTerms.Aggregate((t1, t2) => t1 + " " + t2).
                Trim() : string.Empty;
        }


        private class StringEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return 0;
            }
        }

        private class SandoQueryHyperLink : Hyperlink
        {
            public String Query { private set; get; }
            public int Index { private set; get; }

            internal SandoQueryHyperLink(Run run, String query, int index)
                : base(run)
            {
                this.Query = query;
                this.Foreground = ColorGenerator.GetHistoryTextColor();
                this.Index = index;
            }
        }

        private void RecommendedQueryOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender as SandoQueryHyperLink != null)
            {
                StartSearchAfterClick(sender, routedEventArgs);
                LogEvents.SelectRecommendedQuery((sender as SandoQueryHyperLink).Query,
                    (sender as SandoQueryHyperLink).Index);
            }
        }


        private void StartSearchAfterClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender as SandoQueryHyperLink != null)
            {
                var reformedQuery = (sender as SandoQueryHyperLink).Query;
                this.SearchBox.Text = reformedQuery;

                this.SearchButton.Command.Execute(this.SearchButton.CommandParameter);
            }
        }

        #endregion

        public void Update(string searchString, IQueryable<ExtensionContracts.ResultsReordererContracts.CodeSearchResult> results)
        {
            //Do nothing
        }

        public void UpdateMessage(string message)
        {
            //Do nothing
        }


        #region Tag Cloud

        private void TagCloudButton_Click(object sender, RoutedEventArgs e)
        {
            var text = this.SearchBox.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                CreateTagCloud(new string[] { });
            }
            else
            {
                TagCloudNavigationSession.CreateNewSession(text);
                CreateTagCloud(new[]{TagCloudNavigationSession.
                    CurrentSession().GetNextTerm()});
            }
        }

        private void ChangeSelectedTag(object sender, RoutedEventArgs e)
        {
            var term = sender == previousTagButton
                ? TagCloudNavigationSession.CurrentSession().GetPreviousTerm()
                    : TagCloudNavigationSession.CurrentSession().GetNextTerm();
            CreateTagCloud(new[] { term });
        }

        private class TagCloudNavigationSession
        {
            private static TagCloudNavigationSession session;
            private readonly string[] terms;
            private readonly object locker = new object();
            private int index;


            public static TagCloudNavigationSession CurrentSession()
            {
                return session;
            }

            public static void CreateNewSession(String query)
            {
                session = new TagCloudNavigationSession(query);
            }

            private TagCloudNavigationSession(String query)
            {
                terms = query.Split().Where(s => !string.IsNullOrWhiteSpace(s)).
                    Distinct().ToArray();
                index = terms.Count() * 10;
            }

            public string GetNextTerm()
            {
                lock (locker)
                {
                    var term = terms[MakeModulePositive()];
                    index++;
                    return term;
                }
            }

            public string GetPreviousTerm()
            {
                lock (locker)
                {
                    var term = terms[MakeModulePositive()];
                    index--;
                    return term;
                }
            }

            private int MakeModulePositive()
            {
                var result = index;
                for (; result < 0; result += terms.Count()) ;
                for (; result >= terms.Count(); result -= terms.Count()) ;
                return result;
            }
        }

        private void CreateTagCloud(String[] words)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                var dictionary = ServiceLocator.Resolve<DictionaryBasedSplitter>();
                var builder = new TagCloudBuilder(dictionary, words);
                var hyperlinks = builder.Build().Select(CreateHyperLinkByShapedWord);

                if (Thread.CurrentThread == Dispatcher.Thread)
                {
                    UpdateTagCloudWindow(words, hyperlinks);
                }
                else
                {
                    Dispatcher.Invoke((Action)(() => UpdateTagCloudWindow(words
                        , hyperlinks)));
                }
            });
        }


        private void UpdateLabel(string[] highlightedTerms)
        {
            var terms = this.SearchBox.Text.Split().Select(s => s.Trim().
                ToLower()).Distinct().Where(t => !string.IsNullOrWhiteSpace(t)).
                    ToArray();
            tagCloudTitleTextBlock.Inlines.Clear();
            if (!terms.Any())
            {
                tagCloudTitleTextBlock.Inlines.Add(new Run("Tag cloud for this project")
                {
                    //  FontSize = 24, 
                    Foreground = Brushes.CadetBlue
                });
            }
            else
            {
                var runs = terms.Select(t => new Run(t + " ")
                {
                    FontSize = highlightedTerms.Contains(t) ? 28 : 24,
                    Foreground = highlightedTerms.Contains(t)
                                    ? Brushes.CadetBlue : Brushes.CadetBlue
                }).ToArray();
                runs.Last().Text = runs.Last().Text.Trim();
                tagCloudTitleTextBlock.Inlines.AddRange(runs);
            }
        }

        private void UpdateTagCloudWindow(string[] title, IEnumerable<Hyperlink> hyperlinks)
        {
            string currentQuery;
            if (!title.Any())
            {
                UpdateLabel(new string[] { });
                previousTagButton.Visibility = Visibility.Hidden;
                nextTagButton.Visibility = Visibility.Hidden;
                currentQuery = string.Empty;
            }
            else
            {
                currentQuery = title.Aggregate((w1, w2) => w1 + " " + w2);
                UpdateLabel(title);
                previousTagButton.Visibility = Visibility.Visible;
                nextTagButton.Visibility = Visibility.Visible;
            }
            TagCloudPopUpWindow.IsOpen = false;
            TagCloudTextBlock.Inlines.Clear();
            foreach (var link in hyperlinks)
            {
                TagCloudTextBlock.Inlines.Add(link);
                TagCloudTextBlock.Inlines.Add(" ");
            }
            TagCloudPopUpWindow.IsOpen = true;
            LogEvents.TagCloudShowing(currentQuery);
        }

        private Hyperlink CreateHyperLinkByShapedWord(IShapedWord shapedWord)
        {
            var link = new SandoQueryHyperLink(new Run(shapedWord.Word),
                this.SearchBox.Text + " " + shapedWord.Word, 0)
            {
                FontSize = shapedWord.FontSize,
                Foreground = shapedWord.Color,
                IsEnabled = true,
                TextDecorations = null,
            };
            link.Click += (sender, args) => LogEvents.AddWordFromTagCloud(this.SearchBox.Text,
                "TOFIXTHE", shapedWord.Word);
            link.Click += StartSearchAfterClick;
            link.Click += (sender, args) => TagCloudPopUpWindow.IsOpen = false;
            return link;
        }

        #endregion

    }
}
