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
        private DTE2 _dte;

        public IntelliTraceConsoleRunner(DTE2 dte)
        {
            Contract.Requires(dte != null, "Received a null VS DTE object");
            Contract.Ensures(File.Exists(_pathToIntelliTraceExe), "intellitrace.exe could not be found in it usual location.");

            _dte = dte;

            _pathToIntelliTraceExe = Path.Combine(ConsoleUtils.GetVisualStudioInstallationPath(dte),
                                            @"CommonExtensions\Microsoft\TestWindow\intellitrace.exe");

            _pathToVsTestConsoleExe = Path.Combine(ConsoleUtils.GetVisualStudioInstallationPath(dte),
                                            @"CommonExtensions\Microsoft\TestWindow\vstest.console.exe"); 
        }

        public void SelectResultsUsingIntelliTrace(string testName)
        {
            ExecuteIntelliTrace("some command");
        }


        private string ExecuteIntelliTrace(string command)
        {
            var commandPlus = "/UseVsixExtensions:true " + command;
            return ConsoleUtils.ExecuteCommandInConsole(_pathToIntelliTraceExe, commandPlus);
        }

    }
}
