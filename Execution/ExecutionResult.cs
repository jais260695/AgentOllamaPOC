namespace AgentOllamaPOC.Execution;

public sealed class ExecutionResult<T>
{
    public required T Output { get; init; }

    public string? Model { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public string? FinishReason { get; init; }
}