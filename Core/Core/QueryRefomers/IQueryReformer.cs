using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sando.Core.Tools;

namespace Sando.Core.QueryRefomers
{
    public interface IInitializable
    {
        void Initialize(string directory);
    }


    public interface IQueryReformer
    {
        void SetOriginalQuery(IEnumerable<String> words);
        void Reform();
        IEnumerable<IReformedQuery> GetReformedQueries();
    }

    public interface IReformedQuerySorter
    {
        IEnumerable<IReformedQuery> SortReformedQueries(IEnumerable<IReformedQuery> queries);
    }

    public abstract class AbstractWordReformer
    {
        protected readonly DictionaryBasedSplitter localDictionary;

        protected AbstractWordReformer(DictionaryBasedSplitter localDictionary)
        {
            this.localDictionary = localDictionary;
        }

        public IEnumerable<ReformedWord> GetReformedTarget(String target)
        {
            if (IsReformingWord(target))
            {
                return GetReformedTargetInternal(target).Where(w =>  this.localDictionary.
                    DoesWordExist(w.NewTerm, DictionaryOption.IncludingStemming) || DoAllWordsExist(w.NewTerm)).
                        TrimIfOverlyLong(GetMaximumReformCount());
            }
            return Enumerable.Empty<ReformedWord>();
        }

        private bool DoAllWordsExist(string p)
        {
            if (p.Contains(" ") && !p.Contains("\""))
            {
                var terms = p.Split(' ');
                foreach (var v in terms)
                    if (!localDictionary.DoesWordExist(v, DictionaryOption.IncludingStemming))
                        return false;
                return true;
            }
            return false;
        }

        public virtual bool IgnoreExistance()
        {
            return false;
        }

      
        private bool IsReformingWord(string target)
        {
            return !localDictionary.DoesWordExist(target, DictionaryOption.IncludingStemming) && target.Length 
                >= QuerySuggestionConfigurations.MINIMUM_WORD_LENGTH_TO_REFORM;
        }

        protected abstract IEnumerable<ReformedWord> GetReformedTargetInternal(String target);
        protected abstract int GetMaximumReformCount();
    }

    public abstract class AbstractContextSensitiveWordReformer : AbstractWordReformer
    {
        protected AbstractContextSensitiveWordReformer(DictionaryBasedSplitter localDictionary) 
            : base(localDictionary)
        {
        }

        public abstract void SetContextWords(IEnumerable<String> words);
    }

    public enum TermChangeCategory
    {
        NOT_CHANGED,
        MISSPELLING,
        SE_SYNONYM,
        GENERAL_SYNONYM,
        COOCCUR,
        ACRONYM_EXPAND,
        SPLITTING
    }

    public interface IReformedQuery : IEquatable<IReformedQuery>
    {
        IEnumerable<ReformedWord> ReformedWords { get; }
        IEnumerable<String> WordsAfterReform { get; }
        String ReformExplanation { get; }
        String QueryString { get; }
        String OriginalQueryString { get; }
        int CoOccurrenceCount { get; }
        int EditDistance { get; }
    }


    public class ReformedWord : IEquatable<ReformedWord>
    {
        public TermChangeCategory Category { get; private set; }
        public string OriginalTerm { get; private set; }
        public string NewTerm { get; private set; }
        public int DistanceFromOriginal { get; set; }
        public string ReformExplanation { get; private set; }

        public ReformedWord(TermChangeCategory category, String originalTerm,
            String newTerm, String reformExplanation)
        {
            this.Category = category;
            this.OriginalTerm = originalTerm;
            this.NewTerm = newTerm;
            this.ReformExplanation = reformExplanation;
            this.DistanceFromOriginal = new Levenshtein().LD(this.OriginalTerm,
                this.NewTerm);
        }

        public bool Equals(ReformedWord other)
        {
            return this.OriginalTerm.Equals(other.OriginalTerm) && this.NewTerm.Equals(other.NewTerm);
        }
    }

    public class SynonymReformedWord : ReformedWord
    {
        public SynonymReformedWord(TermChangeCategory category, string originalTerm, string
            newTerm, string reformExplanation, int SynonymSimilarityScore)
                : base(category, originalTerm, newTerm, reformExplanation)
        {
            this.SynonymSimilarityScore = SynonymSimilarityScore;
        }

        public int SynonymSimilarityScore { private set; get; }
    }
}
