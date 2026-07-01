using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;


namespace AgentOllamaPOC.Services;


public class GeminiChatClientFactory
{

    public IChatClient Create()
    {

        var apiKey =
            Environment.GetEnvironmentVariable(
                "GOOGLE_API_KEY"
            );


        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception(
                "GOOGLE_API_KEY missing"
            );
        }


        var client =
            new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint =
                    new Uri(
                    "https://generativelanguage.googleapis.com/v1beta/openai/"
                    )
                });


        var chatClient =
            client
            .GetChatClient(
                "gemini-3.1-flash-lite"
            )
            .AsIChatClient();



        return new ChatClientBuilder(chatClient)
            .UseFunctionInvocation()
            .Build();

    }

}