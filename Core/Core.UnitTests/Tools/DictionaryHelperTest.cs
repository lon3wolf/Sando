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
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("a8045000").Count() == 1);
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("a8045000").First().Equals("a"));
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("00000000000000000000000000000000").Count() == 0);
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("00000000ffffffff").Count() == 1);
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("00000000ffffffff").First().Equals("ffffffff"));
            Assert.IsTrue(DictionaryHelper.GetMatchedWords("011011000000").Count() == 0);
            
        }
    } 
    
}
