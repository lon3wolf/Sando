using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sando.Core.Logging.Events;
using Sando.Core.QueryRefomers;
using Sando.Core.Tools;
using Sando.DependencyInjection;

namespace Sando.Indexer.Searching.Criteria
{
    public class SearchCriteriaReformer
    {
        private const int TERM_MINIMUM_LENGTH = 2;

        public static void ReformSearchCriteria(SimpleSearchCriteria criteria)
        {            
            //try without splitting search terms
            var specialTerms = GetSpecialTerms(criteria.SearchTerms);
            var terms = criteria.SearchTerms.Where(t => !t.StartsWith("\"")||!t.EndsWith("\"")).
                Select(t => t.NormalizeText()).Where(t => !String.IsNullOrWhiteSpace(t)).
                    Distinct().ToList();
            var originalTerms = terms.ToList();
            var queries = GetReformedQuery(terms.Distinct()).ToList();

            if (queries.Count > 0)
            {
                PopulateRecommendations(criteria, terms, originalTerms, queries);
            }
            else
            {
                criteria.Explanation = String.Empty;
                criteria.Reformed = false;
                criteria.RecommendedQueries = Enumerable.Empty<String>().AsQueryable();                
            }
            terms.AddRange(specialTerms);
            criteria.SearchTerms = ConvertToSortedSet(terms);
        }

        private static void PopulateRecommendations(SimpleSearchCriteria criteria, List<string> terms, List<string> originalTerms, List<IReformedQuery> queries)
        {
            //LogEvents.AddSearchTermsToOriginal(queries.First());
            var query = queries.First();
            var termsToAdd = query.WordsAfterReform.Except(terms);
            foreach (var term in termsToAdd)
                if (term.Contains(" ")&&!term.Contains("\""))
                    terms.AddRange(term.Split(' '));
                else
                    terms.Add(term);
            criteria.Explanation = GetExplanation(query, originalTerms);
            criteria.Reformed = true;
            criteria.RecommendedQueries = queries.GetRange(1, queries.Count - 1).
                Select(n => n.QueryString).AsQueryable();
            if (queries.Count > 1)
            {
                LogEvents.IssueRecommendedQueries(queries.GetRange(1, queries.Count - 1).
                    ToArray());
            }
        }

        //Removes words that are *very* similar, like if "open" and "openn" are both there, it removes one of them.
        private static void RemoveSimilarWords(List<string> terms)
        {
            List<string> toRemove = new List<string>();
            foreach (var term in terms)
                foreach (var term2 in terms)
                    if (!term2.Equals(term) && term2.Contains(term) && term2.Length - term.Length < 2)
                        toRemove.Add(term);
            foreach (var term in toRemove)
                terms.Remove(term);
        }                           


        private static String[] GetSpecialTerms(IEnumerable<string> searchTerms)
        {
            return searchTerms.Where(t => !t.NormalizeText().Equals(t, 
                StringComparison.InvariantCultureIgnoreCase)).ToArray();
        }

        private static SortedSet<String> ConvertToSortedSet(IEnumerable<string> list)
        {
            var set = new SortedSet<string>();
            foreach (var s in list)
            {
                set.Add(s);
            }
            return set;
        }


        private static String GetExplanation(IReformedQuery query, List<String> originalTerms)
        {
            var appended = false;
            var sb = new StringBuilder();
            sb.Append("Added search term(s):");
            foreach (var term in query.ReformedWords.Where(term => !originalTerms.Contains(term.NewTerm)))
            {
                appended = true;
                sb.Append(" " + term.NewTerm + ", ");
            }
            return appended ? sb.ToString().TrimEnd(new char[]{',', ' '}) + ". " : String.Empty;
        }

        private static IEnumerable<IReformedQuery> GetReformedQuery(IEnumerable<string> words)
        {
            words = words.ToList();
            var reformer = ServiceLocator.Resolve<QueryReformerManager>();
            return reformer.ReformTermsSynchronously(words).Where(r => r.ReformedWords.Any(w => 
                w.Category != TermChangeCategory.NOT_CHANGED)).ToList();
        }
    }
}
