using System.Collections.Generic;
using Lucene.Net.IndexProvider.Models;

namespace Lucene.Net.IndexProvider.FilterBuilder
{
    public class IndexListResult<T>
    {
        public IList<IndexResult<T>> Hits { get; set; }
        public int Count { get; set; }
        public float MaxScore { get; set; }
    }
    
    public class IndexListResult
    {
        public IList<IndexResult<object>> Hits { get; set; }
        public int Count { get; set; }
        public float MaxScore { get; set; }
    }
}