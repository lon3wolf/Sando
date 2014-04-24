using System;
using NUnit.Framework;
using Sando.Indexer.Documents;
using UnitTestHelpers;
using Lucene.Net.Analysis;
using System.IO;
using Portal.LuceneInterface;
using System.Collections.Generic;

namespace Sando.Indexer.UnitTests.Documents
{
	[TestFixture]
	public class SandoDocumentStringExtensionTest
	{
		[Test]
		public void SandoDocumentStringExtension_ToSandoSearchableReturnsValidString()
		{            
            CheckSplits("SetFileExtension", "SetFileExtension Set File Extension");
            CheckSplits("donothing", "donothing");            
		}

        private static void CheckSplits(string testString, string expectedSplits)
        {
            StringReader r = new StringReader(testString);
            TokenStream ts = new WhitespaceTokenizer(r);
            WordDelimiterFilter filter = new WordDelimiterFilter(ts, 1, 1, 1, 1, 1);            
            var toFind = new HashSet<string>();
            foreach (var term in expectedSplits.Split())
                toFind.Add(term);
            Token token = filter.Next();
            while (token!=null && !String.IsNullOrEmpty(token.ToString()))
            {                
                toFind.Remove(token.Term());
                token = filter.Next();
            }
            Assert.AreEqual(0, toFind.Count);
        }


		[TestFixtureSetUp]
		public void SetUp()
		{
			TestUtils.InitializeDefaultExtensionPoints();
		}
	}
}
