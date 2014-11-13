using EnvDTE80;
using Sando.Core.Logging.Events;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.UI.Actions;
using Sando.UI.Base;
using Sando.UI.View;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Sando.UI.ViewModel
{
    public class SearchResultViewModel : BaseViewModel
    {

        #region Properties

        private CodeSearchResult _selectedSearchResult;

        public CodeSearchResult SelectedSearchResult
        {
            get
            {
                return this._selectedSearchResult;
            }
            set
            {
                this._selectedSearchResult = value;
                OnPropertyChanged("SelectedSearchResult");
            }
        }

        public List<CodeSearchResult> RealSearchResults
        {
            get;
            set;
        }

        public ObservableCollection<CodeSearchResultWrapper> SearchResults
        {
            get;
            set;
        }

        public TypeColumnHeaderViewModel TypeColumnHeaderViewModel
        {
            get;
            set;

        }

        #endregion

        public SearchResultViewModel()
        {
            this.RealSearchResults = new List<CodeSearchResult>();
            this.SearchResults = new ObservableCollection<CodeSearchResultWrapper>();
            this.TypeColumnHeaderViewModel = new TypeColumnHeaderViewModel();

            var searchManager = SearchManagerFactory.GetUserInterfaceSearchManager();
            searchManager.SearchResultUpdated += this.Update;

            var dte = ServiceLocator.Resolve<DTE2>();
            if (dte != null)
            {
                dte.Events.SolutionEvents.BeforeClosing += () =>
                {

                    Application.Current.Dispatcher.Invoke(new Action(delegate()
                    {

                        //clear the state
                        this.RealSearchResults.Clear();
                        this.SearchResults.Clear();
                        this.TypeColumnHeaderViewModel.ClearState();

                    }));

                };
                dte.Events.WindowEvents.WindowActivated += WindowEvents_WindowActivated;
            }                                       
        }

        #region Command

        

        #endregion

        #region Public Methods

        public void ClearSearchResults()
        {
            this.SearchResults.Clear();
            this.RealSearchResults.Clear();
            this.TypeColumnHeaderViewModel.ClearState();
        }

        #endregion

        #region Update Search Results

        public delegate void UiUpdateDelagate(IEnumerable<CodeSearchResult> results);

        private void UpdateSearchResults(IEnumerable<CodeSearchResult> results)
        {
            try
            {
                foreach (var codeSearchResult in results)
                {
                    this.RealSearchResults.Add(codeSearchResult);

                    this.TypeColumnHeaderViewModel.AddSearchResultType(codeSearchResult.ProgramElementType, 
                        (sender, args) => {

                            if (args.PropertyName == "IsChecked")
                            {
                                FilterSearchResults();
                            }
                        
                        });
                }
                FilterSearchResults();
            }
            catch (Exception ee)
            {
                LogEvents.UIGenericError(this, ee);
            }
        }

        /// <summary>
        /// This function clears the result set and adds the results from scratch each time an item
        /// is checked or unchecked. It doesn't distinguish operation type(check or uncheck) and increment
        /// filter.
        /// TODO: May be refactor to increment filter
        /// </summary>
        private void FilterSearchResults()
        {
            var selectedType = this.TypeColumnHeaderViewModel.SearchResultTypes.Where(searchResultType => {

                if (searchResultType.IsChecked)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }).Select<SearchResultType,ProgramElementType>(searchResultType => {
            
                return searchResultType.TypeName;
            
            });

            this.SearchResults.Clear();

            foreach (var searchResult in this.RealSearchResults)
            {

                if (selectedType.Contains(searchResult.ProgramElementType))
                {
                    this.SearchResults.Add(new CodeSearchResultWrapper(searchResult));
                }

            }
            
        }

        private static int MINIMUM_KEY_LENGTH_FOR_HIGHLIGHTING = 2;
        private static int MAXIMUM_LINES_IN_HIGHLIGHT = 100;

        public int[] GenerateHighlight(string raw, string searchKey, out string highlight_out,
            out string highlightRaw_out)
        {
            try
            {
                if (searchKey != null && searchKey.Length < MINIMUM_KEY_LENGTH_FOR_HIGHLIGHTING)
                {
                    searchKey = "";
                }
                StringBuilder highlight = new StringBuilder();
                StringBuilder highlight_Raw = new StringBuilder();

                string[] lines = raw.Split('\n');
                StringBuilder newLine = new StringBuilder();

                string[] searchKeys = GetKeys(searchKey);
                string[] containedKeys;

                var highlightOffsets = new List<int>();
                int fileOffset = 0;

                foreach (string line in lines)
                {
                    containedKeys = GetContainedSearchKeys(searchKeys, line);

                    if (containedKeys.Length != 0)
                    {

                        string temp_line = string.Copy(line);
                        int loc;
                        //One line contain multiple words
                        foreach (string key in containedKeys)
                        {
                            newLine.Clear();
                            while ((loc = temp_line.IndexOf(key, StringComparison.InvariantCultureIgnoreCase)) >= 0)
                            {

                                string replaceKey = "|~S~|" + temp_line.Substring(loc, key.Length) + "|~E~|";
                                newLine.Append(temp_line.Substring(0, loc) + replaceKey);
                                temp_line = temp_line.Remove(0, loc + key.Length);

                            }

                            newLine.Append(temp_line);
                            temp_line = newLine.ToString();

                        }

                        if (highlightOffsets.Count <= MAXIMUM_LINES_IN_HIGHLIGHT)
                        {
                            highlightOffsets.Add(fileOffset);
                            highlight.Append(newLine + Environment.NewLine);
                        }

                        if (fileOffset <= MAXIMUM_LINES_IN_HIGHLIGHT)
                        {
                            highlight_Raw.Append(newLine + Environment.NewLine);
                        }
                    }
                    else
                    {
                        if (fileOffset <= MAXIMUM_LINES_IN_HIGHLIGHT)
                        {
                            highlight_Raw.Append(line + Environment.NewLine);
                        }
                    }

                    fileOffset++;
                }

                highlight_out = highlight.ToString().Replace("\t", "    ");
                highlightRaw_out = highlight_Raw.ToString().Replace("\t", "    ");
                return highlightOffsets.ToArray();
            }
            catch (Exception e)
            {
                highlightRaw_out = raw;
                var lines = raw.Split('\n');
                var keys = GetKeys(searchKey);
                var sb = new StringBuilder();
                var offesets = new List<int>();
                for (int i = 0; i < lines.Count(); i++)
                {
                    var containedKeys = GetContainedSearchKeys(keys, lines.ElementAt(i));
                    if (containedKeys.Any())
                    {
                        sb.AppendLine(lines.ElementAt(i));
                        offesets.Add(i);
                    }
                }
                highlight_out = sb.ToString();
                return offesets.ToArray();
            }
        }

        public static string[] GetKeys(string searchKey)
        {
            SandoQueryParser parser = new SandoQueryParser();
            var description = parser.Parse(searchKey);
            var terms = description.SearchTerms;
            HashSet<string> keys = new HashSet<string>();
            foreach (var term in terms)
            {
                keys.Add(DictionaryHelper.GetStemmedQuery(term));
                keys.Add(term);
            }
            foreach (var quote in description.LiteralSearchTerms)
            {

                var toAdd = quote.Substring(1);
                toAdd = toAdd.Substring(0, toAdd.Length - 1);
                //unescape '\' and '"'s
                toAdd = toAdd.Replace("\\\"", "\"");
                toAdd = toAdd.Replace("\\\\", "\\");
                keys.Add(toAdd);
            }
            return keys.ToArray();
        }

        //Return the contained search key
        private string[] GetContainedSearchKeys(string[] searchKeys, string line)
        {
            searchKeys = RemovePartialWords(searchKeys.Where(k => line.IndexOf(k,
                StringComparison.InvariantCultureIgnoreCase) >= 0).ToArray());
            var containedKeys = new Dictionary<String, int>();
            foreach (string key in searchKeys)
            {
                var index = line.IndexOf(key, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    containedKeys.Add(key, index);
                }
            }
            return containedKeys.OrderBy(p => p.Value).Select(p => p.Key).ToArray();
        }

        private string[] RemovePartialWords(string[] words)
        {
            var removedIndex = new List<int>();
            var sortedWords = words.OrderByDescending(w => w.Length).ToList();
            for (int i = sortedWords.Count() - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (sortedWords[j].IndexOf(sortedWords[i], StringComparison.
                        InvariantCultureIgnoreCase) >= 0)
                    {
                        removedIndex.Add(i);
                        break;
                    }
                }
            }
            foreach (var index in removedIndex.Distinct().OrderByDescending(i => i))
            {
                sortedWords.RemoveAt(index);
            }
            return sortedWords.ToArray();
        }

        #endregion

        private void Update(string searchString, IQueryable<CodeSearchResult> results)
        {
            object[] parameter = { results };
            var exceptions = new ConcurrentQueue<Exception>();
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(results, item =>
                {
                    try
                    {
                        string highlight;
                        string highlightRaw;
                        item.HighlightOffsets = GenerateHighlight(item.Raw, searchString,
                            out highlight, out highlightRaw);
                        item.Highlight = highlight;
                        item.HighlightRaw = highlightRaw;
                    }
                    catch (Exception exc)
                    {
                        exceptions.Enqueue(exc);
                    }
                }
               );
            }).
                //then update the UI in the UI thread
            ContinueWith(updateUi => Application.Current.Dispatcher.Invoke(new UiUpdateDelagate(UpdateSearchResults), parameter));
        }

        void WindowEvents_WindowActivated(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
        {
            if (LostFocus != null && LostFocus.Caption.Equals("Sando Search"))
            {
                this.SelectedSearchResult = null;                            
            }
        }

    }

    public class TypeColumnHeaderViewModel : BaseViewModel
    {

        private bool _isPopupOpen;
        private HashSet<ProgramElementType> _types;

        public TypeColumnHeaderViewModel()
        {
            this._types = new HashSet<ProgramElementType>();
            this.SearchResultTypes = new ObservableCollection<SearchResultType>();
            this._isPopupOpen = false;
        }

        public bool IsPopupOpen
        {
            get
            {
                return this._isPopupOpen;
            }
            set
            {
                this._isPopupOpen = value;
                OnPropertyChanged("IsPopupOpen");
            }
        }

        public ObservableCollection<SearchResultType> SearchResultTypes
        {
            get;
            set;
        }

        public void ClearState()
        {
            this.IsPopupOpen = false;
            this.SearchResultTypes.Clear();
            this._types.Clear();
        }

        public void AddSearchResultType(ProgramElementType type, PropertyChangedEventHandler propertyChangedHandler)
        {
            if (!this._types.Contains(type))
            {
                var searchResultType = new SearchResultType(type);
                searchResultType.PropertyChanged += propertyChangedHandler;
                this.SearchResultTypes.Add(searchResultType);
                this._types.Add(type);
            }
        }
    }

    public class SearchResultType:BaseViewModel
    {

        private bool _isChecked;

        public SearchResultType(ProgramElementType type)
        {
            this.TypeName = type;
            this.IsChecked = true;
        }

        public ProgramElementType TypeName
        {
            get;
            private set;
        }

        public bool IsChecked
        {
            get
            {
                return this._isChecked;
            }
            set
            {
                this._isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

    }

    public class CodeSearchResultWrapper : BaseViewModel
    {

        public ICommand SearchResultOpenFileCommand
        {
            get;
            set;
        }

        public CodeSearchResult CodeSearchResult
        {
            get;
            set;
        }

        public CodeSearchResultWrapper(CodeSearchResult codeSearchResult)
        {
            this.CodeSearchResult = codeSearchResult;

            this.SearchResultOpenFileCommand = new RelayCommand(SearchResultOpenFile);
        }

        public void SearchResultOpenFile(object param)
        {
            FileOpener.OpenFile(this.CodeSearchResult.HighlightInfo.FullFilePath, 
                                this.CodeSearchResult.HighlightInfo.StartLineNumber);
        }
    }
}
