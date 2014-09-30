using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Sando.Core.Logging.Events;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.TaskFactoryContracts;
using Sando.Indexer.Documents;
using Sando.Indexer.Documents.Converters;
using Sando.Indexer.Exceptions;
using Sando.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Timers;

namespace Sando.Indexer
{
    public class DocumentIndexer : IDisposable
    {
        #region Private Fields

        private readonly object _lock = new object();
        private bool _disposed;
        private bool _disposingInProcess = false;
        private bool _hasIndexChanged;
        private IndexSearcher _indexSearcher;
        private System.Timers.Timer commitChanges;
        private System.Timers.Timer refreshIndexSearcher;
        private string DELETE_INIDACTOR = "DELETE_ME";
        private DirectoryInfo directoryInfo;        

        #endregion Private Fields

        #region Public Constructors

        public DocumentIndexer(TimeSpan? refreshIndexSearcherThreadInterval = null, TimeSpan? commitChangesThreadInterval = null)
        {
            try
            {
                var solutionKey = ServiceLocator.Resolve<SolutionKey>();
                directoryInfo = new System.IO.DirectoryInfo(PathManager.Instance.GetIndexPath(solutionKey));
                DeleteOldFolderIfIndicated(solutionKey);
                LuceneIndexesDirectory = FSDirectory.Open(directoryInfo);
                Analyzer = ServiceLocator.Resolve<Analyzer>();
                IndexWriter = new IndexWriter(LuceneIndexesDirectory, Analyzer, IndexWriter.MaxFieldLength.LIMITED);
                Reader = IndexWriter.GetReader();
                _indexSearcher = new IndexSearcher(Reader);
                QueryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, Configuration.Configuration.GetValue("DefaultSearchFieldName"), Analyzer);
                SetupTimedUpdates(refreshIndexSearcherThreadInterval, commitChangesThreadInterval);
                IndexWriter.SetRAMBufferSizeMB(128);
            }
            catch (CorruptIndexException corruptIndexEx)
            {
                LogEvents.IndexCorruptError(this, corruptIndexEx);
                throw new IndexerException(TranslationCode.Exception_Indexer_LuceneIndexIsCorrupt, corruptIndexEx);
            }
            catch (LockObtainFailedException lockObtainFailedEx)
            {
                LogEvents.IndexLockObtainFailed(this, lockObtainFailedEx);
                throw new IndexerException(TranslationCode.Exception_Indexer_LuceneIndexAlreadyOpened, lockObtainFailedEx);
            }
            catch (System.IO.IOException ioEx)
            {
                LogEvents.IndexIOError(this, ioEx);
                throw new IndexerException(TranslationCode.Exception_General_IOException, ioEx, ioEx.Message);
            }
        }

