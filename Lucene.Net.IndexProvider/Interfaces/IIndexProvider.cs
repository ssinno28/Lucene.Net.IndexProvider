using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lucene.Net.IndexProvider.FilterBuilder;

namespace Lucene.Net.IndexProvider.Interfaces
{
    public interface IIndexProvider
    {
        Task CreateIndexIfNotExists(Type contentType);
        Task CreateIndexIfNotExists(string indexName);
        Task DeleteIndex(string indexName);
        Task Store(IList<object> contentItems, Type contentType);
        Task Store<T>(IList<T> contentItems);
        Task Delete(string indexName, string documentId);
        Task<bool> Update(object contentItem, string id);
        object GetDocumentById(Type contentType, string id);
        T GetDocumentById<T>(string id);
        Task<bool> SwapIndex(string tempIndex, string index);
        FilterBuilder.FilterBuilder Search();
        Task<ListResult<T>> GetByFilters<T>(IList<Filter> filters, int? page = null, int? pageSize = null);
        Task<ListResult> GetByFilters(IList<Filter> filters, Type contentType, int? page = null, int? pageSize = null);
    }
}