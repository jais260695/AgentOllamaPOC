using AgentOllamaPOC.Memory;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Infrastructure;

public sealed class PromptBuilder
{
    private readonly MemoryService _memoryService;

    private readonly PromptService _promptService;

    private readonly ConversationSummaryService _summaryService;

    public PromptBuilder(MemoryService memoryService, PromptService promptService, ConversationSummaryService summaryService)
    {
        _memoryService = memoryService;
        _promptService = promptService;
        _summaryService = summaryService;
    }

    public async Task<List<ChatMessage>> BuildAsync( AgentContext context, string promptFile = "DefaultPrompt.txt" , CancellationToken cancellationToken = default , bool isHistoryRequired = true)
    {
        var messages = new List<ChatMessage>();

        var prompt = _promptService.GetPrompt(promptFile);

        messages.Add(new ChatMessage(ChatRole.System, prompt));

        if (context.IncludeConversationSummary)
        {
            var summary = await _summaryService.GetSummaryAsync(context.Conversation.Id, cancellationToken);

            if (summary != null)
            {
                messages.Add(new ChatMessage(ChatRole.System, $"Conversation Summary:\n{summary.Summary}"));
            }
        }


        if (context.IncludeHistory)
        {
            var history = await _memoryService.GetHistoryAsync(conversationId: context.Conversation.Id, cancellationToken: cancellationToken);

            messages.AddRange(history);
        }


        if(context.IncludeCurrentQuestion)
        {
            messages.Add(new ChatMessage(ChatRole.User, context.Question));
        }

        return messages;
    }
}