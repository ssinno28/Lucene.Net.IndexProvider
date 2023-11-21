using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.IndexProvider.Models;

namespace Lucene.Net.IndexProvider.Interfaces;

public interface IIndexConfigurationManager
{
    List<LuceneConfig> Configurations { get; }
    void AddConfiguration(LuceneConfig config);
    LuceneConfig GetConfiguration(string indexName);
}