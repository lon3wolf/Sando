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
        //this should be twice Lucene's default number of terms
        private const int MaxNumberOfTermsInFile = 20000;

        public List<ProgramElement> Parse(string filename)
        {
            var list = new List<ProgramElement>();
            var termSeparators = new char[] {' ', '\n', '\t', '\r'};
            try
            {
                int numberOfTermsRead = 0;
                using (var sr = new StreamReader(filename))
                {
                    StringBuilder fileText = new StringBuilder();
                    fileText.Append(Environment.NewLine); //in order to start line numbers at 1 instead of 0
                    string line = String.Empty; 
                    while ((line = sr.ReadLine()) != null)
                    {
                        fileText.Append(line + Environment.NewLine);
                        numberOfTermsRead += line.Split(termSeparators, StringSplitOptions.RemoveEmptyEntries).Length;                       

                        if (numberOfTermsRead >= MaxNumberOfTermsInFile)
                        {
                            break;
                        }
                    }

                    var fileString = fileText.ToString();
                    if (!String.IsNullOrWhiteSpace(fileString))
                    {
                        var element = new TextFileElement(filename, fileString, fileString);
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
