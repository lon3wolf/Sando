using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE80;

namespace Sando.Validation
{
    public class IntelliTraceConsoleRunner
    {
        private string _pathToIntelliTraceExe;
        private string _pathToVsTestConsoleExe;
        private const string PathToCollectionPlanXml = @"../collectionplan.xml";
        private string _pathToLogFile;
        private DTE2 _dte;

        public IntelliTraceConsoleRunner(DTE2 dte)
        {
            Contract.Requires(dte != null, "Received a null VS DTE object");
            Contract.Ensures(File.Exists(_pathToVsTestConsoleExe), "vstest.console.exe could not be found in it usual location.");
            Contract.Ensures(File.Exists(_pathToIntelliTraceExe), "intellitrace.exe could not be found in it usual location.");

            _dte = dte;

            _pathToIntelliTraceExe = Path.Combine(ConsoleUtils.GetVisualStudioInstallationPath(dte),
                                            @"CommonExtensions\Microsoft\IntelliTrace\" + dte.Version + @".0\IntelliTrace.exe");

            _pathToVsTestConsoleExe = Path.Combine(ConsoleUtils.GetVisualStudioInstallationPath(dte),
                                            @"CommonExtensions\Microsoft\TestWindow\vstest.console.exe");

            _pathToLogFile = Path.GetTempFileName();
        }

        public IntelliTraceConsoleRunner(string intelliTracePath, string vsTestConsolePath)
        {
            _pathToIntelliTraceExe = intelliTracePath;
            _pathToVsTestConsoleExe = vsTestConsolePath;
        }

        public void SelectResultsUsingIntelliTrace(string testName, string testLibraryPath)
        {
            var command = "\"" + _pathToVsTestConsoleExe + "\" /UseVsixExtensions:true /Tests:" + testName + " \"" + testLibraryPath + "\"";
            ExecuteIntelliTrace(command);
        }

        private string ExecuteIntelliTrace(string command)
        {
            var collectionPlanCommand = "/cp:\"" + PathToCollectionPlanXml + "\"";
            var logFileCommand = "/logfile:\"" + _pathToLogFile + "\"";
            var commandPlus = " " + collectionPlanCommand + " " + logFileCommand + " " + command;
            return ConsoleUtils.ExecuteCommandInConsole(_pathToIntelliTraceExe, commandPlus);
        }

    }
}
