using System;
using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class InfrastructurePathAnalyzer
{
    private readonly InfrastructureGraphIndex _index;

    public InfrastructurePathAnalyzer(
        InfrastructureGraphIndex index)
    {
        ArgumentNullException.ThrowIfNull(index);

        _index = index;
    }

    public GraphPathResult FindShortestPath(
        string sourceId,
        string targetId,
        bool includeReverseRelationships = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var source =
            _index.FindNode(sourceId);

        var target =
            _index.FindNode(targetId);

        if (source is null || target is null)
        {
            return NotFound(
                sourceId,
                targetId);
        }

        if (sourceId.Equals(
                targetId,
                StringComparison.OrdinalIgnoreCase))
        {
            return new GraphPathResult
            {
                SourceId = sourceId,
                TargetId = targetId,
                Found = true,
                Nodes = [source],
                Edges = []
            };
        }

        var queue =
            new Queue<string>();

        var visited =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var previousNode =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        var previousEdge =
            new Dictionary<string, GraphEdge>(
                StringComparer.OrdinalIgnoreCase);

        queue.Enqueue(sourceId);
        visited.Add(sourceId);

        while (queue.Count > 0)
        {
            var current =
                queue.Dequeue();

            foreach (var candidate in
                     EnumerateNeighbors(
                         current,
                         includeReverseRelationships))
            {
                if (!visited.Add(candidate.NodeId))
                {
                    continue;
                }

                previousNode[candidate.NodeId] =
                    current;

                previousEdge[candidate.NodeId] =
                    candidate.Edge;

                if (candidate.NodeId.Equals(
                        targetId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BuildPath(
                        sourceId,
                        targetId,
                        previousNode,
                        previousEdge);
                }

                queue.Enqueue(candidate.NodeId);
            }
        }

        return NotFound(
            sourceId,
            targetId);
    }

    private IEnumerable<PathCandidate>
        EnumerateNeighbors(
            string nodeId,
            bool includeReverseRelationships)
    {
        foreach (var edge in
                 _index.GetOutgoingEdges(nodeId))
        {
            yield return new PathCandidate(
                edge.TargetId,
                edge);
        }

        if (!includeReverseRelationships)
        {
            yield break;
        }

        foreach (var edge in
                 _index.GetIncomingEdges(nodeId))
        {
            yield return new PathCandidate(
                edge.SourceId,
                edge);
        }
    }

    private GraphPathResult BuildPath(
        string sourceId,
        string targetId,
        IReadOnlyDictionary<string, string> previousNode,
        IReadOnlyDictionary<string, GraphEdge> previousEdge)
    {
        var nodeIds =
            new List<string>();

        var edges =
            new List<GraphEdge>();

        var current =
            targetId;

        nodeIds.Add(current);

        while (!current.Equals(
                   sourceId,
                   StringComparison.OrdinalIgnoreCase))
        {
            if (!previousNode.TryGetValue(
                    current,
                    out var previous))
            {
                return NotFound(
                    sourceId,
                    targetId);
            }

            if (!previousEdge.TryGetValue(
                    current,
                    out var edge))
            {
                return NotFound(
                    sourceId,
                    targetId);
            }

            edges.Add(edge);

            current = previous;

            nodeIds.Add(current);
        }

        nodeIds.Reverse();
        edges.Reverse();

        var nodes =
            new List<GraphNode>();

        foreach (var id in nodeIds)
        {
            var node =
                _index.FindNode(id);

            if (node is not null)
            {
                nodes.Add(node);
            }
        }

        return new GraphPathResult
        {
            SourceId = sourceId,
            TargetId = targetId,
            Found = true,
            Nodes = nodes,
            Edges = edges
        };
    }

    private static GraphPathResult NotFound(
        string sourceId,
        string targetId)
    {
        return new GraphPathResult
        {
            SourceId = sourceId,
            TargetId = targetId,
            Found = false
        };
    }

    private sealed record PathCandidate(
        string NodeId,
        GraphEdge Edge);
}
