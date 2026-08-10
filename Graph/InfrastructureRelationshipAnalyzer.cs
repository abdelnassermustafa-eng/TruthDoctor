using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

public sealed class InfrastructureRelationshipAnalyzer
{
    private readonly InfrastructureGraphIndex _index;
    private readonly RelationshipSemanticsRegistry _semantics;

    public InfrastructureRelationshipAnalyzer(
        InfrastructureGraphIndex index,
        RelationshipSemanticsRegistry? semantics = null)
    {
        ArgumentNullException.ThrowIfNull(index);

        _index = index;

        _semantics =
            semantics ??
            new RelationshipSemanticsRegistry();
    }

    public IReadOnlyList<GraphEdge> GetOutgoing(
        string nodeId,
        RelationshipKind kind)
    {
        return _index
            .GetOutgoingEdges(nodeId)
            .Where(edge =>
                _semantics.ResolveKind(
                    edge.Relationship) == kind)
            .ToList();
    }

    public IReadOnlyList<GraphEdge> GetIncoming(
        string nodeId,
        RelationshipKind kind)
    {
        return _index
            .GetIncomingEdges(nodeId)
            .Where(edge =>
                _semantics.ResolveKind(
                    edge.Relationship) == kind)
            .ToList();
    }

    public IReadOnlyList<GraphEdge>
        GetDependencyRelationships(
            string nodeId)
    {
        return _index
            .GetOutgoingEdges(nodeId)
            .Where(edge =>
                _semantics.Resolve(
                    edge.Relationship)
                    .IsDependency)
            .ToList();
    }

    public IReadOnlyList<GraphEdge>
        GetConnectivityRelationships(
            string nodeId)
    {
        return _index
            .GetOutgoingEdges(nodeId)
            .Where(edge =>
                _semantics.Resolve(
                    edge.Relationship)
                    .IsConnectivity)
            .ToList();
    }

    public IReadOnlyList<GraphEdge>
        GetSecurityRelationships(
            string nodeId)
    {
        return _index
            .GetOutgoingEdges(nodeId)
            .Where(edge =>
                _semantics.Resolve(
                    edge.Relationship)
                    .IsSecurity)
            .ToList();
    }

    public RelationshipSemantic Describe(
        GraphEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        return _semantics.Resolve(
            edge.Relationship);
    }
}
