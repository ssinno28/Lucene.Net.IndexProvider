using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.Search;

namespace Lucene.Net.IndexProvider.FilterBuilder
{
    public class FilterBuilder
    {
        private readonly IIndexProvider _indexProvider;

        private readonly List<Filter> _filters = new List<Filter>();
        private int? _page = null;
        private int? _pageSize = null;

        public FilterBuilder(IIndexProvider indexProvider)
        {
            _indexProvider = indexProvider;
        }

        public FilterBuilder Must(Func<Query> getQueryAction)
        {
            _filters.Add(new Filter
            {
                OccurType = Occur.MUST,
                Query = getQueryAction()
            });

            return this;
        }        
        
        public FilterBuilder Should(Func<Query> getQueryAction)
        {
            _filters.Add(new Filter
            {
                OccurType = Occur.SHOULD,
                Query = getQueryAction()
            });

            return this;
        }        
        
        public FilterBuilder MustNot(Func<Query> getQueryAction)
        {
            _filters.Add(new Filter
            {
                OccurType = Occur.MUST_NOT,
                Query = getQueryAction()
            });

            return this;
        }

        public FilterBuilder Paged(int page, int pageSize)
        {
            _page = page;
            _pageSize = pageSize;
            return this;
        }

        public async Task<ListResult<T>> ListResult<T>()
        {
            return await _indexProvider.GetByFilters<T>(_filters, _page, _pageSize);
        }

        public async Task<ListResult> ListResult(Type contentType)
        {
            return await _indexProvider.GetByFilters(_filters, contentType, _page, _pageSize);
        }

        public async Task<bool> Any(Type contentType)
        {
            var result = await _indexProvider.GetByFilters(_filters, contentType, _page, _pageSize);
            return result != null && result.Hits.Any();
        }

        public async Task<bool> Any<T>()
        {
            var result = await _indexProvider.GetByFilters(_filters, typeof(T), _page, _pageSize);
            return result != null && result.Hits.Any();
        }
    }
}