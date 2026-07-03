using AgentOllamaPOC.Memory.Interfaces;
using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Execution;

public sealed class ConversationManager : IConversationManager
{
    private Conversation? _current;

    public Conversation Current => _current ??= new Conversation();

    public Conversation Create()
    {
        _current = new Conversation();
        return _current;
    }

    public Task<Conversation> GetAsync(Guid conversationId)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(Conversation conversation)
    {
        throw new NotImplementedException();
    }
}