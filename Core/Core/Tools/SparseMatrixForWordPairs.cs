using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace Sando.Core.Tools
{
    public class SparseMatrixForWordPairs : IWordCoOccurrenceMatrix
    {

        SerializableDictionary<string, SerializableDictionary<string, int>> theMatrix = new SerializableDictionary<string, SerializableDictionary<string, int>>();
        private const string fileName = "SparseMatrix.txt";
        private const int GRAM_NUMBER = 3;
        private const int MAX_COOCCURRENCE_WORDS_COUNT = 100;
        private const int MAX_WORD_LENGTH = 3;
        private System.Threading.Tasks.TaskScheduler scheduler;
        private TaskFactory factory;
        private string directory;
        private XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<string, SerializableDictionary<string, int>>));

        public SparseMatrixForWordPairs()
        {
            factory = new TaskFactory();
        }

        public SparseMatrixForWordPairs(TaskScheduler scheduler)
        {            
            factory = new TaskFactory(scheduler);
        }

        public void AddWordPair(string one, string two, int count = 1)
        {
            if (one.CompareTo(two)==1)
            {
                AddWords(two, one, count);
            }
            else
            {
                AddWords(one, two, count);
            }
        }

    

        private int CountPairs(string one, string two)
        {
            lock (theMatrix)
            {
                if (theMatrix.ContainsKey(one))
                {
                    if (theMatrix[one].ContainsKey(two))
                    {
                        return theMatrix[one][two];
                    }
                }                
            }
            return 0;
        }

        private void AddWords(string one, string two, int count)
        {
            lock (theMatrix)
            {
                if (theMatrix.ContainsKey(one))
                {
                    if (theMatrix[one].ContainsKey(two))
                    {
                        theMatrix[one][two]++;
                    }
                    else
                    {
                        theMatrix[one][two] = count;
                    }
                }
                else
                {
                    theMatrix[one] = new SerializableDictionary<string, int>();
                    theMatrix[one][two] = count;
                }
            }
        }

   
        private void ClearMemory()
        {
            theMatrix = new SerializableDictionary<string, SerializableDictionary<string, int>>();
        }


        public int GetCoOccurrenceCount(string one, string two)
        {
            if (one.CompareTo(two) == 1)
            {
                return CountPairs(two, one);
            }
            else
            {
                return CountPairs(one, two);
            }
        }

        public Dictionary<string, int> GetCoOccurredWordsAndCount(string word)
        {
            lock (theMatrix)
            {
                if (theMatrix.ContainsKey(word))
                {
                    return new Dictionary<string,int>(theMatrix[word]);
                }
                else
                {
                    return new Dictionary<string, int>();
                }
            }
        }

        public Dictionary<string, int> GetAllWordsAndCount()
        {
            lock (theMatrix)
            {
                return theMatrix.Keys.ToDictionary(w => w, w => theMatrix[w][w]);
            }
        }

        public IEnumerable<IMatrixEntry> GetEntries(Predicate<IMatrixEntry> predicate)
        {
            List<IMatrixEntry> entries = new List<IMatrixEntry>();
            lock(theMatrix)
            {
                foreach(var key in theMatrix.Keys)
                    foreach(var key2 in theMatrix[key].Keys)
                        entries.Add(new MatrixEntry(key,key2,1));
            }
            return entries;
        }

    
        public void Dispose()
        {
            lock (theMatrix)
            {
                FileStream stream = new FileStream(GetMatrixFilePath(), FileMode.Create);
                try
                {                    
                    serializer.Serialize(stream, theMatrix);
                }
                finally
                {
                    stream.Close();
                }
            }
        }

        private string GetMatrixFilePath()
        {
            return Path.Combine(directory, fileName);
        }

        public void Initialize(string directoryIn)
        {
            directory = directoryIn;
            lock (theMatrix)
            {
                ClearMemory();
                ReadFromFile();
            }
        }

        private void ReadFromFile()
        {
            if (File.Exists(GetMatrixFilePath()))
            {
                FileStream stream = new FileStream(GetMatrixFilePath(), FileMode.Open);
                lock (theMatrix)
                {
                    try
                    {
                        theMatrix = (SerializableDictionary<string, SerializableDictionary<string, int>>)serializer.Deserialize(stream);
                    }
                    finally
                    {
                        stream.Close();
                    }
                }
            }
        }


        /////////////////////////////////////////////////////////
        ///Everything below this line still needs to be reviewed. 
        ////////////////////////////////////////////////////////

        public void HandleCoOcurrentWordsAsync(IEnumerable<String> words)
        {
            if (factory != null)
            {
                Action action = () =>
                {
                    HandleCoOcurrentWordsSync(words);
                };
                factory.StartNew(action);
            }
            else
            {
                HandleCoOcurrentWordsSync(words);
            }
        }


        public void HandleCoOcurrentWordsSync(IEnumerable<string> words)
        {
                words = SelectWords(words.ToList()).ToList();                
                var entries = GetBigramEntries(words);
                foreach (var entry in entries)
                {
                    AddWordPair(entry.Column, entry.Row);
                }
            
        }

        private static IEnumerable<String> SelectWords(List<string> words)
        {
            words = FilterOutBadWords(words).ToList();
            words = (words.Count > MAX_COOCCURRENCE_WORDS_COUNT)
                ? words.GetRange(0, MAX_COOCCURRENCE_WORDS_COUNT)
                   : words;
            return words.Distinct();
        }

        private static IEnumerable<String> FilterOutBadWords(IEnumerable<String> words)
        {
            return words.Where(w => w.Length >= MAX_WORD_LENGTH
                || w.Contains(' ') || w.Contains(':'));
        }

        private static IEnumerable<MatrixEntry> GetBigramEntries(IEnumerable<string> words)
        {
            var list = words.ToList();
            var allEntries = new List<MatrixEntry>();
            int i;
            for (i = 0; i + GRAM_NUMBER - 1 < list.Count; i++)
            {
                allEntries.AddRange(InternalGetEntries(list.GetRange(i, GRAM_NUMBER)));
            }

            // Check if having leftovers.
            if (i + GRAM_NUMBER - 1 != list.Count - 1 && list.Any())
            {
                allEntries.AddRange(InternalGetEntries(list.GetRange(i, list.Count - i)));
            }

            return allEntries;
        }

        private static IEnumerable<MatrixEntry> InternalGetEntries(IEnumerable<string> words)
        {
            var list = words.ToList();
            var allEntries = new List<MatrixEntry>();
            for (int i = 0; i < list.Count; i++)
            {
                var word1 = list.ElementAt(i);
                for (int j = i; j < list.Count; j++)
                {
                    var word2 = list.ElementAt(j);
                    allEntries.Add(new MatrixEntry(word1, word2));
                    allEntries.Add(new MatrixEntry(word2, word1));
                }
            }
            return allEntries;
        }

        private class MatrixEntry : IMatrixEntry
        {
            public string Row { get; private set; }
            public string Column { get; private set; }
            public int Count { get; private set; }

            public MatrixEntry(String Row, String Column, int Count = 0)
            {
                this.Row = Row;
                this.Column = Column;
                this.Count = Count;
            }
        }



    }

    	
 
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue>
        : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
 
        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
 
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
 
            if (wasEmpty)
                return;
 
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
 
                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
 
                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();
 
                this.Add(key, value);
 
                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }
 
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
 
            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");
 
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
 
                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
 
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
