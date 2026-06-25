using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.Text.Json;


namespace AgentOllamaPOC.Agents;


public class GithubAgent
{
    private readonly AIAgent _agent;


    public GithubAgent(
        IChatClient chatClient,
        McpClient mcpClient)
    {


        var mcpTools =
            mcpClient
            .ListToolsAsync()
            .GetAwaiter()
            .GetResult();



        var aiTools = new List<AITool>();


        foreach (var mcpTool in mcpTools)
        {


            var tool =
                AIFunctionFactory.Create(
                    async (object input) =>
                    {
                        Console.WriteLine(JsonSerializer.Serialize(input));
                        var args = input switch
                        {
                            JsonElement je => JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText())!,
                            Dictionary<string, object?> dict => dict,
                            _ => JsonSerializer.Deserialize<Dictionary<string, object?>>(
                                    JsonSerializer.Serialize(input)
                                )!
                        };
                        Console.WriteLine(JsonSerializer.Serialize(args));
                        //  Tool: search_repositories
                        //  var args =
                        //    new Dictionary<string, object?>
                        //    {
                        //        { "query", "user:jais260695" }
                        //    };

                        var result =
                                await mcpClient.CallToolAsync(
                                    mcpTool.Name,
                                    args
                                );

                        return "RAW_TOOL_OUTPUT:\n" +
       System.Text.Json.JsonSerializer.Serialize(result.Content);
                        //return result.Content
                        //        .FirstOrDefault()
                        //        ?.ToString()
                        //        ?? "";


                    },
                    mcpTool.Name,
                    mcpTool.Description
                
                );


            aiTools.Add(tool);
        }


        _agent =
            chatClient.AsAIAgent(

                instructions:
                """
                You are a GitHub assistant.

                GENERAL RULES:
                - Always use GitHub tools when the user asks for GitHub data.
                - Never answer from memory.
                - Never invent repository information.
                - Use tool results to generate the final answer.
                - Respond only in English.

                TOOL USAGE RULES:
                - Use the exact parameter names defined by the tool.
                - Never rename tool parameters.
                - Never create alternative parameter names.

                For list_commits tool:

                The tool signature is:

                list_commits(
                    owner: string,
                    repo: string,
                    branch?: string
                )

                Parameter mapping:
                - GitHub username/organization -> owner
                - Repository name -> repo
                - Branch name -> branch

                Example:

                User:
                "fetch commits of CodeReviewAgent repository owned by jais260695 in main branch"

                Correct tool arguments:

                {
                    "owner": "jais260695",
                    "repo": "CodeReviewAgent",
                    "branch": "main"
                }

                Incorrect examples:
                {
                    "repository_owner": "jais260695",
                    "repository_name": "CodeReviewAgent",
                    "branch_name": "main"
                }

                Never use repository_owner, repository_name, or branch_name.

                """,

                tools: aiTools
            );

    }



    public async Task<string> AskAsync(
        string question)
    {

        var response =
            await _agent.RunAsync(
                question
                );


        return response.ToString();

    }
}