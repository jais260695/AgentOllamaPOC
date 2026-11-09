
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgentOllamaPOC.Infrastructure;

public static class AgentTelemetry
{
    public const string SourceName = "AgentOllamaPOC";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> AgentExecutions =
        Meter.CreateCounter<long>("agent.executions");

    private static readonly Counter<long> AgentFailures =
        Meter.CreateCounter<long>("agent.failures");

    private static readonly Histogram<double> AgentDuration =
        Meter.CreateHistogram<double>("agent.execution.duration", "ms");

    private static readonly Counter<long> LlmRequests =
        Meter.CreateCounter<long>("llm.requests");

    private static readonly Histogram<double> LlmDuration =
        Meter.CreateHistogram<double>("llm.request.duration", "ms");

    private static readonly Counter<long> ToolCalls =
        Meter.CreateCounter<long>("agent.tool.calls");

    private static readonly Histogram<double> ToolDuration =
        Meter.CreateHistogram<double>("agent.tool.duration", "ms");

    public static Activity? StartActivity(string name, string? agentName = null)
    {
        var activity = ActivitySource.StartActivity(name);

        if (activity is not null && agentName is not null)
            activity.SetTag("agent.name", agentName);

        return activity;
    }

    public static void RecordAgentExecution(string agentName, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "agent.name", agentName },
            { "outcome", success ? "success" : "failure" }
        };

        AgentExecutions.Add(1, tags);
        AgentDuration.Record(durationMs, tags);

        if (!success)
            AgentFailures.Add(1, tags);
    }

    public static void RecordLlmRequest(string provider, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "llm.provider", provider },
            { "outcome", success ? "success" : "failure" }
        };

        LlmRequests.Add(1, tags);
        LlmDuration.Record(durationMs, tags);
    }

    public static void RecordToolCall(string toolName, double durationMs, bool success)
    {
        var tags = new TagList
        {
            { "tool.name", toolName },
            { "outcome", success ? "success" : "failure" }
        };

        ToolCalls.Add(1, tags);
        ToolDuration.Record(durationMs, tags);
    }
}
