using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class GraphPathResult
{
    public string SourceId { get; init; } = "";

    public string TargetId { get; init; } = "";

    public bool Found { get; init; }

    public IReadOnlyList<GraphNode> Nodes { get; init; } =
        [];

    public IReadOnlyList<GraphEdge> Edges { get; init; } =
        [];

    public int HopCount =>
        Edges.Count;
}
