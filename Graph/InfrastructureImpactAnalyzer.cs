using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

public sealed class InfrastructureImpactAnalyzer
{
    private readonly InfrastructureGraphIndex _index;
    private readonly InfrastructureGraphTraversal _traversal;
    private readonly RelationshipSemanticsRegistry _semantics;

    public InfrastructureImpactAnalyzer(
        InfrastructureGraphIndex index,
        RelationshipSemanticsRegistry? semantics = null)
    {
        ArgumentNullException.ThrowIfNull(index);

        _index = index;

        _semantics =
            semantics ??
            new RelationshipSemanticsRegistry();

        _traversal =
            new InfrastructureGraphTraversal(
                index,
                _semantics);
    }

    public ImpactAnalysisResult Analyze(
        string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceId);

        var resource =
            _index.FindNode(resourceId);

        if (resource is null)
        {
            return new ImpactAnalysisResult
            {
                ResourceId = resourceId
            };
        }

        var directDependents =
            _index
                .GetIncomingEdges(resourceId)
                .Where(edge =>
                    _semantics
                        .Resolve(edge.Relationship)
                        .IsDependency)
                .Select(edge =>
                    _index.FindNode(edge.SourceId))
                .Where(node => node is not null)
                .Cast<GraphNode>()
                .DistinctBy(node => node.Id)
                .ToList();

        var affected =
            _traversal
                .GetTransitiveDependents(resourceId)
                .ToList();

        var byDomain =
            affected
                .GroupBy(
                    node => node.DomainId,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(),
                    StringComparer.OrdinalIgnoreCase);

        var byResourceType =
            affected
                .GroupBy(
                    node => node.ResourceType,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(),
                    StringComparer.OrdinalIgnoreCase);

        return new ImpactAnalysisResult
        {
            ResourceId = resourceId,

            DirectDependentCount =
                directDependents.Count,

            TotalAffectedCount =
                affected.Count,

            DirectDependents =
                directDependents,

            AffectedResources =
                affected,

            AffectedByDomain =
                byDomain,

            AffectedByResourceType =
                byResourceType
        };
    }
}
