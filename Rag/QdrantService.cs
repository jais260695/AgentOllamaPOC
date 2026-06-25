using Qdrant.Client;
using Qdrant.Client.Grpc;
using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Rag;

public class QdrantService
{

    private readonly QdrantClient _client;
    private const string CollectionName = "github_code";

    public QdrantService()
    {
        _client = new QdrantClient("localhost",6334);
    }

    public async Task InitializeAsync()
    {
        var exists = await _client.CollectionExistsAsync(CollectionName);

        if (!exists)
        {
            await  _client.CreateCollectionAsync(
                        CollectionName,
                        new VectorParams
                        {
                            Size = 768,
                            Distance = Distance.Cosine
                        }
                    );
        }
    }

    public async Task InsertAsync(DocumentChunk chunk)
    {
        await _client.UpsertAsync(
                CollectionName,
                new[]
                {
                    new PointStruct
                    {
                        Id = new PointId
                        {
                            Uuid = Guid.NewGuid().ToString()
                        },
                        Vectors = chunk.Vector,
                        Payload =
                        {

                            ["file"] = chunk.FileName,
                            ["content"] = chunk.Content

                        }
                    }
                }
            );

    }

    public async Task<List<string>> SearchAsync(float[] vector)
    {
        var results = await _client.SearchAsync(
                                CollectionName,
                                vector,
                                limit: 5
                            );

        return  results.Select(x =>
                    x.Payload["content"]
                    .StringValue
                ).ToList();
    }
}