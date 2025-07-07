using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.IndexProvider.Models;

public class LuceneSession
{
    public IndexWriter Writer { get; set; }
    public SearcherManager SearcherManager { get; set; }
}