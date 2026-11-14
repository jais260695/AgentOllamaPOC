using AgentOllamaPOC.Guardrails.Abstractions;
using AgentOllamaPOC.Guardrails.Configuration;
using AgentOllamaPOC.Guardrails.Models;
using Microsoft.Extensions.Options;

namespace AgentOllamaPOC.Guardrails.Input;

public sealed class InputLengthGuardrail : IGuardrail
{
    private readonly GuardrailOptions _options;

    public InputLengthGuardrail(IOptions<GuardrailOptions> options)
    {
        _options = options.Value;

        if (_options.MaxInputLength <= 0)
        {
            throw new InvalidOperationException("MaxInputLength must be greater than zero.");
        }
    }

    public string Name => "InputLengthGuardrail";

    public GuardrailStage Stage => GuardrailStage.Input;

    public int Order => 1;

    public Task<GuardrailResult> EvaluateAsync(GuardrailContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Content))
        {
            return Task.FromResult( GuardrailResult.Block( "Input is empty or contains only whitespace.","Please enter a valid question."));
        }

        if (context.Content.Length > _options.MaxInputLength)
        {
            return Task.FromResult(
                    GuardrailResult.Block(
                        $"Input length {context.Content.Length} exceeds the configured limit.",
                        $"Your message is too long. Please keep it under {_options.MaxInputLength} characters."
                        )
                );
        }

        return Task.FromResult(GuardrailResult.Allow());
    }
}