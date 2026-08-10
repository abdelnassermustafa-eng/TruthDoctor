namespace TruthDoctor.Graph;

public sealed class GraphEdge
{
    public required string SourceId { get; init; }

    public required string TargetId { get; init; }

    public required string Relationship { get; init; }
}
