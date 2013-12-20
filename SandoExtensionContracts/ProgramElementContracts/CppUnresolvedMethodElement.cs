using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Xml.Linq;

namespace Sando.ExtensionContracts.ProgramElementContracts
{
	public class CppUnresolvedMethodElement : MethodElement
	{
        public CppUnresolvedMethodElement(string name, int definitionLineNumber, int definitionColumnNumber, string fullFilePath, string snippet, string arguments, 
			string returnType, string body, string className, bool isConstructor, string [] headerFiles)
            : base(name, definitionLineNumber, definitionColumnNumber, fullFilePath, snippet, AccessLevel.Protected, arguments, returnType, body, 
					Guid.NewGuid(), className, String.Empty, isConstructor)
		{
            Contract.Requires(className != null, "CppUnresolvedMethodElement:Constructor - class name cannot be null!");
            //Contract.Requires(headerFiles.Length > 0, "CppSplitMethodElement:Constructor - there have to be some header files defined here");
            Contract.Requires(headerFiles != null, "CppUnresolvedMethodElement:Constructor - headerFiles cannot be null!");

			IncludeFileNames = headerFiles;
			IsResolved = false;
		}

		public bool TryResolve(CppUnresolvedMethodElement unresolvedMethod, List<ProgramElement> headerElements, out MethodElement outMethodElement) 
		{
			AccessLevel accessLevel; 
			Guid classId;

			outMethodElement = null;
			if(ResolveClassId(ClassName, headerElements, out classId) == false) return false;
			if(ResolveAccessType(Name, headerElements, out accessLevel) == false) return false;

			IsResolved = true;
		    outMethodElement =
                Activator.CreateInstance(unresolvedMethod.GetResolvedType(), Name, DefinitionLineNumber, DefinitionColumnNumber, FullFilePath, RawSource, accessLevel,
		                                 Arguments, ReturnType, Body,
		                                 classId, ClassName, String.Empty, IsConstructor) as MethodElement;
		    SetCustomFields(unresolvedMethod, outMethodElement);
			return true;
		}

		//TODO: Remove this method
		public MethodElement Copy()
		{
            var outMethodElement = Activator.CreateInstance(GetResolvedType(), Name, DefinitionLineNumber, DefinitionColumnNumber, FullFilePath, RawSource, AccessLevel.Protected,
                                         Arguments, ReturnType, Body,
                                         Guid.NewGuid(), ClassName, String.Empty, IsConstructor) as MethodElement;
            SetCustomFields(this, outMethodElement);
		    return outMethodElement;
		}

	    private void SetCustomFields(CppUnresolvedMethodElement oldElement, MethodElement newElement)
	    {
	        foreach (var property in (oldElement as ProgramElement).GetCustomProperties())
	        {
	            if (!property.Name.Equals(ProgramElement.CustomTypeTag))
	            {
	                var newProperty = newElement.GetType().GetProperty(property.Name);
                    var oldProperty= oldElement.GetType().GetProperty(property.Name);
	                var oldGet = oldProperty.GetGetMethod(false);
	                var newSet = newProperty.GetSetMethod();
	                object[] parameters = {oldGet.Invoke(oldElement, null)};
	                newSet.Invoke(newElement, parameters);
	            }
	        }	    
	    }

	    protected virtual Type GetResolvedType()
	    {
	        return typeof (MethodElement);
	    }

	    private bool ResolveClassId(string className, List<ProgramElement> includeElements, out Guid outGuid)
		{
            var id = from element in includeElements
                     where (element is ClassElement || element is StructElement) && element.Name == className
                     select element.Id;
            if (id.Any())
            {
                outGuid = id.First();
                return true;
            }
            else
            {
                outGuid = Guid.Empty;
                return false;
            }
		}

		private bool ResolveAccessType(string funcName, List<ProgramElement> includeElements, out AccessLevel outAccessLevel)
		{
            var level = from element in includeElements
                        where element is MethodPrototypeElement && element.Name == funcName
                        select ((MethodPrototypeElement)element).AccessLevel;
            if (level.Any())
            {
                outAccessLevel = level.First();
                return true;
            }
            else
            {
                outAccessLevel = AccessLevel.Protected;
                return false;
            }
		}
		
		public virtual string[] IncludeFileNames { get; private set; }
		public virtual bool IsResolved { get; set; }
	}
}
