using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ABB.SrcML.VisualStudio;
using ABB.SrcML.Utilities;
using Sando.ExtensionContracts.ServiceContracts;
using Sando.UI;
using ABB.SrcML;

namespace Sando.IntegrationTests
{
    [TestClass]
    public class TestHelpers
    {
        internal static Scaffold<ISandoGlobalService> TestScaffold;
        internal static Scaffold<ISrcMLGlobalService> SrcMLTestScaffold;
        private static UIPackage TestPackage;

        [AssemblyInitialize]
        public static void AssemblySetup(TestContext testContext)
        {
            SrcMLTestScaffold = Scaffold<ISrcMLGlobalService>.Setup(new SrcMLServicePackage(), typeof(SSrcMLGlobalService));
            TestPackage = new UIPackage();
            TestScaffold = Scaffold<ISandoGlobalService>.Setup(TestPackage, typeof(SSandoGlobalService));            
        }

        public static void StartupCompleted()
        {
            TestPackage.StartupCompleted();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            TestScaffold.Cleanup();
            SrcMLTestScaffold.Cleanup();
        }

        internal static string GetSolutionDirectory()
        {
            var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            while (currentDirectory != null && !Directory.Exists(Path.Combine(currentDirectory.FullName, "LIBS")))
            {
                currentDirectory = currentDirectory.Parent;
            }
            return currentDirectory.FullName;
        }
        internal static void CopyDirectory(string sourcePath, string destinationPath)
        {
            foreach (var fileTemplate in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var fileName = fileTemplate.Replace(sourcePath, destinationPath);
                var directoryName = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                File.Copy(fileTemplate, fileName);
            }
        }

 

        internal static bool WaitForServiceToFinish(ISrcMLGlobalService service, int millisecondsTimeout)
        {
            System.Threading.Thread.Sleep(2000);
            if (service.IsUpdating)
            {
                ManualResetEvent mre = new ManualResetEvent(false);
                EventHandler action = (o, e) => { mre.Set(); };
                service.UpdateArchivesCompleted += action;
                mre.WaitOne(millisecondsTimeout);
                service.UpdateArchivesCompleted -= action;
            }
            System.Threading.Thread.Sleep(10000);
            return !service.IsUpdating;
        }

        internal static IEnumerable<Project> GetProjects(Solution solution)
        {
            var projects = solution.Projects;
            var enumerator = projects.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Project currentProject = enumerator.Current as Project;
                if (null != currentProject)
                {
                    yield return currentProject;
                }
            }
        }
    }
}
