namespace TruthDoctor.Graph;

public sealed class TopologyEdge
{
    public string SourceId { get; init; } = "";

    public string TargetId { get; init; } = "";

    public string Relationship { get; init; } = "";

    public RelationshipKind Kind { get; init; } =
        RelationshipKind.Unknown;

    /// <summary>
    /// Number of equivalent graph relationships represented by this
    /// projected topology edge.
    /// </summary>
    public int Multiplicity { get; init; } = 1;
}
