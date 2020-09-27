using Lucene.Net.Util;

namespace Lucene.Net.IndexProvider.Models
{
    public class LuceneConfig
    {
        public string Path { get; set; }
        public LuceneVersion LuceneVersion { get; set; }
        public int BatchSize { get; set; }
    }
}