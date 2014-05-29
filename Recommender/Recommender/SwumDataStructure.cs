using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;

namespace Sando.Recommender
{
    public class SwumDataStructure
    {
        private Dictionary<string, SwumDataRecord> signaturesToSwum;

        public SwumDataStructure()
        {
            signaturesToSwum = new Dictionary<string, SwumDataRecord>();
        }

        public void AddRecord(string signature, SwumDataRecord record)
        {
            lock (signaturesToSwum)
            {
                signaturesToSwum[signature] = record;
            }
        }

        public SwumDataRecord GetSwumForSignature(string methodSignature)
        {
            Contract.Requires(methodSignature != null, "SwumDataRecord:GetSwumForSignature - signature cannot be null!");

            SwumDataRecord result = null;
            lock (signaturesToSwum)
            {
                if (signaturesToSwum.ContainsKey(methodSignature))
                {
                    result = signaturesToSwum[methodSignature];
                }
            }
            return result;
        }

        public void RemoveSourceFile(string sourcePath)
        {
            var fullPath = Path.GetFullPath(sourcePath);
            var recordsToRemove = new HashSet<string>();
            lock (signaturesToSwum)
            {
                foreach (var signature in signaturesToSwum.Keys)
                {
                    var sdr = signaturesToSwum[signature];
                    if (sdr.FileNames.Contains(fullPath))
                    {
                        sdr.FileNames.Remove(fullPath);
                        if (!sdr.FileNames.Any())
                        {
                            recordsToRemove.Add(signature);
                        }
                    }
                }

                //remove signatures that no longer have any file names
                //(This is separate from the above loop because you can't delete keys while you're enumerating them.)
                foreach (var signature in recordsToRemove)
                {
                    signaturesToSwum.Remove(signature);
                }
            }
        }

        public void Clear()
        {
            lock (signaturesToSwum)
            {
                signaturesToSwum.Clear();
            }
        }

        public Dictionary<string, SwumDataRecord> GetAllSwumData()
        {
            var currentSwum = new Dictionary<string, SwumDataRecord>();
            lock (signaturesToSwum)
            {
                foreach (var entry in signaturesToSwum)
                {
                    currentSwum[entry.Key] = entry.Value;
                }
            }
            return currentSwum;
        }

        public bool ContainsFile(string sourcePath)
        {
            var fullPath = Path.GetFullPath(sourcePath);
            lock (signaturesToSwum)
            {
                if (signaturesToSwum.Keys.Select(sig => signaturesToSwum[sig]).Any(sdr => sdr.FileNames.Contains(fullPath)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
