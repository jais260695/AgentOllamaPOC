namespace AgentOllamaPOC.Memory.Models;

public class ConversationSummary
{
    public Guid ConversationId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public int LastProcessedMessageCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}