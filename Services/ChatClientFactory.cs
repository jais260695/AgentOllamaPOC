using Microsoft.Extensions.AI;
using OllamaSharp;


namespace AgentOllamaPOC.Services;


public class ChatClientFactory
{

    public IChatClient Create()
    {


        var ollama =
            new OllamaApiClient(

                new Uri(
                    "http://localhost:11434"
                ),

                "llama3.1"

            );



        return new ChatClientBuilder(ollama)

            .UseFunctionInvocation()

            .Build();

    }

}