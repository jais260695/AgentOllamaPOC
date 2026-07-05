using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Memory.Interfaces;

public interface IMemoryStore
{
    Task AppendAsync(Guid conversationId, ChatMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, int start = -10, int stop = -1, CancellationToken cancellationToken = default);

    Task ClearAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task<int> TrimHistoryAsync(Guid conversationId, int keepLastMessages, CancellationToken cancellationToken = default);

    Task<int> GetMessageCountAsync(Guid conversationId, CancellationToken cancellationToken = default);
}