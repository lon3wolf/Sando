using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sando.UI.Monitoring
{
    class ConcurrentIndexingMonitor
    {
        private static Object lockObject = new Object();

        public static void ReleaseLock(string sourceFilePath)
        {
            lock (lockObject)
            {
                indexingFiles.Remove(sourceFilePath);
            }
        }

        static HashSet<string> indexingFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        public static bool TryToLock(string sourceFilePath)
        {
            lock (lockObject)
            {
                if (indexingFiles.Contains(sourceFilePath))
                    return true;
                else
                {
                    indexingFiles.Add(sourceFilePath);
                    return false;
                }
            }
        }

    }
}
