using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sando.Core.Tools;
using System.Diagnostics;

namespace Sando.Core.UnitTests.Tools
{
    [TestFixture]
    class BigMatrixTest
    {
        public IWordCoOccurrenceMatrix matrix;

        public BigMatrixTest()
        {
            this.matrix = new SparseMatrixForWordPairs();
        }

        [SetUp]
        public void ReadData()
        {
            matrix.Initialize(@"TestFiles\");
        }


        private void AssertWordPairExist(string word1, string word2)
        {
            Assert.IsTrue(matrix.GetCoOccurrenceCount(word1, word2) > 0);
        }

        private void AssertWordPairNonExist(string word1, string word2)
        {
            Assert.IsTrue(matrix.GetCoOccurrenceCount(word1, word2) == 0);
        }

        [Test]
        public void DifferentWordPairsThatExist()
        {
            AssertWordPairExist("dog", "enkiixinzfompqv");
            AssertWordPairExist("cat", "kdwehypeuoiadtg");
            AssertWordPairExist("czwagzxqgxittuy", "bird");
        }

        [Test]
        public void SameNonLocalDictionaryWordNeverExist()
        {
            AssertWordPairNonExist("fast", "fast");
            AssertWordPairNonExist("jamming", "jamming");
            AssertWordPairNonExist("red", "red");
        }

        [Test]
        public void SameNonLocalDictionaryWordsExist()
        {
            AssertWordPairExist("dog", "dog");
            AssertWordPairExist("bird", "bird");
            AssertWordPairExist("cat", "cat");
        }



        [Test]
        public void DifferentWordPairsThatDoesNotExist()
        {
            AssertWordPairNonExist("sando", "apple");
            AssertWordPairNonExist("confidence", "lackof");
            AssertWordPairNonExist("test", "configuration");
            AssertWordPairNonExist("yo", "nomad");   
        }

        [Test]
        public void AssertCanGetWordsAndCount()
        {
            var dic = matrix.GetAllWordsAndCount();
            Assert.IsNotNull(dic);
            Assert.IsTrue(dic.Any());
        }
    }
}
