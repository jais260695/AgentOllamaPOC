namespace AgentOllamaPOC.Memory.Models;

public sealed class StoredMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }
}