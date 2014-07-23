using System;
using Lucene.Net.Documents;
using Sando.ExtensionContracts.ProgramElementContracts;
using System.Collections.Generic;

namespace Sando.Indexer.Documents
{
	public class TextFileDocument : SandoDocument
	{
		public TextFileDocument(TextFileElement classElement)
			: base(classElement)
		{
		}

		public TextFileDocument(Document document)
			: base(document)
		{
		}

        public override List<Field> GetFieldsForLucene()
		{
            var fields = new List<Field>();
			var textLineElement = (TextFileElement) programElement;
            AddBodyField(fields, new Field(SandoField.Body.ToString(), textLineElement.Body, Field.Store.YES, Field.Index.ANALYZED));
            return fields;
		}

        public override object[] GetParametersForConstructor(string name, ProgramElementType programElementType, string fullFilePath, int definitionLineNumber, int definitionColumnNumber, string snippet, Document document)
		{
            string body = document.GetField(SandoField.Body.ToString()).StringValue();
            return new object[] { name, definitionLineNumber, definitionColumnNumber, fullFilePath, snippet, body };
		}
	}
}
