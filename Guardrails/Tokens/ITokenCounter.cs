using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Guardrails.Tokens;
public interface ITokenCounter
{
    ValueTask<int> CountTokensAsync(
        IList<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
