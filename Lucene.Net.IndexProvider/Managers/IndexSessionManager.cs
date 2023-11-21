using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.Store;
using Microsoft.AspNetCore.Http;

namespace Lucene.Net.IndexProvider.Managers;

public class IndexSessionManager : IIndexSessionManager
{
    private readonly string SESSIONS_KEY = "INDEX_SESSIONS";

    private readonly IIndexConfigurationManager _configurationManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDictionary<string, IndexWriter> _backupContextSessions = new Dictionary<string, IndexWriter>();
    private readonly ILocalIndexPathFactory _localIndexPathFactory;

    public IndexSessionManager(
        IIndexConfigurationManager configurationManager,
        IHttpContextAccessor httpContextAccessor,
        ILocalIndexPathFactory localIndexPathFactory)
    {
        _configurationManager = configurationManager;
        _httpContextAccessor = httpContextAccessor;
        _localIndexPathFactory = localIndexPathFactory;
    }

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

    public IDictionary<string, IndexWriter> ContextSessions
    {
        get
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return _backupContextSessions;
            }

            if (!_httpContextAccessor.HttpContext.Items.TryGetValue(SESSIONS_KEY, out var sessions))
            {
                sessions = new Dictionary<string, IndexWriter>();
                _httpContextAccessor.HttpContext.Items.Add(SESSIONS_KEY, sessions);
            }

            return (IDictionary<string, IndexWriter>)sessions;
        }
    }
}