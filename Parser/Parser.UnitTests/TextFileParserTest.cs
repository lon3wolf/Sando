using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sando.Parser;

namespace Sando.Parser.UnitTests
{
	[TestFixture]
	class TextFileParserTest
	{

		[Test]
		public void ParseXAMLFile()
		{
			var parser = new TextFileParser();
			var elements = parser.Parse("..\\..\\Parser\\Parser.UnitTests\\TestFiles\\SearchViewControl.xaml.txt");
			Assert.IsNotNull(elements);
			Assert.AreEqual(elements.Count, 1);
            Assert.IsTrue(elements.ElementAt(0).RawSource.Contains("</UserControl>"));
		}

        [Test]
        public void ParseTxtFile()
        {
            var parser = new TextFileParser();
            var elements = parser.Parse("..\\..\\Parser\\Parser.UnitTests\\TestFiles\\LongFile.txt");
            Assert.IsNotNull(elements);
            Assert.AreEqual(elements.Count, 1);
            Assert.IsTrue(elements.ElementAt(0).RawSource.Contains("the")); //first word
            Assert.IsTrue(elements.ElementAt(0).RawSource.Contains("16761152")); //second number

            //this file is too big, so a word near the end of it should not have been parsed
            Assert.IsFalse(elements.ElementAt(0).RawSource.Contains("aaffggjjkkllmmnnooppqqrrssttuuvvwwxxyyzz")); 
        }

        [Test]
        public void ParseCodeMaidTxtFile()
        {
            var parser = new TextFileParser();
            var elements = parser.Parse("..\\..\\IntegrationTests\\TestFiles\\TextFilesTestFiles\\CodeMaidTest.txt");
            Assert.IsNotNull(elements);
            Assert.AreEqual(elements.Count, 1);
            Assert.IsTrue(elements.ElementAt(0).RawSource.Contains("IconUnlock")); //first word
        }

		[Test]
		public void ParseXAMLFile2()
		{
			var parser = new XMLFileParser();
			var elements = parser.Parse("..\\..\\Parser\\Parser.UnitTests\\TestFiles\\SearchViewControl.xaml.txt");
			Assert.IsNotNull(elements);
			Assert.AreEqual(84, elements.Count);
			foreach(var element in elements)
			{
				if(element.DefinitionLineNumber == 204)
				{
					Assert.AreEqual("Auto 0 -5,0,0,0 2 0", element.Name);
				}

			}
		}


	}
}
