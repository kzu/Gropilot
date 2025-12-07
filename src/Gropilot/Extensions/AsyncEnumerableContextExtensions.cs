namespace Gropilot;

/// <summary>
/// Provides extensions for properly flowing execution context across 
/// yield return in async streams. 
/// </summary>
/// <remarks>
/// This is a known issue that may be resolved in some future version of .NET. 
/// See https://github.com/dotnet/runtime/issues/47802#issuecomment-772700977.
/// </remarks>
public static class AsyncEnumerableExecutionFlowExtensions
{
    /// <summary>
    /// Wraps the async enumerable in an execution flow preserving enumerable. 
    /// Only use if you need <see cref="AsyncLocal{T}"/> to preserve its value 
    /// across <see cref="IAsyncEnumerable{T}"/> operations, such as in a <c>await foreach</c> 
    /// and <c>yield return</c> scenario.
    /// </summary>
    public static IAsyncEnumerable<T> WithExecutionFlow<T>(this IAsyncEnumerable<T> source)
        => new ExecutionFlowAsyncEnumerable<T>(Throw.IfNull(source));

    class ExecutionFlowAsyncEnumerable<T>(IAsyncEnumerable<T> inner) : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var context = ExecutionContext.Capture();
            if (context != null)
                return new ExecutionFlowEnumerator(inner.GetAsyncEnumerator(cancellationToken), context);

            return inner.GetAsyncEnumerator(cancellationToken);
        }

        class ExecutionFlowEnumerator(IAsyncEnumerator<T> inner, ExecutionContext context) : IAsyncEnumerator<T>
        {
            public T Current => inner.Current;
            public async ValueTask DisposeAsync()
            {
                await inner.DisposeAsync();
                ExecutionContext.Restore(context);
            }
            public async ValueTask<bool> MoveNextAsync()
            {
                ExecutionContext.Restore(context);
                var result = await inner.MoveNextAsync();
                ExecutionContext.Restore(context);
                return result;
            }
        }
    }

}
