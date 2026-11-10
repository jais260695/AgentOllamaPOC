
namespace AgentOllamaPOC;

public sealed class LlmProviderOptions
{
    public const string SectionName = "Llm";

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}
