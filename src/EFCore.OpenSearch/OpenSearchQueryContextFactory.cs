using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;

namespace EFCore.OpenSearch;

public class OpenSearchQueryContextFactory : IQueryContextFactory
{
    private readonly QueryContextDependencies _dependencies;
    private readonly IServiceProvider _serviceProvider;

    public OpenSearchQueryContextFactory(QueryContextDependencies dependencies, IServiceProvider serviceProvider)
    {
        _dependencies = dependencies;
        _serviceProvider = serviceProvider;
    }

    public QueryContext Create()
    {
        var client = _serviceProvider.GetRequiredService<OpenSearchClient>();
        var extension = _serviceProvider.GetRequiredService<DbContextOptions>().FindExtension<OpenSearchOptionsExtension>();
        return new OpenSearchQueryContext(_dependencies, client, extension.IndexPrefix);
    }
}
