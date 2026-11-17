using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Guardrails.Tokens;

public sealed class GenericTokenCounter : ITokenCounter
{
    // Conservative approximation: approximately 1 token per 3 characters.
    private const int CharactersPerToken = 3;

    public ValueTask<int> CountTokensAsync(
        IList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        cancellationToken.ThrowIfCancellationRequested();

        var characterCount = 0;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Account for message role and basic message structure.
            characterCount += 4;

            if (message.Text is not null)
                characterCount += message.Text.Length;

            if (message.Role == ChatRole.Assistant)
                characterCount += 1;
        }

        var estimatedTokens = (int)Math.Ceiling(
            (double)characterCount / CharactersPerToken);

        return ValueTask.FromResult(estimatedTokens);
    }
}