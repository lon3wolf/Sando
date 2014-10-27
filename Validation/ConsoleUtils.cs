using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;
using Process = System.Diagnostics.Process;

namespace Sando.Validation
{
    public static class ConsoleUtils
    {

        public static string GetVisualStudioInstallationPath(DTE2 dte)
        {
            string installationPath = null;
            string version = dte.Version;
            if (Environment.Is64BitOperatingSystem)
            {
                installationPath = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\" + version + @"\",
                    "InstallDir",
                    null);
            }
            else
            {
                installationPath = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\" + version + @"\",
                    "InstallDir",
                    null);
            }
            return installationPath;
        }

        public static string ExecuteCommandInConsole(string executable, string arguments)
        {
            var outputWaitHandle = new AutoResetEvent(false);
            var errorWaitHandle = new AutoResetEvent(false);

            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = false;
            startInfo.FileName = "\"" + executable + "\"";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = arguments;

            var timeout = 5000; //5 seconds
            var output = new StringBuilder();
            var error = new StringBuilder();

            try
            {
                using (var exeProcess = new Process())
                {
                    exeProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    exeProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    exeProcess.StartInfo = startInfo;
                    exeProcess.Start();
                    exeProcess.BeginOutputReadLine();
                    exeProcess.BeginErrorReadLine();

                    if (exeProcess.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        // Process completed. Check process.ExitCode here.
                    }
                    else
                    {
                        // Timed out.
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return output.ToString();
        }


    }
}
