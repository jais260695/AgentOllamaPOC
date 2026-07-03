using AgentOllamaPOC.Data.Repositories;
using AgentOllamaPOC.Infrastructure;
using AgentOllamaPOC.Memory.Models;
using AgentOllamaPOC.Models;
using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Memory;

public class ConversationSummaryService
{
    private readonly IChatClient _chatClient;
    private readonly PromptService _promptService;
    private readonly IConversationSummaryRepository _repository;
    private readonly MemoryService _memoryService;

    public ConversationSummaryService(
        IChatClient chatClient,
        PromptService promptService,
        IConversationSummaryRepository repository,
        MemoryService memoryService)
    {
        _repository = repository;
        _memoryService = memoryService;
        _chatClient = chatClient;
        _promptService = promptService;
    }

    public async Task SummarizeAsync(AgentContext context, CancellationToken cancellationToken = default)
    {

        var existingSummary = await _repository.GetAsync(context.Conversation.Id, cancellationToken);

        var recentMessages = await _memoryService.GetHistoryAsync(context.Conversation.Id, cancellationToken: cancellationToken);

        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(existingSummary?.Summary))
        {
            messages.Add(new ChatMessage(ChatRole.System, $"Existing Conversation Summary:\n{existingSummary.Summary}"));
        }

        messages.AddRange(recentMessages);

        messages.Add(
            new(ChatRole.System, _promptService.GetPrompt("ConversationSummaryPrompt.txt"))
        );

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        var summary =   existingSummary ?? new ConversationSummary
                        {
                            ConversationId = context.Conversation.Id,
                            CreatedAtUtc = DateTime.UtcNow
                        };

        summary.Summary = response.Text ?? string.Empty;
        summary.UpdatedAtUtc = DateTime.UtcNow;

        await _repository.SaveAsync(summary,cancellationToken);
    }

    public async Task<ConversationSummary?> GetSummaryAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var summary =  await _repository.GetAsync(conversationId, cancellationToken);

        return summary;
    }
}