using System.Collections.Generic;
using System.Threading.Tasks;
using Lucene.Net.IndexProvider.Interfaces;
using Lucene.Net.IndexProvider.Tests.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lucene.Net.IndexProvider.Tests.Controllers;

[ApiController]
[Route("[controller]")]
public class BlogPostController : ControllerBase
{
    private readonly IIndexProvider _indexProvider;

    public BlogPostController(IIndexProvider indexProvider)
    {
        _indexProvider = indexProvider;
    }

    [HttpPost]
    public async Task<BlogPost> Post(BlogPost dto)
    {
        await _indexProvider.Store(new List<object>() { dto }, nameof(BlogPost));
        return dto;
    }

    [HttpGet]
    [Route("{id}")]
    public BlogPost Get(string id)
    {
        return _indexProvider.GetDocumentById<BlogPost>(id).Hit;
    }
}