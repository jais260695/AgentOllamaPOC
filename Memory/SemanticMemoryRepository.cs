using AgentOllamaPOC.Memory.Interfaces;
using AgentOllamaPOC.Memory.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AgentOllamaPOC.Memory;

public sealed class SemanticMemoryRepository : ISemanticMemoryRepository
{
    private readonly QdrantClient _client;

    private const string CollectionName = "semantic_memory";

    public SemanticMemoryRepository(QdrantClient client)
    {
        _client = client;
    }

    public async Task InitializeAsync()
    {
        var exists = await _client.CollectionExistsAsync(CollectionName);

        if (exists)
            return;

        await _client.CreateCollectionAsync(
            CollectionName,
            new VectorParams
            {
                Size = 768,
                Distance = Distance.Cosine
            });
    }

    public async Task StoreAsync(
        SemanticMemory memory,
        CancellationToken cancellationToken = default)
    {
        await _client.UpsertAsync(
            CollectionName,
            new[]
            {
                new PointStruct
                {
                    Id = new PointId
                    {
                        Uuid = memory.Id.ToString()
                    },

                    Vectors = memory.Embedding.ToArray(),

                    Payload =
                    {
                        ["userId"] = memory.UserId.ToString(),
                        ["content"] = memory.Content,
                        ["source"] = memory.Source,
                        ["importance"] = memory.Importance,
                        ["createdUtc"] = memory.CreatedUtc.ToString("O")
                    }
                }
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SemanticMemory>> SearchAsync(
        Guid userId,
        ReadOnlyMemory<float> embedding,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        var results = await SearchInternalAsync(
            userId,
            embedding,
            top,
            cancellationToken);

        return results
            .Select(Map)
            .ToList();
    }

    public async Task<SemanticMemory?> FindSimilarAsync(
        Guid userId,
        ReadOnlyMemory<float> embedding,
        double threshold = 0.92,
        CancellationToken cancellationToken = default)
    {
        var results = await SearchInternalAsync(
            userId,
            embedding,
            1,
            cancellationToken);

        var result = results.FirstOrDefault();

        if (result == null)
            return null;

        if (result.Score < threshold)
            return null;

        return Map(result);
    }

    private async Task<IReadOnlyList<ScoredPoint>> SearchInternalAsync(
        Guid userId,
        ReadOnlyMemory<float> embedding,
        int top,
        CancellationToken cancellationToken)
    {
        var filter = new Filter();

        filter.Must.Add(
            new Condition
            {
                Field = new FieldCondition
                {
                    Key = "userId",
                    Match = new Match
                    {
                        Keyword = userId.ToString()
                    }
                }
            });

        return await _client.SearchAsync(
            collectionName: CollectionName,
            vector: embedding.ToArray(),
            filter: filter,
            limit: (ulong)top,
            cancellationToken: cancellationToken);
    }

    private static SemanticMemory Map(
        ScoredPoint point)
    {
        return new SemanticMemory
        {
            Id = Guid.Parse(point.Id.Uuid),

            UserId = Guid.Parse(
                point.Payload["userId"].StringValue),

            Content = point.Payload["content"].StringValue,

            Source = point.Payload["source"].StringValue,

            Importance = point.Payload["importance"].DoubleValue,

            CreatedUtc = DateTime.Parse(
                point.Payload["createdUtc"].StringValue),

            // We don't retrieve vectors during search.
            Embedding = ReadOnlyMemory<float>.Empty
        };
    }
}