namespace AgentOllamaPOC.Execution;

public sealed class StreamingChunk
{
    public string Text { get; init; } = string.Empty;

    public bool IsCompleted { get; init; }

    public ExecutionResult? FinalResult { get; init; }
}