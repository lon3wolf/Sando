﻿using Lucene.Net.Documents;
using Sando.Core;
using System;

namespace Sando.Indexer.Documents
{
	public abstract class SandoDocument
	{
		protected SandoDocument(ProgramElement programElement)
		{
			this.programElement = programElement;
		}

		public Document GetDocument()
		{
			if(document == null)
			{
				document = new Document();
				document.Add(new Field("Id", programElement.Id.ToString(), Field.Store.YES, Field.Index.NO));
				document.Add(new Field("Name", programElement.Name, Field.Store.YES, Field.Index.ANALYZED));
				document.Add(new Field("ProgramElementType", programElement.ProgramElementType.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));                
				document.Add(new Field("FullFilePath", StandardizeFilePath(programElement.FullFilePath), Field.Store.YES, Field.Index.NOT_ANALYZED));
				document.Add(new Field("DefinitionLineNumber", programElement.DefinitionLineNumber.ToString(), Field.Store.YES, Field.Index.NO));
				AddDocumentFields();
			}
			return document;
		}

		protected abstract void AddDocumentFields();

		protected ProgramElement programElement;
		protected Document document;

        internal static string StandardizeFilePath(string fullFilePath)
        {
            if (fullFilePath.Contains("/"))
            {
                string old = "/";
                string rep = "\\";
                var path = fullFilePath.Replace(old, rep);
                return path;
            }
            return fullFilePath;
        }

	}
}
