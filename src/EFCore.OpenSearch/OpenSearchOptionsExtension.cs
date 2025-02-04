using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;

namespace EFCore.OpenSearch;

public class OpenSearchOptionsExtension : IDbContextOptionsExtension
{
    public OpenSearchClient Client { get; }
    public string IndexPrefix { get; }

    public OpenSearchOptionsExtension(OpenSearchClient client, string indexPrefix)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        IndexPrefix = indexPrefix;
    }

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(Client);
    }

    public long GetServiceProviderHashCode() => Client.GetHashCode() ^ IndexPrefix.GetHashCode();
    public void Validate(IDbContextOptions options) { }
    public string LogFragment => $"Using OpenSearch at {Client.ConnectionSettings.ConnectionPool.Nodes.FirstOrDefault()?.Uri}";

    public DbContextOptionsExtensionInfo Info => throw new NotImplementedException();
}
