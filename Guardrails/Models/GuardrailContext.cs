namespace AgentOllamaPOC.Guardrails.Models;
public sealed record GuardrailContext
{
    public required GuardrailStage Stage { get; init; }

    public required string Content { get; init; }

    public string? Operation { get; init; }
}
