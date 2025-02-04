using Microsoft.EntityFrameworkCore.Query;
using OpenSearch.Client;
using System.Linq.Expressions;

namespace EFCore.OpenSearch;

public class OpenSearchQueryProvider<T> : IAsyncQueryProvider
{
    private readonly OpenSearchClient _client;
    private readonly string _indexName;

    public OpenSearchQueryProvider(OpenSearchClient client, string indexName)
    {
        _client = client;
        _indexName = indexName;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetGenericArguments().First();
        var queryableType = typeof(OpenSearchQueryable<>).MakeGenericType(elementType);
        return (IQueryable)Activator.CreateInstance(queryableType, new object[] { this, expression })!;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new OpenSearchQueryable<TElement>(new OpenSearchQueryProvider<TElement>(_client, _indexName), expression);
    }

    public object? Execute(Expression expression)
    {
        return ExecuteQuery(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return (TResult)ExecuteQuery(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        return ExecuteQueryAsync<TResult>(expression, cancellationToken).GetAwaiter().GetResult();
    }

    private object ExecuteQuery(Expression expression)
    {
        var queryDsl = OpenSearchExpressionTranslator.Translate(expression, out int? skip, out int? take, out var selectedFields, out var sortDescriptor, out var trackTotalHits);

        var searchRequest = new SearchRequest(_indexName)
        {
            Query = queryDsl,
            From = skip,
            Size = take,
            Sort = (IList<ISort>)sortDescriptor,
            TrackTotalHits = trackTotalHits,
        };

        if (selectedFields.Count > 0)
        {
            searchRequest.Source = new Union<bool, ISourceFilter>(new SourceFilter { Includes = selectedFields.ToArray() });
        }

        var response = _client.Search<object>(searchRequest);
        if (!response.IsValid)
        {
            throw new Exception($"OpenSearch query error: {response.DebugInformation}");
        }

        if (trackTotalHits)
        {
            return (int)response.Total;
        }

        return response.Documents;
    }

    private async Task<TResult> ExecuteQueryAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var queryDsl = OpenSearchExpressionTranslator.Translate(expression, out int? skip, out int? take, out var selectedFields, out var sortDescriptor, out var trackTotalHits);

        var searchRequest = new SearchRequest(_indexName)
        {
            Query = queryDsl,
            From = skip,
            Size = take,
            Sort = (IList<ISort>)sortDescriptor,
            TrackTotalHits = trackTotalHits
        };

        if (selectedFields.Count > 0)
        {
            searchRequest.Source = new Union<bool, ISourceFilter>(new SourceFilter { Includes = selectedFields.ToArray() });
        }

        var response = await _client.SearchAsync<object>(searchRequest, cancellationToken);
        if (!response.IsValid)
        {
            throw new Exception($"OpenSearch query error: {response.DebugInformation}");
        }

        if (trackTotalHits)
        {
            return (TResult)(object)(int)response.Total;
        }

        return (TResult)(object)response.Documents;
    }
}
