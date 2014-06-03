using Sando.Core.Logging.Events;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.Indexer.Searching.Criteria;
using Sando.Recommender;
using Sando.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sando.UI.View
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    public partial class SearchView : UserControl
    {

        private SearchViewModel _searchViewModel;
        private SearchManager _searchManager;

        public SearchView()
        {
            InitializeComponent();

            this.DataContextChanged += SearchView_DataContextChanged;

            this._searchManager = SearchManagerFactory.GetUserInterfaceSearchManager();
            //_searchManager.AddListener(this);
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

        private void SearchBox_Populating(object sender, PopulatingEventArgs e)
        {
            e.Cancel = true;
            var recommender = new QueryRecommender();
            ServiceLocator.RegisterInstance<QueryRecommender>(recommender);
            var recommendationWorker = new BackgroundWorker();
            var queryString = this.searchBox.Text;
            recommendationWorker.DoWork += (sender1, args) =>
            {
                var result = recommender.GenerateRecommendations(queryString);

                Dispatcher.Invoke(new Action(delegate(){
                    
                    this.searchBox.ItemsSource = result;

                    this.searchBox.PopulateComplete();

                }), null);

                
            };
            recommendationWorker.RunWorkerAsync();

        }

        private void SearchBox_OnKeyUpHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (null != this.searchBox.Text)
                {
                    BeginSearch(this.searchBox.Text);
                }
            }
        }

        private void SearchAsync(String text, SimpleSearchCriteria searchCriteria)
        {
            var searchWorker = new BackgroundWorker();
            searchWorker.DoWork += SearchWorker_DoWork;
            var workerSearchParams = new WorkerSearchParameters { Query = text, Criteria = searchCriteria };
            searchWorker.RunWorkerAsync(workerSearchParams);
        }

        private class WorkerSearchParameters
        {
            public SimpleSearchCriteria Criteria { get; set; }
            public String Query { get; set; }
        }

        void SearchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var searchParams = (WorkerSearchParameters)e.Argument;
            _searchManager.Search(searchParams.Query, searchParams.Criteria);
        }

        private void BeginSearch(string searchString)
        {
            AddSearchHistory(searchString);

            SimpleSearchCriteria Criteria = new SimpleSearchCriteria();

            //Store the search key
            //this.searchKey = searchBox.Text;

            //Clear the old recommendation.
            this.searchBox.ItemsSource = null;

            var selectedAccessLevels = this._searchViewModel.AccessLevels.Where(a => a.Checked).Select(a => a.Access).ToList();
            if (selectedAccessLevels.Any())
            {
                Criteria.SearchByAccessLevel = true;
                Criteria.AccessLevels = new SortedSet<AccessLevel>(selectedAccessLevels);
            }
            else
            {
                Criteria.SearchByAccessLevel = false;
                Criteria.AccessLevels.Clear();
            }

            var selectedProgramElementTypes =
                this._searchViewModel.ProgramElements.Where(e => e.Checked).Select(e => e.ProgramElement).ToList();
            if (selectedProgramElementTypes.Any())
            {
                Criteria.SearchByProgramElementType = true;
                Criteria.ProgramElementTypes = new SortedSet<ProgramElementType>(selectedProgramElementTypes);
            }
            else
            {
                Criteria.SearchByProgramElementType = false;
                Criteria.ProgramElementTypes.Clear();
            }

            SearchAsync(searchString, Criteria);
        }

        private void AddSearchHistory(String query)
        {
            var history = ServiceLocator.Resolve<SearchHistory>();
            history.IssuedSearchString(query);
        }
    }
}
