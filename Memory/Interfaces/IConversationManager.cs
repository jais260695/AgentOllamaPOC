using AgentOllamaPOC.Models;

namespace AgentOllamaPOC.Memory.Interfaces;

public interface IConversationManager
{
    Conversation Create();

    Task<Conversation> GetAsync(Guid conversationId);

    Task SaveAsync(Conversation conversation);
}