using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lucene.Net.DocumentMapper.Helpers;
using Lucene.Net.Index;
using Lucene.Net.IndexProvider.Helpers;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Tests.Models;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lucene.Net.IndexProvider.Tests
{
    public class IndexProviderTests : IAsyncLifetime
    {
        private string _settingsPath;
        private string _indexPath;
        private IIndexProvider _indexProvider;
        private ServiceProvider _serviceProvider;
        private Mock<ILocalIndexPathFactory> _mockLocalIndexPathFactory;

        [Fact]
        public async Task Test_Paging_Functionality()
        {
            var pagedPosts =
                await _indexProvider.Search()
                    .Paged(2, 5)
                    .ListResult<BlogPost>();

            Assert.Equal(10, pagedPosts.Count);
            Assert.Equal("10", pagedPosts.Hits.Last().Hit.Id);
        }

        [Fact]
        public async Task Test_Get_All()
        {
            var listResult =
                await _indexProvider.Search()
                    .ListResult(typeof(BlogPost));

            Assert.Equal(10, listResult.Count);
        }

        [Fact]
        public async Task Test_Filter_On_List()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Should(() => new TermQuery(new Term("TagIds", "11")))
                    .ListResult<BlogPost>();

            Assert.Equal(1, taggedPost.Count);
            Assert.Equal("10", taggedPost.Hits[0].Hit.Id);
        }

        [Fact]
        public async Task Test_Return_Multiple_By_Id()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Should(() => new TermQuery(new Term("Id", "1")))
                    .Should(() => new TermQuery(new Term("Id", "2")))
                    .ListResult(typeof(BlogPost));

            Assert.Equal(2, taggedPost.Count);
        }

        [Fact]
        public async Task Test_Returns_Single_Result()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Must(() => new TermQuery(new Term("Id", "1")))
                    .SingleResult<BlogPost>();

            Assert.NotNull(taggedPost);
        }

        [Fact]
        public async Task Test_Returns_Single_Result_Object()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Must(() => new TermQuery(new Term("Id", "1")))
                    .SingleResult(typeof(BlogPost));

            Assert.NotNull(taggedPost);
        }

        [Fact]
        public async Task Test_Returns_Single_Result_Object_Null()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Must(() => new TermQuery(new Term("Id", "100")))
                    .SingleResult(typeof(BlogPost));

            Assert.Null(taggedPost);
        }

        [Fact]
        public async Task Test_Sort_By_Date_Asc()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Sort(() => new SortField("PublishedDate", SortFieldType.STRING, true))
                    .ListResult<BlogPost>();

            Assert.Equal("1", taggedPost.Hits.First().Hit.Id);
            Assert.Equal("3", taggedPost.Hits.Last().Hit.Id);
        }

        [Fact]
        public async Task Test_Sort_By_Date_Desc()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Sort(() => new SortField("PublishedDate", SortFieldType.STRING))
                    .ListResult<BlogPost>();

            Assert.Equal("3", taggedPost.Hits.First().Hit.Id);
            Assert.Equal("1", taggedPost.Hits.Last().Hit.Id);
        }

        [Fact]
        public async Task Test_Sort_By_DateTimeOffset_Desc()
        {
            var taggedPost =
                await _indexProvider.Search()
                    .Sort(() => new SortField(nameof(BlogPost.PublishedDateOffset), SortFieldType.STRING))
                    .ListResult<BlogPost>();

            Assert.Equal("3", taggedPost.Hits.First().Hit.Id);
            Assert.Equal("1", taggedPost.Hits.Last().Hit.Id);
        }

        [Fact]
        public async Task Test_Filter_Phrase()
        {
            var listResult =
                await _indexProvider.Search()
                    .Must(() =>
                    {
                        var query = new PhraseQuery()
                        {
                            new Term("Body", "My test body")
                        };

                        return query;
                    })
                    .ListResult<BlogPost>();

            Assert.Equal(1, listResult.Count);
            Assert.Equal("9", listResult.Hits[0].Hit.Id);
        }

        [Fact]
        public async Task Test_Filter_By_DateRange()
        {
            var listResult =
                await _indexProvider.Search()
                    .Must(() =>
                    {
                        var dateTimeOffset =
                            new DateTimeOffset(2023, 8, 22, 1, 0, 0, new TimeSpan(-5, 0, 0));

                        var dateString = dateTimeOffset.AddDays(-10).ToString("yyyyMMddHHmmss.fffzzzzz");
                        var upperDateString = dateTimeOffset.ToString("yyyyMMddHHmmss.fffzzzzz");
                        var rangeQuery =
                            TermRangeQuery.NewStringRange(
                                nameof(BlogPost.PublishedDateOffset), 
                                dateString, 
                                upperDateString, 
                                true,
                                true);

                        return rangeQuery;
                    })
                    .ListResult<BlogPost>();

            Assert.Equal(5, listResult.Count);
        }

        [Fact]
        public void Test_Get_Document_By_Id()
        {
            var blogPost = _indexProvider.GetDocumentById<BlogPost>("8");
            Assert.Equal("8", blogPost.Hit.Id);
        }

        [Fact]
        public void Test_Get_Document_By_Id_Not_Found()
        {
            var blogPost = _indexProvider.GetDocumentById<BlogPost>("13");
            Assert.Equal(null, blogPost.Hit);
        }

        [Fact]
        public async Task Test_Search_Nested_Properties()
        {
            var listResult =
                await _indexProvider.Search()
                    .Must(() => new TermQuery(new Term("Tags.Name", "my-test-tag")))
                    .ListResult<BlogPost>();

            Assert.Equal(1, listResult.Count);
            Assert.Equal("9", listResult.Hits[0].Hit.Id);
        }

        [Fact]
        public async Task Test_Fuzzy_Query()
        {
            var listResult =
                await _indexProvider.Search()
                    .Must(() => new FuzzyQuery(new Term("Body", "My tests body")))
                    .ListResult<BlogPost>();

            Assert.Equal(1, listResult.Count);
            Assert.Equal("9", listResult.Hits[0].Hit.Id);
        }

        [Fact]
        public async Task Test_Check_index()
        {
            var result = await _indexProvider.CheckHealth<BlogPost>();
            Assert.True(result);
        }

        public async Task InitializeAsync()
        {
            _settingsPath = Path.GetFullPath(Path.Combine($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}", @"..\..\..\settings"));
            _indexPath = $"{_settingsPath}\\PersonalBlog\\index";
            _mockLocalIndexPathFactory = new Mock<ILocalIndexPathFactory>();
            _mockLocalIndexPathFactory.Setup(x => x.GetLocalIndexPath())
                .Returns(_indexPath);

            var services = new ServiceCollection()
                .AddLuceneDocumentMapper()
                .AddLuceneProvider()
                .AddLogging(x => x.AddConsole());

            services.Add(new ServiceDescriptor(typeof(ILocalIndexPathFactory), _mockLocalIndexPathFactory.Object));
            _serviceProvider = services.BuildServiceProvider();

            var dateTime = DateTime.Now;
            _indexProvider = _serviceProvider.GetService<IIndexProvider>();
            await _indexProvider.CreateIndexIfNotExists(typeof(BlogPost));

            var dateTimeOffset = new DateTimeOffset(2023, 8, 22, 1, 0, 0, new TimeSpan(-5, 0, 0));

            await _indexProvider.Store(new List<BlogPost>
            {
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "1",
                    PublishedDate = DateTime.Now.AddDays(-1),
                    PublishedDateOffset = dateTimeOffset.AddDays(-1)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "2",
                    PublishedDate = DateTime.Now.AddDays(-100),
                    PublishedDateOffset = dateTimeOffset.AddDays(-100)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "3",
                    PublishedDate = DateTime.Now.AddDays(-120),
                    PublishedDateOffset = dateTimeOffset.AddDays(-120)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "4",
                    PublishedDate = DateTime.Now.AddDays(-11),
                    PublishedDateOffset = dateTimeOffset.AddDays(-11)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "5",
                    PublishedDate = DateTime.Now.AddDays(-19),
                    PublishedDateOffset = dateTimeOffset.AddDays(-19)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "6",
                    PublishedDate = DateTime.Now.AddDays(-13),
                    PublishedDateOffset = dateTimeOffset.AddDays(-13)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "7",
                    PublishedDate = DateTime.Now.AddDays(-7),
                    PublishedDateOffset = dateTimeOffset.AddDays(-7)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "8",
                    PublishedDate = DateTime.Now.AddDays(-8),
                    PublishedDateOffset = dateTimeOffset.AddDays(-8)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "9",
                    Body = "My test body",
                    Tags = new List<Tag>()
                    {
                        new Tag()
                        {
                            Id = "1",
                            Name = "my-test-tag"
                        }
                    },
                    PublishedDate = DateTime.Now.AddDays(-9),
                    PublishedDateOffset = dateTimeOffset.AddDays(-9)
                },
                new BlogPost
                {
                    Name = "My Test Blog Post",
                    Id = "10",
                    TagIds = new List<string>()
                    {
                        "11",
                        "2"
                    },
                    PublishedDate = DateTime.Now.AddDays(-10),
                    PublishedDateOffset = dateTimeOffset.AddDays(-10)
                }
            });
        }

        public async Task DisposeAsync()
        {
            await _indexProvider.DeleteIndex(nameof(BlogPost));
        }
    }
}