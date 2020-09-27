using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.DocumentMapper.Attributes;
using Lucene.Net.DocumentMapper.Interfaces;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.IndexProvider.FilterBuilder;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Models;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.Extensions.Logging;
using Directory = Lucene.Net.Store.Directory;
using Filter = Lucene.Net.IndexProvider.FilterBuilder.Filter;

namespace Lucene.Net.IndexProvider
{
    /// <summary>
    /// Represents the default implementation of an IIndexProvider, based on Lucene
    /// </summary>
    public class LuceneIndexProvider : IIndexProvider
    {
        private readonly LuceneConfig _luceneConfig;
        private readonly IDocumentMapper _mapper;
        private readonly ILogger<LuceneIndexProvider> _logger;

        public LuceneIndexProvider(LuceneConfig luceneConfig, IDocumentMapper mapper, ILoggerFactory loggerFactory)
        {
            _luceneConfig = luceneConfig;
            _mapper = mapper;
            _logger = loggerFactory.CreateLogger<LuceneIndexProvider>();

            // Ensures the directory exists
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (string.IsNullOrEmpty(_luceneConfig.Path)) return;

            var directory = new DirectoryInfo(_luceneConfig.Path);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }

        public Directory GetDirectory(string indexName)
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(_luceneConfig.Path, indexName));
            return FSDirectory.Open(directoryInfo);
        }

        public object GetDocumentById(Type contentType, string id)
        {
            var directory = GetDirectory(contentType.Name);
            object contentItem = null;

            try
            {
                using (var indexReader = DirectoryReader.Open(directory))
                {
                    var indexSearcher = new IndexSearcher(indexReader);

                    var query = new TermQuery(new Term(GetKeyName(contentType), id));
                    var hits = indexSearcher.Search(query, _luceneConfig.BatchSize);

                    var doc = indexSearcher.Doc(hits.ScoreDocs[0].Doc);
                    contentItem = _mapper.Map(doc, contentType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get document for id {0} and content type", id, contentType.Name);
            }

            return contentItem;
        }

        public T GetDocumentById<T>(string id)
        {
            return (T)GetDocumentById(typeof(T), id);
        }

        private string GetKeyName(Type contentType)
        {
            var keyProperty =
                contentType.GetProperties()
                    .FirstOrDefault(x =>
                        x.GetCustomAttribute<SearchAttribute>() != null &&
                        x.GetCustomAttribute<SearchAttribute>().IsKey
                    );

            return keyProperty == null ? "Id" : keyProperty.Name;
        }

        public Task<bool> SwapIndex(string tempIndex, string index)
        {
            return Task.Run(async () =>
            {
                string tempIndexPath = Path.Combine(_luceneConfig.Path, tempIndex);
                string indexPath = Path.Combine(_luceneConfig.Path, index);

                if (!System.IO.Directory.Exists(tempIndexPath))
                {
                    _logger.LogInformation("The index to be swapped {0} does not exist", index);
                    return false;
                }

                await DeleteIndex(index);
                System.IO.Directory.Move(tempIndexPath, indexPath);

                return true;
            });
        }

        public FilterBuilder.FilterBuilder Search()
        {
            return new FilterBuilder.FilterBuilder(this);
        }

        public async Task<ListResult<T>> GetByFilters<T>(IList<Filter> filters, int? page = null, int? pageSize = null)
        {
            var contentType = typeof(T);
            var listResult = await GetByFilters(filters, contentType, page, pageSize);

            return new ListResult<T>
            {
                Count = listResult.Count,
                Items = listResult.Items.Cast<T>().ToList()
            };
        }


        public async Task<ListResult> GetByFilters(IList<Filter> filters, Type contentType, int? page = null, int? pageSize = null)
        {
            var directory = GetDirectory(contentType.Name);
            List<object> contentItems = new List<object>();

            return await Task.Run(() =>
            {
                int count;
                using (var indexReader = DirectoryReader.Open(directory))
                {
                    var indexSearcher = new IndexSearcher(indexReader);
                    Query query = new MatchAllDocsQuery();

                    if (filters.Any())
                    {
                        query = new BooleanQuery();
                        foreach (var filter in filters)
                        {
                            ((BooleanQuery)query).Add(filter.Query, filter.OccurType);
                        }
                    }

                    var hits = indexSearcher.Search(query, _luceneConfig.BatchSize);
                    count = hits.TotalHits;

                    if (page.HasValue && pageSize.HasValue)
                    {
                        int first = (int)((page - 1) * pageSize);
                        int last = (int)(page * pageSize);

                        for (int i = first; i < last && i < hits.TotalHits; i++)
                        {
                            Document doc = indexSearcher.Doc(hits.ScoreDocs[i].Doc);
                            var contentItem = _mapper.Map(doc, contentType);
                            contentItems.Add(contentItem);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < hits.TotalHits; i++)
                        {
                            Document doc = indexSearcher.Doc(hits.ScoreDocs[i].Doc);
                            var contentItem = _mapper.Map(doc, contentType);
                            contentItems.Add(contentItem);
                        }
                    }
                }

                return new ListResult
                {
                    Items = contentItems,
                    Count = count
                };
            });
        }

        public Task<bool> Update(object contentItem, string id)
        {
            string indexName = contentItem.GetType().Name;
            return Task.Run(() =>
            {
                try
                {
                    using (var analyzer = new StandardAnalyzer(_luceneConfig.LuceneVersion))
                    {
                        var config = new IndexWriterConfig(_luceneConfig.LuceneVersion, analyzer);
                        using (var writer = new IndexWriter(GetDirectory(indexName), config))
                        {
                            var doc = _mapper.Map(contentItem);
                            writer.UpdateDocument(new Term(GetKeyName(contentItem.GetType()), id), doc);

                            if (writer.HasDeletions())
                            {
                                writer.ForceMergeDeletes();
                            }
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not update content item {0}", id);
                    return false;
                }
            });
        }

        public Task CreateIndexIfNotExists(Type contentType)
        {
            return CreateIndexIfNotExists(contentType.Name);
        }

        public Task CreateIndexIfNotExists(string indexName)
        {
            bool exists = DirectoryReader.IndexExists(GetDirectory(indexName));
            if (exists) return Task.FromResult(0);

            return Task.Run(() =>
            {
                using (var analyzer = new StandardAnalyzer(_luceneConfig.LuceneVersion))
                {
                    var config = new IndexWriterConfig(_luceneConfig.LuceneVersion, analyzer);
                    using (new IndexWriter(GetDirectory(indexName), config))
                    {
                    }
                }
            });
        }

        public Task DeleteIndex(string indexName)
        {
            return Task.Run(() =>
            {
                var directory = new DirectoryInfo(Path.Combine(_luceneConfig.Path, indexName));
                if (!directory.Exists) return;

                directory.Delete(true);
            });
        }

        public Task Store(IList<object> contentItems, Type contentType)
        {
            contentItems = contentItems.ToArray();

            if (!contentItems.Any())
            {
                return Task.FromResult(0);
            }

            return Task.Run(() =>
            {
                using (var analyzer = new StandardAnalyzer(_luceneConfig.LuceneVersion))
                {
                    var config = new IndexWriterConfig(_luceneConfig.LuceneVersion, analyzer);
                    using (var writer = new IndexWriter(GetDirectory(contentType.Name), config))
                    {
                        foreach (var contentItem in contentItems)
                        {
                            try
                            {
                                var doc = _mapper.Map(contentItem);
                                writer.AddDocument(doc);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, $"Could not add document to index");
                            }
                        }
                    }
                }
            });
        }

        public Task Store<T>(IList<T> contentItems)
        {
            return Store(contentItems.Cast<object>().ToList(), typeof(T));
        }

        public Task Delete<T>(string documentId)
        {
            return Delete(typeof(T), documentId);
        }

        public Task Delete(Type contentType, string documentId)
        {
            return Task.Run(() =>
            {
                using (var analyzer = new StandardAnalyzer(_luceneConfig.LuceneVersion))
                {
                    var config = new IndexWriterConfig(_luceneConfig.LuceneVersion, analyzer);
                    using (var writer = new IndexWriter(GetDirectory(contentType.Name), config))
                    {
                        writer.DeleteDocuments(new Term(GetKeyName(contentType), documentId));
                        if (writer.HasDeletions())
                        {
                            writer.ForceMergeDeletes();
                        }
                    }
                }
            });
        }
    }
}
