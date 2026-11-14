namespace AgentOllamaPOC.Guardrails.Configuration;

public sealed class GuardrailOptions
{
    public const string SectionName = "Guardrails";

    public int MaxInputLength { get; set; } = 1000;
}
