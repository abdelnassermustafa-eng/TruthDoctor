using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class InfrastructureGraph
{
    public Dictionary<string, GraphNode> Nodes { get; } =
        [];

    public List<GraphEdge> Edges { get; } =
        [];

    public void AddNode(GraphNode node)
    {
        Nodes[node.Id] = node;
    }

    public void AddEdge(GraphEdge edge)
    {
        Edges.Add(edge);
    }
}
