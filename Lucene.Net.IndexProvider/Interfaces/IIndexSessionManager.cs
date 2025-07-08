using Lucene.Net.Index;
using Lucene.Net.IndexProvider.Models;
using System.Collections.Generic;

namespace Lucene.Net.IndexProvider.Interfaces;

public interface IIndexSessionManager
{
    LuceneSession GetSessionFrom(string indexName);
    void Commit(string indexName);
    IDictionary<string, LuceneSession> ContextSessions { get; }
    void CloseSession(string indexName);
    void AddLock(string indexName);
    void ReleaseLock(string indexName);
}