namespace AgentOllamaPOC.Memory.Models;

public sealed class SemanticMemory
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public ReadOnlyMemory<float> Embedding { get; init; }

    public DateTime CreatedUtc { get; init; } = DateTime.Now;

    public string Source { get; init; } = "conversation";

    public double Importance { get; init; } = 1;
}
