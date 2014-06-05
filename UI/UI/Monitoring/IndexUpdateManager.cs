using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;
using Sando.Core.Extensions;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.Indexer;
using Sando.Indexer.Documents;
using Sando.Indexer.IndexState;
using Sando.Core.Logging.Events;
using Sando.Parser;


namespace Sando.UI.Monitoring
{
    public class IndexUpdateManager
	{
        private readonly DocumentIndexer _currentIndexer;

        public event IndexUpdated indexUpdated;
        public delegate void IndexUpdated(ReadOnlyCollection<ProgramElement> updatedElements);

        public IndexUpdateManager()
        {
            _currentIndexer = ServiceLocator.Resolve<DocumentIndexer>();
        }

        public void Update(string filePath, XElement xElement)
        {
            var fileInfo = new FileInfo(filePath);            
            try
            {
                var codeParser = ExtensionPointsRepository.Instance.GetParserImplementation(fileInfo.Extension);
                var textFileParser = new TextFileParser();
                var parsed = new List<ProgramElement>();
                if (codeParser != null)
                {
                    parsed.AddRange(codeParser.Parse(filePath, xElement));

                    //double parse code with the text file parser until we can parse everything in the file (e.g. C# attributes)
                    parsed.AddRange(textFileParser.Parse(filePath, xElement));
                }
                else if (!IsBinaryFile(filePath))
                {
                    parsed.AddRange(textFileParser.Parse(filePath, xElement));
                }

                var unresolvedElements = parsed.FindAll(pe => pe is CppUnresolvedMethodElement);
                if (unresolvedElements.Count > 0)
                {
                    //first generate program elements for all the included headers
                    var headerElements = CppHeaderElementResolver.GenerateCppHeaderElements(filePath, unresolvedElements);

                    //then try to resolve
                    foreach (CppUnresolvedMethodElement unresolvedElement in unresolvedElements)
                    {
                        var document = CppHeaderElementResolver.GetDocumentForUnresolvedCppMethod(unresolvedElement, headerElements);
                        if (document != null)
                        {
                            //writeLog( "- DI.AddDocument()");
                            _currentIndexer.AddDocument(document);
                        }
                    }
                }

                foreach (var programElement in parsed)
                {
                    if (!(programElement is CppUnresolvedMethodElement))
                    {
                        var document = DocumentFactory.Create(programElement);
                        if (document != null)
                        {
                            //writeLog( "- DI.AddDocument()");
                            _currentIndexer.AddDocument(document);
                        }
                    }
                }
                if(indexUpdated!=null)
                    indexUpdated(parsed.AsReadOnly());
            }
            catch (Exception e)
            {
                LogEvents.UIIndexUpdateError(this, e);
            }
        }


        /*
         * from: http://stackoverflow.com/questions/910873/how-can-i-determine-if-a-file-is-binary-or-text-in-c
         */
        private bool IsBinaryFile(string filePath, int sampleSize = 10240)
        {
            var buffer = new char[sampleSize];
            string sampleContent;

            using (var sr = new StreamReader(filePath))
            {
                int length = sr.Read(buffer, 0, sampleSize);
                sampleContent = new string(buffer, 0, length);
            }

            //Look for 4 consecutive binary zeroes
            if (sampleContent.Contains("\0\0\0\0"))
                return true;

            return false;
        }

	}

   
}