        public DocumentIndexer()
            : this(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
        {
        }

        #endregion Public Constructors

        #region Public Properties

        public Lucene.Net.Store.Directory LuceneIndexesDirectory { get; set; }

        public QueryParser QueryParser { get; protected set; }

        public IndexReader Reader { get; private set; }

        #endregion Public Properties

        #region Protected Properties

        protected Analyzer Analyzer { get; set; }

        protected IndexWriter IndexWriter { get; set; }

        #endregion Protected Properties

        #region Public Methods

        public void AddDeletionFile()
        {
            if (directoryInfo != null)
            {
                File.Create(directoryInfo.FullName + "\\" + DELETE_INIDACTOR);
            }
        }

        public virtual void AddDocument(SandoDocument sandoDocument)
        {
            Contract.Requires(sandoDocument != null, "DocumentIndexer:AddDocument - sandoDocument cannot be null!");

            Document tempDoc = sandoDocument.GetDocument();
            IndexWriter.AddDocument(tempDoc);
            lock (_lock)
            {
                if (!_hasIndexChanged) //if _hasIndexChanged is false, then turn it into true
                    _hasIndexChanged = true;
            }
        }

        public void ClearIndex()
        {
            lock (_lock)
            {
                IndexWriter.GetDirectory().EnsureOpen();
                IndexWriter.DeleteAll();
                CommitChanges();
            }
        }

        public virtual void DeleteDocuments(string fullFilePath, bool commitImmediately = false)
        {
            if (String.IsNullOrWhiteSpace(fullFilePath))
                return;
            var term = new Term("FullFilePath", ConverterFromHitToProgramElement.StandardizeFilePath(fullFilePath));
            IndexWriter.DeleteDocuments(new TermQuery(term));
            lock (_lock)
            {
                if (commitImmediately)
                    CommitChanges();
                else
                    if (!_hasIndexChanged) //if _hasIndexChanged is false, then turn it into true
                        _hasIndexChanged = true;
            }
        }

        public void Dispose()
        {
            _disposingInProcess = true;
            Dispose(false);
        }

        public void Dispose(bool killReaders)
        {
            _disposingInProcess = true;
            lock (_lock)
            {
                try
                {
                    CommitChanges();
                }
                catch (AlreadyClosedException)
                {
                    //This is expected in some cases
                }
                Dispose(true, killReaders);
                GC.SuppressFinalize(this);
            }
        }

        public void ForceFlush()
        {
            lock (_lock)
            {
                IndexWriter.Flush();
            }
        }

        public void ForceReaderRefresh()
        {
            lock (_lock)
            {
                CommitChanges();
            }
        }

        public IEnumerable<string> GetDocumentList()
        {
            for (int i = 0; i < Reader.MaxDoc(); i++)
            {
                if (Reader.IsDeleted(i))
                    continue;
                Document doc = Reader.Document(i);
                yield return doc.GetField(SandoField.FullFilePath.ToString()).StringValue();
            }
        }

        public int GetNumberOfIndexedDocuments()
        {
            return Reader.NumDocs();
        }

        public bool IsDisposingOrDisposed()
        {
            return _disposingInProcess || _disposed;
        }

        public void NUnit_CloseIndexSearcher()
        {
            _indexSearcher.GetIndexReader().Close();
        }

        public List<Tuple<Document, float>> Search(Query query, TopScoreDocCollector collector)
        {
            lock (_lock)
            {
                try
                {
                    return RunSearch(query, collector);
                }
                catch (AlreadyClosedException)
                {
                    UpdateSearcher();
                    return RunSearch(query, collector);
                }
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing, bool killReaders)
        {
            _disposingInProcess = true;
            if (!_disposed)
            {
                if (disposing)
                {
                    if (refreshIndexSearcher != null)
                        refreshIndexSearcher.Stop();
                    if (commitChanges != null)
                        commitChanges.Stop();
                    IndexWriter.Close();
                    IndexReader indexReader = _indexSearcher.GetIndexReader();
                    if (indexReader != null)
                        indexReader.Close();
                    _indexSearcher.Close();
                    LuceneIndexesDirectory.Close();
                    try
                    {
                        Analyzer.Close();
                    }
                    catch (NullReferenceException)
                    {
                        //already closed, ignore
                    }
                }

                _disposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void CommitChanges()
        {
            IndexWriter.Commit();
            UpdateSearcher();
            _hasIndexChanged = false;
        }

        private void DeleteOldFolderIfIndicated(SolutionKey solutionKey)
        {
            if (File.Exists(directoryInfo.FullName + "\\" + DELETE_INIDACTOR))
            {
                System.IO.Directory.Delete(directoryInfo.FullName, true);
                PathManager.Instance.GetIndexPath(solutionKey);
            }
        }

        private bool IsUsable()
        {
            try
            {
                _indexSearcher.Search(new TermQuery(new Term("asdf")), 1);
            }
            catch (AlreadyClosedException)
            {
                return false;
            }
            return true;
        }

        private void PeriodicallyCommitChangesIfNeeded(object sender, ElapsedEventArgs e)
        {
            //_scheduler.StartNew(() =>
            //{
            lock (_lock)
            {
                if (_hasIndexChanged)
                    CommitChanges();
            }
            //}, new CancellationTokenSource());
        }

        private void PeriodicallyRefreshIndexSearcherIfNeeded(object sender, ElapsedEventArgs e)
        {
            //_scheduler.StartNew(() =>
            //{
            lock (_lock)
            {
                if (!IsUsable())
                {
                    UpdateSearcher();
                }
            }
            //}, new CancellationTokenSource());
        }

        private List<Tuple<Document, float>> RunSearch(Query query, TopScoreDocCollector collector)
        {
            _indexSearcher.Search(query, collector);

            var hits = collector.TopDocs().ScoreDocs;
            var documents =
                hits.AsEnumerable().Select(h => new Tuple<Document, float>(_indexSearcher.Doc(h.doc), h.score)).ToList();
            return documents;
        }

        private void SetupTimedUpdates(TimeSpan? refreshIndexSearcherThreadInterval, TimeSpan? commitChangesThreadInterval)
        {
            refreshIndexSearcher = new System.Timers.Timer();
            refreshIndexSearcher.Elapsed += PeriodicallyRefreshIndexSearcherIfNeeded;
            refreshIndexSearcher.Interval = refreshIndexSearcherThreadInterval.HasValue ? refreshIndexSearcherThreadInterval.Value.Seconds * 1000 : TimeSpan.FromSeconds(10).Milliseconds;
            refreshIndexSearcher.Start();

            commitChanges = new System.Timers.Timer();
            commitChanges.Elapsed += PeriodicallyCommitChangesIfNeeded;
            commitChanges.Interval = commitChangesThreadInterval.HasValue ? commitChangesThreadInterval.Value.Seconds * 1000 : TimeSpan.FromSeconds(10).Milliseconds;
            commitChanges.Start();
        }

        private void UpdateSearcher()
        {
            try
            {
                var oldReader = Reader;
                Reader = Reader.Reopen(true);
                if (Reader != oldReader)
                {
                    //_indexSearcher.Close(); - don't need this, because we create IndexSearcher by passing the IndexReader to it, so Close do nothing
                    oldReader.Close();
                    _indexSearcher = new IndexSearcher(Reader);
                }
            }
            catch (AlreadyClosedException)
            {
                try
                {
                    Reader = IndexWriter.GetReader();
                    _indexSearcher = new IndexSearcher(Reader);
                }
                catch (AlreadyClosedException)
                {
                    //solution has been closed, ignore exception
                }
            }
        }

        #endregion Private Methods
    }
}