namespace AgentOllamaPOC.Guardrails.Models;
public sealed record GuardrailResult
{
    public required GuardrailDecision Decision { get; init; }

    public string? Reason { get; init; }

    public string? UserMessage { get; init; }

    public static GuardrailResult Allow() =>
        new() 
        { 
            Decision = GuardrailDecision.Allow 
        };

    public static GuardrailResult Block(
        string reason,
        string userMessage) =>
        new()
        {
            Decision = GuardrailDecision.Block,
            Reason = reason,
            UserMessage = userMessage
        };

    public static GuardrailResult RequireApproval(
        string reason,
        string userMessage) =>
        new()
        {
            Decision = GuardrailDecision.RequireApproval,
            Reason = reason,
            UserMessage = userMessage
        };
}
