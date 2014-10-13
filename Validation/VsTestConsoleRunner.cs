using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE80;
using Sando.DependencyInjection;
using System.IO;

namespace Validation
{
    public class VsTestConsoleRunner
    {
        private string _pathToVsTestConsoleExe;
        //@"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";

        public VsTestConsoleRunner()
        {
            //determine where the console runner is located and whether it exists
            _pathToVsTestConsoleExe = "";            
        }

        /*
         * returns a pair of test name and the full path of the library containing the test
         */
        public List<Tuple<String, String>> DiscoverTests()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            var libraryList = FindAllLibrariesInSolution(dte);
            foreach (var library in libraryList)
            {

                //C:\Users\kosta>"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\
                //CommonExtensions\Microsoft\TestWindow\vstest.console.exe" /ListTests:"C:\Users\k
                //osta\Documents\sando\bin\Debug\Parser.UnitTests.dll" /UseVsixExtensions:true


            }

            return null;
        }

        /*
         * returns the full path of all libraries produced by this solution
         */ 
        public List<String> FindAllLibrariesInSolution(DTE2 dte)
        {
            var libraryList = new List<String>();
            var openSolution = dte.Solution;           
            for (int i = 1; i < openSolution.Projects.Count; i++)
            {
                var project = openSolution.Projects.Item(i);
                if (!String.IsNullOrWhiteSpace(project.FullName))
                {
                    libraryList.Add(GetAssemblyPath(project));
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

    }
}
