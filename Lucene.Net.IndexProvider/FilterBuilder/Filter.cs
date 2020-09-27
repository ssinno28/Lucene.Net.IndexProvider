using Lucene.Net.Search;

namespace Lucene.Net.IndexProvider.FilterBuilder
{
    public class Filter
    {
        public Query Query { get; set; }
        public Occur OccurType { get; set; }
    }
}