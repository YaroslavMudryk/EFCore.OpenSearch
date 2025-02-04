﻿using System.Collections;
using System.Linq.Expressions;

namespace EFCore.OpenSearch;

public class OpenSearchQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>
{
    private readonly OpenSearchQueryProvider<T> _provider;
    private readonly Expression _expression;

    public OpenSearchQueryable(OpenSearchQueryProvider<T> provider, Expression expression = null)
    {
        _provider = provider;
        _expression = expression ?? Expression.Constant(this);
    }

    public Type ElementType => typeof(T);
    public Expression Expression => _expression;
    public IQueryProvider Provider => _provider;

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_provider.Execute(_expression)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new AsyncEnumerator<T>(Task.Run(() => _provider.ExecuteAsync<IEnumerable<T>>(_expression, cancellationToken)));
    }
}
