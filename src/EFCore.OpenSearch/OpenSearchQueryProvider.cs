using Microsoft.EntityFrameworkCore.Query;
using OpenSearch.Client;
using OpenSearch.Net;
using System.Linq.Expressions;
using System.Text.Json;

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
        return new OpenSearchQueryable<T>(this, expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new OpenSearchQueryable<TElement>(new OpenSearchQueryProvider<TElement>(_client, _indexName), expression);
    }

    public object? Execute(Expression expression)
    {
        return ExecuteQuery(expression, null, null!);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return (TResult)ExecuteQuery(expression, null, null!);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        return ExecuteQueryAsync<TResult>(expression, null, cancellationToken).GetAwaiter().GetResult();
    }

    private object ExecuteQuery(Expression expression, QueryContainer? externalQuery, AggregationDictionary aggregations)
    {
        var internalQuery = OpenSearchExpressionTranslator.Translate(expression, out int? skip, out int? take, out var selectedFields, out var sortDescriptor, out bool trackTotalHits);

        var searchRequest = new SearchRequest(_indexName)
        {
            Query = externalQuery ?? internalQuery,
            From = skip,
            Size = take,
            Sort = (IList<ISort>)sortDescriptor,
            TrackTotalHits = trackTotalHits,
            Aggregations = aggregations
        };

        if (selectedFields.Count > 0)
        {
            searchRequest.Source = new Union<bool, ISourceFilter>(new SourceFilter { Includes = selectedFields.ToArray() });
        }

        var queryString = _client.SourceSerializer.SerializeToString(searchRequest);

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

    private async Task<TResult> ExecuteQueryAsync<TResult>(Expression expression, QueryContainer? externalQuery, CancellationToken cancellationToken)
    {
        var internalQuery = OpenSearchExpressionTranslator.Translate(expression, out int? skip, out int? take, out var selectedFields, out var sortDescriptor, out bool trackTotalHits);

        var searchRequest = new SearchRequest(_indexName)
        {
            Query = externalQuery ?? internalQuery,
            From = skip,
            Size = take,
            Sort = (IList<ISort>)sortDescriptor,
            TrackTotalHits = trackTotalHits
        };

        if (selectedFields.Count > 0)
        {
            searchRequest.Source = new Union<bool, ISourceFilter>(new SourceFilter { Includes = selectedFields.ToArray() });
        }

        var queryString = _client.SourceSerializer.SerializeToString(searchRequest);

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

    public async Task<TAggregationResult> AggregateAsync<TAggregationResult>(
        Expression expression,
        AggregationDictionary aggregationDictionary,
        CancellationToken cancellationToken = default)
        where TAggregationResult : class, new()
    {
        var queryDsl = OpenSearchExpressionTranslator.Translate(expression, out _, out _, out _, out _, out _);

        var searchRequest = new SearchRequest(_indexName)
        {
            Query = queryDsl,
            Size = 0,
            Aggregations = aggregationDictionary
        };

        var response = await _client.SearchAsync<object>(searchRequest, cancellationToken);
        if (!response.IsValid)
        {
            throw new Exception($"OpenSearch aggregation query error: {response.DebugInformation}");
        }

        return MapAggregationResult<TAggregationResult>(response.Aggregations);
    }

    private static TAggregationResult MapAggregationResult<TAggregationResult>(AggregateDictionary aggregations)
        where TAggregationResult : class, new()
    {
        var json = JsonSerializer.Serialize(aggregations);
        return JsonSerializer.Deserialize<TAggregationResult>(json) ?? new TAggregationResult();
    }

    public string GetQueryString(Expression expression, AggregationDictionary? aggregationDictionary = null)
    {
        var queryDsl = OpenSearchExpressionTranslator.Translate(expression, out int? skip, out int? take, out var selectedFields, out var sortDescriptor, out bool trackTotalHits);

        var searchRequest = new SearchRequest(_indexName)
        {
            Query = queryDsl,
            From = skip,
            Size = take,
            Sort = (IList<ISort>)sortDescriptor,
            TrackTotalHits = trackTotalHits,
            Aggregations = aggregationDictionary
        };

        var json = _client.RequestResponseSerializer.SerializeToString(searchRequest);
        return json;
    }
}
