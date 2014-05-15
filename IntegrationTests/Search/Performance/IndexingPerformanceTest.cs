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
        private string PerformanceOutputFile = TestUtils.SolutionDirectory + "\\SandoPerformanceData.txt";
        private string ExternalProjectsDirectory = TestUtils.SolutionDirectory + "\\ScalabilityTestProjects";

        private AutomaticallyIndexingTestClass automaticallyIndexingTestClass;

        [TestFixtureSetUp]
        public void Setup()
        {
            automaticallyIndexingTestClass = new AutomaticallyIndexingTestClass();
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
                GitDownloadProjectSource("https://git01.codeplex.com/sando");
            }
            PerformAndWritePerfromanceExperiments(fileStream, sandoDirectory);

            var nugetDirectory = Path.Combine(ExternalProjectsDirectory, "nuget");
            if (!System.IO.Directory.Exists(nugetDirectory))
            {
                GitDownloadProjectSource("https://git01.codeplex.com/nuget");
            }
            PerformAndWritePerfromanceExperiments(fileStream, nugetDirectory);

            fileStream.Close();
        }

        private void PerformAndWritePerfromanceExperiments(StreamWriter fileStream, string projectDir)
        {
            Stopwatch timer = new Stopwatch();
            double sumExpTimes = 0.0;
            for (int j = 0; j < NumberOfExperimentsToAverage; j++)
            {
                MeasureIndexingTime(projectDir, timer);
                sumExpTimes += timer.Elapsed.TotalSeconds;
            }
            var avgExpTime = sumExpTimes / NumberOfExperimentsToAverage;
            fileStream.WriteLine("Time for " + Path.GetFileName(projectDir) + " indexing = " + avgExpTime + "secs");
        }

        private void GitDownloadProjectSource(string url)
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem running git - " + ex.ToString());
            }
        }

        private void MeasureIndexingTime(string projectDirName, Stopwatch timer)
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
            ServiceLocator.Resolve<DocumentIndexer>().ClearIndex();
            Thread.Sleep(TimeSpan.FromSeconds(10));
            
            automaticallyIndexingTestClass.TearDown();
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
