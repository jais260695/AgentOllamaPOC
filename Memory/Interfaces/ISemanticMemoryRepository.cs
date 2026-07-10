using AgentOllamaPOC.Memory.Models;

namespace AgentOllamaPOC.Memory.Interfaces;

public interface ISemanticMemoryRepository
{
    Task InitializeAsync();

    Task StoreAsync(
        SemanticMemory memory,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SemanticMemory>> SearchAsync(
        Guid userId,
        ReadOnlyMemory<float> embedding,
        int top = 5,
        CancellationToken cancellationToken = default);

    Task<SemanticMemory?> FindSimilarAsync(
        Guid userId,
        ReadOnlyMemory<float> embedding,
        double threshold = 0.92,
        CancellationToken cancellationToken = default);
}