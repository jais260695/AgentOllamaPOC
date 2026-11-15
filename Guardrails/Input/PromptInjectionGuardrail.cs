using AgentOllamaPOC.Guardrails.Abstractions;
using AgentOllamaPOC.Guardrails.Models;
using System.Text.RegularExpressions;

namespace AgentOllamaPOC.Guardrails.Input;

public sealed class PromptInjectionGuardrail : IGuardrail
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private static readonly Regex[] InjectionPatterns =
    [
        new(
            @"\b(?:ignore|disregard|forget|override)\s+(?:all\s+)?(?:the\s+)?(?:previous|prior|above|earlier)\s+(?:system\s+|developer\s+)?(?:instructions?|rules?|prompts?)\b",
            Options,
            RegexTimeout),

        new(
            @"\b(?:reveal|show|print|repeat|disclose)\b.{0,80}\b(?:system prompt|developer instructions|hidden instructions)\b",
            Options,
            RegexTimeout),

        new(
            @"\b(?:act|behave|respond)\s+as\s+(?:an?\s+)?(?:unrestricted|unfiltered|jailbroken)\b",
            Options,
            RegexTimeout),

        new(
            @"\bdo\s+anything\s+now\b",
            Options,
            RegexTimeout),

        new(
            @"\b(?:bypass|disable|turn off|remove)\b.{0,60}\b(?:safety|security)\s+(?:filters?|rules?|guardrails?)\b",
            Options,
            RegexTimeout)
    ];

    public string Name => "PromptInjectionGuardrail";

    public GuardrailStage Stage => GuardrailStage.Input;

    public int Order => 2;

    public Task<GuardrailResult> EvaluateAsync(GuardrailContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Content))
        {
            return Task.FromResult(GuardrailResult.Allow());
        }

        var detected = InjectionPatterns.Any(pattern => pattern.IsMatch(context.Content));

        if (detected)
        {
            return Task.FromResult(
                        GuardrailResult.Block(
                                "Potential prompt injection pattern detected.",
                                "Your message contains instructions that cannot be processed. Please rephrase your request."
                            )
                    );
        }

        return Task.FromResult(GuardrailResult.Allow());
    }
}