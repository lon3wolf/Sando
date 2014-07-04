using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Snowball;
using NUnit.Framework;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer;
using Sando.Indexer.Searching;
using Sando.SearchEngine;
using Sando.UI.Monitoring;
using UnitTestHelpers;
using Sando.Recommender;
using Sando.Indexer.IndexFiltering;
using Sando.UI.Options;
using Configuration.OptionsPages;
using ABB.SrcML;
using System.Threading;
using Sando.Core.Tools;
using ABB.SrcML.VisualStudio.SrcMLService;
using Sando.UI.View;
using System.Diagnostics;
using System.Text;
using Sando.Indexer.Documents;
using Lucene.Net.Analysis.Standard;
using Sando.Core.QueryRefomers;
using Sando.UI;
using System.Threading.Tasks;
using ABB.SrcML.Utilities;
using ABB.VisualStudio;
using System.Reflection;
using Sando.Indexer.Splitter;
using Sando.Indexer.Searching.Criteria;

namespace Sando.IntegrationTests.Search
{
    public class AutomaticallyIndexingTestClass : ISrcMLGlobalService
    { 
        public void Reset()
        {
             
        }

        [TestFixtureSetUp]
        public void Setup()
        {            
            SrcMLArchiveEventsHandlers.MAX_PARALLELISM = 8;
            IndexSpecifiedFiles(GetFilesDirectory(), GetIndexDirName());
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _srcMLArchive.Dispose();
            ServiceLocator.Resolve<IndexFilterManager>().Dispose();
            ServiceLocator.Resolve<DocumentIndexer>().Dispose();            
            DeleteTestDirectoryContents();
        }


        public virtual TimeSpan? GetTimeToCommit()
        {
            return null;
        }

        public virtual string GetIndexDirName()
        {
            throw new NotImplementedException();
        }

        public virtual string GetFilesDirectory()
        {
            throw new System.NotImplementedException();
        }

        private void IndexSpecifiedFiles(string filesInThisDirectory, string indexDirName)
        {
            filesInThisDirectory = Path.GetFullPath(filesInThisDirectory);
            CreateSystemWideDefaults(indexDirName);
            CreateKey(filesInThisDirectory);
            CreateIndexer();
            CreateArchive(filesInThisDirectory);            
            CreateSwum();            
            AddFilesToIndex(filesInThisDirectory);
            WaitForIndexing();            
        }

        public void WaitForIndexing()
        {
            while (_handler.TaskCount() > 0)
                Thread.Sleep(1000);
            while (((IEnumerable<Task>)GetATestingScheduler().GetType().InvokeMember("GetScheduledTasks",
                   BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                   null, GetATestingScheduler(), null)).Count() != 0)
                Thread.Sleep(1000);
            ServiceLocator.Resolve<DocumentIndexer>().ForceFlush();
            ServiceLocator.Resolve<DocumentIndexer>().ForceReaderRefresh();
        }

 
        public static TaskScheduler GetATestingScheduler(){
            if(scheduler == null)
                scheduler = new LimitedConcurrencyLevelTaskScheduler(100, true);
            return scheduler;
        }

 
        public void AddFilesToIndex(string filesInThisDirectory)
        {
            _handler = new SrcMLArchiveEventsHandlers(GetATestingScheduler());
            var files = GetFileList(filesInThisDirectory);
            foreach (var file in files)
            {
                if (Path.GetExtension(Path.GetFullPath(file)).Equals(".cs") ||
                    Path.GetExtension(Path.GetFullPath(file)).Equals(".cpp") ||
                    Path.GetExtension(Path.GetFullPath(file)).Equals(".c") ||
                    Path.GetExtension(Path.GetFullPath(file)).Equals(".h") ||
                    Path.GetExtension(Path.GetFullPath(file)).Equals(".cxx") ||
                    Path.GetExtension(Path.GetFullPath(file)).Equals(".txt")
                    )
                    HandleFileUpdated(file);
            }
            done = true;
        }

        protected virtual void HandleFileUpdated(string file)
        {
            _handler.SourceFileChanged(this, new FileEventRaisedArgs(FileEventType.FileAdded, file));
        }

