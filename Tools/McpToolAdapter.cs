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

        //foreach (var t in tools)
        //{
        //    _logger.LogInformation("MCP Tool: {Tool} \n Schema: {Schema}", t.Name, t.JsonSchema);
        //}

        //Console.WriteLine("===== MCP TOOLS =====");

        //foreach (var tool in tools)
        //{
        //    Console.WriteLine(tool.Name);

        //    Console.WriteLine(tool.Description);

        //    Console.WriteLine(
        //        tool.JsonSchema
        //    );
        //}

        //Console.WriteLine("====================");

        var aiTools = new List<AITool>();

        foreach (var mcpTool in tools)
        {

            //_logger.LogInformation("Registering MCP Tool {Tool}", mcpTool.Name);
            var toolName = mcpTool.Name;

            var function = AIFunctionFactory.Create(
                                async (JsonElement jsonArguments) => {

                                    _logger.LogInformation($"TOOL EXECUTED: {toolName}");

                                    var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonArguments.GetRawText())?? new();

                                    _logger.LogInformation(JsonSerializer.Serialize(arguments));

                                    var result = await _client.CallToolAsync(toolName, arguments);

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