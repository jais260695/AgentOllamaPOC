using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Agents;

public interface IAgent
{
    string Name { get; }

    Task<ExecutionResult<T>> AskAsync<T>(AgentContext context, CancellationToken cancellationToken);

    IAsyncEnumerable<StreamingChunk<T>> AskStreamingAsync<T>(AgentContext context, CancellationToken cancellationToken);
}
