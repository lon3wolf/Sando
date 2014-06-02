using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using Gma.DataStructures.StringSearch;

namespace Sando.Recommender
{
    public class SwumDataStructure
    {
        private Dictionary<string, SwumDataRecord> signaturesToSwum;
        private ITrie<SwumDataRecord> trie;

        public SwumDataStructure()
        {
            signaturesToSwum = new Dictionary<string, SwumDataRecord>();
            trie = new PatriciaTrie<SwumDataRecord>();
        }

        public void AddRecord(string signature, SwumDataRecord record)
        {
            record.Signature = signature;

            lock (signaturesToSwum)
            {
                signaturesToSwum[signature] = record;
            }
            
            lock (trie)
            {
                foreach (var actionWord in record.Action.ToLowerInvariant().Split(' '))
                { 
                    if (!String.IsNullOrEmpty(actionWord))
                    {
                        trie.Add(actionWord.Trim(), record);
                    }
                }

                foreach (var indirectObjectWord in record.IndirectObject.ToLowerInvariant().Split(' ') )
                {
                    if (!String.IsNullOrEmpty(indirectObjectWord))
                    {
                        trie.Add(indirectObjectWord.Trim(), record);
                    }
                }

                foreach (var themeWord in record.Theme.Split(' '))
                {
                    if (!String.IsNullOrEmpty(themeWord))
                    {
                        trie.Add(themeWord.Trim().ToLowerInvariant(), record);
                    }
                }
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

            lock (trie)
            {
                trie = new PatriciaTrie<SwumDataRecord>();
                //and let garbage collector deal with previous trie
            }
        }

        public List<SwumDataRecord> GetAllSwumData()
        {
            var currentSwum = new List<SwumDataRecord>();
            lock (signaturesToSwum)
            {
                currentSwum.AddRange(signaturesToSwum.Select(entry => entry.Value));
            }
            return currentSwum;
        }

        public Dictionary<string, SwumDataRecord> GetAllSwumDataBySignature()
        {
            var currentSwum = new Dictionary<string, SwumDataRecord>();
            lock (signaturesToSwum)
            {
                foreach (var sigToSwum in signaturesToSwum)
                {
                    currentSwum[sigToSwum.Key] = sigToSwum.Value;                    
                }
            }
            return currentSwum;
        }

        public List<SwumDataRecord> GetSwumDataForTerm(String term)
        {
            var trieRecs = new List<SwumDataRecord>();
            var finalRecs = new List<SwumDataRecord>();
            lock (trie)
            {
                trieRecs = trie.Retrieve(term).ToList();

                //ensure that these records haven't been removed from signaturesToSwum
                lock (signaturesToSwum)
                {
                    finalRecs.AddRange(trieRecs.Where(trieRec => signaturesToSwum.ContainsKey(trieRec.Signature)));
                }
            }
            return finalRecs;
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
