using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AgentOllamaPOC.Agents;

public class RouterAgent : BaseAgent
{

    private readonly GithubAgent _githubAgent;
    private readonly RagAgent _ragAgent;
    private readonly ILogger<RouterAgent> _logger;
    private readonly IAgentExecutor _executor;

    public override string Name => "RouterAgent";

    public RouterAgent(GithubAgent githubAgent,RagAgent ragAgent, ILogger<RouterAgent> logger, IAgentExecutor executor) : base()
    {
        _githubAgent = githubAgent;
        _ragAgent = ragAgent;
        _logger = logger;
        _executor = executor;
    }

    public override async Task<ExecutionResult> AskAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteAsync(context, cancellationToken);

        return route switch
        {
            "githubagent" => await _githubAgent.AskAsync(context, cancellationToken),
            "ragagent" => await _ragAgent.AskAsync(context, cancellationToken),
            _ => (await _executor.ExecuteAsync(context: context, cancellationToken: cancellationToken))
        };
    }

    public override async IAsyncEnumerable<StreamingChunk> AskStreamingAsync(AgentContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {


        var route = await GetRouteAsync(context, cancellationToken);

        IAsyncEnumerable<StreamingChunk> stream = route switch
        {
            "githubagent" => _githubAgent.AskStreamingAsync(context, cancellationToken),
            "ragagent" => _ragAgent.AskStreamingAsync(context, cancellationToken),
            _ => _executor.ExecuteStreamingAsync(context: context, cancellationToken: cancellationToken)
        };

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            yield return chunk;
        }

    }

    private async Task<string> GetRouteAsync( AgentContext context, CancellationToken cancellationToken)
    {
        var result = await _executor.ExecuteAsync(
                            context: context with { IncludeHistory = false },
                            promptFile: "RouterAgentPrompt.txt",
                            cancellationToken: cancellationToken
                      );

        var route = result.Text.Trim().ToLowerInvariant();

        _logger.LogInformation("ROUTER DECISION -> {Route}", route);

        return route;
    }

}