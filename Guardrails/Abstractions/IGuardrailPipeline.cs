using AgentOllamaPOC.Guardrails.Models;

namespace AgentOllamaPOC.Guardrails.Abstractions;
public interface IGuardrailPipeline
{
    Task<GuardrailResult> ExecuteAsync(
        GuardrailContext context,
        CancellationToken cancellationToken = default);
}
