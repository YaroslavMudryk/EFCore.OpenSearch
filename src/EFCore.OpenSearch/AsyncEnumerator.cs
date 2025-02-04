namespace EFCore.OpenSearch;

public class AsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly Task<IEnumerable<T>> _task;
    private IEnumerator<T> _enumerator;

    public AsyncEnumerator(Task<IEnumerable<T>> task)
    {
        _task = task;
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_enumerator == null)
        {
            _enumerator = (await _task).GetEnumerator();
        }
        return _enumerator.MoveNext();
    }

    public T Current => _enumerator.Current;

    public ValueTask DisposeAsync()
    {
        _enumerator?.Dispose();
        return ValueTask.CompletedTask;
    }
}
