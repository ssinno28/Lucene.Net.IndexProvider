using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Models;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Lucene.Net.IndexProvider.Helpers
{
    public static class ServiceCollectionHelpers
    {
        public static IServiceCollection AddLuceneProvider(this IServiceCollection services, string indexesPath)
        {
            var luceneConfig = new LuceneConfig
            {
                BatchSize = BooleanQuery.MaxClauseCount,
                LuceneVersion = LuceneVersion.LUCENE_48,
                Path = indexesPath
            };

            services.AddSingleton(luceneConfig);
            services.AddScoped<IIndexProvider, LuceneIndexProvider>();  

            return services;
        }
    }
}