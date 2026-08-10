using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class ResourceGraphContext
{
    public GraphNode? Resource { get; init; }

    public ImpactAnalysisResult Impact { get; init; } =
        new();

    public GraphRelationshipQueryResult Dependencies
    { get; init; } =
        new();

    public GraphRelationshipQueryResult Dependents
    { get; init; } =
        new();

    public GraphRelationshipQueryResult Security
    { get; init; } =
        new();

    public GraphRelationshipQueryResult Connectivity
    { get; init; } =
        new();

    public GraphRelationshipQueryResult Contains
    { get; init; } =
        new();

    public GraphRelationshipQueryResult ContainedBy
    { get; init; } =
        new();

    public IReadOnlyList<GraphNode> Neighborhood
    { get; init; } =
        [];
}
