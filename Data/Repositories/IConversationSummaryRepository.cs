using AgentOllamaPOC.Memory.Models;

namespace AgentOllamaPOC.Data.Repositories;

public interface IConversationSummaryRepository
{
    Task<ConversationSummary?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task SaveAsync(ConversationSummary summary,CancellationToken cancellationToken = default);
}