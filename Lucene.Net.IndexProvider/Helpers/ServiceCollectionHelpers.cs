using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lucene.Net.IndexProvider.Helpers
{
    public static class ServiceCollectionHelpers
    {
        public static IServiceCollection AddLuceneProvider(this IServiceCollection services)
        {
            services.TryAddSingleton<IIndexSessionManager, IndexSessionManager>();
            services.TryAddSingleton<IIndexConfigurationManager, IndexConfigurationManager>();

            services.AddScoped<IIndexProvider, LuceneIndexProvider>();  

            return services;
        }
    }
}