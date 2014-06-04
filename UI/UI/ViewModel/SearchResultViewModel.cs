using Sando.Core.Logging.Events;
using Sando.Core.Tools;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.ExtensionContracts.SearchContracts;
using Sando.UI.Base;
using Sando.UI.View;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Sando.UI.ViewModel
{
    public class SearchResultViewModel : BaseViewModel, ISearchResultListener
    {

        #region Properties

        public ObservableCollection<CodeSearchResult> SearchResults
        {
            get;
            set;
        }

        #endregion

        public SearchResultViewModel()
        {

            this.SearchResults = new ObservableCollection<CodeSearchResult>();

            var searchManager = SearchManagerFactory.GetUserInterfaceSearchManager();
            searchManager.AddListener(this);

        }

        #region Public Methods

        public void ClearSearchResults()
        {

            this.SearchResults.Clear();

        }

        #endregion

        #region ISearchResultListener Implementation

        public void Update(string searchString, IQueryable<CodeSearchResult> results)
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

        public void UpdateMessage(string message)
        {
            //Do nothing for now.
        }

        public void UpdateRecommendedQueries(IQueryable<string> queries)
        {
            //Do nothing for now
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
                    SearchResults.Add(codeSearchResult);
                }
            }
            catch (Exception ee)
            {
                LogEvents.UIGenericError(this, ee);
            }
        }

        public int[] GenerateHighlight(string raw, string searchKey, out string highlight_out,
            out string highlightRaw_out)
        {
            try
            {
                StringBuilder highlight = new StringBuilder();
                StringBuilder highlight_Raw = new StringBuilder();

                string[] lines = raw.Split('\n');
                StringBuilder newLine = new StringBuilder();

                string[] searchKeys = GetKeys(searchKey);
                string[] containedKeys;


                var highlightOffsets = new List<int>();
                int offest = 0;
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
                        highlightOffsets.Add(offest);
                        highlight.Append(newLine + Environment.NewLine);
                        highlight_Raw.Append(newLine + Environment.NewLine);
                    }
                    else
                    {
                        highlight_Raw.Append(line + Environment.NewLine);
                    }
                    offest++;
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
    }
}
