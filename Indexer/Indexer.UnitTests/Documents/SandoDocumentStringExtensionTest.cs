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
		public void SandoDocumentStringExtension_SplitNormalStuff()
		{            
            CheckSplits("SetFileExtension", "SetFileExtension Set File Extension");
            CheckSplits("donothing", "donothing");
            CheckSplits("SandoDocument", "Sando Document sandodocument");
            CheckSplits("openFile", "open file openfile");
            CheckSplits("CamelIdSplitterTests", "camel id splitter tests camelidsplittertests");            
		}

        [Test]
        public void SandoDocumentStringExtension_SplitWithUnderscores()
        {
            //size_t
            CheckSplits("size_t", "size_t sizet size t");
            CheckSplits("_internalID", "_internalID internal ID internalID");
            CheckSplits("_firstVisibleLine", "_firstVisibleLine first visible line firstvisibleline");
            CheckSplits("LANG_INDEX_TYPE2", "LANG_INDEX_TYPE2 lang index type 2 LANGINDEXTYPE2");
            CheckSplits("Set_File_Extension", "SetFileExtension Set File Extension Set_File_Extension");
            CheckSplits("uni16BE_NoBOM", "uni16BE_NoBOM uni 16 BE No BOM uni16BENoBOM");            
            CheckSplits("TAB_DRAWINACTIVETAB", "TAB_DRAWINACTIVETAB TABDRAWINACTIVETAB TAB DRAWINACTIVETAB");            
            
        }

        [Test]
        public void SandoDocumentStringExtension_SplitWeirdStuff()
        {
            CheckSplits("XMLelement", "xml element xmlelement lelement");            
            CheckSplits("executeSrcML", "execute src ml executesrcml");            
            CheckSplits("UIOpenFile", "UI Open File UIOpenFile");            
        }

        private static void CheckSplits(string testString, string expectedSplits)
        {
            StringReader r = new StringReader(testString);
            TokenStream ts = new WhitespaceTokenizer(r);
            WordDelimiterFilter filter = new WordDelimiterFilter(ts, 1, 1, 1, 1, 1);            
            var toFind = new HashSet<string>();
            foreach (var term in expectedSplits.Split())
                toFind.Add(term.ToLower());
            Token token = filter.Next();
            while (token!=null && !String.IsNullOrEmpty(token.ToString()))
            {                
                toFind.Remove(token.Term().ToLower());
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
