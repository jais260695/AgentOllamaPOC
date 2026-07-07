namespace AgentOllamaPOC.Execution;

public sealed class StreamingChunk<T>
{
    public string Text { get; init; } = string.Empty;

    public bool IsCompleted { get; init; }

    public ExecutionResult<T>? FinalResult { get; init; }
}