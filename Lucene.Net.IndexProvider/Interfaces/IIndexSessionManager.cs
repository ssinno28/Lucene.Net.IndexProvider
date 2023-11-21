using System.Collections.Generic;
using Lucene.Net.Index;

namespace Lucene.Net.IndexProvider.Interfaces;

public interface IIndexSessionManager
{
    IndexWriter GetSessionFrom(string indexName);
    IndexWriter GetTransientSession(string indexName);
    void CloseSessionOn(string indexName);
    IDictionary<string, IndexWriter> ContextSessions { get; }
}