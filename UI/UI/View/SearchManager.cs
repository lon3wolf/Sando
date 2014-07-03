using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Configuration.OptionsPages;
using Sando.Core;
using Sando.Core.Extensions;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.ExtensionContracts.SearchContracts;
using Sando.ExtensionContracts.SplitterContracts;
using Sando.Indexer.Searching;
using Sando.Recommender;
using Sando.SearchEngine;
using Sando.Indexer.Searching.Criteria;
using Sando.Core.Tools;
using Sando.Core.Logging;
using Sando.UI.Monitoring;
using Sando.Indexer;
using Sando.Core.Logging.Events;
using Sando.Indexer.Searching.Metrics;
using Lucene.Net.Analysis;
using Sando.Indexer.Metrics;

namespace Sando.UI.View
{
    
    public class SearchManagerFactory
    {
        private static SearchManager _uiSearchManager;

        public static SearchManager GetUserInterfaceSearchManager()
        {
            return _uiSearchManager ?? (_uiSearchManager = new SearchManager());
        }

        public static SearchManager GetNewBackgroundSearchManager()
        {
            return new SearchManager();
        }
    }


    public class SearchManager
    {

        internal SearchManager()
        {
        }


        public void Search(String searchString, SimpleSearchCriteria searchCriteria, bool interactive = true)
        {            
            if (!EnsureSolutionOpen())
                return;
                        
            try
            {
                var codeSearcher = new CodeSearcher(new IndexerSearcher());
                if (String.IsNullOrEmpty(searchString))
                    return;
                
                var solutionKey = ServiceLocator.ResolveOptional<SolutionKey>(); //no opened solution
                if (solutionKey == null)
                {
                    this.UpdateMessage("Sando searches only the currently open Solution.  Please open a Solution and try again.");
                    return;
                }

                searchString = ExtensionPointsRepository.Instance.GetQueryRewriterImplementation().RewriteQuery(searchString);

				PreRetrievalMetrics preMetrics = new PreRetrievalMetrics(ServiceLocator.Resolve<DocumentIndexer>().Reader, ServiceLocator.Resolve<Analyzer>());
				LogEvents.PreSearch(this, preMetrics.MaxIdf(searchString), preMetrics.AvgIdf(searchString), preMetrics.AvgSqc(searchString), preMetrics.AvgVar(searchString));
                LogEvents.PreSearchQueryAnalysis(this, QueryMetrics.ExamineQuery(searchString).ToString(), QueryMetrics.DiceCoefficient(QueryMetrics.SavedQuery, searchString));
                QueryMetrics.SavedQuery = searchString;

				//var criteria = GetCriteria(searchString, searchCriteria);
                var results = codeSearcher.Search(searchCriteria, true).AsQueryable();
                var resultsReorderer = ExtensionPointsRepository.Instance.GetResultsReordererImplementation();
                results = resultsReorderer.ReorderSearchResults(results);

                var returnString = new StringBuilder();

                if (searchCriteria.IsQueryReformed())
                {
                    returnString.Append(searchCriteria.GetQueryReformExplanation());
                }

                if (!results.Any())
                {
                    returnString.Append("No results found. ");
                }
                else
                {
                    returnString.Append(results.Count() + " results returned. ");
                }

                if (null != this.SearchResultUpdated)
                {
                    this.SearchResultUpdated(searchString, results);
                }

                this.UpdateMessage(returnString.ToString());

                if (null != this.RecommendedQueriesUpdated)
                {
                    this.RecommendedQueriesUpdated(searchCriteria.GetRecommendedQueries());
                }

                //_searchResultListener.Update(searchString, results);
                //_searchResultListener.UpdateMessage(returnString.ToString());
                //_searchResultListener.UpdateRecommendedQueries(searchCriteria.GetRecommendedQueries());

                LogEvents.PostSearch(this, results.Count(), searchCriteria.NumberOfSearchResultsReturned, PostRetrievalMetrics.AvgScore(results.ToList()), PostRetrievalMetrics.StdDevScore(results.ToList()));
            }
            catch (Exception e)
            {
                this.UpdateMessage("Sando is experiencing difficulties. See log file for details."); 
                LogEvents.UISandoSearchingError(this, e);
            }
        }

        private bool EnsureSolutionOpen()
        {
            DocumentIndexer indexer = null;
            var isOpen = true;
            try
            {
                indexer = ServiceLocator.Resolve<DocumentIndexer>();
                if (indexer == null || indexer.IsDisposingOrDisposed())
                {
                    this.UpdateMessage("Sando searches only the currently open Solution.  Please open a Solution and try again.");
                    isOpen = false;
                }
            }
            catch (Exception e)
            {
                this.UpdateMessage("Sando searches only the currently open Solution.  Please open a Solution and try again.");
                if (indexer != null)
                    LogEvents.UISolutionOpeningError(this, e);
                isOpen = false;
            }
            return isOpen;
        }

        public event UpdateSearchResult SearchResultUpdated;

        public event UpdateSearchCompletedMessage SearchCompletedMessageUpdated;

        public event UpdateRecommendedQueries RecommendedQueriesUpdated;

        private void UpdateMessage(string message)
        {
            if (null != this.SearchCompletedMessageUpdated)
            {
                this.SearchCompletedMessageUpdated(message);
            }
        }
    }

    public delegate void UpdateSearchResult(string searchString, IQueryable<CodeSearchResult> results);

    public delegate void UpdateSearchCompletedMessage(string message);

    public delegate void UpdateRecommendedQueries(IQueryable<String> queries);
}
