using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Models;
using AgentOllamaPOC.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;


namespace AgentOllamaPOC.Agents;


public class RagAgent : BaseAgent
{
    private readonly ILogger<RagAgent> _logger;
    private readonly IAgentExecutor _executor;
    private readonly RagToolService _rag;


    public override string Name => "RagAgent";

    public RagAgent(RagToolService rag,ILogger<RagAgent> logger, IAgentExecutor executor) : base()
    {
        _logger = logger;
        _executor = executor;
        _rag = rag;
    }



    public override async Task<ExecutionResult> AskAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var tool =
            AIFunctionFactory.Create(
                _rag.SearchRepositoryKnowledgeAsync,
                "search_repository_knowledge",
                "Search repository indexed knowledge using the exact user question"
            );

        var result = await _executor.ExecuteAsync(
                                context, 
                                "RagAgentPrompt.txt", 
                                new ExecutionOptions
                                {
                                    Tools = new[] { tool },
                                    Temperature = 0.1f
                                }, 
                                cancellationToken
                      );

        return result;
    }

    public override async IAsyncEnumerable<StreamingChunk> AskStreamingAsync(AgentContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tool =
            AIFunctionFactory.Create(
                _rag.SearchRepositoryKnowledgeAsync,
                "search_repository_knowledge",
                "Search repository indexed knowledge using the exact user question"
            );

        await foreach (var chunk in _executor.ExecuteStreamingAsync(
                                            context,
                                            "RagAgentPrompt.txt",
                                            new ExecutionOptions
                                            {
                                                Tools = new[] { tool },
                                                Temperature = 0.1f
                                            },
                                            cancellationToken
                                      )
        ) 
        {
            yield return chunk;
        }
    }

}