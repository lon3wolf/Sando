﻿using System.IO;
using Sando.ExtensionContracts.ProgramElementContracts;

namespace Sando.ExtensionContracts.ResultsReordererContracts
{
    /// <summary>
    /// Class defined to create return result from Lucene indexer
    /// </summary>
   public class CodeSearchResult
   {
       #region Public Properties
       /// <summary>
       /// Gets or sets the score.
       /// </summary>
       /// <value>
       /// The search score.
       /// </value>
       public double Score { get; private set; }

       /// <summary>
       /// Gets or sets the element.
       /// </summary>
       /// <value>
       /// Sando Program Element.
       /// </value>
       public ProgramElement Element
       {
           get;
           private set;
       }

        public string ParentOrFile
        {
            get
            {
                if(string.IsNullOrEmpty(Parent))
                {
                    return Path.GetFileName(this.FileName);
                }else
                {
                    string fileName = Path.GetFileName(this.FileName);
                    if (fileName.StartsWith(Parent))
                    {
                        return fileName;
                    }
                    return Parent +" ("+fileName+")";
                }
            }
        }

       public ProgramElementType ProgramElementType
       {
           get { return Element.ProgramElementType; }
       }

        public string Type
        {
            get
            {          
               return Element.GetName();                
            }
        }

       public string Snippet
       {
           get
           {
               var snip = Element.Snippet;
               return FixSnip(snip);               
           }           
       }

       public static string FixSnip(string snip)
       {
           int toRemove = 0;
           if (snip.StartsWith("\t\t"))
           {
               toRemove = 2;
           }
           else if (snip.StartsWith("\t"))
           {
               toRemove = 1;
           }
           else if (snip.StartsWith(" "))
           {
               toRemove = snip.Length - snip.TrimStart(' ').Length;
           }
           if(toRemove>0)
           {
               var newSnip = "";
               var split = snip.Split('\n');
               foreach (var line in split)
               {
                   if(line.Length>toRemove+1)
                   newSnip += line.Remove(0,toRemove)+"\n";
               }
               return newSnip;
           }
           //if (snip.StartsWith("\r\n"))
           //{
           //    snip = snip.Substring(2);
           //}
           return snip;
       }
    	public string FileName
    	{
    		get
    		{
    			var fileName = Path.GetFileName(Element.FullFilePath);
    			return fileName;
    		}    		
    	}
	   /*
        private static string Shorten(string fileName)
        {
            if (fileName.Length > 17)
            {
                fileName = fileName.Substring(0, 17) + "..";
            }
            return fileName;
        }*/

        public string Parent
    	{
    		get
    		{
    			var method = Element as MethodElement;
				if(method !=null)
				{
					return method.ClassName;
				}else
				{
					return "";
				}

    		}
    	}
       

        public string Name
        {
            get
            {
				return Element.Name;
            }
        }
	   

       #endregion
       #region Constructor
       /// <summary>
        /// Initializes a new instance of the <see cref="CodeSearchResult"/> class.
        /// </summary>
        /// <param name="programElement">program element.</param>
        /// <param name="score">search score.</param>
	   public CodeSearchResult(ProgramElement programElement, double score)
	   {
		   this.Element = programElement;
		   this.Score = score;
	   }
       #endregion
       
    }
}
