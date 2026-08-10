using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class GraphRelationshipQueryResult
{
    public string ResourceId { get; init; } = "";

    public string Query { get; init; } = "";

    public IReadOnlyList<GraphNode> Resources { get; init; } =
        [];

    public IReadOnlyList<GraphEdge> Relationships { get; init; } =
        [];

    public int Count =>
        Resources.Count;
}
