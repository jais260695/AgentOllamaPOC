namespace AgentOllamaPOC.Guardrails.Configuration;

public sealed class TokenGuardrailOptions
{
    public const string SectionName = "TokenGuardrails";

    public int MaxInputTokensPerCall { get; set; } = 8000;

    public int MaxOutputTokensPerCall { get; set; } = 2000;

    public int MaxTotalTokensPerRequest { get; set; } = 15000;
}