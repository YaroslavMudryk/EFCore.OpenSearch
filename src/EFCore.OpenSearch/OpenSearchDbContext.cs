using Microsoft.EntityFrameworkCore;
using OpenSearch.Client;

namespace EFCore.OpenSearch;

public class OpenSearchDbContext : DbContext
{
    private readonly OpenSearchClient _client;
    private readonly Dictionary<Type, string> _indexMappings = new();

    public OpenSearchDbContext(DbContextOptions options) : base(options)
    {
        var extension = options.FindExtension<OpenSearchOptionsExtension>();
        _client = extension?.Client ?? throw new ArgumentNullException(nameof(OpenSearchClient), "OpenSearchClient was not provided.");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entityTypeName = entityType.ClrType;
            var indexName = entityType.GetAnnotation("OpenSearch:IndexName")?.Value as string ?? entityTypeName.Name.ToLower();
            _indexMappings[entityTypeName] = indexName;
        }

        base.OnModelCreating(modelBuilder);
    }

    public OpenSearchQueryable<T> Set<T>() where T : class
    {
        if (!_indexMappings.TryGetValue(typeof(T), out var indexName))
        {
            throw new InvalidOperationException($"Index not configured for entity {typeof(T).Name}");
        }
        return new OpenSearchQueryable<T>(new OpenSearchQueryProvider<T>(_client, indexName));
    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int changes = 0;

        var trackedEntities = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in trackedEntities)
        {
            var entity = entry.Entity;
            var indexName = _indexMappings[entity.GetType()];

            switch (entry.State)
            {
                case EntityState.Added:
                    var addResponse = await _client.IndexDocumentAsync(entity);
                    if (addResponse.IsValid) changes++;
                    break;

                case EntityState.Modified:
                    var updateResponse = await _client.UpdateAsync<object>(new DocumentPath<object>(entity), u => u.Doc(entity));
                    if (updateResponse.IsValid) changes++;
                    break;

                case EntityState.Deleted:
                    var deleteResponse = await _client.DeleteAsync<object>(new DocumentPath<object>(entity));
                    if (deleteResponse.IsValid) changes++;
                    break;
            }
        }

        return changes;
    }
}
