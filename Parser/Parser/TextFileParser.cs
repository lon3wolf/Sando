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
                    string line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        fileText.Append(line + Environment.NewLine);
                        numberOfTermsRead += line.Split(termSeparators, StringSplitOptions.RemoveEmptyEntries).Length;                       

                        if (numberOfTermsRead >= MaxNumberOfTermsInFile)
                        {
                            break;
                        }
                    }

                    if (fileText.Length!=0)
                    {
                        var element = new TextFileElement(filename, fileText.ToString(), fileText.ToString());
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
