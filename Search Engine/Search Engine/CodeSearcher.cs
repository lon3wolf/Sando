﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer.Searching;
using Sando.Indexer.Searching.Criteria;
using Sando.DependencyInjection;

namespace Sando.SearchEngine
{
    public class CodeSearcher
    {
        private readonly IIndexerSearcher<SimpleSearchCriteria> _simpleSearcher;

        public CodeSearcher()
        {
            this._simpleSearcher = new SimpleIndexerSearcher();
        }

        public virtual List<CodeSearchResult> Search(string searchString, bool rerunWithWildcardIfNoResults = false)
		{
			Contract.Requires(String.IsNullOrWhiteSpace(searchString), "CodeSearcher:Search - searchString cannot be null or an empty string!");

            var searchCriteria = CriteriaBuilderFactory.GetBuilder().GetCriteria(searchString);

            return Search(searchCriteria, rerunWithWildcardIfNoResults);
		}

        public virtual List<CodeSearchResult> Search(SimpleSearchCriteria searchCriteria, bool rerunWithWildcardIfNoResults = false)
        {
            Contract.Requires(searchCriteria != null, "CodeSearcher:Search - searchCriteria cannot be null!");

            var searchResults = _simpleSearcher.Search(searchCriteria).ToList();
            if (!searchResults.Any() && rerunWithWildcardIfNoResults && !QuotesInQuery(searchCriteria))
                searchResults = RerunQueryWithWildcardAtTheEnd(searchCriteria, searchResults);
            return searchResults;
        }

        private bool QuotesInQuery(SearchCriteria searchCriteria)
        {
            var simple = searchCriteria as SimpleSearchCriteria;
            if (simple != null)
            {
                return simple.SearchTerms.Any(t => t.Contains("\""));
            }
            else
            {
                return false;
            }
        }

        private List<CodeSearchResult> RerunQueryWithWildcardAtTheEnd(SearchCriteria searchCriteria, List<CodeSearchResult> searchResults)
        {
            var simple = searchCriteria as SimpleSearchCriteria;
            if (simple != null)
            {
                var terms = simple.SearchTerms;
                if (terms.Count == 1)
                {
                    var term = simple.SearchTerms.First();
                    simple.SearchTerms.Clear();
                    simple.SearchTerms.Add(term + "*");
                    searchResults = _simpleSearcher.Search(simple).ToList();
                }
            }
            return searchResults;
        }
    }
}
