using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Sando.Core.Logging.Events;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ResultsReordererContracts;
using System;

namespace Sando.UI.Actions
{
    public delegate void FileOpenedEventHandler(object sender, EventArgs e);

    public static class FileOpener
    {
        #region Private Fields

        private static DTE2 _dte;

        #endregion Private Fields

        #region Public Events

        public static event FileOpenedEventHandler FileOpened;

        #endregion Public Events

        #region Public Methods

        public static bool Is2012OrLater()
        {
            InitDte2();
            if (_dte.Version.Contains("11.0") || _dte.Version.Contains("12.0") || _dte.Version.Contains("13.0"))
                return true;
            return false;
        }

        public static void OpenFile(string filePath, int lineNumber)
        {
            try
            {
                InitDte2();
                Window window = _dte.ItemOperations.OpenFile(filePath, Constants.vsViewKindTextView);
                var selection = (TextSelection)_dte.ActiveDocument.Selection;
                selection.GotoLine(lineNumber);

                LogEvents.OpeningCodeSearchResult(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

                if (FileOpened != null)
                {
                    FileOpened(null, new EventArgs());
                }
            }
            catch (Exception e)
            {
                LogEvents.UIOpenFileError(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, e);
                //ignore, we don't want this feature ever causing a crash
            }
        }


        #endregion Public Methods

        #region Private Methods

        private static void InitDte2()
        {
            try
            {
                if (_dte == null)
                {
                    _dte = ServiceLocator.Resolve<DTE2>();
                }
            }
            catch (Exception e)
            {
                LogEvents.UIOpenFileError(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, e);
                //ignore, we don't want this feature ever causing a crash
            }
        }

        #endregion Private Methods
    }
}