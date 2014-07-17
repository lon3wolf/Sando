using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using NUnit.Framework;
using Sando.Core.QueryRefomers;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer;
using Sando.Indexer.Documents;
using Sando.Indexer.Searching;
using Sando.UnitTestHelpers;
using UnitTestHelpers;
using System.Threading;
using Sando.Indexer.Searching.Criteria;
using Configuration.OptionsPages;

namespace Sando.SearchEngine.UnitTests
{

    [TestFixture]
    public class CodeSearcherFixture
    {
		private DocumentIndexer _indexer;
    	private string _indexerPath;
		private SolutionKey _solutionKey;


    	[Test]
        public void TestCreateCodeSearcher()
        {
            Assert.DoesNotThrow(() => new CodeSearcher());            
        }

        [Test]     
        public void PerformBasicSearch()
        {
        	CodeSearcher cs = new CodeSearcher();            
            List<CodeSearchResult> result = cs.Search("SimpleName");
            Assert.True(result.Count > 0);                                 
        }

		[TestFixtureSetUp]
    	public void CreateIndexer()
		{
			TestUtils.InitializeDefaultExtensionPoints();

			_indexerPath = Path.GetTempPath() + "luceneindexer";
		    Directory.CreateDirectory(_indexerPath);
			_solutionKey = new SolutionKey(Guid.NewGuid(), "C:/SolutionPath");
            ServiceLocator.RegisterInstance(_solutionKey);
            ServiceLocator.RegisterInstance<Analyzer>(new SimpleAnalyzer());
            _indexer = new DocumentIndexer(TestUtils.GetATestingScheduler());
            ServiceLocator.RegisterInstance(_indexer);
            ServiceLocator.RegisterInstance<ISandoOptionsProvider>(new FakeOptionsProvider(String.Empty, 20, false, new List<string>()));

            // xige
            var dictionary = new DictionaryBasedSplitter();
            dictionary.Initialize(PathManager.Instance.GetIndexPath(ServiceLocator.Resolve<SolutionKey>()));
            ServiceLocator.RegisterInstance(dictionary);

            var reformer = new QueryReformerManager(dictionary);
            reformer.Initialize(null);
            ServiceLocator.RegisterInstance(reformer);

            var history = new SearchHistory();
            history.Initialize(PathManager.Instance.GetIndexPath
                (ServiceLocator.Resolve<SolutionKey>()));
            ServiceLocator.RegisterInstance(history);

    		ClassElement classElement = SampleProgramElementFactory.GetSampleClassElement(
				accessLevel: AccessLevel.Public,
				definitionLineNumber: 11,
				extendedClasses: "SimpleClassBase",
				fullFilePath: "C:/Projects/SimpleClass.cs",
				implementedInterfaces: "IDisposable",
				name: "SimpleName",
				namespaceName: "Sanod.Indexer.UnitTests"
    		);
    		SandoDocument sandoDocument = DocumentFactory.Create(classElement);
    		_indexer.AddDocument(sandoDocument);
			MethodElement methodElement = SampleProgramElementFactory.GetSampleMethodElement(
				accessLevel: AccessLevel.Protected,
    		    name: "SimpleName",
				returnType: "Void",
				fullFilePath: "C:/stuff"
			);
    		sandoDocument = DocumentFactory.Create(methodElement);
    		_indexer.AddDocument(sandoDocument);
            Thread.Sleep(2000);
    	}

		[TestFixtureTearDown]
    	public void ShutdownIndexer()
    	{
			_indexer.ClearIndex();
			_indexer.Dispose(true);   
    	}
    }
}
