using Lucene.Net.Store;

namespace Lucene.Net.IndexProvider.Interfaces;

public interface ILuceneDirectoryFactory
{
    Directory GetIndexDirectory(string indexName);
}