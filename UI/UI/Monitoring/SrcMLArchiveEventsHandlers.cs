using System;
using System.IO;
using ABB.SrcML;
using ABB.SrcML.VisualStudio.SrcMLService;
using Sando.Core.Extensions;
using Sando.Core.Logging;
using Sando.DependencyInjection;
using Sando.Indexer;
using Sando.Indexer.IndexFiltering;
using Sando.Recommender;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Xml.Linq;
using Sando.Core.Logging.Events;
using System.Linq;
using Sando.UI.View;
using Sando.ExtensionContracts.TaskFactoryContracts;
using System.Diagnostics;
using System.ComponentModel;
using System.Xml;
using System.Reflection;
using Sando.Core.Logging.Persistence;
using Lucene.Net.Store;


namespace Sando.UI.Monitoring
{
    public class SrcMLArchiveEventsHandlers : ITaskScheduler
    {

        private ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
        private ConcurrentBag<CancellationTokenSource> cancellers = new ConcurrentBag<CancellationTokenSource>();
        private TaskScheduler scheduler;
        public TaskFactory factory;
        public static SrcMLArchiveEventsHandlers Instance;
        public Action WhenDoneWithTasks = null;
        public Action WhenStartedFirstTask = null;
        public bool HaveTasks = false;

        public static int MAX_PARALLELISM = 2;


        private SrcMLArchiveEventsHandlers()
        {
            throw new NotImplementedException();
        }

        public SrcMLArchiveEventsHandlers(TaskScheduler aScheduler)
        {
            scheduler = aScheduler;
            factory = new TaskFactory(scheduler);
            Instance = this;            
        }

        public void SourceFileChanged(object sender, FileEventRaisedArgs args)
        {
            SourceFileChanged(sender, args, false);
        }

        private UIPackage GetPackage()
        {
            if (package == null)
            {
                package = ServiceLocator.Resolve<UIPackage>();
            }
            return package;
        }

        public int TaskCount()
        {
            lock (tasksTrackerLock)
                return tasks.Count;
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

        private bool CanBeIndexed(string fileName) {
            var fileExtension = Path.GetExtension(fileName);
            var hasFileExtension = (fileExtension != null && !fileExtension.Equals(String.Empty));
            if(hasFileExtension) {
                return ServiceLocator.Resolve<IndexFilterManager>().ShouldFileBeIndexed(fileName);
            }
            return false;
        }
        public void SourceFileChanged(object sender, FileEventRaisedArgs args, bool commitImmediately = false)
        {
            var cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;            
            Action action =  () =>
            {
                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken.ThrowIfCancellationRequested();
                }
                var documentIndexer = ServiceLocator.Resolve<DocumentIndexer>();

                if(CanBeIndexed(args.FilePath)) {
                    
                    string sourceFilePath = args.FilePath;
                    string oldSourceFilePath = args.OldFilePath;

                    ProcessFileEvent(sender as ISrcMLGlobalService, args, commitImmediately, documentIndexer);
                } else {
                    documentIndexer.DeleteDocuments(args.FilePath, commitImmediately);
                }
            };            
            StartNew(action, cancelTokenSource);
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
                if (xelement == null && args.EventType!= FileEventType.FileDeleted) return;
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
                        // FileRenamed is repurposed. Now means you may already know about it, so check and only parse if not existing
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

        private object tasksTrackerLock = new object();        
        private int counter=0;
        private UIPackage package;
   

        public void MonitoringStopped(object sender, EventArgs args)
        {
            ClearTasks();

            LogEvents.UIMonitoringStopped(this);
            var currentIndexer = ServiceLocator.ResolveOptional<DocumentIndexer>();
            if (currentIndexer != null)
            {
                currentIndexer.Dispose(false);  // Because in SolutionMonitor: public void StopMonitoring(bool killReaders = false)
            }
            if (SwumManager.Instance != null)
            {
                SwumManager.Instance.PrintSwumCache();
                SwumManager.Instance.Clear();
            }
        }

        public void ClearTasks()
        {
            lock (tasksTrackerLock)
            {
                foreach (var cancelToken in cancellers)
                    cancelToken.Cancel();
            }
        }



    }
}