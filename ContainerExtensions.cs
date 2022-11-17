using Microsoft.Azure.Cosmos;

namespace Adamijak.Azure.Cosmos.Extensions;
public static class ContainerExtensions
{
    public static Task<IEnumerable<ItemResponse<T>>> UpsertItemsAsync<T>(this Container container, IEnumerable<T> items, PartitionKey partitionKey = default, ItemRequestOptions? requestOptions = default, int chunkSize = 300, CancellationToken cancelToken = default)
    {
        return UpsertItemsAsync(container, items.Select(i => (i, partitionKey)), requestOptions, chunkSize, cancelToken);
    }

    public static async Task<IEnumerable<ItemResponse<T>>> UpsertItemsAsync<T>(this Container container, IEnumerable<(T, PartitionKey)> items, ItemRequestOptions? requestOptions = default, int chunkSize = 300, CancellationToken cancelToken = default)
    {
        var values = new List<ItemResponse<T>>();
        foreach (var chunk in items.Chunk(chunkSize))
        {
            var tasks = new List<Task<ItemResponse<T>>>();
            foreach (var (item, partitionKey) in chunk)
            {
                tasks.Add(container.UpsertItemAsync(item, partitionKey, requestOptions, cancelToken));
            }
            values.AddRange(await Task.WhenAll(tasks));
        }
        return values;
    }

}
