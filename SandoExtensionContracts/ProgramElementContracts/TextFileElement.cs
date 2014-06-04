using System;
using System.Diagnostics.Contracts;
using System.IO;


namespace Sando.ExtensionContracts.ProgramElementContracts
{
    public class TextFileElement : ProgramElement
    {
        public TextFileElement(string fullFilePath, string snippet, string body)
			: base(Path.GetFileName(fullFilePath), 0, 0, fullFilePath, snippet)
        {
			Contract.Requires(!String.IsNullOrWhiteSpace(body), "TextFileElement:Constructor - body cannot be null or an empty string!");

			Body = body;
        }

        public TextFileElement(string name, int definitionLineNumber, int definitionColumnNumber, string fullFilePath, string snippet, string body) 
			: this(fullFilePath, snippet, body)
		{
		}


		public virtual string Body { get; private set; }

        public override ProgramElementType ProgramElementType
        {
            get { return ProgramElementType.TextFile; }
        }
    }
}
