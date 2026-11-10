using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Infrastructure;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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

    public override async Task<ExecutionResult<T>> AskAsync<T>(AgentContext context, CancellationToken cancellationToken = default)
    {
        using var activity = AgentTelemetry.StartActivity("agent.delegate", Name);

        var routeDecision = await GetRouteAsync(context, cancellationToken);

        activity?.SetTag("agent.route", routeDecision.Route.ToString());
        activity?.SetTag("agent.route.confidence", routeDecision.Confidence);

        return routeDecision.Route switch
        {
            RouteDecisionType.GithubAgent => await _githubAgent.AskAsync<T>(context, cancellationToken),
            RouteDecisionType.RagAgent => await _ragAgent.AskAsync<T>(context, cancellationToken),
            _ => (await _executor.ExecuteAsync<T>(context: context, cancellationToken: cancellationToken))
        };
    }

    public override async IAsyncEnumerable<StreamingChunk<T>> AskStreamingAsync<T>(AgentContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        using var activity = AgentTelemetry.StartActivity("agent.delegate", Name);

        var routeDecision = await GetRouteAsync(context, cancellationToken);

        activity?.SetTag("agent.route", routeDecision.Route.ToString());
        activity?.SetTag("agent.route.confidence", routeDecision.Confidence);

        IAsyncEnumerable<StreamingChunk<T>> stream = routeDecision.Route switch
        {
            RouteDecisionType.GithubAgent => _githubAgent.AskStreamingAsync<T>(context, cancellationToken),
            RouteDecisionType.RagAgent => _ragAgent.AskStreamingAsync<T>(context, cancellationToken),
            _ => _executor.ExecuteStreamingAsync<T>(context: context, cancellationToken: cancellationToken)
        };

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            yield return chunk;
        }

    }

    private async Task<RouteDecision> GetRouteAsync( AgentContext context, CancellationToken cancellationToken)
    {
        using var activity = AgentTelemetry.StartActivity("agent.route", Name);

        var result = await _executor.ExecuteAsync<RouteDecision>(
                            context: context with { IncludeHistory = false },
                            promptFile: "RouterAgentPrompt.txt",
                            cancellationToken: cancellationToken
                      );

        var routeDecision = result.Output;
        activity?.SetTag("agent.route", routeDecision.Route.ToString());
        activity?.SetTag("agent.route.confidence", routeDecision.Confidence);


        _logger.LogInformation("Router Decision => Route: {Route}, Confidence: {Confidence}, Reason: {Reason}",
                                routeDecision.Route, routeDecision.Confidence, routeDecision.Reason);



        return routeDecision;
    }

}