using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

public sealed class InfrastructureGraphIndex
{
    private readonly InfrastructureGraph _graph;

    private readonly Dictionary<string, List<GraphNode>>
        _nodesByProvider =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<GraphNode>>
        _nodesByDomain =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<GraphNode>>
        _nodesByResourceType =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<GraphEdge>>
        _outgoing =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<GraphEdge>>
        _incoming =
            new(StringComparer.OrdinalIgnoreCase);

    public InfrastructureGraphIndex(
        InfrastructureGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        _graph = graph;

        Build();
    }

    public InfrastructureGraph Graph => _graph;

    public GraphNode? FindNode(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _graph.Nodes.TryGetValue(
            id,
            out var node)
            ? node
            : null;
    }

    public IReadOnlyList<GraphNode> FindByProvider(
        string providerId)
    {
        return GetNodes(
            _nodesByProvider,
            providerId);
    }

    public IReadOnlyList<GraphNode> FindByDomain(
        string domainId)
    {
        return GetNodes(
            _nodesByDomain,
            domainId);
    }

    public IReadOnlyList<GraphNode> FindByResourceType(
        string resourceType)
    {
        return GetNodes(
            _nodesByResourceType,
            resourceType);
    }

    public IReadOnlyList<GraphEdge> GetOutgoingEdges(
        string nodeId)
    {
        return GetEdges(
            _outgoing,
            nodeId);
    }

    public IReadOnlyList<GraphEdge> GetIncomingEdges(
        string nodeId)
    {
        return GetEdges(
            _incoming,
            nodeId);
    }

    public IReadOnlyList<GraphNode> GetDependencies(
        string nodeId)
    {
        return GetOutgoingEdges(nodeId)
            .Select(edge =>
                FindNode(edge.TargetId))
            .Where(node => node is not null)
            .Cast<GraphNode>()
            .DistinctBy(node => node.Id)
            .ToList();
    }

    public IReadOnlyList<GraphNode> GetDependents(
        string nodeId)
    {
        return GetIncomingEdges(nodeId)
            .Select(edge =>
                FindNode(edge.SourceId))
            .Where(node => node is not null)
            .Cast<GraphNode>()
            .DistinctBy(node => node.Id)
            .ToList();
    }

    public IReadOnlyList<GraphNode> GetNeighbors(
        string nodeId)
    {
        return GetDependencies(nodeId)
            .Concat(GetDependents(nodeId))
            .DistinctBy(node => node.Id)
            .ToList();
    }

    private void Build()
    {
        foreach (var node in _graph.Nodes.Values)
        {
            AddNodeIndex(
                _nodesByProvider,
                node.ProviderId,
                node);

            AddNodeIndex(
                _nodesByDomain,
                node.DomainId,
                node);

            AddNodeIndex(
                _nodesByResourceType,
                node.ResourceType,
                node);
        }

        foreach (var edge in _graph.Edges)
        {
            AddEdgeIndex(
                _outgoing,
                edge.SourceId,
                edge);

            AddEdgeIndex(
                _incoming,
                edge.TargetId,
                edge);
        }
    }

    private static void AddNodeIndex(
        Dictionary<string, List<GraphNode>> index,
        string key,
        GraphNode node)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!index.TryGetValue(
                key,
                out var nodes))
        {
            nodes = [];
            index[key] = nodes;
        }

        nodes.Add(node);
    }

    private static void AddEdgeIndex(
        Dictionary<string, List<GraphEdge>> index,
        string key,
        GraphEdge edge)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!index.TryGetValue(
                key,
                out var edges))
        {
            edges = [];
            index[key] = edges;
        }

        edges.Add(edge);
    }

    private static IReadOnlyList<GraphNode> GetNodes(
        Dictionary<string, List<GraphNode>> index,
        string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return [];
        }

        return index.TryGetValue(
            key,
            out var nodes)
            ? nodes
            : [];
    }

    private static IReadOnlyList<GraphEdge> GetEdges(
        Dictionary<string, List<GraphEdge>> index,
        string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return [];
        }

        return index.TryGetValue(
            key,
            out var edges)
            ? edges
            : [];
    }
}
