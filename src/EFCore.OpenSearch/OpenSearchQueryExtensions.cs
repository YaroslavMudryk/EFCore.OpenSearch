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

    public static Task<TAggregationResult> AggregateAsync<T, TAggregationResult>(
        this IQueryable<T> source,
        AggregationDictionary aggregationDictionary,
        CancellationToken cancellationToken = default)
        where TAggregationResult : class, new()
    {
        if (source is OpenSearchQueryable<T> openSearchQueryable)
        {
            return openSearchQueryable.Provider is OpenSearchQueryProvider<T> provider
                ? provider.AggregateAsync<TAggregationResult>(openSearchQueryable.Expression, aggregationDictionary, cancellationToken)
                : throw new NotSupportedException("AggregateAsync can only be used on OpenSearchQueryable<T>.");
        }

        throw new NotSupportedException("AggregateAsync can only be used on OpenSearchQueryable<T>.");
    }

    public static string GetQueryString<T>(this IQueryable<T> source, AggregationDictionary? aggregationDictionary = null)
    {
        if (source is OpenSearchQueryable<T> openSearchQueryable)
        {
            return openSearchQueryable.Provider is OpenSearchQueryProvider<T> provider
                ? provider.GetQueryString(openSearchQueryable.Expression, aggregationDictionary)
                : throw new NotSupportedException("GetQueryString can only be used on OpenSearchQueryable<T>.");
        }

        throw new NotSupportedException("GetQueryString can only be used on OpenSearchQueryable<T>.");
    }
}
