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
        private Dictionary<int, SwumDataRecord> hashOfSignaturesToSwumRecord;
        private ITrie<SwumDataRecord> trie;

        public SwumDataStructure()
        {
            hashOfSignaturesToSwumRecord = new Dictionary<int, SwumDataRecord>();
            trie = new PatriciaTrie<SwumDataRecord>();
        }

        public void AddRecord(int signature, SwumDataRecord record)
        {
            record.Signature = signature;

            lock (hashOfSignaturesToSwumRecord)
            {
                hashOfSignaturesToSwumRecord[signature] = record;
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
            lock (hashOfSignaturesToSwumRecord)
            {
                if (hashOfSignaturesToSwumRecord.ContainsKey(methodSignature.GetHashCode()))
                {
                    result = hashOfSignaturesToSwumRecord[methodSignature.GetHashCode()];
                }
            }
            return result;
        }

        public SwumDataRecord GetSwumForSignature(int methodSignature)
        {
            Contract.Requires(methodSignature != null, "SwumDataRecord:GetSwumForSignature - signature cannot be null!");

            SwumDataRecord result = null;
            lock (hashOfSignaturesToSwumRecord)
            {
                if (hashOfSignaturesToSwumRecord.ContainsKey(methodSignature))
                {
                    result = hashOfSignaturesToSwumRecord[methodSignature];
                }
            }
            return result;
        }

        public void RemoveSourceFile(string sourcePath)
        {
            var fullPath = Path.GetFullPath(sourcePath);
            var recordsToRemove = new HashSet<int>();
            lock (hashOfSignaturesToSwumRecord)
            {
                foreach (var signature in hashOfSignaturesToSwumRecord.Keys)
                {
                    var sdr = hashOfSignaturesToSwumRecord[signature];
                    if (sdr.FileNameHashes.Contains(fullPath.GetHashCode()))
                    {
                        sdr.FileNameHashes.Remove(fullPath.GetHashCode());
                        if (!sdr.FileNameHashes.Any())
                        {
                            recordsToRemove.Add(signature);
                        }
                    }
                }

                //remove signatures that no longer have any file names
                //(This is separate from the above loop because you can't delete keys while you're enumerating them.)
                foreach (var signature in recordsToRemove)
                {
                    hashOfSignaturesToSwumRecord.Remove(signature);
                }
            }
        }

        public void Clear()
        {
            lock (hashOfSignaturesToSwumRecord)
            {
                hashOfSignaturesToSwumRecord.Clear();
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
            lock (hashOfSignaturesToSwumRecord)
            {
                currentSwum.AddRange(hashOfSignaturesToSwumRecord.Select(entry => entry.Value));
            }
            return currentSwum;
        }

        public Dictionary<int, SwumDataRecord> GetAllSwumDataBySignature()
        {
            var currentSwum = new Dictionary<int, SwumDataRecord>();
            lock (hashOfSignaturesToSwumRecord)
            {
                foreach (var sigToSwum in hashOfSignaturesToSwumRecord)
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
                lock (hashOfSignaturesToSwumRecord)
                {
                    finalRecs.AddRange(trieRecs.Where(trieRec => hashOfSignaturesToSwumRecord.ContainsKey(trieRec.Signature)));
                }
            }
            return finalRecs;
        }

        public bool ContainsFile(string sourcePath)
        {
            var fullPath = Path.GetFullPath(sourcePath);
            lock (hashOfSignaturesToSwumRecord)
            {
                if (hashOfSignaturesToSwumRecord.Keys.Select(sig => hashOfSignaturesToSwumRecord[sig]).Any(sdr => sdr.FileNameHashes.Contains(fullPath.GetHashCode())))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
