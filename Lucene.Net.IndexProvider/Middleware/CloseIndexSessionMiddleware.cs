using System.Threading.Tasks;
using Lucene.Net.IndexProvider.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Lucene.Net.IndexProvider.Middleware
{
    public class CloseIndexSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IIndexSessionManager _sessionManager;
        private readonly IIndexConfigurationManager _configurationManager;

        public CloseIndexSessionMiddleware(RequestDelegate next, IIndexSessionManager sessionManager, IIndexConfigurationManager configurationManager)
        {
            _next = next;
            _sessionManager = sessionManager;
            _configurationManager = configurationManager;
        }

        public Task Invoke(HttpContext context) => InvokeAsync(context); // Stops VS from nagging about async method without ...Async suffix.

        async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                foreach (var config in _configurationManager.Configurations)
                {
                    foreach (var configIndexType in config.IndexTypes)
                    {
                        _sessionManager.CloseSessionOn(configIndexType.Name);
                    }
                }
            }
        }
    }
}