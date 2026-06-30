using Microsoft.Extensions.AI;
using OllamaSharp;
namespace AgentOllamaPOC.Rag;
public class EmbeddingService
{

    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public EmbeddingService()
    {

        _generator = new OllamaApiClient(
                            new Uri("http://localhost:11434"),
                            "nomic-embed-text"
                         );
    }

    public async Task<float[]> GenerateAsync(string text)
    {
        var embedding = await _generator.GenerateAsync(text);

        return embedding.Vector.ToArray();

    }

}

