using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Agents;

public interface IAgent
{
    string Name { get; }

    Task<ExecutionResult> AskAsync(AgentContext context,CancellationToken cancellationToken);

    IAsyncEnumerable<StreamingChunk> AskStreamingAsync(AgentContext context, CancellationToken cancellationToken);
}
