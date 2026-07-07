using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Agents;

public abstract class BaseAgent: IAgent
{
    public abstract string Name { get; }

    public abstract Task<ExecutionResult<T>> AskAsync<T>(AgentContext context, CancellationToken cancellationToken);

    public abstract IAsyncEnumerable<StreamingChunk<T>> AskStreamingAsync<T>(AgentContext context, CancellationToken cancellationToken);

}