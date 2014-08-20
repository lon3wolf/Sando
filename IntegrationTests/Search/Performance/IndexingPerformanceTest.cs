using NUnit.Framework;
using Sando.DependencyInjection;
using Sando.Indexer;
using Sando.IntegrationTests.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnitTestHelpers;
using Sando.Recommender;

namespace Sando.IntegrationTests.Search.Performance
{
    [TestFixture]
    public class IndexingPerformanceTest
    {
        private const int NumberOfExperimentsToAverage = 5;
        private string PerformanceOutputFile = TestUtils.SolutionDirectory + "\\SandoPerformanceData.txt";
        private string ExternalProjectsDirectory = TestUtils.SolutionDirectory + "\\ScalabilityTestProjects";

        private AutomaticallyIndexingTestClass automaticallyIndexingTestClass;
        private bool _indexedAProject;

        [TestFixtureSetUp]
        public void Setup()
        {
            automaticallyIndexingTestClass = new AutomaticallyIndexingTestClass();
            _indexedAProject = false;
        }

        [Test]
        [Ignore]
        public void ExternalIndexingPerformanceTest()
        {
            var fileStream = File.AppendText(PerformanceOutputFile);

            fileStream.WriteLine("*** ExternalIndexingPerformanceTest ***");
            fileStream.WriteLine("Date = " + DateTime.UtcNow);

            var sandoDirectory = Path.Combine(ExternalProjectsDirectory, "sando");
            if (! System.IO.Directory.Exists(sandoDirectory))
            {
                GitDownloadProjectSource("https://git01.codeplex.com/sando", fileStream);
            }
            PerformAndWritePerformanceExperiments(fileStream, sandoDirectory);

            var nugetDirectory = Path.Combine(ExternalProjectsDirectory, "nuget");
            if (!System.IO.Directory.Exists(nugetDirectory))
            {
                GitDownloadProjectSource("https://git01.codeplex.com/nuget", fileStream);
            }
            PerformAndWritePerformanceExperiments(fileStream, nugetDirectory);

            var linuxDirectory = Path.Combine(ExternalProjectsDirectory, "linux");
            if (!System.IO.Directory.Exists(linuxDirectory))
            {
                GitDownloadProjectSource("https://github.com/torvalds/linux.git", fileStream);
            }
            PerformAndWritePerformanceExperiments(fileStream, linuxDirectory);

            /*
            var cmsswDirectory = Path.Combine(ExternalProjectsDirectory, "cmssw");
            if (!System.IO.Directory.Exists(cmsswDirectory))
            {
                GitDownloadProjectSource("https://github.com/cms-sw/cmssw.git", fileStream);
            }
            PerformAndWritePerformanceExperiments(fileStream, cmsswDirectory);
            */

            fileStream.Close();
        }

        private void PerformAndWritePerformanceExperiments(StreamWriter fileStream, string projectDir)
        {
            var projectName = Path.GetFileName(projectDir);

            Stopwatch indexingTimer = new Stopwatch();
            double sumIndexingTimes = 0.0;
            int indexedDocs = 0;

            var wordsToGenerateRecommendationFor = new string[] { projectName, "hello world", "performance", "module", "bug" };
            Stopwatch recommendationTimer = new Stopwatch();
            double sumRecTimes = 0.0;

            for (int j = 0; j < NumberOfExperimentsToAverage; j++)
            {
                indexedDocs = MeasureIndexingTime(projectDir, indexingTimer);
                sumIndexingTimes += indexingTimer.Elapsed.TotalSeconds;
                indexingTimer.Reset();

                MeasureRecommendationTime(wordsToGenerateRecommendationFor, recommendationTimer);
                sumRecTimes += recommendationTimer.Elapsed.TotalSeconds;
                recommendationTimer.Reset();

                CleanUpAfterIndexing();
            }

            fileStream.Write("*** " + projectName + ": ");
            fileStream.Write(automaticallyIndexingTestClass.GetFileList(projectDir).Count + " source files, ");
            fileStream.WriteLine(indexedDocs + " program elements");

            var avgIndexingTime = sumIndexingTimes / NumberOfExperimentsToAverage;
            fileStream.WriteLine("Time for " + projectName + " indexing = " + avgIndexingTime + "secs");

            var avgRecTime = sumRecTimes / (NumberOfExperimentsToAverage * wordsToGenerateRecommendationFor.Count());
            fileStream.WriteLine("Time for " + projectName + " recommendation generation = " + avgRecTime + "secs");
        }

        private void GitDownloadProjectSource(string url, StreamWriter fileStream)
        {
            CreateExternalProjectsDirectory();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = true;
            startInfo.RedirectStandardOutput = false;
            startInfo.RedirectStandardInput = false;
            startInfo.WorkingDirectory = ExternalProjectsDirectory;
            startInfo.FileName = "git.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "clone " + url;

            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                    fileStream.WriteLine(url + " -- git.exe process exitted with code: {0}", exeProcess.ExitCode);
                }
            }
            catch (Exception ex)
            {
                fileStream.WriteLine(url + " -- problem running git.exe - " + ex.ToString());
            }
        }

        private int MeasureIndexingTime(string projectDirName, Stopwatch timer)
        {
            automaticallyIndexingTestClass.CreateSystemWideDefaults(Path.GetFileName(projectDirName) + "_IndexDir");
            automaticallyIndexingTestClass.CreateKey(projectDirName);
            automaticallyIndexingTestClass.CreateIndexer();
            automaticallyIndexingTestClass.CreateArchive(projectDirName);
            automaticallyIndexingTestClass.CreateSwum(); 

            timer.Start();

            automaticallyIndexingTestClass.AddFilesToIndex(projectDirName);
            automaticallyIndexingTestClass.WaitForIndexing();
            ServiceLocator.Resolve<DocumentIndexer>().ForceReaderRefresh();

            timer.Stop();

            Thread.Sleep(TimeSpan.FromSeconds(10));
            _indexedAProject = true;
            var indexedDocs = ServiceLocator.Resolve<DocumentIndexer>().GetNumberOfIndexedDocuments();
            return indexedDocs;
        }

        private void MeasureRecommendationTime(string[] wordsToGenerateRecommendationFor, Stopwatch timer)
        {

            if (_indexedAProject)
            {       
                var recommender = ServiceLocator.Resolve<QueryRecommender>();

                timer.Start();

                for (int i = 0; i < wordsToGenerateRecommendationFor.Count(); i++)
                {
                    recommender.GenerateRecommendations(wordsToGenerateRecommendationFor[i]);
                }

                timer.Stop();
            }
        }

        private void CleanUpAfterIndexing()
        {
            if (_indexedAProject)
            {
                ServiceLocator.Resolve<DocumentIndexer>().ClearIndex();
                Thread.Sleep(TimeSpan.FromSeconds(10));
                automaticallyIndexingTestClass.TearDown();
                _indexedAProject = false;
            }
        }

        private void CreateExternalProjectsDirectory()
        {
            if (! System.IO.Directory.Exists(ExternalProjectsDirectory))
            {
                System.IO.Directory.CreateDirectory(ExternalProjectsDirectory);
            }
        }

    }
}
