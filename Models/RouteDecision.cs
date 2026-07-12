namespace AgentOllamaPOC.Models;
public sealed class RouteDecision
{
    public RouteDecisionType Route { get; set; } = RouteDecisionType.humanagent;

    public float Confidence { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public enum RouteDecisionType
{
    githubagent,
    ragagent,
    humanagent,
}
