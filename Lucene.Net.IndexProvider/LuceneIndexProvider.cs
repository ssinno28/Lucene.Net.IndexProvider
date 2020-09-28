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

        private Directory GetDirectory(string indexName)
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(_luceneConfig.Path, indexName));
            return FSDirectory.Open(directoryInfo);
        }

        /// <summary>
        /// Gets a single document for the supplied key
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public IndexResult<object> GetDocumentById(Type contentType, string id)
        {
            var directory = GetDirectory(contentType.Name);
            IndexResult<object> indexResult = null;

            try
            {
                using (var indexReader = DirectoryReader.Open(directory))
                {
                    var indexSearcher = new IndexSearcher(indexReader);

                    var query = new TermQuery(new Term(GetKeyName(contentType), id));
                    var hits = indexSearcher.Search(query, _luceneConfig.BatchSize);

                    var doc = indexSearcher.Doc(hits.ScoreDocs[0].Doc);
                    indexResult = new IndexResult<object>
                    {
                        Hit = _mapper.Map(doc, contentType),
                        Score = hits.ScoreDocs[0].Score
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not get document for id {0} and content type", id, contentType.Name);
            }

            return indexResult;
        }

        /// <summary>
        /// Gets a single document for the supplied key and casts it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public IndexResult<T> GetDocumentById<T>(string id)
        {
            var indexResult = GetDocumentById(typeof(T), id);
            return new IndexResult<T>
            {
                Hit = (T)indexResult.Hit,
                Score = indexResult.Score
            };
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

        /// <summary>
        /// Swaps a newly created index with an older one
        /// </summary>
        /// <param name="tempIndex"></param>
        /// <param name="index"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Exposes a fluent api for searching against th index
        /// </summary>
        /// <returns></returns>
        public FilterBuilder.FilterBuilder Search()
        {
            return new FilterBuilder.FilterBuilder(this);
        }

        public async Task<ListResult<T>> GetByFilters<T>(IList<Filter> filters, int? page = null, int? pageSize = null)
        {
            var contentType = typeof(T);
            var listResult = await GetByFilters(filters, contentType, page, pageSize);

            IList<IndexResult<T>> indexResults = new List<IndexResult<T>>();
            foreach (var listResultItem in listResult.Hits)
            {
                indexResults.Add(new IndexResult<T>()
                {
                    Hit = (T)listResultItem.Hit,
                    Score = listResultItem.Score
                });
            }

            return new ListResult<T>
            {
                Count = listResult.Count,
                Hits = indexResults,
                MaxScore = listResult.MaxScore
            };
        }


        public async Task<ListResult> GetByFilters(IList<Filter> filters, Type contentType, int? page = null, int? pageSize = null)
        {
            var directory = GetDirectory(contentType.Name);
            List<IndexResult<object>> indexResults = new List<IndexResult<object>>();

            return await Task.Run(() =>
            {
                int count;
                float maxScore;
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
                    maxScore = hits.MaxScore;

                    if (page.HasValue && pageSize.HasValue)
                    {
                        int first = (int)((page - 1) * pageSize);
                        int last = (int)(page * pageSize);

                        for (int i = first; i < last && i < hits.TotalHits; i++)
                        {
                            Document doc = indexSearcher.Doc(hits.ScoreDocs[i].Doc);
                            var contentItem = _mapper.Map(doc, contentType);
                            indexResults.Add(new IndexResult<object>
                            {
                                Hit = contentItem,
                                Score = hits.ScoreDocs[i].Score
                            });
                        }
                    }
                    else
                    {
                        for (int i = 0; i < hits.TotalHits; i++)
                        {
                            Document doc = indexSearcher.Doc(hits.ScoreDocs[i].Doc);
                            var contentItem = _mapper.Map(doc, contentType);
                            indexResults.Add(new IndexResult<object>
                            {
                                Hit = contentItem,
                                Score = hits.ScoreDocs[i].Score
                            });
                        }
                    }
                }

                return new ListResult
                {
                    Hits = indexResults,
                    Count = count,
                    MaxScore = maxScore
                };
            });
        }

        /// <summary>
        /// Updates a single document based on the key supplied
        /// </summary>
        /// <param name="contentItem"></param>
        /// <param name="id"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates the index for a given type if it doesn't exist
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public Task CreateIndexIfNotExists(Type contentType)
        {
            return CreateIndexIfNotExists(contentType.Name);
        }

        /// <summary>
        /// Creates the index for a given type if it doesn't exist
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Deletes an index for the given type
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public Task DeleteIndex(string indexName)
        {
            return Task.Run(() =>
            {
                var directory = new DirectoryInfo(Path.Combine(_luceneConfig.Path, indexName));
                if (!directory.Exists) return;

                directory.Delete(true);
            });
        }

        /// <summary>
        /// Takes a list of objects and stores them to the same index
        /// </summary>
        /// <param name="contentItems"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Takes a list of objects and stores them in the same index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contentItems"></param>
        /// <returns></returns>
        public Task Store<T>(IList<T> contentItems)
        {
            return Store(contentItems.Cast<object>().ToList(), typeof(T));
        }

        /// <summary>
        /// Deletes a document for the given key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public Task Delete<T>(string documentId)
        {
            return Delete(typeof(T), documentId);
        }

        /// <summary>
        /// Deletes a document for the given key
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="documentId"></param>
        /// <returns></returns>
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
