using AgentOllamaPOC.Infrastructure;
using AgentOllamaPOC.Rag;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace AgentOllamaPOC.Tools;

public class RagToolService
{
    private const string ToolName = "rag.search_repository_knowledge";

    private readonly RagService _rag;
    private readonly ILogger<RagToolService> _logger;

    public RagToolService(RagService rag, ILogger<RagToolService> logger)
    {
        _rag = rag;
        _logger = logger;
    }

    public async Task<string> SearchRepositoryKnowledgeAsync([Required] string query)
    {
        using var activity = AgentTelemetry.StartActivity("tool.execute");

        activity?.SetTag("tool.name", ToolName);
        activity?.SetTag("tool.type", "rag");

        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            _logger.LogInformation("RAG TOOL EXECUTED");

            _logger.LogInformation(query);

            success = true;

            return await _rag.SearchAsync(query);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error,"RAG tool execution failed");

            _logger.LogError(ex, "Tool execution failed: {ToolName}", ToolName);

            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (!success)
            {
                activity?.SetStatus(ActivityStatusCode.Error,"Tool execution failed");
            }

            AgentTelemetry.RecordToolCall(ToolName,stopwatch.Elapsed.TotalMilliseconds, success);
        }

    }

}