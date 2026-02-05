using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Models;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.IndexProvider.Managers;

public class IndexSessionManager : IIndexSessionManager
{
    private readonly IIndexConfigurationManager _configurationManager;
    private readonly ILuceneDirectoryFactory _directoryFactory;

    public IndexSessionManager(
        IIndexConfigurationManager configurationManager,
        ILuceneDirectoryFactory directoryFactory)
    {
        _configurationManager = configurationManager;
        _directoryFactory = directoryFactory;
    }

    private readonly Lazy<Dictionary<string, LuceneSession>> _contextSessions =
        new(() => new Dictionary<string, LuceneSession>(StringComparer.OrdinalIgnoreCase));

    private readonly Lazy<Dictionary<string, ManualResetEventSlim>> _sessionLocks =
        new(() => new Dictionary<string, ManualResetEventSlim>(StringComparer.OrdinalIgnoreCase));

    public IDictionary<string, LuceneSession> ContextSessions => _contextSessions.Value;
    private IDictionary<string, ManualResetEventSlim> SessionLocks => _sessionLocks.Value;

    public LuceneSession GetSessionFrom(string indexName)
    {
        if (SessionLocks.TryGetValue(indexName, out var sessionLock))
        {
            bool signaled = sessionLock.Wait(TimeSpan.FromSeconds(5));
            if (!signaled)
            {
                throw new TimeoutException($"Lock for index '{indexName}' could not be acquired within the timeout period.");
            }
        }

        if (!ContextSessions.TryGetValue(indexName, out var context))
        {
            var config = _configurationManager.GetConfiguration(indexName);

            var analyzer = new StandardAnalyzer(config.LuceneVersion);
            var indexConfig = new IndexWriterConfig(config.LuceneVersion, analyzer);
            var writer = new IndexWriter(_directoryFactory.GetIndexDirectory(indexName), indexConfig);
            var searchManager = new SearcherManager(writer, true, null);

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

    public void AddLock(string indexName)
    {
        if (!SessionLocks.ContainsKey(indexName))
        {
            SessionLocks[indexName] = new ManualResetEventSlim(false);
        }
    }

    public void ReleaseLock(string indexName)
    {
        if (SessionLocks.TryGetValue(indexName, out var sessionLock))
        {
            sessionLock.Set();
            SessionLocks.Remove(indexName);
        }
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