        public List<string> GetFileList(string filesInThisDirectory, List<string> incoming = null)
        {
            if (filesInThisDirectory.EndsWith("LIBS") || filesInThisDirectory.EndsWith("bin") || filesInThisDirectory.EndsWith("Debug"))
                return incoming;
            if (incoming == null)
                incoming = new List<string>();
            incoming.AddRange(Directory.EnumerateFiles(filesInThisDirectory));
            var dirs = new List<string>();
            dirs.AddRange(Directory.EnumerateDirectories(filesInThisDirectory));
            foreach (var dir in dirs)
                GetFileList(dir, incoming);
            return incoming;
        }

        public void CreateSwum()
        {
            SwumManager.Instance.Initialize(PathManager.Instance.GetIndexPath(ServiceLocator.Resolve<Sando.Core.Tools.SolutionKey>()), false);
            SwumManager.Instance.Archive = _srcMLArchive;
        }


        public void CreateArchive(string filesInThisDirectory)
        {
            var srcMlArchiveFolder = Path.Combine(_indexPath, "archive");
            var srcMLFolder = Path.Combine(".", "SrcML", "CSharp");
            Directory.CreateDirectory(srcMlArchiveFolder);
            var generator = new SrcMLGenerator(TestUtils.SrcMLDirectory);
            _srcMLArchive = new SrcMLArchive(_indexPath,  false, generator);
        }



        public void CreateIndexer()
        {
            ServiceLocator.Resolve<UIPackage>();

            ServiceLocator.RegisterInstance(new IndexFilterManager());

            Analyzer analyzer = SnowballAndWordSplittingAnalyzer.GetAnalyzer();
            ServiceLocator.RegisterInstance<Analyzer>(analyzer);

            var currentIndexer = new DocumentIndexer(TestUtils.GetATestingScheduler());
            ServiceLocator.RegisterInstance(currentIndexer);
            ServiceLocator.RegisterInstance(new IndexUpdateManager());
            currentIndexer.ClearIndex();            
            ServiceLocator.Resolve<InitialIndexingWatcher>().SetInitialIndexingStarted();

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

        }



        public void CreateKey(string filesInThisDirectory)
        {
            Directory.CreateDirectory(_indexPath);
            var key = new Sando.Core.Tools.SolutionKey(Guid.NewGuid(), filesInThisDirectory);
            ServiceLocator.RegisterInstance(key);
        }

