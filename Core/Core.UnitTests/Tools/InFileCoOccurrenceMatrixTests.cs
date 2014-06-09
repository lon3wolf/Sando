using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sando.Core.Tools;

namespace Sando.Core.UnitTests.Tools
{
    [TestFixture]
    class InFileCoOccurrenceMatrixTests
    {
        public IWordCoOccurrenceMatrix matrix;

        public InFileCoOccurrenceMatrixTests()
        {
            this.matrix = new SparseMatrixForWordPairs();
        }


        //[Test]
        public void TestPerformanceOfMatrix()
        {
            Stopwatch stopwatch = new Stopwatch();            
            matrix.Initialize(@"TestFiles\SandoMatrix");
            stopwatch.Start();
            matrix.Dispose();
            stopwatch.Stop();
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 4000 , stopwatch.ElapsedMilliseconds+"");
        }
        
    }
}
