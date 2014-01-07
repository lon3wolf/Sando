using ABB.SrcML;
using NUnit.Framework;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.UI.Monitoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnitTestHelpers;

namespace Sando.IntegrationTests.Search
{

    [TestFixture]
	public class DoubleIndexingTest: AutomaticallyIndexingTestClass
	{
        [Test]
        public void CheckForDuplicateEntriesWith_OpenFile_Query()
        {
            var keywords = "open file";
            _results = GetResults(keywords);
            Predicate<CodeSearchResult> predicate = el => el.ProgramElement.ProgramElementType == ProgramElementType.Field && (el.ProgramElement.Name == "fileName") && (el.ParentOrFile == "FileNameTemplate.cs") && (el.ProgramElement.DefinitionLineNumber == 12);
            var methodSearchResult = CheckExistance(keywords, predicate);
            Assert.IsTrue(methodSearchResult.Count == 1, String.Format("Generated {0} duplicates",methodSearchResult.Count));
        }

        [Test]
        public void CheckForDuplicateEntriesWith_HandleFile_Query()
        {
            var keywords = "handle file";
            _results = GetResults(keywords);
            Predicate<CodeSearchResult> predicate = el => el.ProgramElement.ProgramElementType == ProgramElementType.Class && (el.ProgramElement.Name == "ImageCapture") && (el.ParentOrFile == "ImageCapture.cs");
            var methodSearchResult = CheckExistance(keywords, predicate);
            Assert.IsTrue(methodSearchResult.Count == 1, String.Format("Generated {0} duplicates", methodSearchResult.Count));
        }

        [Test]
        public void CheckForDuplicateEntriesAfterReinsertingFiles_OpenFile_Query()
        {
            var files = GetFileList(GetFilesDirectory());
            foreach (var file in files)
                HandleFileUpdated(file);
            var keywords = "open file";
            _results = GetResults(keywords);
            Predicate<CodeSearchResult> predicate = el => el.ProgramElement.ProgramElementType == ProgramElementType.Field && (el.ProgramElement.Name == "fileName") && (el.ParentOrFile == "FileNameTemplate.cs") && (el.ProgramElement.DefinitionLineNumber == 12);
            var methodSearchResult = CheckExistance(keywords, predicate);
            Assert.IsTrue(methodSearchResult.Count == 1, String.Format("Generated {0} duplicates", methodSearchResult.Count));
        }


        protected override void HandleFileUpdated(string file)
        {        
            for (int i = 0; i < 100; i++ )
                _handler.SourceFileChanged(this, new FileEventRaisedArgs(FileEventType.FileAdded, file));         
        }

        public override string GetIndexDirName()
        {
            return "TestFilesSearchingTest";
        }

        public override string GetFilesDirectory()
        {
            //return "..\\..\\IntegrationTests\\TestFiles";
            return Path.Combine(TestUtils.SolutionDirectory, "IntegrationTests", "TestFiles");
        }

        public override TimeSpan? GetTimeToCommit()
        {
            return TimeSpan.FromSeconds(4);
        }

   
    }
}
