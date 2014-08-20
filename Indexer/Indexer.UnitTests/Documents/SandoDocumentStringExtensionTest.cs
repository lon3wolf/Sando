using System;
using NUnit.Framework;
using Sando.Indexer.Documents;
using UnitTestHelpers;
using Lucene.Net.Analysis;
using System.IO;
using Portal.LuceneInterface;
using System.Collections.Generic;
using System.Diagnostics;
using Sando.Indexer.Splitter;

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
        public void SandoDocumentStringExtension_SplitWithGUIDs()
        {
            CheckSplits("7e03caf3-06ed-4ff5-962a-effa1fb2f383", "7e03caf3-06ed-4ff5-962a-effa1fb2f383 7e03caf306ed4ff5962aeffa1fb2f383 7e03caf3 06ed 4ff5 962a effa1fb2f383");
            CheckSplits("7e03caf306ed4ff5962aeffa1fb2f383", "7e03caf306ed4ff5962aeffa1fb2f383");
        }

        [Test]
        public void SandoDocumentStringExtension_SplitWithUnderscoresAndDashes()
        {
            CheckSplits("size-t", "size-t sizet size t");
            CheckSplits("private SolutionEvents _solutionEvents;", "private solution events solutionevents _solutionevents");
            CheckSplits("size_t", "size_t sizet size t");            
            CheckSplits("uni16BE_NoBOM", "uni16BE_NoBOM uni 16 BE No BOM uni16BENoBOM");                        
            CheckSplits("_internalID", "_internalID internal ID internalID");
            CheckSplits("_firstVisibleLine", "_firstVisibleLine first visible line firstvisibleline");
            CheckSplits("LANG_INDEX_TYPE2", "LANG_INDEX_TYPE2 lang index type 2 LANGINDEXTYPE2");
            CheckSplits("Set_File_Extension", "SetFileExtension Set File Extension Set_File_Extension");            
            CheckSplits("TAB_DRAWINACTIVETAB", "TAB_DRAWINACTIVETAB TABDRAWINACTIVETAB TAB DRAWINACTIVETAB");
            CheckSplits("size_t", "size_t sizet size t");            
        }

        [Test]
        public void SandoDocumentStringExtension_SplitWithSymbols()
        {
            CheckSplits("OpenFile(Text f", "open file f text openfile");
            CheckSplits("if (Char.IsLower((char)i)) code |= LOWER;", "if char is lower islower i code");            
            CheckSplits(" Assert.AreEqual(foundSplits.Count, expectToFindThisMany, string.Join(\", \", notExpected));  ", "assert are equal areequal found splits foundsplits count expecttofindthismany expect to find this many string join not expected notexpected");
        }

        [Test]
        public void SandoDocumentStringExtension_SplitWeirdStuff()
        {
            CheckSplits("XMLelement", "xml element xmlelement xm lelement");            
            CheckSplits("executeSrcML", "execute src ml executesrcml");            
            CheckSplits("UIOpenFile", "UIO pen UI Open File UIOpenFile");            
        }

        private static void CheckSplits(string testString, string expectedSplits)
        {            
            StringReader r = new StringReader(testString);
            var filter = SnowballAndWordSplittingAnalyzer.GetStandardFilterSet(r);
            
            int expectToFindThisMany = expectedSplits.Split().Length;
            var expectedSplitWords = new HashSet<string>();            
            foreach (var term in expectedSplits.Split())
                expectedSplitWords.Add(term.ToLower());
            var notExpected = new HashSet<string>();

            Token token = filter.Next();
            HashSet<string> foundSplits = new HashSet<string>() ;
            while (token!=null && !String.IsNullOrEmpty(token.ToString()))
            {
                Debug.WriteLine(token.Term());
                int before = expectedSplitWords.Count;
                expectedSplitWords.Remove(token.Term());
                int after = expectedSplitWords.Count;
                if (before == after)
                    notExpected.Add(token.Term());
                foundSplits.Add(token.Term());
                token = filter.Next();                
            }
            Assert.AreEqual(0, expectedSplitWords.Count);
            Assert.AreEqual(foundSplits.Count, expectToFindThisMany, string.Join(", ", notExpected));            
        }

    


		[TestFixtureSetUp]
		public void SetUp()
		{
			TestUtils.InitializeDefaultExtensionPoints();
		}
	}
}
