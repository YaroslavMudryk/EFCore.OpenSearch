using OpenSearch.Client;

namespace EFCore.OpenSearch;

public static class OpenSearchQueryExtensions
{
    public static IQueryable<T> WithQuery<T>(this IQueryable<T> source, QueryContainer query)
    {
        if (source is OpenSearchQueryable<T> openSearchQueryable)
        {
            openSearchQueryable.SetExternalQuery(query);
            return openSearchQueryable;
        }

        throw new NotSupportedException("WithQuery can only be used on OpenSearchQueryable<T>.");
    }
}
