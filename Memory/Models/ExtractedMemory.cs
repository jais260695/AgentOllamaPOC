namespace AgentOllamaPOC.Memory.Models;

public sealed class ExtractedMemory
{
    public string Content { get; set; } = string.Empty;

    public double Importance { get; set; }
}