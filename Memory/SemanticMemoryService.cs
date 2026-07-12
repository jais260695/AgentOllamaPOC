using AgentOllamaPOC.Memory.Interfaces;
using AgentOllamaPOC.Memory.Models;
using AgentOllamaPOC.Rag;
using Microsoft.Extensions.Logging;

public sealed class SemanticMemoryService
{
    private readonly EmbeddingService _embeddingService;
    private readonly ISemanticMemoryRepository _repository;
    private readonly ILogger<SemanticMemoryService> _logger;

    public SemanticMemoryService(
        EmbeddingService embeddingService,
        ISemanticMemoryRepository repository,
        ILogger<SemanticMemoryService> logger)
    {
        _embeddingService = embeddingService;
        _repository = repository;
        _logger = logger;
    }

    public async Task StoreAsync(
        Guid userId,
        IEnumerable<ExtractedMemory> memories,
        CancellationToken cancellationToken = default)
    {
        foreach (var memory in memories)
        {
            if (memory.Importance < 0.70)
            {
                _logger.LogDebug(
                    "Skipping low importance memory: {Memory}",
                    memory.Content);

                continue;
            }

            var embedding =
                await _embeddingService.GenerateAsync(
                    memory.Content);

            var existing =
                await _repository.FindSimilarAsync(
                    userId,
                    embedding,
                    cancellationToken: cancellationToken);

            if (existing is not null)
            {
                _logger.LogInformation(
                    "Duplicate semantic memory skipped: {Memory}",
                    memory.Content);

                continue;
            }

            await _repository.StoreAsync(
                new SemanticMemory
                {
                    UserId = userId,
                    Content = memory.Content,
                    Embedding = embedding,
                    Importance = memory.Importance
                },
                cancellationToken);

            _logger.LogInformation(
                "Stored semantic memory: {Memory}",
                memory.Content);
        }
    }

    public async Task<IReadOnlyList<string>> SearchAsync(
        Guid userId,
        string query,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        var embedding =
            await _embeddingService.GenerateAsync(query);

        var memories =
            await _repository.SearchAsync(
                userId,
                embedding,
                top,
                cancellationToken);

        return memories
            .Select(x => x.Content)
            .ToList();
    }
}