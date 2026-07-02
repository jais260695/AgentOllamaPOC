using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Agents;

public abstract class BaseAgent: IAgent
{
    public abstract string Name { get; }

    public abstract Task<ExecutionResult> AskAsync(AgentContext context, CancellationToken cancellationToken);

    public abstract IAsyncEnumerable<StreamingChunk> AskStreamingAsync(AgentContext context, CancellationToken cancellationToken);

}