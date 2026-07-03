using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Memory.Interfaces;

public interface IMemoryStore
{
    Task AppendAsync(Guid conversationId, ChatMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, int limit = 10, CancellationToken cancellationToken = default);

    Task ClearAsync(Guid conversationId, CancellationToken cancellationToken = default);
}