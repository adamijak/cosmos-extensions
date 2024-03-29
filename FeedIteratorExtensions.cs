using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;

namespace Adamijak.Azure.Cosmos.Extensions;
public static class FeedIteratorExtensions
{
    public static async Task ForEach<T>(this FeedIterator<T> iterator, Action<T> func, CancellationToken cancelToken = default)
    {
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);

            foreach (var value in values)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                func(value);
            }
        }
    }

    public static async Task ForEachAsync<T>(this FeedIterator<T> iterator, Func<T, Task> func, CancellationToken cancelToken = default)
    {
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);

            foreach (var value in values)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                await func(value);
            }
        }
    }

    public static async Task ForEachAsync<T>(this FeedIterator<T> iterator, Func<T, Task> func, int maxWorkerCount = 5, CancellationToken cancelToken = default)
    {
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);

            var queue = new ConcurrentQueue<T>(values);

            var tasks = new List<Task>();
            foreach (var i in Enumerable.Range(0, Math.Min(values.Count, maxWorkerCount)))
            {
                tasks.Add(ForEachTaskAsync(queue, func, cancelToken));
            }
            await Task.WhenAll(tasks);
        }
    }

    private static async Task ForEachTaskAsync<T>(ConcurrentQueue<T> queue, Func<T, Task> func, CancellationToken cancelToken = default)
    {
        while (!queue.IsEmpty)
        {
            if (cancelToken.IsCancellationRequested)
            {
                return;
            }
            if (queue.TryDequeue(out var value))
            {
                await func(value);
            }
        }
    }

    public static async Task ChunkedForEachAsync<T>(this FeedIterator<T> iterator, Func<T, Task> func, int chunkSize = 5, CancellationToken cancelToken = default)
    {
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);

            var chunks = values.Chunk(chunkSize);
            foreach (var chunk in chunks)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                var tasks = new List<Task>();
                foreach (var value in chunk)
                {
                    tasks.Add(func(value));
                }
                await Task.WhenAll(tasks);

            }
        }
    }
    
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this FeedIterator<T> iterator, [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);
            foreach(var value in values)
            {
                yield return value;
            }
        }
    }
    
    [Obsolete("ReadAllAsync is deprecated, please use ToAsyncEnumerable instead.")]
    public static async IAsyncEnumerable<T> ReadAllAsync<T>(this FeedIterator<T> iterator, [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);
            foreach(var value in values)
            {
                yield return value;
            }
        }
    }
}
