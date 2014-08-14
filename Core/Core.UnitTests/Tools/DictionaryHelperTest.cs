using NUnit.Framework;
using Sando.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sando.Core.UnitTests.Tools
{
    [TestFixture]
    public class DictionaryHelperTest
    {
        [Test]
        public void TestTrim()
        {
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("Bob").Count()==1);
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("Bob Sue").Count() == 2);
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("Bob sue Dog Cat KillBill").Count() == 6);
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("Bob?").First().Equals("bob"));
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("?Bob?").First().Equals("bob"));
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("如果所有装载的模 tom").Contains("tom"));
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("如果所有装载的模 tom").Count()==1);
        }
    } 
    
}
