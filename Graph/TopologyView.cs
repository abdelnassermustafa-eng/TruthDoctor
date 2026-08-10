using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class TopologyView
{
    public string SelectedResourceId { get; init; } = "";

    public IReadOnlyList<TopologyNode> Nodes { get; init; } =
        [];

    public IReadOnlyList<TopologyEdge> Edges { get; init; } =
        [];
}
