using NUnit.Framework;
using Sando.DependencyInjection;
using Sando.Indexer;
using Sando.IntegrationTests.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnitTestHelpers;

namespace Sando.IntegrationTests.Search.Performance
{
    [TestFixture]
    public class IndexingPerformanceTest
    {

        private const int NumberOfExperimentsToAverage = 5;
        private int[] CorpusMultipliers = { 1, 2, 5, 10 };

        private AutomaticallyIndexingTestClass automaticallyIndexingTestClass;

        [TestFixtureSetUp]
        public void Setup()
        {
            automaticallyIndexingTestClass = new AutomaticallyIndexingTestClass();
        }

        [Test]
        //[Ignore]
        public void SelfIndexingPerformanceTest()
        {
            var fileStream = new StreamWriter(TestUtils.SolutionDirectory + "\\SandoPerformanceData.txt");

            fileStream.WriteLine("Sando Git Hash = 7d47a1ec749383adb153efd3874f4fd13c8022f5");
            fileStream.WriteLine("Date = " + DateTime.UtcNow);

            for (int n = 0; n < CorpusMultipliers.Length; n++)
            {
                Stopwatch timer = new Stopwatch();
                double sumExpTimes = 0.0;
                for (int j = 0; j < NumberOfExperimentsToAverage; j++)
                {
                    MeasurePerformanceOfSelfIndexing(CorpusMultipliers[n], timer);
                    sumExpTimes += timer.Elapsed.TotalSeconds;
                }
                var avgExpTime = sumExpTimes / NumberOfExperimentsToAverage;
                fileStream.WriteLine("Time for " + CorpusMultipliers[n] + " self indexing ops = " + avgExpTime + "secs");
                timer.Reset();
            }

            fileStream.Close();
        }

        private void MeasurePerformanceOfSelfIndexing(int corpusMultiplier, Stopwatch timer)
        {
            var indexDirName = GetIndexDirName() + "_" + corpusMultiplier;
            var filesInThisDirectory = TestUtils.SolutionDirectory;

            automaticallyIndexingTestClass.CreateSystemWideDefaults(indexDirName);
            automaticallyIndexingTestClass.CreateKey(filesInThisDirectory);
            automaticallyIndexingTestClass.CreateIndexer();
            automaticallyIndexingTestClass.CreateArchive(filesInThisDirectory);
            automaticallyIndexingTestClass.CreateSwum(); 

            for (int i = 0; i < corpusMultiplier; i++)            
            {
                timer.Start();

                automaticallyIndexingTestClass.AddFilesToIndex(filesInThisDirectory);
                automaticallyIndexingTestClass.WaitForIndexing();
                ServiceLocator.Resolve<DocumentIndexer>().ForceReaderRefresh();

                timer.Stop();

                Thread.Sleep(TimeSpan.FromSeconds(10));
                ServiceLocator.Resolve<DocumentIndexer>().ClearIndex();
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            
            automaticallyIndexingTestClass.TearDown();
        }

        private string GetIndexDirName()
        {
            return "IndexingPerformanceTest";
        }

    }
}
