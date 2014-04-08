using System;
using System.IO;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sando.Core;
using Sando.Core.Logging;
using Sando.DependencyInjection;
using Sando.Core.Tools;
using Sando.Core.Logging.Persistence;

namespace Sando.UI.View
{
    public class ViewManager
    {

        private readonly IToolWindowFinder _toolWindowFinder;
        private const string Introducesandoflag = "IntroduceSandoFlag";

        private FlagManager flagManager = new FlagManager(Introducesandoflag);

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        public void ShowToolWindow(object sender, EventArgs e)
        {
            ShowSando();
        }

        /// <summary>
        /// Side affect is creating the tool window if it doesn't exist yet
        /// </summary>
        /// <returns></returns>
        private IVsWindowFrame GetWindowFrame()
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = _toolWindowFinder.FindToolWindow(typeof(SearchToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            _toolWindowFinder.UpdateIndexingFilesList();
            return windowFrame;
        }

        private bool _isRunning;

        public ViewManager(IToolWindowFinder finder)
        {
            _toolWindowFinder = finder;
        }

        public void EnsureViewExists()
        {
            if (!_isRunning)
            {
                GetWindowFrame();
                _isRunning = true;
            }
        }


        public void ShowSando()
        {
            var windowFrame = GetWindowFrame();
            // Dock Sando to the bottom of Visual Studio.
            windowFrame.SetFramePos(VSSETFRAMEPOS.SFP_fDockRight, Guid.Empty, 0, 0, 0, 0);            
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            flagManager.CreateFlag();
        }

 

        public void ShowToolbar()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            var cbs = ((CommandBars) dte.CommandBars);
            CommandBar cb = cbs["Sando Toolbar"];
            cb.Visible = true;
        }

        public bool ShouldShow()
        {
            return flagManager.DoesFlagExist();
        }

  
    }

    public class FlagManager{

        private string flagName;

        public FlagManager(string Introducesandoflag)
        {            
            this.flagName = Introducesandoflag;
        }

        public void CreateFlag()
        {
            try
            {
                File.Create(GetFullPathForFlag());
            }
            catch (IOException ioe)
            {
                //ignore if two people are writing to this
            }
        }
        
        public bool DoesFlagExist()
        {
            return !File.Exists(GetFullPathForFlag());
        }

        private string GetFullPathForFlag()
        {
            return Path.Combine(PathManager.Instance.GetExtensionRoot(), flagName);
        }
    }

    public  interface IToolWindowFinder
    {
        ToolWindowPane FindToolWindow(Type type, int i, bool b);

        void UpdateIndexingFilesList();
    }
}
