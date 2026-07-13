using System.Runtime.Serialization;

namespace AgentOllamaPOC.Models;
public sealed class RouteDecision
{
    public RouteDecisionType Route { get; set; } = RouteDecisionType.Default;

    public float Confidence { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public enum RouteDecisionType
{
    [EnumMember(Value = "GithubAgent")]
    GithubAgent,

    [EnumMember(Value = "RagAgent")]
    RagAgent,

    [EnumMember(Value = "Default")]
    Default
}
