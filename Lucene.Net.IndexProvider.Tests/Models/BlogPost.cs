using System;
using System.Collections.Generic;
using Lucene.Net.DocumentMapper.Attributes;

namespace Lucene.Net.IndexProvider.Tests.Models
{
    public class BlogPost
    {
        public string Id { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTimeOffset PublishedDateOffset { get; set; }
        public bool IsPublished { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public string SeoDescription { get; set; }
        public string SeoTitle { get; set; }
        public string Excerpt { get; set; }
        public string ThumbnailUrl { get; set; }
        public IList<string> TagIds { get; set; }
        public object Category { get; set; }
        public Category Category2 { get; set; }
        public IList<Tag> Tags { get; set; }
    }
}