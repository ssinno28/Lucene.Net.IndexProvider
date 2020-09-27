using System.Collections.Generic;

namespace Lucene.Net.IndexProvider.FilterBuilder
{
    public class ListResult<T>
    {
        public IList<T> Items { get; set; }
        public int Count { get; set; }
    }
    
    public class ListResult
    {
        public IList<object> Items { get; set; }
        public int Count { get; set; }
    }
}