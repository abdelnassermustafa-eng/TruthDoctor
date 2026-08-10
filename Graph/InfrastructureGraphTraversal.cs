using System;
using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class InfrastructureGraphTraversal
{
    private readonly InfrastructureGraphIndex _index;
    private readonly RelationshipSemanticsRegistry _semantics;

    public InfrastructureGraphTraversal(
        InfrastructureGraphIndex index,
        RelationshipSemanticsRegistry? semantics = null)
    {
        ArgumentNullException.ThrowIfNull(index);

        _index = index;

        _semantics =
            semantics ??
            new RelationshipSemanticsRegistry();
    }

    public IReadOnlyList<GraphNode> GetTransitiveDependencies(
        string nodeId)
    {
        return Traverse(
            nodeId,
            edge => edge.TargetId);
    }

    public IReadOnlyList<GraphNode> GetTransitiveDependents(
        string nodeId)
    {
        return Traverse(
            nodeId,
            edge => edge.SourceId,
            reverse: true);
    }

    private List<GraphNode> Traverse(
        string startNodeId,
        Func<GraphEdge, string> nextNode,
        bool reverse = false)
    {
        var visited = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var queue = new Queue<string>();

        var result = new List<GraphNode>();

        queue.Enqueue(startNodeId);

        visited.Add(startNodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            var edges =
                reverse
                    ? _index.GetIncomingEdges(current)
                    : _index.GetOutgoingEdges(current);

            foreach (var edge in edges)
            {
                if (!_semantics
                        .Resolve(edge.Relationship)
                        .IsDependency)
                {
                    continue;
                }

                var id = nextNode(edge);

                if (!visited.Add(id))
                    continue;

                var node = _index.FindNode(id);

                if (node is null)
                    continue;

                result.Add(node);

                queue.Enqueue(id);
            }
        }

        return result;
    }
}
