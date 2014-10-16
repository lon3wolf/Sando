using EnvDTE80;
using Sando.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validation
{
    public static class TestValidator
    {
        private static List<Tuple<string, string>> _testLibraryTupleList;
        private static VsTestConsoleRunner _testRunner;

        public static void Initialize()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            _testRunner = new VsTestConsoleRunner(dte);

            //TODO: do this in a background thread or something
            _testLibraryTupleList = _testRunner.DiscoverTests();
        }



    }
}
