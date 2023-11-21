# Lucene.Net.IndexProvider
![CI](https://github.com/ssinno28/Lucene.Net.IndexProvider/workflows/CI/badge.svg)

A simple service that helps to abstract common operations when interacting with lucene.net indexes. The index provider exposes methods for managing indexes (creating, deleting and swapping) as well as CRUD operations. 

It will need to be wired up with DI like so:

```c#
            services.AddLuceneProvider();
            services.AddLuceneDocumentMapper();
            
            services.AddSingleton<ILocalIndexPathFactory, LocalIndexPathFactory>();
```

In the configure app method in the Startup.cs file you will need to set up a config for each index and add the close session middleware: 

```c#
varr indexConfigManager = serviceProvider.GetService<IIndexConfigurationManager>();
            indexConfigManager.AddConfiguration(new LuceneConfig()
            {
                BatchSize = 500000,
                LuceneVersion = LuceneVersion.LUCENE_48,
                IndexTypes = new List<Type>()
                {
                    nameof(BlogPost),
                }
            });

            app.UseMiddleware<CloseIndexSessionMiddleware>();
```

You'll also want to setup a LocalIndexPathFactory class that implements `ILocalIndexPathFactory` and add it as an injected service.

```c#
    public class LocalIndexPathFactory : ILocalIndexPathFactory
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LocalIndexPathFactory(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public string GetLocalIndexPath()
        {
            return Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data", "index");
        }
    }
```

In order to query against the index there is a fluent API that allows you to pass in as many query types you want:

```c#
 var listResult =
                await _indexProvider.Search()
                    .Must(() => new TermQuery(new Term("Tags.Name", "my-test-tag")))
                    .ListResult<BlogPost>();
```

You can concatenate as many Must, Should and MustNot queries as you would like and call ListResults at the end to get the items and count.

