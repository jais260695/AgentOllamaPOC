using Microsoft.Extensions.AI;
using OllamaSharp;

namespace AgentOllamaPOC.Services;

public class ChatClientFactory
{

    public IChatClient Create()
    {

        var ollama = new OllamaApiClient(
                            new Uri("http://localhost:11434"),
                            "qwen2.5:7b"
                        );

        return new ChatClientBuilder(ollama).UseFunctionInvocation().Build();

    }

}