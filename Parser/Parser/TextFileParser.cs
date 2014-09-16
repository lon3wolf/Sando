using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Sando.Core.Logging.Events;
using Sando.ExtensionContracts.ParserContracts;
using Sando.ExtensionContracts.ProgramElementContracts;
using System.Text;

namespace Sando.Parser
{
    public class TextFileParser : IParser
    {
        //Lucene's max term length is 16383, we choose something a bit lower to chunk on.
        private const int MaxLengthOfTermInLucene = 15500;

        private const int MaxLengthOfFileToIndex = 10000;

        public List<ProgramElement> Parse(string filename)
        {
            var list = new List<ProgramElement>();
            var termSeparators = new char[] {' ', '\n', '\t', '\r'};
            try
            {
                int charactersInCurrentChunk = 0;
                int currentLineNumber = 1;
                int startingLineNumber = 1;
                using (var sr = new StreamReader(filename))
                {
                    StringBuilder fileText = new StringBuilder();
                    //fileText.Append(Environment.NewLine); //in order to start line numbers at 1 instead of 0
                    string line = String.Empty; 
                    while ((line = sr.ReadLine()) != null)
                    {
                        fileText.Append(line + Environment.NewLine);
                        charactersInCurrentChunk += line.Length;
                        currentLineNumber++;

                        if (charactersInCurrentChunk >= MaxLengthOfTermInLucene)
                        {
                            if (!String.IsNullOrWhiteSpace(fileText.ToString()))
                            {
                                var element = new TextFileElement(startingLineNumber, 0, filename, fileText.ToString(), fileText.ToString());
                                list.Add(element);
                                startingLineNumber = currentLineNumber;
                                charactersInCurrentChunk = 0;
                                fileText = new StringBuilder();
                            }
                        }
                        if (currentLineNumber > MaxLengthOfFileToIndex)
                            break;
                    }

                    var fileString = fileText.ToString();
                    if (!String.IsNullOrWhiteSpace(fileString))
                    {
                        var element = new TextFileElement(startingLineNumber, 0, filename, fileText.ToString(), fileText.ToString());
                        list.Add(element);
                    }
                }
            }
            catch (Exception e)
            {
                LogEvents.ParserGenericFileError(this, filename);
            }

            return list;
        }

        public List<ProgramElement> Parse(string fileName, System.Xml.Linq.XElement sourceElements)
        {
            return Parse(fileName);
        }
    }
}
