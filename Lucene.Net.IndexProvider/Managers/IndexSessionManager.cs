using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.IndexProvider.Interfaces;
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

    private readonly Lazy<Dictionary<string, IndexWriter>> _contextSessions =
        new Lazy<Dictionary<string, IndexWriter>>(
            () => new Dictionary<string, IndexWriter> (StringComparer.OrdinalIgnoreCase));

    public IDictionary<string, IndexWriter> ContextSessions => _contextSessions.Value;

    public IndexWriter GetSessionFrom(string indexName)
    {
        if (!ContextSessions.TryGetValue(indexName, out var context))
        {
            var config = _configurationManager.GetConfiguration(indexName);

            var analyzer = new StandardAnalyzer(config.LuceneVersion);
            var indexConfig = new IndexWriterConfig(config.LuceneVersion, analyzer);
            var writer = new IndexWriter(GetDirectory(indexName), indexConfig);

            ContextSessions.Add(indexName, writer);
            return writer;
        }

        return context;
    }

    private FSDirectory GetDirectory(string indexName)
    {
        string localPath = _localIndexPathFactory.GetLocalIndexPath();
        var directoryInfo = new DirectoryInfo(Path.Combine(localPath, indexName));
        return FSDirectory.Open(directoryInfo);
    }

    public IndexWriter GetTransientSession(string indexName)
    {
        var config = _configurationManager.GetConfiguration(indexName);

        var analyzer = new StandardAnalyzer(config.LuceneVersion);
        var indexConfig = new IndexWriterConfig(config.LuceneVersion, analyzer);
        var writer = new IndexWriter(GetDirectory(indexName), indexConfig);

        return writer;
    }

    public void CloseSessionOn(string indexName)
    {
        if (ContextSessions.TryGetValue(indexName, out var context) && !context.IsClosed)
        {
            context.Commit();
            context.Dispose();
            ContextSessions.Remove(indexName);
        }
    }
}