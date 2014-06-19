using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sando.Core.Tools;
using Sando.Indexer.Searching.Criteria;

namespace Sando.Indexer.UnitTests
{
    [TestFixture]
    public class QueryParsingAndConvertingTests
    {

        [Test]
        public void TestIfQueryParsesToEmptySearchTerm()
        {
            var simple = CriteriaBuilder.GetBuilder().GetCriteria("g_u16ActiveFault");
            Assert.IsFalse(simple.SearchTerms.Where(x => String.IsNullOrWhiteSpace(x)).ToList().Count >= 1);
        }
    }
}
