using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using EnvDTE;
using EnvDTE80;
using Sando.Core;
using Sando.Core.Extensions.Logging;
using Sando.DependencyInjection;
using Sando.Indexer;
// Code changed by JZ: solution monitor integration
using System.Xml.Linq;

// TODO: clarify where SolutionMonitorFactory (now in Sando), SolutionKey (now in Sando), ISolution (now in SrcML.NET) should be.
//using ABB.SrcML.VisualStudio.SolutionMonitor;
// End of code changes

namespace Sando.UI.Monitoring
{
	class SolutionMonitorFactory
	{
        private static DocumentIndexer currentIndexer;

		private const string Lucene = "\\lucene";
        private const string srcML = "\\srcMlArchives";

	    public static string LuceneDirectory { get; set; }

        // Code changed by JZ: solution monitor integration
        // These 3 variables are moved from Sando's SolutionMonitor
        private static string _currentPath;
        private static bool _initialIndexDone;
        // Use the IndexUpdateManager class as a (temporary) bridge between SolutionMonitorFactory and DocumentIndexer.
        private static IndexUpdateManager _indexUpdateManager;

        /// <summary>
        /// Constructor.
        /// Use SrcML.NET's SolutionMonior, instead of Sando's SolutionMonitor
        /// </summary>
        /// <param name="isIndexRecreationRequired"></param>
        /// <returns></returns>
        public static ABB.SrcML.VisualStudio.SolutionMonitor.SolutionMonitor CreateMonitor(bool isIndexRecreationRequired)
		{
			var openSolution = ServiceLocator.Resolve<DTE2>().Solution;
			return CreateMonitor(openSolution, isIndexRecreationRequired);
		}

        /// <summary>
        /// Constructor.
        /// Use SrcML.NET's SolutionMonior, instead of Sando's SolutionMonitor
        /// </summary>
        /// <param name="openSolution"></param>
        /// <param name="isIndexRecreationRequired"></param>
        /// <returns></returns>
        private static ABB.SrcML.VisualStudio.SolutionMonitor.SolutionMonitor CreateMonitor(Solution openSolution, bool isIndexRecreationRequired)
		{
			Contract.Requires(openSolution != null, "A solution must be open");

			//TODO if solution is reopen - the guid should be read from file - future change
            var solutionId = Guid.NewGuid();
            var solutionPath = openSolution.FileName;
            var luceneDirectoryForSolution = GetLuceneDirectoryForSolution(openSolution);
            var sandoAssemblyDirectoryPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            var solutionKey = new SolutionKey(solutionId, solutionPath, luceneDirectoryForSolution, sandoAssemblyDirectoryPath);
            ServiceLocator.RegisterInstance(solutionKey);

            currentIndexer = DocumentIndexerFactory.CreateIndexer(AnalyzerType.Snowball);
			if(isIndexRecreationRequired)
			{
				currentIndexer.DeleteDocuments("*");
				currentIndexer.CommitChanges();
			}

            // Create a new instance of SrcML.NET's solution monitor
            var currentMonitor = new ABB.SrcML.VisualStudio.SolutionMonitor.SolutionMonitor(ABB.SrcML.VisualStudio.SolutionMonitor.SolutionWrapper.Create(openSolution));
            // Use the IndexUpdateManager class as a (temporary) bridge between SolutionMonitorFactory and DocumentIndexer.
            _indexUpdateManager = new IndexUpdateManager(currentIndexer);

            // These variables are moved from Sando's SolutionMonitor
            _currentPath = solutionKey.IndexPath;
            _initialIndexDone = false;

			return currentMonitor;
		}

        /// <summary>
        /// Update index for a source file.
        /// TODO: This method might be refactored to another class.
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="xelement">XElement of the source file, generated by SrcML.NET</param>
        public static void UpdateIndex(string sourceFilePath, XElement xElement)
        {
            _indexUpdateManager.Update(sourceFilePath, xElement);
        }

        /// <summary>
        /// Delete index for a source file.
        /// TODO: This method might be refactored to another class.
        /// </summary>
        /// <param name="sourceFilePath"></param>
        public static void DeleteIndex(string sourceFilePath)
        {
            //writeLog("- DI.DeleteDocuments()");
            currentIndexer.DeleteDocuments(sourceFilePath);
        }

