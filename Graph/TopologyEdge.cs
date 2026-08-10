namespace TruthDoctor.Graph;

public sealed class TopologyEdge
{
    public string SourceId { get; init; } = "";

    public string TargetId { get; init; } = "";

    public string Relationship { get; init; } = "";

    public RelationshipKind Kind { get; init; } =
        RelationshipKind.Unknown;
}
