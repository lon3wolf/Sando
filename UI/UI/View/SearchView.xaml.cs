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

namespace Sando.UI.View
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    public partial class SearchView : UserControl
    {

        private SearchViewModel _searchViewModel;
        private QueryRecommender _recommender;

        public SearchView()
        {
            InitializeComponent();

            this._recommender = new QueryRecommender();
            ServiceLocator.RegisterInstance<QueryRecommender>(this._recommender);

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

        private void UpdateRecommendedQueries(IQueryable<string> queries)
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
                this.Foreground = GetHistoryTextColor();
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

        internal static Brush GetHistoryTextColor()
        {
            if (FileOpener.Is2012OrLater())
            {
                var key = Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowTabMouseOverTextKey;
                var color = (Brush)Application.Current.Resources[key];
                var other = (Brush)Application.Current.Resources[Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowBackgroundKey];
                if (color.ToString().Equals(other.ToString()))
                {
                    return (Brush)Application.Current.Resources[Microsoft.VisualStudio.Shell.VsBrushes.HelpSearchResultLinkSelectedKey];
                }
                else
                    return color;
            }
            else
            {
                var key = Microsoft.VisualStudio.Shell.VsBrushes.HelpSearchResultLinkSelectedKey;
                return (Brush)Application.Current.Resources[key];
            }
        }

        #endregion
    }
}
