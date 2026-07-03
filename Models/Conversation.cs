using Microsoft.Extensions.AI;

namespace AgentOllamaPOC.Models;

public sealed class Conversation
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string? Summary { get; set; }

    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}