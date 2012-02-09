﻿using System;
using NUnit.Framework;
using Sando.Core;
using Sando.Indexer.Documents;
using Sando.Indexer.UnitTests.Helpers;

namespace Sando.Indexer.UnitTests
{
    [TestFixture]
	public class DocumentFactoryTest
	{
    	[Test]
		public void DocumentFactory_CreateReturnsClassDocumentForValidClassElement()
		{
			try
			{
				ProgramElement programElement = SampleProgramElementFactory.GetSampleClassElement();
				SandoDocument sandoDocument = DocumentFactory.Create(programElement);
				Assert.True(sandoDocument != null, "Null returned from DocumentFactory!");
				Assert.True(sandoDocument is ClassDocument, "ClassDocument must be returned for ClassElement object!");
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message + ". " + ex.StackTrace);
			}
		}

		[Test]
		public void DocumentFactory_CreateReturnsCommentDocumentForValidCommentElement()
		{
			try
			{
				ProgramElement programElement = SampleProgramElementFactory.GetSampleCommentElement();
				SandoDocument sandoDocument = DocumentFactory.Create(programElement);
				Assert.True(sandoDocument != null, "Null returned from DocumentFactory!");
				Assert.True(sandoDocument is CommentDocument, "CommentDocument must be returned for CommentElement object!");
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message + ". " + ex.StackTrace);
			}
		}

		[Test]
		public void DocumentFactory_CreateReturnsDocCommentDocumentForValidDocCommentElement()
		{
			try
			{
				ProgramElement programElement = SampleProgramElementFactory.GetSampleDocCommentElement();
				SandoDocument sandoDocument = DocumentFactory.Create(programElement);
				Assert.True(sandoDocument != null, "Null returned from DocumentFactory!");
				Assert.True(sandoDocument is DocCommentDocument, "DocCommentDocument must be returned for DocCommentElement object!");
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message + ". " + ex.StackTrace);
			}
		}

		[Test]
		public void DocumentFactory_CreateReturnsEnumDocumentForValidEnumElement()
		{
			try
			{
				ProgramElement programElement = SampleProgramElementFactory.GetSampleEnumElement();
				SandoDocument sandoDocument = DocumentFactory.Create(programElement);
				Assert.True(sandoDocument != null, "Null returned from DocumentFactory!");
				Assert.True(sandoDocument is EnumDocument, "EnumDocument must be returned for EnumElement object!");
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message + ". " + ex.StackTrace);
			}
		}

		[Test]
		public void DocumentFactory_CreateReturnsFieldDocumentForValidFieldElement()
		{
			try
			{
				ProgramElement programElement = SampleProgramElementFactory.GetSampleFieldElement();
				SandoDocument sandoDocument = DocumentFactory.Create(programElement);
				Assert.True(sandoDocument != null, "Null returned from DocumentFactory!");
				Assert.True(sandoDocument is FieldDocument, "FieldDocument must be returned for FieldElement object!");
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message + ". " + ex.StackTrace);
			}
		}

		[Test]
		public void DocumentFactory_CreateReturnsMethodDocumentForValidMethodElement()
		{
			try
			{
				ProgramElement programElement = SampleProgramElementFactory.GetSampleMethodElement();
				SandoDocument sandoDocument = DocumentFactory.Create(programElement);
				Assert.True(sandoDocument != null, "Null returned from DocumentFactory!");
				Assert.True(sandoDocument is MethodDocument, "MethodDocument must be returned for MethodElement object!");
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message + ". " + ex.StackTrace);
			}
		}

		[Test]
		public void DocumentFactory_CreateReturnsPropertyDocumentForValidPropertyElement()
		{
			try
			{
				ProgramElement programElement = SampleProgramElementFactory.GetSamplePropertyElement();
				SandoDocument sandoDocument = DocumentFactory.Create(programElement);
				Assert.True(sandoDocument != null, "Null returned from DocumentFactory!");
				Assert.True(sandoDocument is PropertyDocument, "PropertyDocument must be returned for PropertyElement object!");
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message + ". " + ex.StackTrace);
			}
		}
	}
}