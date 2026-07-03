using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace AgentOllamaPOC.Tools;

public class McpToolAdapter
{
    private readonly McpClient _client;

    private ILogger<McpToolAdapter> _logger;

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

                                    _logger.LogInformation($"TOOL EXECUTED: {toolName}");

                                    var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonArguments.GetRawText())?? new();

                                    _logger.LogInformation(JsonSerializer.Serialize(arguments));

                                    var result = await _client.CallToolAsync(toolName, arguments);

                                    var firstContent = result.Content.FirstOrDefault();
                                    var contentType = firstContent?.GetType().Name;
                                    _logger.LogInformation($"Content type: {contentType}");

                                    return result.Content.FirstOrDefault()?.ToString() ?? string.Empty;
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