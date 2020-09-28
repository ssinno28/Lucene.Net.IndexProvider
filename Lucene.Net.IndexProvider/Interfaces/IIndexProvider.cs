using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lucene.Net.IndexProvider.FilterBuilder;
using Lucene.Net.IndexProvider.Models;

namespace Lucene.Net.IndexProvider.Interfaces
{
    public interface IIndexProvider
    {
        Task CreateIndexIfNotExists(Type contentType);
        Task CreateIndexIfNotExists(string indexName);
        Task DeleteIndex(string indexName);
        Task Store(IList<object> contentItems, Type contentType);
        Task Store(IList<object> contentItems, string indexName);
        Task Store<T>(IList<T> contentItems);
        Task Delete(Type contentType, string documentId);
        Task Delete<T>(string documentId);
        Task<bool> Update(object contentItem, string id);
        IndexResult<object> GetDocumentById(Type contentType, string id);
        IndexResult<T> GetDocumentById<T>(string id);
        Task<bool> SwapIndex(string tempIndex, string index);
        FilterBuilder.FilterBuilder Search();
        Task<ListResult<T>> GetByFilters<T>(IList<Filter> filters, IList<Sort> sorts, int? page = null, int? pageSize = null);
        Task<ListResult> GetByFilters(IList<Filter> filters, IList<Sort> sorts, Type contentType, int? page = null, int? pageSize = null);
    }
}