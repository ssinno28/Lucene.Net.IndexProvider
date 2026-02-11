using Lucene.Net.Store;
using System.Collections.Generic;

namespace Lucene.Net.IndexProvider.Interfaces;

public interface IDirectoryManager
{
    IDictionary<string, Directory> Directories { get; }
    Directory GetDirectory(string indexName);
}