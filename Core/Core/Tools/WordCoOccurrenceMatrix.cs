using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Sando.Core.QueryRefomers;

namespace Sando.Core.Tools
{
    public interface IMatrixEntry
    {
        string Row { get; }
        string Column { get; }
        int Count { get; }
    }

    public interface IWordCoOccurrenceMatrix : IDisposable, IInitializable
    {
        int GetCoOccurrenceCount(String word1, String word2);
        Dictionary<String, int> GetCoOccurredWordsAndCount(String word);
        Dictionary<string, int> GetAllWordsAndCount();
        IEnumerable<IMatrixEntry> GetEntries(Predicate<IMatrixEntry> predicate);
        void AddWordPair(string one, string two, int count);

    }

}
