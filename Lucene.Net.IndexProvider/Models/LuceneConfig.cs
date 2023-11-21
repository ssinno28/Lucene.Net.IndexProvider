using System;
using System.Collections.Generic;
using Lucene.Net.Util;

namespace Lucene.Net.IndexProvider.Models
{
    public class LuceneConfig
    {
        public LuceneVersion LuceneVersion { get; set; }
        public int BatchSize { get; set; }
        public IList<string> Indexes { get; set; }
    }
}