        public void CreateSystemWideDefaults(string indexDirName)
        {
            _indexPath = Path.Combine(Path.GetTempPath(), indexDirName);
            TestUtils.InitializeDefaultExtensionPoints();
            ServiceLocator.RegisterInstance<ISandoOptionsProvider>(new FakeOptionsProvider(_indexPath,40,false,new List<string>()));
            ServiceLocator.RegisterInstance(new SrcMLArchiveEventsHandlers(GetATestingScheduler()));
            ServiceLocator.RegisterInstance(new InitialIndexingWatcher());
        }


    
        private void DeleteTestDirectoryContents()
        {
            var deleted = false;
            while (!deleted)
            {
                try
                {
                    Directory.Delete(_indexPath, true);
                    deleted = true;
                }
                catch (DirectoryNotFoundException e)
                {
                    deleted = true;
                }
                catch (Exception e)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private string _indexPath;
        private SrcMLArchive _srcMLArchive;
        protected SrcMLArchiveEventsHandlers _handler;

        protected List<CodeSearchResult> EnsureRankingPrettyGood(string keywords, Predicate<CodeSearchResult> predicate, int expectedLowestRank)
        {
            _results = GetResults(keywords);
            if (expectedLowestRank > 0)
            {
                var methodSearchResult = CheckExistance(keywords, predicate);
                CheckRanking(keywords, expectedLowestRank, methodSearchResult.FirstOrDefault());
            }
            return _results;
        }

        private void CheckRanking(string keywords, int expectedLowestRank, CodeSearchResult methodSearchResult)
        {
            var rank = _results.IndexOf(methodSearchResult) + 1;
            Assert.IsTrue(rank <= expectedLowestRank,
                          "Searching for " + keywords + " doesn't return a result in the top " + expectedLowestRank + "; rank=" +
                          rank);
        }

        protected List<CodeSearchResult> CheckExistance(string keywords, Predicate<CodeSearchResult> predicate)
        {
            var methodSearchResult = _results.FindAll(predicate);
            if (methodSearchResult == null||methodSearchResult.Count==0)
            {
                string info = PrintFailInformation();
                Assert.Fail("Failed to find relevant search result for search: " + keywords+"\n"+info);                
            }
            return methodSearchResult;
        }

        public string PrintFailInformation(bool includeFiles = true)
        {
            StringBuilder info = new StringBuilder();
            if (includeFiles)
            {
                info.AppendLine("Indexed Documents: " + ServiceLocator.Resolve<DocumentIndexer>().GetNumberOfIndexedDocuments());
                foreach (var file in GetFileList(GetFilesDirectory()))
                    info.AppendLine("file: " + file);
            }
            if (_results != null)
                foreach (var result in _results)
                    info.AppendLine(result.Name + " in " + result.FileName);
            else
                info.AppendLine("Returned 0 results");
            return info.ToString();
        }

        protected List<CodeSearchResult> GetResults(string keywords)
        {
            var manager = SearchManagerFactory.GetNewBackgroundSearchManager();
            manager.SearchResultUpdated += this.Update;
            manager.SearchCompletedMessageUpdated += this.UpdateMessage;

            _results = null;
            var criteria = CriteriaBuilderFactory.GetBuilder().GetCriteria(keywords);
            manager.Search(keywords, criteria);
            int i = 0;
            while (_results == null)
            {
                Thread.Sleep(50);
                i++;
                if (i > 100)
                    break;
            }
            return _results;
        }

        public System.Xml.Linq.XElement GetXElementForSourceFile(string sourceFilePath)
        {
            return _srcMLArchive.GetXElementForSourceFile(sourceFilePath);
        }

        public ISrcMLArchive GetSrcMLArchive()
        {
            return _srcMLArchive;
        }

        public bool IsMonitoring { get { return true; } }
        public bool IsUpdating { get { return !done; } }
        public event EventHandler MonitoringStarted;
        public event EventHandler MonitoringStopped;
        public event EventHandler UpdateArchivesStarted;
        public event EventHandler UpdateArchivesCompleted;

        public event EventHandler<FileEventRaisedArgs> SourceFileChanged;

        protected List<CodeSearchResult> _results;
        protected string _myMessage;
        private bool done = false;
        private static TaskScheduler scheduler;


        public void StopMonitoring()
        {
            throw new NotImplementedException();
        }

        public void Update(string searchString, IQueryable<CodeSearchResult> results)
        {
            var newResults = new List<CodeSearchResult>();
            foreach(var result in results)
                 newResults.Add(result);
            _results = newResults;
        }

        public void UpdateMessage(string message)
        {
            _myMessage = message;            
        }

        public class FakeOptionsProvider : ISandoOptionsProvider
        {
            private string _myIndex;
            private int _myResultsNumber;
			private bool _myAllowLogs;
            private List<string> _myFileExtensions;

            public FakeOptionsProvider(string index, int num, bool allowLogs, List<string> fileExtensions)
            {
                _myIndex = index;
                _myResultsNumber = num;
				_myAllowLogs = allowLogs;
                _myFileExtensions = fileExtensions;
            }

            public SandoOptions GetSandoOptions()
            {
                return new SandoOptions(_myIndex,_myResultsNumber, _myAllowLogs, _myFileExtensions);
            }
        }


       

        public void StartMonitoring()
        {
            throw new NotImplementedException();
        }

        public void AddDirectoryToMonitor(string pathToDirectory)
        {
            throw new NotImplementedException();
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<string> MonitoredDirectories
        {
            get { throw new NotImplementedException(); }
        }

        public void RemoveDirectoryFromMonitor(string pathToDirectory)
        {
            throw new NotImplementedException();
        }

        public double ScanInterval
        {
            get
            {
                return 60;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double SaveInterval
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryAdded;

        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryRemoved;
    }
}
