using Configuration.OptionsPages;
using Sando.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnitTestHelpers;

namespace Sando.Indexer.UnitTests
{
    public class IndexerTestUtils
    {

         private class OptionsProvider : ISandoOptionsProvider
        {

            public SandoOptions GetSandoOptions()
            {
                return new SandoOptions("", 40, false, new List<string>());
            }
        }

         public static void IntializeFrameworkForUnitTests()
         {
             ServiceLocator.RegisterInstance<ISandoOptionsProvider>(new OptionsProvider());
             TestUtils.InitializeFrameworkForUnitTests();
         }

    }
}
