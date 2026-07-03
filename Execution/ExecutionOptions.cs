using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Execution;

public sealed class ExecutionOptions
{
    public IEnumerable<AITool>? Tools { get; init; }

    public float? Temperature { get; init; }

    public int? MaxOutputTokens { get; init; }

    public bool Streaming { get; init; } = false;
}