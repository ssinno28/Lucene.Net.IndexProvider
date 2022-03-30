# Lucene.Net.IndexProvider
![CI](https://github.com/ssinno28/Lucene.Net.IndexProvider/workflows/CI/badge.svg)

A simple service that helps to abstract common operations when interacting with lucene.net indexes. The index provider exposes methods for managing indexes (creating, deleting and swapping) as well as CRUD operations. 

It will need to be wired up with DI like so:

```c#
            services.AddLuceneProvider();
            services.AddLuceneDocumentMapper();
```

You'll also want to setup a LocalIndexPathFactory class that implements `ILocalIndexPathFactory` and add it as an injected service.

In order to query against the index there is a fluent API that allows you to pass in as many query types you want:

```c#
 var listResult =
                await _indexProvider.Search()
                    .Must(() => new TermQuery(new Term("Tags.Name", "my-test-tag")))
                    .ListResult<BlogPost>();
```

You can concatenate as many Must, Should and MustNot queries as you would like and call ListResults at the end to get the items and count.


