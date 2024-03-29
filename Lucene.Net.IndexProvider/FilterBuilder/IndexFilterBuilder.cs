﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Models;
using Lucene.Net.Search;

namespace Lucene.Net.IndexProvider.FilterBuilder
{
    public class IndexFilterBuilder
    {
        private readonly IIndexProvider _indexProvider;

        private readonly List<IndexFilter> _filters = new List<IndexFilter>();
        private readonly List<Sort> _sorts = new List<Sort>();

        private int? _page = null;
        private int? _pageSize = null;

        public IndexFilterBuilder(IIndexProvider indexProvider)
        {
            _indexProvider = indexProvider;
        }

        public IndexFilterBuilder Must(Func<Query> getQueryAction)
        {
            _filters.Add(new IndexFilter
            {
                OccurType = Occur.MUST,
                Query = getQueryAction()
            });

            return this;
        }        
        
        public IndexFilterBuilder Should(Func<Query> getQueryAction)
        {
            _filters.Add(new IndexFilter
            {
                OccurType = Occur.SHOULD,
                Query = getQueryAction()
            });

            return this;
        }        
        
        public IndexFilterBuilder MustNot(Func<Query> getQueryAction)
        {
            _filters.Add(new IndexFilter
            {
                OccurType = Occur.MUST_NOT,
                Query = getQueryAction()
            });

            return this;
        }

        public IndexFilterBuilder Sort(Func<SortField> getSortFunc)
        {
            _sorts.Add(new Sort()
            {
                SortField = getSortFunc()
            });

            return this;
        }

        public IndexFilterBuilder Paged(int page, int pageSize)
        {
            _page = page;
            _pageSize = pageSize;
            return this;
        }

        public async Task<IndexListResult<T>> ListResult<T>()
        {
            return await _indexProvider.GetByFilters<T>(_filters, _sorts, _page, _pageSize);
        }    
        
        public async Task<IndexResult<T>> SingleResult<T>()
        {
            var results = await _indexProvider.GetByFilters<T>(_filters, _sorts, _page, _pageSize);
            return results.Hits.FirstOrDefault();
        }        
        
        public async Task<IndexResult<object>> SingleResult(Type contentType)
        {
            var results = await _indexProvider.GetByFilters(_filters, _sorts, contentType, _page, _pageSize);
            return results.Hits.FirstOrDefault();
        }

        public async Task<IndexListResult> ListResult(Type contentType)
        {
            return await _indexProvider.GetByFilters(_filters, _sorts, contentType, _page, _pageSize);
        }

        public async Task<bool> Any(Type contentType)
        {
            var result = await _indexProvider.GetByFilters(_filters, _sorts, contentType, _page, _pageSize);
            return result != null && result.Hits.Any();
        }

        public async Task<bool> Any<T>()
        {
            var result = await _indexProvider.GetByFilters(_filters, _sorts, typeof(T), _page, _pageSize);
            return result != null && result.Hits.Any();
        }
    }
}