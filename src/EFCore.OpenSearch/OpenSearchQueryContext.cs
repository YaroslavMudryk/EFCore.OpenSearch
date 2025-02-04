using Microsoft.EntityFrameworkCore.Query;
using OpenSearch.Client;

namespace EFCore.OpenSearch;

public class OpenSearchQueryContext : QueryContext
{
    public OpenSearchClient Client { get; }
    public string IndexName { get; }

    public OpenSearchQueryContext(QueryContextDependencies dependencies, OpenSearchClient client, string indexName)
        : base(dependencies)
    {
        Client = client;
        IndexName = indexName;
    }
}
