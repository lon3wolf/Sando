using Sando.Core.Tools;
using Sando.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sando.Core.QueryRefomers
{
    internal class SplitterReformer : AbstractWordReformer
    {
        internal SplitterReformer(DictionaryBasedSplitter localDictionary)
            : base(localDictionary)
        {
        }

        private string GetReformMessage(string originalWord, string newWord)
        {
            return "Splitting Correction";
        }

        protected override IEnumerable<ReformedWord> GetReformedTargetInternal(string word)
        {
            var dictionarySplittedTerms = ServiceLocator.Resolve<DictionaryBasedSplitter>().
                ExtractWords(word).Where(t => t.Length >= 2).ToList();
            if (dictionarySplittedTerms.Count > 1)
            { 
                var correctItem = new ReformedWord
                    (TermChangeCategory.SPLITTING, word, String.Join(" ", dictionarySplittedTerms),
                        GetReformMessage("", ""));
                correctItem.DistanceFromOriginal = dictionarySplittedTerms.Count();
                List<ReformedWord> items = new List<ReformedWord>();
                items.Add(correctItem);
                return items;                
            }
            return Enumerable.Empty<ReformedWord>();
        }

        protected override int GetMaximumReformCount()
        {
            return QuerySuggestionConfigurations.SIMILAR_WORDS_MAX_COUNT;
        }
    }
}
