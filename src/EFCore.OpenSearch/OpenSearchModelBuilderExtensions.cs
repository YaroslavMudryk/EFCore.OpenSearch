using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EFCore.OpenSearch;

public static class OpenSearchModelBuilderExtensions
{
    public static EntityTypeBuilder<T> ToOpenSearchIndex<T>(this EntityTypeBuilder<T> builder, string indexName) where T : class
    {
        builder.HasAnnotation("OpenSearch:IndexName", indexName);
        return builder;
    }
}
