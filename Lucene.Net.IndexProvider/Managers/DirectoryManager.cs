using System;
using System.Collections.Generic;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;

namespace Lucene.Net.IndexProvider.Managers;

public class DirectoryManager : IDirectoryManager
{
    private readonly Lazy<IDictionary<string, Directory>> _directories =
        new Lazy<IDictionary<string, Directory>>(() => new Dictionary<string, Directory>());

    private readonly ILogger<DirectoryManager> _logger;
    private readonly ILuceneDirectoryFactory _directoryFactory;

    public DirectoryManager(ILogger<DirectoryManager> logger, ILuceneDirectoryFactory directoryFactory)
    {
        _logger = logger;
        _directoryFactory = directoryFactory;
    }

    public IDictionary<string, Directory> Directories => _directories.Value;

    public Directory GetDirectory(string indexName)
    {
        if (Directories.TryGetValue(indexName, out var directory))
        {
            return directory;
        }
        else
        {
            var newDirectory = _directoryFactory.GetIndexDirectory(indexName);
            Directories[indexName] = newDirectory;
            return newDirectory;
        }
    }

    public void DisposeDirectory(string indexName)
    {
        if (Directories.TryGetValue(indexName, out var directory))
        {
            directory.Dispose();
            Directories.Remove(indexName);
        }
    }
}