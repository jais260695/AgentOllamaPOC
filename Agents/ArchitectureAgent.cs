using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using AgentOllamaPOC.Tools;

namespace AgentOllamaPOC.Agents;


public class ArchitectureAgent
{

    private readonly AIAgent _agent;


    public ArchitectureAgent(IChatClient chatClient)
    {

        var orderTools = new OrderTools();


        var orderFunction =
            AIFunctionFactory.Create(
                orderTools.GetOrderStatus
            );


        _agent =
            chatClient.AsAIAgent(

                instructions:
                """
                You are a senior .NET architect.

                Explain:
                - Microservices
                - Distributed systems
                - C#
                - Design patterns

                If user asks about order status,
                use the available tool.
                """,

                tools:
                [
                    orderFunction
                ]
            );
    }



    public async Task<string> AskAsync(string question)
    {

        var response =
            await _agent.RunAsync(question);


        return response.ToString();
    }
}