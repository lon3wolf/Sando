using ABB.SrcML;
using ABB.SrcML.VisualStudio;
using ABB.VisualStudio;
using Configuration.OptionsPages;
using EnvDTE;
using EnvDTE80;
using Lucene.Net.Analysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Sando.Core.Extensions;
using Sando.Core.Extensions.Configuration;
using Sando.Core.Logging;
using Sando.Core.Logging.Events;
using Sando.Core.Logging.Persistence;
using Sando.Core.QueryRefomers;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.IndexerContracts;
using Sando.ExtensionContracts.ServiceContracts;
using Sando.Indexer;
using Sando.Indexer.IndexFiltering;
using Sando.Indexer.IndexState;
using Sando.Indexer.Searching;
using Sando.Indexer.Splitter;
using Sando.Parser;
using Sando.Recommender;
using Sando.SearchEngine;
using Sando.UI.Monitoring;
using Sando.UI.Options;
using Sando.UI.Service;
using Sando.UI.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sando.UI
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package in the
    // Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(SearchToolWindow), Transient = false, MultiInstances = false, Style = VsDockStyle.Tabbed)]
    [Guid(GuidList.guidUIPkgString)]
    // This attribute starts up our extension early so that it can listen to solution events
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    // Start when solution exists
    //[ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideOptionPage(typeof(SandoDialogPage), "Sando", "General", 1000, 1001, true)]
    [ProvideProfile(typeof(SandoDialogPage), "Sando", "General", 1002, 1003, true)]

    /// <summary>
    /// Add the ProvideServiceAttribute to the VSPackage that provides the global service.
    /// ProvideServiceAttribute registers SSandoGlobalService with Visual Studio. Only the global
    /// service must be registered.
    /// </summary>
    [ProvideService(typeof(SSandoGlobalService))]
    public sealed class UIPackage : Package, IToolWindowFinder, IMissingFilesIncluder
    {
        #region Internal Fields

        //used to determine if a file is text, so Sando can index it.
        [Import(typeof(IFileExtensionRegistryService))]
        internal IFileExtensionRegistryService fileExtensionRegistry = null;

        #endregion Internal Fields

        #region Private Fields

        private DTEEvents _dteEvents;
        private ExtensionPointsConfiguration _extensionPointsConfiguration;
        private bool _setupHandlers = false;
        private SolutionEvents _solutionEvents;
        private SrcMLArchive _srcMLArchive;
        private ViewManager _viewManager;
        private ISrcMLGlobalService srcMLService;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Default constructor of the package. Inside this method you can place any initialization
        /// code that does not require any Visual Studio service because at this point the package
        /// object is created but not sited yet inside Visual Studio environment. The place to do
        /// all the other initialization is the Initialize method.
        /// </summary>
        public UIPackage()
        {
            PathManager.Create(Assembly.GetAssembly(typeof(UIPackage)).Location);
            SandoLogManager.StartDefaultLogging(PathManager.Instance.GetExtensionRoot());

            // Add callback methods to the service container to create the services. Here we update
            // the list of the provided services with the ones specific for this package. Notice
            // that we set to true the boolean flag about the service promotion for the global: to
            // promote the service is actually to proffer it globally using the SProfferService
            // service. For performance reasons we don’t want to instantiate the services now, but
            // only when and if some client asks for them, so we here define only the type of the
            // service and a function that will be called the first time the package will receive a
            // request for the service. This callback function is the one responsible for creating
            // the instance of the service object.
            IServiceContainer serviceContainer = this as IServiceContainer;
            ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
            serviceContainer.AddService(typeof(SSandoGlobalService), callback, true);
            serviceContainer.AddService(typeof(SSandoLocalService), callback);
        }

        #endregion Public Constructors

        #region Public Methods

        public SandoDialogPage GetSandoDialogPage()
        {
            return (GetDialogPage(typeof(SandoDialogPage)) as SandoDialogPage);
        }

        public void OpenSandoOptions()
        {
            try
            {
                string sandoDialogPageGuid = "B0002DC2-56EE-4931-93F7-70D6E9863940";
                var command = new CommandID(
                    VSConstants.GUID_VSStandardCommandSet97,
                    VSConstants.cmdidToolsOptions);
                var mcs = GetService(typeof(IMenuCommandService))
                    as MenuCommandService;
                mcs.GlobalInvoke(command, sandoDialogPageGuid);
            }
            catch (Exception e)
            {
                LogEvents.UIGenericError(this, e);
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Implement the callback method. This is the function that will create a new instance of
        /// the services the first time a client will ask for a specific service type. It is called
        /// by the base class's implementation of IServiceProvider.
        /// </summary>
        /// <param name="container">
        /// The IServiceContainer that needs a new instance of the service. This must be this package.
        /// </param>
        /// <param name="serviceType">The type of service to create.</param>
        /// <returns>The instance of the service.</returns>
        private object CreateService(IServiceContainer container, Type serviceType)
        {
            Trace.WriteLine("    SandoServicePackage.CreateService()");
            //todo: write it to log file

            // Check if the IServiceContainer is this package.
            if (container != this)
            {
                Trace.WriteLine("ServicesPackage.CreateService called from an unexpected service container.");
                return null;
            }

            // Find the type of the requested service and create it.
            if (typeof(SSandoGlobalService) == serviceType)
            {
                // Build the global service using this package as its service provider.
                return new SandoGlobalService(this);
            }
            if (typeof(SSandoLocalService) == serviceType)
            {
                // Build the local service using this package as its service provider.
                return new SandoLocalService(this);
            }

            // If we are here the service type is unknown, so write a message on the debug output
            // and return null.
            //Trace.WriteLine("ServicesPackage.CreateService called for an unknown service type.");
            return null;
        }

        #endregion Private Methods

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation

        #region Package Members

        private bool lastShowValue = false;

        private Action progressAction;

        private TaskScheduler taskScheduler;

        private ITaskManagerService taskSchedulerService;

        // from IMissingFilesIncluder
        public void EnsureNoMissingFilesAndNoDeletedFiles()
        {
            //make sure you're not missing any files
            var indexingTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        SrcMLArchiveEventsHandlers handlers = null;
                        foreach (var fileName in srcMLService.CurrentSrcMLArchive.GetFiles())
                        {
                            if (handlers == null)
                                handlers = ServiceLocator.Resolve<SrcMLArchiveEventsHandlers>();
                            handlers.SourceFileChanged(srcMLService, new FileEventRaisedArgs(FileEventType.FileRenamed, fileName));
                        }
                        break;
                    }
                    catch (NullReferenceException ne)
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                }
            }, new CancellationToken(false), TaskCreationOptions.LongRunning, GetTaskSchedulerService());

            indexingTask.ContinueWith((t) =>
                {
                    var srcMLArchiveEventsHandlers = ServiceLocator.Resolve<SrcMLArchiveEventsHandlers>();
                    //go through all files and delete necessary ones
                    foreach (var file in ServiceLocator.Resolve<DocumentIndexer>().GetDocumentList())
                        if (!PathFileExists(new System.Text.StringBuilder(file)))
                            srcMLArchiveEventsHandlers.SourceFileChanged(srcMLService, new FileEventRaisedArgs(FileEventType.FileDeleted, file));
                },
            new CancellationToken(false), TaskContinuationOptions.LongRunning, GetTaskSchedulerService());
        }

        // http://stackoverflow.com/questions/2225415/why-is-file-exists-much-slower-when-the-file-does-not-exist#2230169
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static bool PathFileExists(System.Text.StringBuilder path);

        public void RegisterSolutionEvents()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            if (dte != null)
            {
                _solutionEvents = dte.Events.SolutionEvents;
                _solutionEvents.Opened += SolutionHasBeenOpened;
                _solutionEvents.BeforeClosing += SolutionAboutToClose;
            }
        }

        public void StartupCompleted()
        {
            try
            {
                if (_viewManager.ShouldShow())
                {
                    _viewManager.ShowSando();
                    try
                    {
                        _viewManager.ShowToolbar();
                    }
                    catch (Exception e)
                    {
                        //ignore, because
                        //will fail during testing in VS IDE host
                    }
                }

                if (ServiceLocator.Resolve<DTE2>().Version.StartsWith("10"))
                {
                    //only need to do this in VS2010, and it breaks things in VS2012
                    Solution openSolution = ServiceLocator.Resolve<DTE2>().Solution;
                    if (openSolution != null && !String.IsNullOrWhiteSpace(openSolution.FullName))
                    {
                        SolutionHasBeenOpened();
                    }
                }

                RegisterSolutionEvents();
            }
            catch (Exception e)
            {
                LogEvents.UIGenericError(this, e);
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited,
        /// so this is the place where you can put all the initilaization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                base.Initialize();
                LogEvents.UISandoBeginInitialization(this);

                SetupDependencyInjectionObjects();

                _viewManager = ServiceLocator.Resolve<ViewManager>();
                AddCommand();
                SetUpLifeCycleEvents();

                //load srml package first?
                taskScheduler = GetTaskSchedulerService();
                ServiceLocator.RegisterInstance(new SrcMLArchiveEventsHandlers(taskScheduler));
                RegisterSrcMLService();
                GetFileExtensionService();
            }
            catch (Exception e)
            {
                LogEvents.UISandoInitializationError(this, e);
            }
        }

        private static void SetupDataLogging()
        {
            var sandoOptions = ServiceLocator.Resolve<ISandoOptionsProvider>().GetSandoOptions();
            if (sandoOptions.AllowDataCollectionLogging)
            {
                SandoLogManager.StartDataCollectionLogging(PathManager.Instance.GetExtensionRoot());
            }
            else
            {
                SandoLogManager.StopDataCollectionLogging();
            }
        }

        private void AddCommand()
        {
            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the tool window
                var toolwndCommandID = new CommandID(GuidList.guidUICmdSet, (int)PkgCmdIDList.sandoSearch);
                var menuToolWin = new MenuCommand(_viewManager.ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }
            KeyBindingsManager.RebindOnceOnly();
        }

        private void DteEventsOnOnBeginShutdown()
        {
            if (_extensionPointsConfiguration != null)
            {
                ExtensionPointsConfigurationFileReader.WriteConfiguration(_extensionPointsConfiguration);
                //After writing the extension points configuration file, the index state file on disk is out of date; so it needs to be rewritten
                IndexStateManager.SaveCurrentIndexState();
            }
            //TODO - kill file processing threads
        }

        private void GetFileExtensionService()
        {
            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            var sp = new ServiceProvider(dte2 as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            var mefContainer = sp.GetService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
            mefContainer.DefaultCompositionService.SatisfyImportsOnce(this);
            if (fileExtensionRegistry != null)
                ServiceLocator.RegisterInstance<IFileExtensionRegistryService>(fileExtensionRegistry);
        }

        private TaskScheduler GetTaskSchedulerService()
        {
            if (taskSchedulerService == null)
            {
                taskSchedulerService = GetService(typeof(STaskManagerService)) as ITaskManagerService;
                if (null == taskSchedulerService)
                {
                    throw new Exception("Can not get the task scheduler global service.");
                }
                else
                {
                    ServiceLocator.RegisterInstance(taskSchedulerService);
                }
            }
            return taskSchedulerService.GlobalScheduler;
        }

        private void RecreateEntireIndex()
        {
            //just recreate the whole index
            var indexingTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        SrcMLArchiveEventsHandlers handlers = null;
                        foreach (var fileName in srcMLService.CurrentSrcMLArchive.GetFiles())
                        {
                            if (handlers == null)
                                handlers = ServiceLocator.Resolve<SrcMLArchiveEventsHandlers>();
                            handlers.SourceFileChanged(srcMLService, new FileEventRaisedArgs(FileEventType.FileAdded, fileName));
                        }
                        break;
                    }
                    catch (NullReferenceException ne)
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                }
            }, new CancellationToken(false), TaskCreationOptions.LongRunning, GetTaskSchedulerService());
        }

        private void RegisterExtensionPoints()
        {
            var extensionPointsRepository = ExtensionPointsRepository.Instance;
            extensionPointsRepository.RegisterParserImplementation(new List<string> { ".cs" }, new SrcMLCSharpParser());
            extensionPointsRepository.RegisterParserImplementation(new List<string> { ".h", ".cpp", ".cxx", ".c" }, new SrcMLCppParser(srcMLService));
            extensionPointsRepository.RegisterWordSplitterImplementation(new WordSplitter());
            extensionPointsRepository.RegisterResultsReordererImplementation(new SortByScoreResultsReorderer());
            extensionPointsRepository.RegisterQueryWeightsSupplierImplementation(new QueryWeightsSupplier());
            extensionPointsRepository.RegisterQueryRewriterImplementation(new DefaultQueryRewriter());
            extensionPointsRepository.RegisterIndexFilterManagerImplementation(new IndexFilterManager());

            var sandoOptions = ServiceLocator.Resolve<ISandoOptionsProvider>().GetSandoOptions();

            var loggerPath = Path.Combine(sandoOptions.ExtensionPointsPluginDirectoryPath, "ExtensionPointsLogger.log");
            var logger = FileLogger.CreateFileLogger("ExtensionPointsLogger", loggerPath);
            _extensionPointsConfiguration = ExtensionPointsConfigurationFileReader.ReadAndValidate(logger);

            if (_extensionPointsConfiguration != null)
            {
                _extensionPointsConfiguration.PluginDirectoryPath = sandoOptions.ExtensionPointsPluginDirectoryPath;
                ExtensionPointsConfigurationAnalyzer.FindAndRegisterValidExtensionPoints(_extensionPointsConfiguration, logger);
            }
        }

        private void RegisterSrcMLHandlers(SrcMLArchiveEventsHandlers srcMLArchiveEventsHandlers)
        {
            if (!_setupHandlers)
            {
                _setupHandlers = true;
                srcMLService.SourceFileChanged += srcMLArchiveEventsHandlers.SourceFileChanged;
                srcMLService.MonitoringStopped += srcMLArchiveEventsHandlers.MonitoringStopped;
            }
        }

        private void RegisterSrcMLService()
        {
            srcMLService = GetService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
            if (null == srcMLService)
            {
                throw new Exception("Cannot get the SrcML global service.");
            }
            else
            {
                ServiceLocator.RegisterInstance(srcMLService);
            }
        }

        /// <summary>
        /// Respond to solution opening. Still use Sando's SolutionMonitorFactory because Sando's
        /// SolutionMonitorFactory has too much indexer code which is specific with Sando.
        /// </summary>
        private void RespondToSolutionOpened(object sender, DoWorkEventArgs ee)
        {
            try
            {
                SolutionKey key = SetupSolutionKey();

                bool isIndexRecreationRequired = IndexStateManager.IsIndexRecreationRequired();
                isIndexRecreationRequired = isIndexRecreationRequired || !PathManager.Instance.IndexPathExists(key);

                //Setup indexers
                ServiceLocator.RegisterInstance(new IndexFilterManager());
                ServiceLocator.RegisterInstance<Analyzer>(SnowballAndWordSplittingAnalyzer.GetAnalyzer());
                var srcMLArchiveEventsHandlers = ServiceLocator.Resolve<SrcMLArchiveEventsHandlers>();
                var currentIndexer = new DocumentIndexer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                ServiceLocator.RegisterInstance(currentIndexer);
                ServiceLocator.RegisterInstance(new IndexUpdateManager());

                if (isIndexRecreationRequired)
                {
                    currentIndexer.ClearIndex();
                }
                RegisterExtensionPoints();
                SwumManager.Instance.Initialize(PathManager.Instance.GetIndexPath(ServiceLocator.Resolve<SolutionKey>()), !isIndexRecreationRequired);
                SetupRecommenderSystem();
                SetupDataLogging();
                LogEvents.SolutionOpened(this, Path.GetFileName(key.GetSolutionPath()));

                if (isIndexRecreationRequired)
                {
                    RecreateEntireIndex();
                }
                else
                {
                    EnsureNoMissingFilesAndNoDeletedFiles();
                }

                RegisterSrcMLHandlers(ServiceLocator.Resolve<SrcMLArchiveEventsHandlers>());
            }
            catch (Exception e)
            {
                LogEvents.UIRespondToSolutionOpeningError(this, e);
            }
        }

        private void SetUpLifeCycleEvents()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            _dteEvents = dte.Events.DTEEvents;
            _dteEvents.OnBeginShutdown += DteEventsOnOnBeginShutdown;
            _dteEvents.OnStartupComplete += StartupCompleted;
        }

        private void SetupRecommenderSystem()
        {
            // xige
            var dictionary = new DictionaryBasedSplitter(taskScheduler);
            ServiceLocator.RegisterInstance(dictionary);
            dictionary.Initialize(PathManager.Instance.GetIndexPath(ServiceLocator.Resolve<SolutionKey>()));
            ServiceLocator.Resolve<IndexUpdateManager>().indexUpdated +=
                dictionary.UpdateProgramElement;
            ServiceLocator.RegisterInstance(dictionary);

            var reformer = new QueryReformerManager(dictionary);
            reformer.Initialize(null);
            ServiceLocator.RegisterInstance(reformer);

            var history = new SearchHistory();
            history.Initialize(PathManager.Instance.GetIndexPath
                (ServiceLocator.Resolve<SolutionKey>()));
            ServiceLocator.RegisterInstance(history);
        }

        private SolutionKey SetupSolutionKey()
        {
            //TODO if solution is reopen - the guid should be read from file - future change
            var solutionId = Guid.NewGuid();
            var openSolution = ServiceLocator.Resolve<DTE2>().Solution;
            var solutionPath = openSolution.FileName;
            var key = new SolutionKey(solutionId, solutionPath);
            ServiceLocator.RegisterInstance(key);
            return key;
        }

        private void SolutionAboutToClose()
        {
            try
            {
                if (_srcMLArchive != null)
                {
                    _srcMLArchive.Dispose();
                    _srcMLArchive = null;
                    ServiceLocator.Resolve<IndexFilterManager>().Dispose();
                    ServiceLocator.Resolve<DocumentIndexer>().Dispose();
                }
                // XiGe: dispose the dictionary.
                ServiceLocator.Resolve<DictionaryBasedSplitter>().Dispose();
                ServiceLocator.Resolve<SearchHistory>().Dispose();
            }
            catch (Exception e)
            {
                LogEvents.UISolutionClosingError(this, e);
            }
        }

        private void SolutionHasBeenOpened()
        {
            var bw = new BackgroundWorker { WorkerReportsProgress = false, WorkerSupportsCancellation = false };
            bw.DoWork += RespondToSolutionOpened;
            bw.RunWorkerAsync();
        }

        #endregion Package Members

        private void SetupDependencyInjectionObjects()
        {
            ServiceLocator.RegisterInstance(GetService(typeof(DTE)) as DTE2);
            ServiceLocator.RegisterInstance(this);
            ServiceLocator.RegisterInstance<IMissingFilesIncluder>(this);
            ServiceLocator.RegisterInstance(new ViewManager(this));
            ServiceLocator.RegisterInstance<ISandoOptionsProvider>(new SandoOptionsProvider());
            ServiceLocator.RegisterInstance(new InitialIndexingWatcher());
        }
    }
}