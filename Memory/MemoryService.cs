using AgentOllamaPOC.Memory.Interfaces;
using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Memory;

public sealed class MemoryService
{
    private readonly IMemoryStore _store;

    public MemoryService(IMemoryStore store)
    {
        _store = store;
    }

    public Task AddUserMessageAsync(Guid conversationId, string message, CancellationToken cancellationToken = default)
    {
        return _store.AppendAsync(conversationId, new ChatMessage(ChatRole.User, message), cancellationToken);
    }

    public Task AddAssistantMessageAsync(Guid conversationId, string message, CancellationToken cancellationToken = default)
    {
        return _store.AppendAsync(conversationId, new ChatMessage(ChatRole.Assistant, message), cancellationToken);
    }

    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, int start = -10, int stop = -1, CancellationToken cancellationToken = default)
    {
        return _store.GetHistoryAsync(conversationId, start, stop, cancellationToken);
    }

    public Task ClearAsync(Guid conversationId , CancellationToken cancellationToken = default)
    {
        return _store.ClearAsync(conversationId, cancellationToken);
    }

    public Task<int> TrimHistoryAsync(Guid conversationId, int keepLastMessages, CancellationToken cancellationToken = default)
    {
        return _store.TrimHistoryAsync(conversationId, keepLastMessages, cancellationToken);
    }

    public Task<int> GetMessageCountAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return _store.GetMessageCountAsync(conversationId, cancellationToken);
    }
}