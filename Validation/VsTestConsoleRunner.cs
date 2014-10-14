using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE80;
using Sando.DependencyInjection;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics.Contracts;
using System.Diagnostics;

namespace Validation
{
    public class VsTestConsoleRunner
    {
        private string _pathToVsTestConsoleExe;
        private DTE2 _dte;

        public VsTestConsoleRunner(DTE2 dte)
        {
            Contract.Requires(dte != null, "Received a null VS DTE object");
            Contract.Ensures(File.Exists(_pathToVsTestConsoleExe), "vstest.console.exe could not be found in it usual location.");

            _dte = dte;

            _pathToVsTestConsoleExe = Path.Combine(GetVisualStudioInstallationPath(), 
                                        @"CommonExtensions\Microsoft\TestWindow\vstest.console.exe"); 
        }

        /*
         * returns a pair of test name and the full path of the library containing the test
         */
        public List<Tuple<string, string>> DiscoverTests()
        {
            var testTuples = new List<Tuple<string, string>>();
            var libraryList = FindAllLibrariesInSolution();
            foreach (var library in libraryList)
            {
                var command = "/ListTests:\"" + library + "\"";
                var output = ExecuteVSTestConsole(command);
                var testList = ParseTestsFromConsoleOutput(output);
                foreach(var test in testList)
                {
                    testTuples.Add(new Tuple<string, string>(test, library));
                }
            }
            return testTuples;
        }

        /*
         * returns the full path of all libraries produced by this solution
         */ 
        public List<String> FindAllLibrariesInSolution()
        {
            var libraryList = new List<String>();
            var openSolution = _dte.Solution;           
            for (int i = 1; i < openSolution.Projects.Count; i++)
            {
                var project = openSolution.Projects.Item(i);
                if (!String.IsNullOrWhiteSpace(project.FullName))
                {
                    var outputFileName = GetAssemblyPath(project);
                    if (outputFileName.EndsWith("dll"))
                    {
                        libraryList.Add(GetAssemblyPath(project));
                    }
                }
            }
            return libraryList;            
        }

        private string GetAssemblyPath(EnvDTE.Project vsProject)
        {
            var fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
            var outputPath = vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            var outputDir = Path.Combine(fullPath, outputPath);
            var outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
            return Path.Combine(outputDir, outputFileName);
        }

        private string GetVisualStudioInstallationPath()
        {
            string installationPath = null;
            string version = _dte.Version;
            if (Environment.Is64BitOperatingSystem)
            {
                installationPath = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\" + version + @"\",
                    "InstallDir",
                    null);
            }
            else
            {
                installationPath = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\" + version + @"\",
                    "InstallDir",
                    null);
            }
            return installationPath;
        }

        private string ExecuteVSTestConsole(string command)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = false;
            startInfo.FileName = "\"" + _pathToVsTestConsoleExe + "\"";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "/UseVsixExtensions:true " + command;

            try
            {
                using (var exeProcess = Process.Start(startInfo))
                {
                    if (exeProcess != null) exeProcess.WaitForExit();

                    using (StreamReader reader = exeProcess.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        return result;
                    }

                }
            }
            catch (Exception ex)
            {
            }

            return String.Empty;
        }

        private List<string> ParseTestsFromConsoleOutput(string output)
        {
            var foundString = @"The following Tests are available";
            var tests = new List<string>();

            if(output.Contains(foundString))
            {

            }
            return tests;
        }

    }
}
