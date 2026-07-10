using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Rag;

public class RagService
{

    private readonly EmbeddingService _embeddingService;

    private readonly RAGQdrantService _qdrantService;

    public RagService(EmbeddingService embeddingService,RAGQdrantService qdrantService)
    {
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
    }

    public async Task IndexDocumentAsync(string fileName,string content)
    {
        var chunks = content.Chunk(500);

        foreach (var chunk in chunks)
        {
            var text = new string(chunk);

            var vector = await _embeddingService.GenerateAsync(text);

            var document = new DocumentChunk
                               {
                                    FileName = fileName,
                                    Content = text,
                                    Vector = vector
                               };

            await _qdrantService.InsertAsync(document);
        }
    }

    public async Task<string> SearchAsync(string question)
    {

        var vector = await _embeddingService.GenerateAsync(question);

        return await _qdrantService.SearchAsync(vector);

    }

}