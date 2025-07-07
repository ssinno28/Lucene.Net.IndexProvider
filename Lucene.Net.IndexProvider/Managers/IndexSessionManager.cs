using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Models;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Lucene.Net.IndexProvider.Managers;

public class IndexSessionManager : IIndexSessionManager
{
    private readonly IIndexConfigurationManager _configurationManager;
    private readonly ILocalIndexPathFactory _localIndexPathFactory;

    public IndexSessionManager(
        IIndexConfigurationManager configurationManager,
        ILocalIndexPathFactory localIndexPathFactory)
    {
        _configurationManager = configurationManager;
        _localIndexPathFactory = localIndexPathFactory;
    }

    private readonly Lazy<Dictionary<string, LuceneSession>> _contextSessions =
        new(() => new Dictionary<string, LuceneSession>(StringComparer.OrdinalIgnoreCase));

    public IDictionary<string, LuceneSession> ContextSessions => _contextSessions.Value;

    public LuceneSession GetSessionFrom(string indexName)
    {
        if (!ContextSessions.TryGetValue(indexName, out var context))
        {
            var config = _configurationManager.GetConfiguration(indexName);

            var analyzer = new StandardAnalyzer(config.LuceneVersion);
            var indexConfig = new IndexWriterConfig(config.LuceneVersion, analyzer);
            var writer = new IndexWriter(GetDirectory(indexName), indexConfig);
            var searchManager = new SearcherManager(writer, true, new SearcherFactory());

            var luceneSession = new LuceneSession
            {
                Writer = writer,
                SearcherManager = searchManager
            };

            ContextSessions.Add(indexName, luceneSession);
            return luceneSession;
        }

        return context;
    }

    private FSDirectory GetDirectory(string indexName)
    {
        string localPath = _localIndexPathFactory.GetLocalIndexPath();
        var directoryInfo = new DirectoryInfo(Path.Combine(localPath, indexName));
        return FSDirectory.Open(directoryInfo);
    }

    public LuceneSession GetTransientSession(string indexName)
    {
        var config = _configurationManager.GetConfiguration(indexName);

        var analyzer = new StandardAnalyzer(config.LuceneVersion);
        var indexConfig = new IndexWriterConfig(config.LuceneVersion, analyzer);
        var writer = new IndexWriter(GetDirectory(indexName), indexConfig);

        var searchManager = new SearcherManager(writer, true, new SearcherFactory());
        var luceneSession = new LuceneSession
        {
            Writer = writer,
            SearcherManager = searchManager
        };

        return luceneSession;
    }

    public void Commit(string indexName)
    {
        if (ContextSessions.TryGetValue(indexName, out var context))
        {
            if (!context.Writer.IsClosed && context.Writer.HasUncommittedChanges())
            {
                context.Writer.Commit();
                context.SearcherManager.MaybeRefresh();
            }
        }
    }

    public void CloseSession(string indexName)
    {
        if (ContextSessions.TryGetValue(indexName, out var context))
        {
            if (!context.Writer.IsClosed)
            {
                context.Writer.Commit();
                context.Writer.Dispose();
            }

            context.SearcherManager.Dispose();

            ContextSessions.Remove(indexName);
        }
    }
}