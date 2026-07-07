using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Models;
using AgentOllamaPOC.Tools;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AgentOllamaPOC.Agents;

public class GithubAgent: BaseAgent
{
    private readonly McpToolAdapter _mcpAdapter;
    private readonly IAgentExecutor _executor;
    private readonly ILogger<GithubAgent> _logger;

    public override string Name => "GithubAgent";
    public GithubAgent(McpToolAdapter mcpAdapter, IAgentExecutor executor, ILogger<GithubAgent> logger) : base()
    {
        _mcpAdapter = mcpAdapter;
        _executor = executor;
        _logger = logger;

    }


    public override async Task<ExecutionResult<T>> AskAsync<T>(AgentContext context, CancellationToken cancellationToken = default)
    {
        var tools = await _mcpAdapter.CreateToolsAsync();

        var result = await _executor.ExecuteAsync<T>(
                                context, 
                                "GithubAgentPrompt.txt", 
                                new ExecutionOptions
                                {
                                    Tools = tools
                                }, 
                                cancellationToken
                      );

        return result;

    }

    public override async IAsyncEnumerable<StreamingChunk<T>> AskStreamingAsync<T>(AgentContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tools = await _mcpAdapter.CreateToolsAsync();

        await foreach (var chunk in _executor.ExecuteStreamingAsync<T>(
                                            context, 
                                            "GithubAgentPrompt.txt",
                                            new ExecutionOptions
                                            {
                                                Tools = tools
                                            },
                                            cancellationToken
                                      )
        )
        {
            yield return chunk;
        }
    }

}