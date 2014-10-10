using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using NUnit.Framework;
using UnitTestHelpers;

namespace Validation.UnitTests
{
    [TestFixture]
    //[Ignore]
    public class VsTestConsoleTester
    {
        private VsTestConsoleRunner _testRunner;
        private DTE2 dte;

        [TestFixtureSetUp]
		public void FixtureSetUp() 
        {            
            Type type = System.Type.GetTypeFromProgID("VisualStudio.DTE.12.0");
            object inst = System.Activator.CreateInstance(type, true);

            //takes a while for VS to start up
            System.Threading.Thread.Sleep(5000);

            dte = (DTE2)inst;
            var solutionPath = Path.Combine(TestUtils.SolutionDirectory, "Sando.sln");
            dte.Solution.Open(solutionPath);

            //takes a while to open Sando solution
            System.Threading.Thread.Sleep(10000);

            _testRunner = new VsTestConsoleRunner();
		}

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            if (dte != null)
            {
                dte.Quit();
            }    
        }

        [Test]
        public void TestDiscoverTests()
        {
            //_testRunner.DiscoverTests();

        }

        [Test]
        public void TestFindAllLibrariesInSolution()
        {
            _testRunner.FindAllLibrariesInSolution(dte);
        }

    }
}
