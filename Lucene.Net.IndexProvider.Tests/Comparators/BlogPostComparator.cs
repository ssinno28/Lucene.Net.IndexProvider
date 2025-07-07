using System;
using System.Collections.Generic;
using Lucene.Net.IndexProvider.Tests.Models;

namespace Lucene.Net.IndexProvider.Tests.Comparators;

public class BlogPostComparator : IComparer<BlogPost>
{
    public int Compare(BlogPost x, BlogPost y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (y is null) return 1;
        if (x is null) return -1;
        var idComparison = string.Compare(x.Id, y.Id, StringComparison.Ordinal);
        if (idComparison != 0) return idComparison;
        var isPublishedComparison = x.IsPublished.CompareTo(y.IsPublished);
        if (isPublishedComparison != 0) return isPublishedComparison;
        var nameComparison = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;
        var bodyComparison = string.Compare(x.Body, y.Body, StringComparison.Ordinal);
        if (bodyComparison != 0) return bodyComparison;
        var seoDescriptionComparison = string.Compare(x.SeoDescription, y.SeoDescription, StringComparison.Ordinal);
        if (seoDescriptionComparison != 0) return seoDescriptionComparison;
        var seoTitleComparison = string.Compare(x.SeoTitle, y.SeoTitle, StringComparison.Ordinal);
        if (seoTitleComparison != 0) return seoTitleComparison;
        var excerptComparison = string.Compare(x.Excerpt, y.Excerpt, StringComparison.Ordinal);
        if (excerptComparison != 0) return excerptComparison;
        return string.Compare(x.ThumbnailUrl, y.ThumbnailUrl, StringComparison.Ordinal);
    }
}