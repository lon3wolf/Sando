using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SF.Snowball.Ext;
using Sando.ExtensionContracts.ProgramElementContracts;

namespace Sando.Core.Tools
{
    public static class DictionaryHelper
    {
        private static readonly Regex _quotesPattern = new Regex("-{0,1}\"[^\"]+\"", RegexOptions.Compiled);
        private static readonly Regex _uppercase = new Regex(@"([A-Z])", RegexOptions.Compiled);
        private static readonly Regex _nonLetters = new Regex(@"[^A-Za-z]", RegexOptions.Compiled);

        public static IEnumerable<String> ExtractElementWords(ProgramElement element)
        {
            var list = new List<string>();
            
            if (element as ClassElement != null)
            {
                AddElementName(element, list);
                list.AddRange(ExtractClassWords(element as ClassElement));
                return list;
            }
            if (element as CommentElement != null)
            {
                list.AddRange(ExtractCommentWords(element as CommentElement));
                return list;
            }
            if (element as FieldElement != null)
            {
                AddElementName(element, list);
                list.AddRange(ExtractFieldWords(element as FieldElement));
                return list;
            }
            if (element as MethodElement != null)
            {
                AddElementName(element, list);
                list.AddRange(ExtractMethodWords(element as MethodElement));
                return list;
            }
            if (element as MethodPrototypeElement != null)
            {
                list.AddRange(ExtractMethodPrototypeWords(element as MethodPrototypeElement));
                return list;
            }
            if (element as PropertyElement != null)
            {
                AddElementName(element, list);
                list.AddRange(ExtractPropertyWords(element as PropertyElement));
                return list;
            }
            if (element as StructElement != null)
            {
                AddElementName(element, list);
                list.AddRange(ExtractStructWords(element as StructElement));
                return list;
            }
            if (element as TextFileElement != null)
            {
                list.AddRange(ExtractTextLineElement(element as TextFileElement));
                return list;
            }
            if (element as XmlXElement != null)
            {
                list.AddRange(ExtractXmlWords(element as XmlXElement));
                return list;
            }

            if (element.GetCustomProperties().Count > 0)
            {
                list.AddRange(ExtractUnknownElementWords(element));
                return list;
            }
            list.Clear();
            return list;
        }

        private static void AddElementName(ProgramElement element, List<string> list)
        {
            var name = NormalizeText(element.Name);
            list.Add(name);
        }


        public static String NormalizeText(this String text)
        {
            return Regex.Replace(text, @"[^A-Za-z]+", "").ToLower();
        }

        public static IEnumerable<String> GetQuotedStrings(String text)
        {
            return GetMatchedWords(_quotesPattern, text);
        }

        private static IEnumerable<String> ExtractMethodWords(MethodElement element)
        {
            return GetDefaultLetterWords(element.RawSource);
        }

        private static IEnumerable<String> ExtractXmlWords(XmlXElement element)
        {
            return GetDefaultLetterWords(element.Body);
        }

        private static IEnumerable<String> ExtractCommentWords(CommentElement element)
        {
            return GetDefaultLetterWords(element.Body);
        }

        private static IEnumerable<String> ExtractClassWords(ClassElement element)
        {
            return GetDefaultLetterWords(element.Name + " " + element.Namespace);
        }

        private static IEnumerable<string> ExtractFieldWords(FieldElement element)
        {
            return GetDefaultLetterWords(new [] {element.Name, element.FieldType});
        }

        private static IEnumerable<string> ExtractPropertyWords(PropertyElement element)
        {
            return GetDefaultLetterWords(element.Body);
        }

        private static IEnumerable<string> ExtractMethodPrototypeWords(MethodPrototypeElement
            element)
        {
            return GetDefaultLetterWords(new []{element.Arguments, element.Name});
        }

        private static IEnumerable<string> ExtractTextLineElement(TextFileElement element)
        {
            return GetDefaultLetterWords(element.Body);
        }

        private static IEnumerable<string> ExtractStructWords(StructElement element)
        {
            return GetDefaultLetterWords(element.Name + " " + element.Namespace);
        }

        private static IEnumerable<string> ExtractUnknownElementWords(ProgramElement element)
        {
            return GetDefaultLetterWords(element.RawSource);
        }


        private static IEnumerable<String> GetDefaultLetterWords(IEnumerable<string> codes)
        {
            return codes.SelectMany(GetDefaultLetterWords);
        }

        private static IEnumerable<String> GetDefaultLetterWords(String code)
        {
            var words = new List<String>();
            words.AddRange(GetMatchedWords( code));
            //words.AddRange(GetMatchedWords( code).Select
            //    (TrimNonLetterPrefix));
            return words;
        }

        public static String TrimNonLetterPrefix(String word)
        {
            var firstLetter = word.First(Char.IsLetter);
            if (word.IndexOf(firstLetter) != 0)
                return word.Substring(word.IndexOf(firstLetter));
            else
                return word;
        }

        static char[] separator = new char[]{' '};

        public static IEnumerable<string> GetMatchedWords(String code)
        {
            //replace weird stuff with space
            var one = _nonLetters.Replace(code, " ");
            //add space before uppercase
            var two = _uppercase.Replace(one, " $1");
            return two.ToLower().Split(separator, StringSplitOptions.RemoveEmptyEntries).Distinct();
        }

        private static IEnumerable<string> GetMatchedWords(Regex pattern, String code)
        {
            var matches = pattern.Matches(code);
            return matches.Cast<Match>().Select(m => m.Groups[0].Value);
        }

        public static IEnumerable<int> GetQuoteStarts(string text)
        {
            var matches = RemoveChildMatches(_quotesPattern.Matches(text).Cast<Match>());
            return matches.Select(m => m.Groups[0].Index);
        }

        public static IEnumerable<int> GetQuoteEnds(string text)
        {
            var matches = RemoveChildMatches(_quotesPattern.Matches(text).Cast<Match>());
            return matches.Select(m => m.Groups[0].Index + m.Groups[0].Length - 1);
        }


        public static string GetStemmedQuery(this String query)
        {
            var stemmer = new EnglishStemmer();
            stemmer.SetCurrent(query);
            stemmer.Stem();
            return stemmer.GetCurrent();
        }

        private static IEnumerable<Match> RemoveChildMatches(IEnumerable<Match> matches)
        {
            var simplifiedMatches = matches.ToList();
            foreach (var match in matches)
            {
                if(matches.Any(m => IsMatchIncluding(m, match)))
                {
                    simplifiedMatches.Remove(match);
                }
            }
            return simplifiedMatches;
        }

        private static Boolean IsMatchIncluding(Match m1, Match m2)
        {
            return !m1.Equals(m2) && m1.Groups[0].Index <= m2.Groups[0].Index &&
                   m1.Groups[0].Length + m1.Groups[0].Index >= m2.Groups[0].Length + m2.Groups[0].Index;
        }
    }
}
