using AgentOllamaPOC.Execution;
using AgentOllamaPOC.Memory.Models;
using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Memory;

public sealed class MemoryExtractionService
{
    private readonly IAgentExecutor _executor;

    public MemoryExtractionService(
        IAgentExecutor executor)
    {
        _executor = executor;
    }

    public async Task<IReadOnlyList<ExtractedMemory>> ExtractAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _executor.ExecuteAsync<MemoryExtractionResponse>(
                context with { IncludeSemanticMemory = false, IncludeHistory = false, IncludeConversationSummary = false },
                "MemoryExtractionPrompt.txt",
                null,
                cancellationToken);

        return result.Output.Memories;
    }
}