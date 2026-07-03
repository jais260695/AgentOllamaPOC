using AgentOllamaPOC.Models;
using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Execution;

public interface IAgentExecutor
{
    Task<ExecutionResult> ExecuteAsync(AgentContext context, string promptFile = "DefaultPrompt.txt", ExecutionOptions? options = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<StreamingChunk> ExecuteStreamingAsync(AgentContext context, string promptFile = "DefaultPrompt.txt", ExecutionOptions? options = null,CancellationToken cancellationToken = default);
}