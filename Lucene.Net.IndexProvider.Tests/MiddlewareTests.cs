using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using Lucene.Net.DocumentMapper.Helpers;
using Lucene.Net.IndexProvider.Helpers;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Middleware;
using Lucene.Net.IndexProvider.Models;
using Lucene.Net.IndexProvider.Tests.Models;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lucene.Net.IndexProvider.Tests;

public class MiddlewareTests
{
    private readonly TestServer _testServer;
    private readonly IFixture _fixture = new Fixture();
    private string _settingsPath;
    private string _indexPath;
    private Mock<ILocalIndexPathFactory> _mockLocalIndexPathFactory;
    private IIndexProvider _indexProvider;

    public MiddlewareTests()
    {
        _settingsPath = Path.GetFullPath(Path.Combine($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}", @"..\..\..\settings"));
        _indexPath = $"{_settingsPath}\\PersonalBlog\\index";
        _mockLocalIndexPathFactory = new Mock<ILocalIndexPathFactory>();
        _mockLocalIndexPathFactory.Setup(x => x.GetLocalIndexPath())
            .Returns(_indexPath);

        var host = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services 
                    .AddLuceneDocumentMapper()
                    .AddLuceneProvider()
                    .AddRouting()
                    .AddLogging(x => x.AddConsole())
                    .AddHttpContextAccessor()
                    .AddControllers();

                services.Add(new ServiceDescriptor(typeof(ILocalIndexPathFactory), _mockLocalIndexPathFactory.Object));
            })
            .Configure(app =>
            {
                var configManager = app.ApplicationServices.GetService<IIndexConfigurationManager>();
                configManager.AddConfiguration(new LuceneConfig()
                {
                    Indexes = new[] { nameof(BlogPost) },
                    BatchSize = 50000,
                    LuceneVersion = LuceneVersion.LUCENE_48
                });

                _indexProvider = app.ApplicationServices.GetService<IIndexProvider>();

                app.UseMiddleware<CloseIndexSessionMiddleware>();
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            });

        _testServer = new TestServer(host);
    }

    [Fact]
    public async Task BlogPostApi_Post_ProperlyClosesSession()
    {
        var blogPostDto = _fixture.Create<BlogPost>();

        var payload = JsonSerializer.Serialize(blogPostDto);
        var testClient = _testServer.CreateClient();
        var response = 
            await testClient.PostAsync("/blogpost", new StringContent(payload, Encoding.UTF8, "application/json"));

        var sessionManager = _testServer.Services.GetService<IIndexSessionManager>();
        Assert.Equal(0, sessionManager.ContextSessions.Count);

        var blogPost = _indexProvider.GetDocumentById(typeof(BlogPost), blogPostDto.Id);
        Assert.NotNull(blogPost.Hit);
    }
}