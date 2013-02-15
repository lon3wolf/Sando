﻿using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Reflection;
using System.IO;
using Sando.Core.Tools;

namespace Sando.Core.Tools
{
    public class PathManager
    {
        private static PathManager _instance;       
        private string _pathToExtensionRoot;

        public static void Create(string path)
        {
            new PathManager(path);
        }

        private PathManager(string pathToExtensionRoot)
        {
            this._pathToExtensionRoot = pathToExtensionRoot;
            _instance = this;
        }

        public static PathManager Instance
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_instance==null)                    
                {
                    throw new NotImplementedException();
                }
                return _instance;
            }
        }


        public string GetExtensionRoot()
        {
            return _pathToExtensionRoot;
        }

        public string GetIndexPath(ABB.SrcML.VisualStudio.SolutionMonitor.SolutionKey solutionKey)
        {
            var solutionName = Path.GetFileNameWithoutExtension(solutionKey.GetSolutionPath()) + solutionKey.GetSolutionPath().GetHashCode();          
            return LuceneDirectoryHelper.GetOrCreateLuceneDirectoryForSolution(solutionName, PathManager.Instance.GetExtensionRoot());                
        }
    }
}
