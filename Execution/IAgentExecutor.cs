using AgentOllamaPOC.Models;
using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Execution;

public interface IAgentExecutor
{
    Task<ExecutionResult<T>> ExecuteAsync<T>(AgentContext context, string promptFile = "DefaultPrompt.txt", ExecutionOptions? options = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<StreamingChunk<T>> ExecuteStreamingAsync<T>(AgentContext context, string promptFile = "DefaultPrompt.txt", ExecutionOptions? options = null,CancellationToken cancellationToken = default);
}