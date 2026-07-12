namespace AgentOllamaPOC.Models;

public sealed record AgentContext
{
    public required Conversation Conversation { get; init; }

    public required string Question { get; init; }

    public bool IncludeHistory { get; init; } = true;

    public bool IncludeCurrentQuestion { get; init; } = true;

    public bool IncludeConversationSummary { get; init; } = true;

    public bool IncludeSemanticMemory { get; init; } = true;
}
