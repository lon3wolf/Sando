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
            const string intelliTraceExe = @"../../LIBS/IntelliTrace/IntelliTrace.exe";
            const string testConsoleExe = @"../../LIBS/IntelliTrace/vstest.console.exe";
            _traceRunner = new IntelliTraceConsoleRunner(intelliTraceExe, testConsoleExe);
		}

        [Test]
        public void TestExecuteIntelliTrace()
        {
            _traceRunner.SelectResultsUsingIntelliTrace("aaa");
        }

    }
}
