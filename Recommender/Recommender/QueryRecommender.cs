using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ABB.Swum;
using ABB.Swum.Nodes;
using System.Diagnostics;
using Sando.Core.Tools;
using Sando.DependencyInjection;

namespace Sando.Recommender {
    public class QueryRecommender {
        /// <summary>
        /// Maps query recommendation strings to an accumulated score for that recommendation
        /// </summary>
        
        
        
        public QueryRecommender() {
            
        }


        public ISwumRecommendedQuery[] GenerateRecommendations(string query) {
            if(string.IsNullOrEmpty(query)) {
                if (query != null)
                {
                    return GetAllSearchHistoryItems();
                }
                return new ISwumRecommendedQuery[0];
            }
            try
            {
                var recommendations = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

                AddRecommendationForEachTerm(query, recommendations);

                //return the recommendations sorted by score in descending order
                List<KeyValuePair<string, int>> listForSorting = recommendations.ToList();
                listForSorting.Sort((firstPair, nextPair) =>
                    {
                        return nextPair.Value.CompareTo(firstPair.Value);
                    }
                );
                return SortRecommendations(query, listForSorting.Select(kvp => kvp.Key).Take(50).ToArray());
            }
            catch (Exception e)
            {
                return new ISwumRecommendedQuery[0];
            }
        }

        private ISwumRecommendedQuery[] SortRecommendations(string query, string[] queries)
        {
            return new SwumQueriesSorter().SelectSortSwumRecommendations(query, queries);
        }


        private ISwumRecommendedQuery[] GetAllSearchHistoryItems()
        {
            return new SwumQueriesSorter().GetAllHistoryItems();
        }

        private void AddRecommendationToDictionary(string rec, int score, Dictionary<string, int> recommendations)
        {
            int count;
            recommendations.TryGetValue(rec, out count);
            recommendations[rec] = count + score;
        }

        private void AddRecommendationToDictionary(Dictionary<string, int> recommendations, string p, int normalWeight, MethodDeclarationNode methodDeclarationNode = null)
        {
            AddRecommendationToDictionary(p.Trim(), normalWeight, recommendations);
            if(methodDeclarationNode !=null)
                AddRecommendationToDictionary(methodDeclarationNode.Name.Trim(), normalWeight, recommendations);
        }


        private void AddRecommendationForEachTerm(String query, Dictionary<String, int> recommendations)
        {
            var terms = query.Split().Where(t => !String.IsNullOrWhiteSpace(t));
            foreach (var term in terms)
            {
                AddTermRecommendationWeightedByPartOfSpeech(term, recommendations);
            }
        }

        /// <summary>
        /// Generates query recommendations. Nouns and verbs are weighted higher than other parts of speech.
        /// The words in the generated recommendations are sorted in the same order as they appear in the method signature.
        /// </summary>
        /// <param name="term">A query term to create recommended completions for.</param>
        private void AddTermRecommendationWeightedByPartOfSpeech(string term, Dictionary<string, int> recommendations) {
            const int normalWeight = 1;

            var swumData = SwumManager.Instance.GetSwumForTerm(term.ToLowerInvariant());
          
            foreach(var swumRecord in swumData) {
                var actionWords = new Collection<WordNode>();
                var themeWords = new Collection<WordNode>();
                var indirectWords = new Collection<WordNode>();
                bool queryInAction = false, queryInTheme = false, queryInIndirect = false;
                int queryActionIndex = -1, queryThemeIndex = -1, queryIndirectIndex = -1;
                if(swumRecord.ParsedAction != null) {
                    actionWords = swumRecord.ParsedAction.GetPhrase();
                    queryActionIndex = FindWordInPhraseNode(swumRecord.ParsedAction, term);
                    if(queryActionIndex > -1) { queryInAction = true; }
                }
                if(swumRecord.ParsedTheme != null) {
                    themeWords = swumRecord.ParsedTheme.GetPhrase();
                    queryThemeIndex = FindWordInPhraseNode(swumRecord.ParsedTheme, term);
                    if(queryThemeIndex > -1) { queryInTheme = true; }
                }
                if(swumRecord.ParsedIndirectObject != null) {
                    indirectWords = swumRecord.ParsedIndirectObject.GetPhrase();
                    queryIndirectIndex = FindWordInPhraseNode(swumRecord.ParsedIndirectObject, term);
                    if(queryIndirectIndex > -1) { queryInIndirect = true; }
                }

                if(queryInAction || queryInTheme || queryInIndirect) {
                    //add words from action
                    for(int i = 0; i < actionWords.Count; i++) {
                        int wordWeight = GetWeightForPartOfSpeech(actionWords[i].Tag);
                        if(queryInAction) {
                            if(i < queryActionIndex) {
                                AddRecommendationToDictionary(recommendations, string.Format("{0} {1}", actionWords[i].Text, term), wordWeight);
                            } else if(queryActionIndex < i) {
                                AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", term, actionWords[i].Text), wordWeight);
                            }
                        } else {
                            //the action words do not contain the query word
                            AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", actionWords[i].Text, term), wordWeight);
                        }
                    }
                    if(queryInAction && actionWords.Count() > 2) {
                        AddRecommendationToDictionary( recommendations, swumRecord.Action, normalWeight);
                    } else if(!queryInAction && actionWords.Count() > 1) {
                        AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", swumRecord.Action, term), normalWeight);
                    }

                    //add words from theme
                    for(int i = 0; i < themeWords.Count; i++) {
                        int wordWeight = GetWeightForPartOfSpeech(themeWords[i].Tag);
                        if(queryInTheme) {
                            if(i < queryThemeIndex) {
                                AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", themeWords[i].Text, term), wordWeight);
                            } else if(queryThemeIndex < i) {
                                AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", term, themeWords[i].Text), wordWeight);
                            }
                        } else {
                            //the theme words do not contain the query word
                            if(queryInAction) {
                                AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", term, themeWords[i].Text), wordWeight);
                            }
                            if(queryInIndirect) {
                                AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", themeWords[i].Text, term), wordWeight);
                            }
                        }
                    }
                    if(queryInTheme && themeWords.Count() > 2) {
                        AddRecommendationToDictionary( recommendations, swumRecord.Theme, normalWeight);
                    } else if(!queryInTheme && themeWords.Count() > 1) {
                        if(queryInAction) {
                            AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", term, swumRecord.Theme), normalWeight);
                        }
                        if(queryInIndirect) {
                            AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", swumRecord.Theme, term), normalWeight);
                        }
                    }

                    //add words from indirect object
                    for(int i = 0; i < indirectWords.Count; i++) {
                        int wordWeight = GetWeightForPartOfSpeech(indirectWords[i].Tag);
                        if(queryInIndirect) {
                            if(i < queryIndirectIndex) {
                                AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", indirectWords[i].Text, term), wordWeight);
                            } else if(queryIndirectIndex < i) {
                                AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", term, indirectWords[i].Text), wordWeight);
                            }
                        } else {
                            //the indirect object words do not contain the query word
                            AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", term, indirectWords[i].Text), wordWeight);
                        }
                    }
                    if(queryInIndirect && indirectWords.Count() > 2) {
                        AddRecommendationToDictionary( recommendations, swumRecord.IndirectObject, normalWeight);
                    } else if(!queryInIndirect && indirectWords.Count() > 1) {
                        AddRecommendationToDictionary( recommendations, string.Format("{0} {1}", term, swumRecord.IndirectObject), normalWeight);
                    }
                }
                AddFullMethodName(term, recommendations, normalWeight, swumRecord);
            }
        }

