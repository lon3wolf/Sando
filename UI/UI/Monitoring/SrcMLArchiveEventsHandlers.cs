using ABB.SrcML;
using ABB.SrcML.VisualStudio;
using Lucene.Net.Store;
using Sando.Core.Extensions;
using Sando.Core.Logging.Events;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.TaskFactoryContracts;
using Sando.Indexer;
using Sando.Indexer.IndexFiltering;
using Sando.Recommender;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sando.UI.Monitoring
{
    public class SrcMLArchiveEventsHandlers : ITaskScheduler
    {
        #region Public Fields

        public TaskFactory factory;
        public bool HaveTasks = false;
        public Action WhenDoneWithTasks = null;
        public Action WhenStartedFirstTask = null;

        #endregion Public Fields

        #region Private Fields

        private ConcurrentBag<CancellationTokenSource> cancellers = new ConcurrentBag<CancellationTokenSource>();
        private UIPackage package;
        private TaskScheduler scheduler;
        private ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
        private object tasksTrackerLock = new object();

        #endregion Private Fields

        #region Public Constructors

        public SrcMLArchiveEventsHandlers(TaskScheduler aScheduler)
        {
            scheduler = aScheduler;
            factory = new TaskFactory(scheduler);
        }

        #endregion Public Constructors

        #region Private Constructors

        private SrcMLArchiveEventsHandlers()
        {
            throw new NotImplementedException();
        }

        #endregion Private Constructors

        #region Public Methods

        public void ClearTasks()
        {
            lock (tasksTrackerLock)
            {
                foreach (var cancelToken in cancellers)
                    cancelToken.Cancel();
            }
        }

        public void MonitoringStopped(object sender, EventArgs args)
        {
            ClearTasks();

            LogEvents.UIMonitoringStopped(this);
            var currentIndexer = ServiceLocator.ResolveOptional<DocumentIndexer>();
            if (currentIndexer != null && !currentIndexer.IsDisposingOrDisposed())
            {
                currentIndexer.Dispose(false);  // Because in SolutionMonitor: public void StopMonitoring(bool killReaders = false)
            }
            if (SwumManager.Instance != null)
            {
                SwumManager.Instance.PrintSwumCache();
                SwumManager.Instance.Clear();
            }
        }

        public void SourceFileChanged(object sender, FileEventRaisedArgs args)
        {
            var currentIndexer = ServiceLocator.ResolveOptional<DocumentIndexer>();
            if (currentIndexer != null && !currentIndexer.IsDisposingOrDisposed())
                SourceFileChanged(sender, args, false);
        }

        public void SourceFileChanged(object sender, FileEventRaisedArgs args, bool commitImmediately = false)
        {
            var cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;
            Action action = () =>
            {
                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken.ThrowIfCancellationRequested();
                }
                var documentIndexer = ServiceLocator.Resolve<DocumentIndexer>();

                if (CanBeIndexed(args.FilePath))
                {
                    string sourceFilePath = args.FilePath;
                    string oldSourceFilePath = args.OldFilePath;

                    ProcessFileEvent(sender as ISrcMLGlobalService, args, commitImmediately, documentIndexer);
                }
                else
                {
                    documentIndexer.DeleteDocuments(args.FilePath, commitImmediately);
                }
            };
            StartNew(action, cancelTokenSource);
        }

        public Task StartNew(Action a, CancellationTokenSource c)
        {
            var task = factory.StartNew(a, c.Token);
            lock (tasksTrackerLock)
            {
                if (tasks.Count() == 0)
                {
                    if (WhenStartedFirstTask != null)
                    {
                        factory.StartNew(WhenStartedFirstTask);
                    }
                    HaveTasks = true;
                }

                tasks.Add(task);
                cancellers.Add(c);
            }
            task.ContinueWith(removeTask => RemoveTask(task, c));
            return task;
        }

        public int TaskCount()
        {
            lock (tasksTrackerLock)
                return tasks.Count;
        }

        #endregion Public Methods

        #region Private Methods

        private static XElement GetXElement(FileEventRaisedArgs args, ISrcMLGlobalService srcMLService)
        {
            try
            {
                return srcMLService.GetXElementForSourceFile(args.FilePath);
            }
            catch (ArgumentException e)
            {
                return XElement.Load(args.FilePath);
            }
        }

        private static void ProcessFileEvent(ISrcMLGlobalService srcMLService, FileEventRaisedArgs args,
            bool commitImmediately, DocumentIndexer documentIndexer)
        {
            string sourceFilePath = args.FilePath;
            var fileExtension = Path.GetExtension(sourceFilePath);
            var parsableToXml = (ExtensionPointsRepository.Instance.GetParserImplementation(fileExtension) != null);
            if (ConcurrentIndexingMonitor.TryToLock(sourceFilePath)) return;
            XElement xelement = null;
            if (parsableToXml)
            {
                xelement = GetXElement(args, srcMLService);
                if (xelement == null && args.EventType != FileEventType.FileDeleted) return;
            }
            var indexUpdateManager = ServiceLocator.Resolve<IndexUpdateManager>();
            try
            {
                switch (args.EventType)
                {
                    case FileEventType.FileAdded:
                        documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant()); //"just to be safe!"
                        indexUpdateManager.Update(sourceFilePath.ToLowerInvariant(), xelement);
                        if (parsableToXml)
                        {
                            SwumManager.Instance.AddSourceFile(sourceFilePath.ToLowerInvariant(), xelement);
                        }
                        break;

                    case FileEventType.FileChanged:
                        documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant());
                        indexUpdateManager.Update(sourceFilePath.ToLowerInvariant(), xelement);
                        if (parsableToXml)
                        {
                            SwumManager.Instance.UpdateSourceFile(sourceFilePath.ToLowerInvariant(), xelement);
                        }
                        break;

                    case FileEventType.FileDeleted:
                        documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant(), commitImmediately);
                        if (parsableToXml)
                        {
                            SwumManager.Instance.RemoveSourceFile(sourceFilePath.ToLowerInvariant());
                        }
                        break;

                    case FileEventType.FileRenamed:
                        // FileRenamed is repurposed. Now means you may already know about it, so
                        // check and only parse if not existing
                        if (!SwumManager.Instance.ContainsFile(sourceFilePath.ToLowerInvariant()))
                        {
                            documentIndexer.DeleteDocuments(sourceFilePath.ToLowerInvariant()); //"just to be safe!"
                            indexUpdateManager.Update(sourceFilePath.ToLowerInvariant(), xelement);
                            if (parsableToXml)
                            {
                                SwumManager.Instance.AddSourceFile(sourceFilePath.ToLowerInvariant(), xelement);
                            }
                        }
                        break;

                    default:
                        // if we get here, a new event was probably added. for now, no-op
                        break;
                }
            }
            catch (AlreadyClosedException ace)
            {
                //ignore, index closed
            }
            ConcurrentIndexingMonitor.ReleaseLock(sourceFilePath);
        }

        private bool CanBeIndexed(string fileName)
        {
            var fileExtension = Path.GetExtension(fileName);
            var hasFileExtension = (fileExtension != null && !fileExtension.Equals(String.Empty));
            if (hasFileExtension)
            {
                return ServiceLocator.Resolve<IndexFilterManager>().ShouldFileBeIndexed(fileName);
            }
            return false;
        }

        private UIPackage GetPackage()
        {
            if (package == null)
            {
                package = ServiceLocator.Resolve<UIPackage>();
            }
            return package;
        }

        private void RemoveTask(Task task, CancellationTokenSource cancelToken)
        {
            lock (tasksTrackerLock)
            {
                tasks.TryTake(out task);
                cancellers.TryTake(out cancelToken);
                if (tasks.Count() == 0)
                {
                    if (WhenDoneWithTasks != null)
                    {
                        factory.StartNew(WhenDoneWithTasks);
                    }
                    HaveTasks = false;
                }
            }
        }

        #endregion Private Methods
    }
}