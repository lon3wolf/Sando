using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sando.Core.Tools
{
    public interface ILogFileAnalyzer
    {
        void StartAnalyze(ILogFile file);
        void FinishAnalysis();
    }

    public interface ILogFile
    {
        String Name { get; }
        String Content { get; }
    }

    public class SandoAnalysisManager
    {
        private readonly SandoLogAnalyzer analyzer;

        public SandoAnalysisManager(string directory)
        {
            this.analyzer = new SandoLogAnalyzer(directory);
        }

        public void Analyze()
        {
            this.analyzer.AddAnalyzer(new NoSearchResultsAnalyzer());
            this.analyzer.AddAnalyzer(new NumberOfUsersAnalyzer());
            this.analyzer.AddAnalyzer(new NumberOfRegularUsers());            
            this.analyzer.StartAnalysis();
        }
        
        private class NoSearchResultsAnalyzer : ILogFileAnalyzer
        {
            private int allQueryCount = 0;
            private int emptyQueryCount = 0;

            public void StartAnalyze(ILogFile file)
            {
                var lines = file.Content.Split('\n');
                lines = lines.Where(l => l.Contains("Sando returned results")).ToArray();
                allQueryCount += lines.Count();
                emptyQueryCount += lines.Count(l => l.Contains("NumberOfResults=0"));
            }

            public void FinishAnalysis()
            {
                var sb = new StringBuilder();
                sb.AppendLine("All query count: " + allQueryCount);
                sb.AppendLine("No result query count: " + emptyQueryCount);
                string result = sb.ToString();
            }
        }

        private class NumberOfUsersAnalyzer : ILogFileAnalyzer
        {
            private readonly Dictionary<string, int> IDs = new Dictionary<string, int>();
            private int count5;
            private int count7;
            private int count15;
            private int count10;
            private int count20; 

            public void StartAnalyze(ILogFile file)
            {
                var id = file.Name.Split('_')[2];
                if (IDs.ContainsKey(id))
                {
                    IDs[id]++;
                }
                else
                {
                    IDs.Add(id, 1);
                }
            }

            public void FinishAnalysis()
            {
                var allKeys = from key in IDs.Keys where IDs[key] > 5 select key;
                count5 = allKeys.Count();
                allKeys = from key in IDs.Keys where IDs[key] > 7 select key;
                count7 = allKeys.Count();
                allKeys = from key in IDs.Keys where IDs[key] > 10 select key;
                count10 = allKeys.Count();
                allKeys = from key in IDs.Keys where IDs[key] > 15 select key;
                count15 = allKeys.Count();
                allKeys = from key in IDs.Keys where IDs[key] > 20 select key;
                count20 = allKeys.Count();
            }
        }


        private class NumberOfRegularUsers : ILogFileAnalyzer
        {
            private readonly Dictionary<string, List<DateTime>> IDs = new Dictionary<string, List<DateTime>>();
            private int count;

            //SandoData_v1.1.2_1246082932_-194626707_2013-11-20-13.13.log
            

            public void StartAnalyze(ILogFile file)
            {
                var id = file.Name.Split('_')[2];
                try
                {
                    var rest = file.Name.Split('_')[4];
                    if (IDs.ContainsKey(id))
                    {
                        IDs[id].Add(DateTime.Parse(rest.Substring(0,10)));
                    }
                    else
                    {
                        var list = new List<DateTime>();
                        list.Add(DateTime.Parse(rest.Substring(0,10)));
                        IDs.Add(id, list);
                    }
                }
                catch (IndexOutOfRangeException ioe)
                {
                    //we don't want to analyze files that don't have the normal naming convention
                }
            }

            public void FinishAnalysis()
            {
                var allKeys = from key in IDs.Keys where IDs[key].Count() > 5 select key;
                count = 0;
                foreach (var key in allKeys)
                {
                    var dateList = IDs[key];
                    bool add = false;
                    foreach(var date in dateList)
                        foreach (var date1 in dateList)                        
                            if ((date1 - date).TotalDays > 10 || (date - date1).TotalDays > 10)
                            {
                                add = true;
                                break;
                            }                        
                    if (add)
                        count++;
                }
            }
        }


        public class SandoLogAnalyzer
        {
            private readonly string directory;
            private readonly List<ILogFileAnalyzer> analyzers;

            public SandoLogAnalyzer(string directory)
            {
                this.directory = directory;
                this.analyzers = new List<ILogFileAnalyzer>();
            }

            private class LogFile : ILogFile
            {
                public String Name { private set; get; }
                public String Content { private set; get; }

                public LogFile(String Name, String Content)
                {
                    this.Name = Name;
                    this.Content = Content;
                }
            }

            private LogFile[] GetLogFiles()
            {
                var files = new List<LogFile>();
                var dir = new DirectoryInfo(directory);

                foreach (FileInfo file in dir.GetFiles("*.log"))
                {
                    var path = file.Name;
                    if (IsFileNameGood(path))
                    {
                        using (var reader = file.OpenText())
                        {
                            var content = reader.ReadToEnd();
                            files.Add(new LogFile(path, content));
                        }
                    }
                }
                return files.ToArray();
            }

            private bool IsFileNameGood(String fileName)
            {
                var shepherd = "2021486822";
                var xi = "1914121570";
                var busy = "1602809067";
                var pat = "174697094";
                var vinay = "222472157";

                var list = new List<String> {shepherd, xi, busy, pat, vinay};
                return list.All(l => !fileName.Contains(l));
            }


            public void AddAnalyzer(ILogFileAnalyzer analyzer)
            {
                this.analyzers.Add(analyzer);
            }

            public void StartAnalysis()
            {
                var logFiles = GetLogFiles();
                foreach (var analyzer in analyzers)
                {
                    foreach (var log in logFiles)
                    {
                        analyzer.StartAnalyze(log);
                    }
                    analyzer.FinishAnalysis();
                }
            }
        }
    }
}