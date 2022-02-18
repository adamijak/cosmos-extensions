using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;

namespace Adamijak.Azure.Cosmos.Extensions;
public static class ContainerExtensions
{

    public static async Task<IEnumerable<ItemResponse<T>>> UpsertItemsAsync<T>(this Container container,
                                                 IEnumerable<T> items,
                                                 PartitionKey partitionKey = default,
                                                 ItemRequestOptions? requestOptions = default,
                                                 CancellationToken cancelToken = default)
    {
        var tasks = new List<Task<ItemResponse<T>>>();
        foreach (var item in items)
        {
            tasks.Add(container.UpsertItemAsync(item, partitionKey, requestOptions, cancelToken));
        }
        return await Task.WhenAll(tasks);
    }


    public static async Task<List<T>> GetItemsAsync<T>(this Container container,
                                                       QueryDefinition query,
                                                       string? continuationToken = default,
                                                       QueryRequestOptions? requestOptions = default,
                                                       CancellationToken cancelToken = default)
    {
        var items = new List<T>();
        using var feed = container.GetItemQueryIterator<T>(query, continuationToken, requestOptions);
        while (feed.HasMoreResults)
        {
            var batch = (await feed.ReadNextAsync(cancelToken)).Resource;
            items.AddRange(batch);
        }
        return items;
    }


    public static async Task<(IEnumerable<T>, string)> GetItemsIterativeAsync<T>(this Container container,
                                                                                 QueryDefinition query,
                                                                                 string? continuationToken = null,
                                                                                 QueryRequestOptions? requestOptions = default,
                                                                                 CancellationToken cancelToken = default)
    {
        using var feed = container.GetItemQueryIterator<T>(query, continuationToken, requestOptions);
        var response = await feed.ReadNextAsync(cancelToken);
        return (response.Resource, response.ContinuationToken);
    }


    public static async IAsyncEnumerable<int> ExecuteOptimisticStoredProcedureAsync<T>(this Container container,
                                                                           IEnumerable<T> items,
                                                                           string storedProcedureId,
                                                                           PartitionKey? partitionKey = default,
                                                                           StoredProcedureRequestOptions? requestOptions = default,
                                                                           [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        var processedCount = 0;
        while (processedCount != items.Count())
        {
            var itemsToProcess = items.Skip(processedCount);

            var count = await container.Scripts.ExecuteStoredProcedureAsync<int>(storedProcedureId, partitionKey ?? PartitionKey.Null, new dynamic[] { itemsToProcess }, requestOptions, cancelToken);
            yield return count;

            processedCount += count;
        }
    }


}