        /// <summary>
        /// Commit index changes.
        /// TODO: This method might be refactored to another class.
        /// </summary>
        public static void CommitIndexChanges()
        {
            //writeLog("- DI.CommitChanges()");
            currentIndexer.CommitChanges();
        }

        /// <summary>
        /// Respond to the StartupCompleted event from SrcML.NET
        /// From Sando's SolutionMonitor
        /// TODO: This method might be refactored to another class.
        /// TODO: CommitIndexChanges() might be removed depending on the strategy of committing index.
        /// </summary>
        public static void StartupCompleted()
        {
            writeLog("Sando: StartupCompleted()");
            _initialIndexDone = true;
            currentIndexer.CommitChanges();
        }

        // From Sando's SolutionMonitor
        /// TODO: This method might be refactored to another class.
        public static void MonitoringStopped()
        {
            writeLog("Sando: MonitoringStopped()");
            if (currentIndexer != null)
            {
                currentIndexer.CommitChanges();
                ////_indexUpdateManager.SaveFileStates();
                currentIndexer.Dispose(false);  // Because in SolutionMonitor: public void StopMonitoring(bool killReaders = false)
                currentIndexer = null;
            }
        }

        // From SolutionMonitor.cs, don't know if it is still useful
        public static void AddUpdateListener(IIndexUpdateListener listener)
        {
            currentIndexer.AddIndexUpdateListener(listener);
        }

        // From SolutionMonitor.cs, don't know if it is still useful
        public static void RemoveUpdateListener(IIndexUpdateListener listener)
        {
            currentIndexer.RemoveIndexUpdateListener(listener);
        }

        // From SolutionMonitor.cs
        public static string GetCurrentDirectory()
        {
            return _currentPath;
        }

        // From SolutionMonitor.cs
        public static bool PerformingInitialIndexing()
        {
            return !_initialIndexDone;
        }

        /* //// Original implementation
        public static SolutionMonitor CreateMonitor(bool isIndexRecreationRequired)
        {
            var openSolution = UIPackage.GetOpenSolution();
            return CreateMonitor(openSolution, isIndexRecreationRequired);
        }

        private static SolutionMonitor CreateMonitor(Solution openSolution, bool isIndexRecreationRequired)
        {
            Contract.Requires(openSolution != null, "A solution must be open");

            //TODO if solution is reopen - the guid should be read from file - future change
            SolutionKey solutionKey = new SolutionKey(Guid.NewGuid(), openSolution.FileName, GetLuceneDirectoryForSolution(openSolution));
            var currentIndexer = DocumentIndexerFactory.CreateIndexer(solutionKey, AnalyzerType.Snowball);
            if (isIndexRecreationRequired)
            {
                currentIndexer.DeleteDocuments("*");
                currentIndexer.CommitChanges();
            }
            var currentMonitor = new SolutionMonitor(SolutionWrapper.Create(openSolution), solutionKey, currentIndexer, isIndexRecreationRequired);
            return currentMonitor;
        }
        */
        // End of code changes



		private static string CreateFolder(string folderName, string parentDirectory)
		{
			if (!File.Exists(parentDirectory + folderName))
			{
				var directoryInfo = Directory.CreateDirectory(parentDirectory + folderName);
				return directoryInfo.FullName;
			}
			else
			{
                return parentDirectory + folderName;
			}
		}

		private static string GetName(Solution openSolution)
		{
			var fullName = openSolution.FullName;
			var split = fullName.Split('\\');
			return split[split.Length - 1]+fullName.GetHashCode();
		}



		private static string GetLuceneDirectoryForSolution(Solution openSolution)
		{            
            return CreateNamedFolder(openSolution, Lucene);
		}

        private static string CreateNamedFolder(Solution openSolution, string x)
        {
            var luceneFolder = CreateFolder(x, LuceneDirectory);
            CreateFolder(GetName(openSolution), luceneFolder + "\\");
            return luceneFolder + "\\" + GetName(openSolution);
        }

        public static string GetSrcMlArchiveFolder(Solution openSolution)
        {
            return CreateNamedFolder(openSolution, srcML);
        }





        // Code changed by JZ: solution monitor integration
        /// <summary>
        /// For debugging.
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="str"></param>
        private static void writeLog( string str)
        {
            FileLogger.DefaultLogger.Info(str);
        }
        // End of code changes



    }
}