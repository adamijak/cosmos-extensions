using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace Greycorbel.T2T.Common.Extensions;
public static class FeedIteratorExtensions
{
    public static async Task ForEach<T>(this FeedIterator<T> iterator, Func<T, Task> func, CancellationToken cancelToken = default)
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

    public static async Task ChunkedForEach<T>(this FeedIterator<T> iterator, Func<T, Task> func, int chunkSize = 5, CancellationToken cancelToken = default)
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

    public static async Task QueuedForEach<T>(this FeedIterator<T> iterator, Func<T, Task> func, int maxTaskCount = 5, CancellationToken cancelToken = default)
    {
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);

            var queue = new ConcurrentQueue<T>(values);

            var tasks = new List<Task>();
            for (var i = 0; i < maxTaskCount; i++)
            {
                tasks.Add(QueuedForEachTask(queue, func, cancelToken));
            }
            await Task.WhenAll(tasks);
        }
    }

    private static async Task QueuedForEachTask<T>(ConcurrentQueue<T> queue, Func<T, Task> func, CancellationToken cancelToken = default)
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

    public static async Task<T[]> GetAll<T>(this FeedIterator<T> iterator, CancellationToken cancelToken = default)
    {
        var allValues = new List<T>();
        while (iterator.HasMoreResults)
        {
            var values = await iterator.ReadNextAsync(cancelToken);
            allValues.AddRange(values);
        }
        return allValues.ToArray();
    }
}