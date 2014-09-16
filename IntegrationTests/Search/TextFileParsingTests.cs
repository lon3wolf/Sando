using NUnit.Framework;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestHelpers;

namespace Sando.IntegrationTests.Search
{
    public class TextFileParsingTests: AutomaticallyIndexingTestClass
	{
        [Test]
        public void ParseClassFileLikeATestFile()
        {
            string keywords = "\"0.7.4.1\"";
            var expectedLowestRank = 2;
            Predicate<CodeSearchResult> predicate = el => el.Name.Equals("noclassclass.cs");
            List<CodeSearchResult> results = EnsureRankingPrettyGood(keywords, predicate, expectedLowestRank);
        }

		
        [Test]
        public void LargeTextFileSearch()
        {
            string keywords = "\"<CommandTable xmlns\"";
            var expectedLowestRank = 3;
            Predicate<CodeSearchResult> predicate = el => el.Name.Equals("codemaidtest.txt");
            List<CodeSearchResult> results = EnsureRankingPrettyGood(keywords, predicate, expectedLowestRank);
        }

        [Test]
        public void LargeTextFileSearchBottomOfFile()
        {
            string keywords = "\"IDSymbol name=\"IconUnlock\"";
            var expectedLowestRank = 3;
            Predicate<CodeSearchResult> predicate = el => el.Name.Equals("codemaidtest.txt");
            List<CodeSearchResult> results = EnsureRankingPrettyGood(keywords, predicate, expectedLowestRank);
        }

        public override string GetIndexDirName()
        {
            return "TextFilesTesting";
        }

        public override string GetFilesDirectory()
        {
            //return "..\\..\\IntegrationTests\\TestFiles";
            return Path.Combine(TestUtils.SolutionDirectory, "IntegrationTests", "TestFiles", "TextFilesTestFiles");
        }

        public override TimeSpan? GetTimeToCommit()
        {
            return TimeSpan.FromSeconds(4);
        }
    
    }
}
