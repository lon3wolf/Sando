using NUnit.Framework;
using Sando.Core.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Sando.Core.UnitTests.Tools
{
    [TestFixture]
    public class LevenshteinTest
    {
        [Test]
        public void DistanceThisThat()
        {
            var compare = new Levenshtein();
            var far = compare.LD("this", "that");
            var close = compare.LD("this", "thiz");
            Assert.True(far > close);
        }

        [Test]
        public void DistanceThisAuthorize()
        {
            var compare = new Levenshtein();
            var far = compare.LD("thiz", "authorize");
            var close = compare.LD("thiz", "this");
            Assert.True(far > close);
        }
    }
}
