﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Sando.Parser.UnitTests
{
    [TestFixture]
    public class CommentTest
    {
        [Test]
		public void SummarizeCommentTest()
        {
            var comment = "//TODO - should fix this if it happens too often\n"+
				"//TODO - need to investigate why this is happening during parsing";
            string commentSummary = SrcMLParsingUtils.GetCommentSummary(comment);
            Assert.IsTrue("TODO - should fix this if it happens too often".Equals(commentSummary));
        }

        [Test]
        public void SummarizeOneLineTest()
        {
            var comment = "//TODO - should fix this if it happens too often";
            string commentSummary = SrcMLParsingUtils.GetCommentSummary(comment);
            Assert.IsTrue("TODO - should fix this if it happens too often".Equals(commentSummary));
        }

                [Test]
        public void SummarizeMultiLineTest()
        {
            var comment = "/// <summary>\n" +
                "/// Used for the culture in SR\n\r" +
                "/// </devdoc>\r\n";
            string commentSummary = SrcMLParsingUtils.GetCommentSummary(comment);
            Assert.IsTrue("Used for the culture in SR".Equals(commentSummary));
        }


        [Test]
        public void SummarizeDashTest()
        {
                    var comment = "// -----------------------------------------------------------------------------\n"+
        "//  <autogeneratedinfo>\n"+
        "//      This code was generated by:\n";
                    string commentSummary = SrcMLParsingUtils.GetCommentSummary(comment);
                    Assert.IsTrue("This code was generated by:".Equals(commentSummary));
                }


        [Test]
        public void SummarizeNoSpaceTest()
        {
            var comment = "        ///<summary>\n"+
        "/// Save the plug-in's settings.\n"+
        "///</summary>\n"+
        "/// ";
            string commentSummary = SrcMLParsingUtils.GetCommentSummary(comment);
            Assert.IsTrue("Save the plug-in's settings.".Equals(commentSummary));
        }

        [Test]
        public void SummarizeSpaceTest()
        {
            var comment =
                "           /// <summary>\n\r" +
                "           /// Specifies the size, in bytes, of the structure. The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).\r\n" +
                "           /// </summary>\r\n";
            string commentSummary = SrcMLParsingUtils.GetCommentSummary(comment);
            Assert.IsTrue("Specifies the size, in bytes, of the structure. The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).".Equals(commentSummary));
        }
    }
}
