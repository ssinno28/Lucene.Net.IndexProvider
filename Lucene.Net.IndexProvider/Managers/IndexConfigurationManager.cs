using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Models;

namespace Lucene.Net.IndexProvider.Managers;

public class IndexConfigurationManager : IIndexConfigurationManager
{
    private readonly Lazy<List<LuceneConfig>> _configurations =
        new Lazy<List<LuceneConfig>>(() => new List<LuceneConfig>());

    private readonly ILoggerFactory _loggerFactory;

    public IndexConfigurationManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    private ILogger Logger => CreateLogger<IndexConfigurationManager>();

    private ILogger CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<IndexConfigurationManager>();
    }

    public List<LuceneConfig> Configurations => _configurations.Value;

    public void AddConfiguration(LuceneConfig config)
    {
        Configurations.Add(config);
    }

    public LuceneConfig GetConfiguration(string indexName)
    {
        var config = Configurations.First(x => x.Indexes.Any(t => t.Equals(indexName)));
        return config;
    }
}