        private void AddFullMethodName(string term, Dictionary<string, int> recommendations, int normalWeight, SwumDataRecord swumRecord)
        {
            if (swumRecord.SwumNodeName.ToLower().Contains(term.ToLower()))
            {
                AddRecommendationToDictionary(swumRecord.SwumNodeName, normalWeight + (int)(normalWeight * 10 / Distance(swumRecord.SwumNodeName, term)), recommendations);
            }               
        }

        static int maxOffset = 5;

        public static float Distance(string s1, string s2)
        {
            if (String.IsNullOrEmpty(s1))
                return
                String.IsNullOrEmpty(s2) ? 0 : s2.Length;
            if (String.IsNullOrEmpty(s2))
                return s1.Length;
            int c = 0;
            int offset1 = 0;
            int offset2 = 0;
            int lcs = 0;
            while ((c + offset1 < s1.Length)
            && (c + offset2 < s2.Length))
            {
                if (s1[c + offset1] == s2[c + offset2]) lcs++;
                else
                {
                    offset1 = 0;
                    offset2 = 0;
                    for (int i = 0; i < maxOffset; i++)
                    {
                        if ((c + i < s1.Length)
                        && (s1[c + i] == s2[c]))
                        {
                            offset1 = i;
                            break;
                        }
                        if ((c + i < s2.Length)
                        && (s1[c] == s2[c + i]))
                        {
                            offset2 = i;
                            break;
                        }
                    }
                }
                c++;
            }
            var returnVal = (s1.Length + s2.Length) / 2 - lcs;
            return returnVal;
        }
       

        /// <summary>
        /// Returns the index of <paramref name="word"/> within <paramref name="phraseNode"/>, or -1 if it's not found.
        /// </summary>
        private int FindWordInPhraseNode(PhraseNode phraseNode, string word) {
            if(phraseNode == null) { throw new ArgumentNullException("phraseNode"); }
            if(word == null) { throw new ArgumentNullException("word"); }
            
            int index = -1;
            for(int i = 0; i < phraseNode.Size(); i++) {
                if(string.Compare(phraseNode[i].Text, word, StringComparison.InvariantCultureIgnoreCase) == 0) {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private int GetWeightForPartOfSpeech(PartOfSpeechTag pos) {
            const int NormalWeight = 1;
            const int PrimaryPosWeight = 5;
            const int PreambleWeight = 0;
            var primaryPos = new[] { PartOfSpeechTag.Noun, PartOfSpeechTag.NounPlural, PartOfSpeechTag.PastParticiple, PartOfSpeechTag.Verb, PartOfSpeechTag.Verb3PS, PartOfSpeechTag.VerbIng };

            int result = NormalWeight;
            if(primaryPos.Contains(pos)) {
                result = PrimaryPosWeight;
            } else if(pos == PartOfSpeechTag.Preamble) {
                result = PreambleWeight;
            }
            return result;
        }
    }
}
