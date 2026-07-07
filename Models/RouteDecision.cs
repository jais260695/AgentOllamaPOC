namespace AgentOllamaPOC.Models;
public sealed class RouteDecision
{
    public string Route { get; set; } = string.Empty;

    public float Confidence { get; set; }

    public string Reason { get; set; } = string.Empty;
}
