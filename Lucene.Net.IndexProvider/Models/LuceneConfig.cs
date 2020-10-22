using Lucene.Net.Util;

namespace Lucene.Net.IndexProvider.Models
{
    public class LuceneConfig
    {
        public LuceneVersion LuceneVersion { get; set; }
        public int BatchSize { get; set; }
    }
}