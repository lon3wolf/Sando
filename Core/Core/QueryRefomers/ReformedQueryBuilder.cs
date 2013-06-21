﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sando.Core.QueryRefomers
{
    public class ReformedQueryBuilder
    {
        private readonly List<List<ReformedWord>> reformedTermLists = new
            List<List<ReformedWord>>();

        private class InternalReformedQuery : IReformedQuery, ICloneable, 
            IEquatable<IReformedQuery>
        {
            private readonly List<ReformedWord> allTerms;

            public InternalReformedQuery()
            {
                this.allTerms = new List<ReformedWord>();
            }

            private InternalReformedQuery(IEnumerable<ReformedWord> allTerms)
            {
                this.allTerms = allTerms.ToList();
            }

            public IEnumerable<ReformedWord> ReformedQuery
            {
                get { return allTerms; }
            }

            public IEnumerable<string> GetReformedTerms {
                get { return allTerms.Select(t => t.OriginalTerm).ToList(); }
            }

            public string ReformExplanation {
                get
                {
                    var sb = new StringBuilder();
                    foreach (ReformedWord reformedTerm in allTerms)
                    {
                        if (reformedTerm.Category != TermChangeCategory.NOT_CHANGED)
                        {
                            sb.Append(reformedTerm.ReformExplanation + ";");
                        }
                    }
                    return sb.ToString();
                }
            }

            public string QueryString {
                get { 
                    var sb = new StringBuilder();
                    foreach (ReformedWord term in allTerms)
                    {
                        sb.Append(term.NewTerm);
                        sb.Append(" ");
                    }
                    return sb.ToString().Trim();
                }
            }

            public object Clone()
            {
                return new InternalReformedQuery(allTerms);
            }

            public void AppendTerm(ReformedWord term)
            {
                allTerms.Add(term);
            }

            public bool Equals(IReformedQuery other)
            {
                return this.QueryString.Equals(other.QueryString);
            }
        }


        public void StartBuilding()
        {
            this.reformedTermLists.Clear();
        }

        public void AddReformedTerms(IEnumerable<ReformedWord> newTerms)
        {
            this.reformedTermLists.Add(newTerms.ToList());
        }

        public IEnumerable<IReformedQuery> GetAllPossibleReformedQueriesSoFar()
        {
            var allReformedQuries = new List<InternalReformedQuery> {new InternalReformedQuery()};
            var allQuries = reformedTermLists.Aggregate(allReformedQuries, 
                (current, reformedTermList) => current.SelectMany(q => 
                    GenerateNewQueriesByAppendingTerms(q, reformedTermList)).ToList());
            return RemoveDuplication(allQuries);
        }

        private IEnumerable<IReformedQuery> RemoveDuplication(IEnumerable<IReformedQuery> queries)
        {
            var set = new HashSet<IReformedQuery>(queries);
            return set.AsEnumerable();
        }

        private IEnumerable<InternalReformedQuery> GenerateNewQueriesByAppendingTerms(InternalReformedQuery query, 
            ICollection<ReformedWord> newTerms)
        {
            var list = new List<InternalReformedQuery>();
            for (int i = 0; i < newTerms.Count; i++)
            {
                var extendedQuery = (InternalReformedQuery) query.Clone();
                extendedQuery.AppendTerm(newTerms.ElementAt(i));
                list.Add(extendedQuery);
            }
            return list;
        }
    }
}
