using Lucene.Net.Store;

namespace Lucene.Net.IndexProvider.Interfaces;

public interface IDirectoryManager
{
    Directory GetDirectory(string indexName);
}