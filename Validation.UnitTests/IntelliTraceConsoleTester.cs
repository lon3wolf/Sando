using System;
using NUnit.Framework;
using Sando.Validation;

namespace Validation.UnitTests
{
    [TestFixture]
    //[Ignore]
    public class IntelliTraceConsoleTester
    {
        private IntelliTraceConsoleRunner _traceRunner;

        [TestFixtureSetUp]
		public void FixtureSetUp() 
        {            
            _traceRunner = new IntelliTraceConsoleRunner("IntelliTrace.exe", "VsTestConsole.exe");
		}

        [Test]
        public void TestExecuteIntelliTrace()
        {
            _traceRunner.SelectResultsUsingIntelliTrace("aaa");
        }

    }
}
