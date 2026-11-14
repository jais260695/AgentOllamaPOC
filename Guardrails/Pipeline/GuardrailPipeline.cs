using AgentOllamaPOC.Guardrails.Abstractions;
using AgentOllamaPOC.Guardrails.Models;
using AgentOllamaPOC.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AgentOllamaPOC.Guardrails.Pipeline;

public sealed class GuardrailPipeline : IGuardrailPipeline
{
    private readonly IReadOnlyList<IGuardrail> _guardrails;
    private readonly ILogger<GuardrailPipeline> _logger;

    public GuardrailPipeline(IEnumerable<IGuardrail> guardrails, ILogger<GuardrailPipeline> logger)
    {
        _guardrails = guardrails.OrderBy(g => g.Stage).ThenBy(g => g.Order).ToList();
        _logger = logger;
    }

    public async Task<GuardrailResult> ExecuteAsync(GuardrailContext context, CancellationToken cancellationToken = default)
    {
        using var activity = AgentTelemetry.StartActivity("guardrails.pipeline");

        activity?.SetTag("guardrail.stage", context.Stage.ToString());

        var applicableGuardrails = _guardrails.Where(g => g.Stage == context.Stage).OrderBy(g => g.Order);

        foreach (var guardrail in applicableGuardrails)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var guardrailActivity = AgentTelemetry.StartActivity("guardrails.evaluate", guardrail.Name);

            guardrailActivity?.SetTag("guardrail.stage", context.Stage.ToString());
            guardrailActivity?.SetTag("guardrail.order", guardrail.Order);

            GuardrailResult result;

            try
            {
                result = await guardrail.EvaluateAsync(context, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                guardrailActivity?.SetTag("guardrail.outcome", "error");
                activity?.SetTag("guardrail.outcome", "error");

                _logger.LogError(ex,"Guardrail {GuardrailName} failed during {Stage} evaluation",guardrail.Name,context.Stage);

                // Fail closed: do not continue agent execution
                // when a guardrail cannot complete.
                return GuardrailResult.Block($"Guardrail '{guardrail.Name}' failed to execute.", "Your request couldn't be processed safely. Please try again.");
            }

            guardrailActivity?.SetTag("guardrail.decision", result.Decision.ToString());

            if (result.Decision != GuardrailDecision.Allow)
            {
                activity?.SetTag("guardrail.outcome", result.Decision.ToString());

                _logger.LogWarning("Guardrail {GuardrailName} returned {Decision} for stage {Stage}",guardrail.Name, result.Decision, context.Stage);

                return result;
            }
        }

        activity?.SetTag("guardrail.outcome", "allowed");

        return GuardrailResult.Allow();
    }
}