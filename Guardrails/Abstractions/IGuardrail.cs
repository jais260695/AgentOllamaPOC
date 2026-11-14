using AgentOllamaPOC.Guardrails.Models;

namespace AgentOllamaPOC.Guardrails.Abstractions;
public interface IGuardrail
{
    string Name { get; }

    GuardrailStage Stage { get; }

    int Order { get; }

    Task<GuardrailResult> EvaluateAsync(
        GuardrailContext context,
        CancellationToken cancellationToken = default);
}
