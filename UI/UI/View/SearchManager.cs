﻿using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Sando.Core.Extensions;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer.Searching;
using Sando.SearchEngine;
using Sando.Indexer.Searching.Criteria;
using Sando.Core.Tools;
using System.Collections.Generic;

namespace Sando.UI.View
{
public  class SearchManager
		{

			private CodeSearcher _currentSearcher;
			private string _currentDirectory = "";
			private bool _invalidated = true;
			private ISearchResultListener _myDaddy;

			public SearchManager(ISearchResultListener daddy)
			{
				_myDaddy = daddy;
			}

			private CodeSearcher GetSearcher(UIPackage myPackage)
			{
				CodeSearcher codeSearcher = _currentSearcher;
				if(codeSearcher == null || !myPackage.GetCurrentDirectory().Equals(_currentDirectory) || _invalidated)
				{
					_invalidated = false;
					_currentDirectory = myPackage.GetCurrentDirectory();
					codeSearcher = new CodeSearcher(IndexerSearcherFactory.CreateSearcher(myPackage.GetCurrentSolutionKey()));
				}
				return codeSearcher;
			}

			public void Search(String searchString, SimpleSearchCriteria searchCriteria = null, bool interactive = true)
			{
				if (!string.IsNullOrEmpty(searchString))
				{				    
					var myPackage = UIPackage.GetInstance();
                    if (myPackage.GetCurrentDirectory() != null)
                    {
                        _currentSearcher = GetSearcher(myPackage);
                        bool searchStringContainedInvalidCharacters = false;
                        IQueryable<CodeSearchResult> results =
                            _currentSearcher.Search(GetCriteria(searchString, out searchStringContainedInvalidCharacters, searchCriteria), GetSolutionName(myPackage)).AsQueryable();
                        IResultsReorderer resultsReorderer =
                            ExtensionPointsRepository.Instance.GetResultsReordererImplementation();
                        results = resultsReorderer.ReorderSearchResults(results);
                        _myDaddy.Update(results);
                        if(myPackage.IsPerformingInitialIndexing() && interactive)
                        {
                            MessageBox.Show("Sando is still performing its initial index of this project, results may be incomplete.", "Indexing in Progress", MessageBoxButton.OK, MessageBoxImage.Warning);    
                        }
                    }else
                    {
                        if(interactive)
                            MessageBox.Show("Sando searches only the currently open Solution.  Please open a Solution and try again.", "Sando Search Scope", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
				}
			}

    private string GetSolutionName(UIPackage myPackage)
    {
        try
        {
            return Path.GetFileNameWithoutExtension(myPackage.GetCurrentSolutionKey().GetSolutionPath());
        }catch(Exception e)
        {
            return "";
        }
    }

    public void SearchOnReturn(object sender, KeyEventArgs e, String searchString, SimpleSearchCriteria searchCriteria)
			{
				if(e.Key == Key.Return)
				{
					Search(searchString, searchCriteria);
				}
			}

			public void MarkInvalid()
			{
				_invalidated = true;
			}

			#region Private Mthods
			/// <summary>
			/// Gets the criteria.
			/// </summary>
			/// <param name="searchString">Search string.</param>
			/// <returns>search criteria</returns>
            private SearchCriteria GetCriteria(string searchString, out bool searchStringContainedInvalidCharacters, SimpleSearchCriteria searchCriteria = null)
			{
				if (searchCriteria == null)
					searchCriteria = new SimpleSearchCriteria();
				var criteria = searchCriteria;
				criteria.NumberOfSearchResultsReturned = UIPackage.GetSandoOptions(UIPackage.GetInstance()).NumberOfSearchResultsReturned;
                searchString = ExtensionPointsRepository.Instance.GetQueryRewriterImplementation().RewriteQuery(searchString);
                searchStringContainedInvalidCharacters = WordSplitter.InvalidCharactersFound(searchString);
			    List<string> searchTerms = WordSplitter.ExtractSearchTerms(searchString);
                criteria.SearchTerms = new SortedSet<string>(searchTerms);
				return criteria;
			}
			#endregion
		}

}
