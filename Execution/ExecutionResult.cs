namespace AgentOllamaPOC.Execution;

public sealed class ExecutionResult
{
    public string Text { get; init; } = string.Empty;

    public string? Model { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public string? FinishReason { get; init; }
}