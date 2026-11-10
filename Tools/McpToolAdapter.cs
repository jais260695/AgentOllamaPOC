using AgentOllamaPOC.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Diagnostics;
using System.Text.Json;

namespace AgentOllamaPOC.Tools;

public class McpToolAdapter
{
    private readonly McpClient _client;

    private readonly ILogger<McpToolAdapter> _logger;

    public McpToolAdapter(McpClient client, ILogger<McpToolAdapter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<AITool>> CreateToolsAsync()
    {
        var tools = await _client.ListToolsAsync();

        var aiTools = new List<AITool>();

        foreach (var mcpTool in tools)
        {

            var toolName = mcpTool.Name;

            var function = AIFunctionFactory.Create(
                                async (JsonElement jsonArguments) => {

                                    using var activity = AgentTelemetry.StartActivity("tool.execute");
                                    
                                    activity?.SetTag("tool.name", toolName);
                                    activity?.SetTag("tool.type", "mcp");

                                    var stopwatch = Stopwatch.StartNew();
                                    var success = false;

                                    try
                                    {

                                        _logger.LogInformation($"TOOL EXECUTED: {toolName}");

                                        var rawArguments = jsonArguments.GetRawText();

                                        Dictionary<string, object?> arguments;

                                        if (jsonArguments.ValueKind != JsonValueKind.Object)
                                        {
                                            var argumentsJson = jsonArguments.ValueKind == JsonValueKind.String ? jsonArguments.GetString()! : jsonArguments.GetRawText();

                                            arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson) ?? new();
                                            //throw new InvalidOperationException(
                                            //    $"Expected JSON object for MCP tool '{toolName}', " +
                                            //    $"but received {jsonArguments.ValueKind}. " +
                                            //    $"Arguments: {rawArguments}");
                                        }
                                        else
                                        {
                                            arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(rawArguments) ?? new();
                                        }



                                        _logger.LogInformation(JsonSerializer.Serialize(arguments));

                                        var result = await _client.CallToolAsync(toolName, arguments);

                                        var firstContent = result.Content.FirstOrDefault();
                                        var contentType = firstContent?.GetType().Name;

                                        activity?.SetTag( "tool.response.content_type", contentType);

                                        _logger.LogInformation("MCP tool completed: {ToolName}", toolName);

                                        success = true;


                                        var toolOutput = firstContent switch
                                        {
                                            TextContentBlock textContent => textContent.Text,
                                            _ => firstContent?.ToString() ?? string.Empty
                                        };

                                        _logger.LogInformation(
                                            "MCP output length: {Length}",
                                            toolOutput.Length);

                                        return toolOutput;
                                    }
                                    catch (Exception ex)
                                    {
                                        activity?.SetStatus( ActivityStatusCode.Error, "MCP tool execution failed");

                                        _logger.LogError(ex,"MCP tool execution failed: {ToolName}",toolName);

                                        throw;
                                    }
                                    finally
                                    {
                                        stopwatch.Stop();

                                        if (!success)
                                        {
                                            activity?.SetStatus(ActivityStatusCode.Error,"MCP tool execution failed");
                                        }

                                        AgentTelemetry.RecordToolCall(toolName,stopwatch.Elapsed.TotalMilliseconds,success);
                                    }
                                },
                                mcpTool.Name,
                                $"""
                                {mcpTool.Description}

                                IMPORTANT:
                                Use this exact schema:

                                {mcpTool.JsonSchema}

                                Do not change parameter names.
                                """
                           );

            aiTools.Add(function);
        }
        return aiTools;

    }

}