using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;

namespace EFCore.OpenSearch;

public static class OpenSearchDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseOpenSearch(
        this DbContextOptionsBuilder optionsBuilder,
        OpenSearchClient client,
        string indexPrefix = "")
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        optionsBuilder.ReplaceService<IModelCacheKeyFactory, OpenSearchModelCacheKeyFactory>();
        optionsBuilder.ReplaceService<IQueryCompiler, OpenSearchQueryCompiler>();
        optionsBuilder.ReplaceService<IQueryContextFactory, OpenSearchQueryContextFactory>();

        optionsBuilder.UseInternalServiceProvider(
            new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<IQueryContextFactory, OpenSearchQueryContextFactory>()
                .BuildServiceProvider()
        );

        var extension = new OpenSearchOptionsExtension(client, indexPrefix);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }
}
