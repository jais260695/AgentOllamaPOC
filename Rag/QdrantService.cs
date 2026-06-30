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
                            ["content"] = chunk.Content,
                            ["type"] = "code",
                            ["length"] = chunk.Content.Length,
                            ["timestamp"] = DateTime.UtcNow.ToString("O")
                        }
                    }
                }
            );

    }

    public async Task<string> SearchAsync(float[] vector)
    {
        var results = await _client.SearchAsync(
                                CollectionName,
                                vector,
                                limit: 5
                            );
        var cont =  results.Select(x => x?.Payload["content"].StringValue ?? string.Empty);

        return string.Join("\n\n", cont);

        //return results.Select(x =>
        //{
        //    var p = x.Payload;
        //    string Get(string key) => p.TryGetValue(key, out var v) ? v.StringValue : null;

        //    return new RagSearchResult
        //    {
        //        Id = x.Id.Uuid,
        //        FileName = Get("file"),
        //        Content = new QdrantContentDto
        //        {
        //            Name = Get("content"),
        //            Path = Get("path"),
        //            Sha = Get("sha"),
        //            Size = int.TryParse(Get("size"), out var size) ? size : (int?)null,
        //            Url = Get("url"),
        //            HtmlUrl = Get("html_url"),
        //            GitUrl = Get("git_url")
        //        },
        //        Type = Get("type"),
        //        Length = int.TryParse(Get("length"), out var length) ? length : 0,
        //        Timestamp = DateTime.TryParse(Get("timestamp"), out var timestamp) ? timestamp : DateTime.UtcNow,
        //        Score = x.Score
        //    };
        //}).ToList();
    }
}