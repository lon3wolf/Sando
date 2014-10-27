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
using System.Threading;

namespace Sando.Validation
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

            _pathToVsTestConsoleExe = Path.Combine(ConsoleUtils.GetVisualStudioInstallationPath(dte), 
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
                var output = ExecuteVsTestConsole(command);
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

        private string ExecuteVsTestConsole(string command)
        {
            var commandPlus = "/UseVsixExtensions:true " + command;
            return ConsoleUtils.ExecuteCommandInConsole(_pathToVsTestConsoleExe, commandPlus);
        }

        private List<string> ParseTestsFromConsoleOutput(string output)
        {
            var foundString = @"The following Tests are available:";
            var tests = new List<string>();

            var foundStringIndex = output.IndexOf(foundString);
            if (foundStringIndex != -1)
            {
                var start = foundStringIndex + foundString.Length;
                var lines = output.Substring(start).Split(Environment.NewLine.ToCharArray(), 
                                                            StringSplitOptions.RemoveEmptyEntries);
                foreach(var line in lines)
                {
                    var words = line.Split(new Char[] { ' ' }, 
                                            StringSplitOptions.RemoveEmptyEntries);
                    if(words.Length == 1 && words.ElementAt(0).Any(Char.IsLetter))
                    {
                        tests.Add(words.ElementAt(0));
                    }
                }
            }

            
            return tests;
        }

    }
}
