using System.Threading;
using EnvDTE80;
using Sando.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sando.Validation
{
    public static class TestValidator
    {
        private static List<Tuple<string, string>> _testLibraryTupleList;
        private static VsTestConsoleRunner _testRunner;

        public static void Initialize()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            _testRunner = new VsTestConsoleRunner(dte);

            var testDiscoveryTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                _testLibraryTupleList = _testRunner.DiscoverTests();
            }, new CancellationToken(false));
            
        }



    }
}
