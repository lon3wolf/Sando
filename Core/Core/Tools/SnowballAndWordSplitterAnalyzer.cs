using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Portal.LuceneInterface;
using Sando.Indexer.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sando.Indexer.Splitter
{
        public class SnowballAndWordSplittingAnalyzer : Analyzer
        {
            private Lucene.Net.Util.Version matchVersion = Lucene.Net.Util.Version.LUCENE_CURRENT;
            private System.Collections.Hashtable stopSet;

            public SnowballAndWordSplittingAnalyzer(string p)
            {
            }

            public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
            {
                TokenStream result = GetStandardFilterSet(reader);
                if (stopSet != null)
                    result = new StopFilter(StopFilter.GetEnablePositionIncrementsVersionDefault(matchVersion),
                    result, stopSet);
                result = new SnowballFilter(result, "English");
                return result;
            }

            public static Lucene.Net.Analysis.TokenStream GetStandardFilterSet(System.IO.TextReader reader)
            {
                var mappingCharFilter = WordDelimiterFilter.GetCharMapper(reader);
                TokenStream ts = new WhitespaceTokenizer(mappingCharFilter);
                WordDelimiterFilter filter = new WordDelimiterFilter(ts, 1, 1, 1, 1, 1);
                TokenStream result = new LowerCaseFilter(filter);
                return result;
            }

            private class SavedStreams
            {
                internal Tokenizer source;
                internal TokenStream result;
            };

            /* Returns a (possibly reused) {@link StandardTokenizer} filtered by a
            * {@link StandardFilter}, a {@link LowerCaseFilter},
            * a {@link StopFilter}, and a {@link SnowballFilter} */

            public override TokenStream ReusableTokenStream(String fieldName, TextReader reader)
            {
                if (overridesTokenStreamMethod)
                {
                    // LUCENE-1678: force fallback to tokenStream() if we
                    // have been subclassed and that subclass overrides
                    // tokenStream but not reusableTokenStream
                    return TokenStream(fieldName, reader);
                }

                SavedStreams streams = (SavedStreams)this.GetPreviousTokenStream();
                if (streams == null)
                {
                    streams = new SavedStreams();
                    streams.source = new StandardTokenizer(matchVersion, reader);
                    streams.result = new Portal.LuceneInterface.WordDelimiterFilter(streams.source, 1, 1, 1, 1, 1);
                    streams.result = new StandardFilter(streams.result);
                    streams.result = new LowerCaseFilter(streams.result);
                    if (stopSet != null)
                        streams.result = new StopFilter(StopFilter.GetEnablePositionIncrementsVersionDefault(matchVersion),
                        streams.result, stopSet);
                    streams.result = new SnowballFilter(streams.result, "English");
                    this.SetPreviousTokenStream(streams);
                }
                else
                {
                    streams.source.Reset(reader);
                }
                return streams.result;
            }

            public static Analyzer GetAnalyzer()
            {
                var snowball = new SnowballAndWordSplittingAnalyzer("English");
                PerFieldAnalyzerWrapper analyzer = new PerFieldAnalyzerWrapper(snowball);
                SandoField[] fields = new SandoField[]{ 
                SandoField.ClassId,
                SandoField.Source,
                SandoField.AccessLevel,
                SandoField.ProgramElementType,
                SandoField.DefinitionLineNumber,
                SandoField.FileExtension,
                SandoField.FullFilePath,
                SandoField.Id,
                SandoField.IsConstructor,
                SandoField.Modifiers,
                SandoField.DefinitionColumnNumber
            };
                foreach (var field in fields)
                    analyzer.AddAnalyzer(field.ToString(), new KeywordAnalyzer());
                return analyzer;
            }

        }

        public class LetterTokenizerWithUnderscores : LetterTokenizer
        {
            public LetterTokenizerWithUnderscores(TextReader reader)
                : base(reader)
            {
            }

            protected override bool IsTokenChar(char c)
            {
                if (c.Equals('_'))
                    return true;
                else
                    return base.IsTokenChar(c);
            }

      
        }      

  
}
