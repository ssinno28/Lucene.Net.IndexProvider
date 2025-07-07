using System.Threading.Tasks;
using Lucene.Net.IndexProvider.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lucene.Net.IndexProvider.Middleware
{
    public class CloseIndexSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IIndexSessionManager _sessionManager;
        private readonly IIndexConfigurationManager _configurationManager;
        private readonly ILogger<CloseIndexSessionMiddleware> _logger;

        public CloseIndexSessionMiddleware(RequestDelegate next, IIndexSessionManager sessionManager, IIndexConfigurationManager configurationManager, ILogger<CloseIndexSessionMiddleware> logger)
        {
            _next = next;
            _sessionManager = sessionManager;
            _configurationManager = configurationManager;
            _logger = logger;
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
                    foreach (var index in config.Indexes)
                    {
                        _sessionManager.Commit(index);
                        _logger.LogInformation($"Closed session for index {index}");
                    }
                }
            }
        }
    }
}