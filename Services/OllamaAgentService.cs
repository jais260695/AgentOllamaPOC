using AgentOllamaPOC.Agents;
using AgentOllamaPOC.Mcp;
using AgentOllamaPOC.Rag;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace AgentOllamaPOC.Services;

public class OllamaAgentService
{
    private readonly GithubAgent _agent;
    private readonly RagService _rag;

    public OllamaAgentService()
    {
        _rag =
           new RagService(
               new EmbeddingService(),
               new QdrantService()
           );


        _rag.InitializeAsync()
            .GetAwaiter()
            .GetResult();

        _rag.IndexDocumentAsync(
                "README.md",
                File.ReadAllText(
                    "C:\\Users\\aj936\\source\\repos\\AgentOllamaPOC\\README.md"
                )
            )
            .GetAwaiter()
            .GetResult();

        var ollama = new OllamaApiClient(
                        new Uri(
                            "http://localhost:11434"
                        ),
                        "qwen2.5:7b"
                    );

        IChatClient chatClient = new ChatClientBuilder(ollama)
                                    .UseFunctionInvocation()
                                    .Build();

        var githubClient = new GithubMcpClient()
                                .ConnectAsync()
                                .GetAwaiter()
                                .GetResult();

        _agent = new GithubAgent(chatClient,githubClient);
    }

    public async Task<string> AskAsync(string question)
    {
        var context = await _rag.SearchAsync(question);

        var prompt = $"""
                        Answer using repository context.

                        Context:
                        {context}


                        Question:
                        {question}
                     """;
    
        return await _agent.AskAsync(prompt);
    }

}