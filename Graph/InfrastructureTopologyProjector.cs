using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Graph;

public sealed class InfrastructureTopologyProjector
{
    private readonly InfrastructureGraphIndex _index;
    private readonly RelationshipSemanticsRegistry _semantics;

    public InfrastructureTopologyProjector(
        InfrastructureGraphIndex index,
        RelationshipSemanticsRegistry? semantics = null)
    {
        ArgumentNullException.ThrowIfNull(index);

        _index = index;

        _semantics =
            semantics ??
            new RelationshipSemanticsRegistry();
    }

    public TopologyView ProjectNeighborhood(
        string selectedResourceId,
        int depth = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            selectedResourceId);

        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(depth));
        }

        var selected =
            _index.FindNode(selectedResourceId);

        if (selected is null)
        {
            return new TopologyView
            {
                SelectedResourceId =
                    selectedResourceId
            };
        }

        var includedIds =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                selectedResourceId
            };

        var frontier =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                selectedResourceId
            };

        for (var level = 0;
             level < depth;
             level++)
        {
            var next =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var nodeId in frontier)
            {
                foreach (var edge in
                         _index.GetOutgoingEdges(nodeId))
                {
                    if (includedIds.Add(edge.TargetId))
                    {
                        next.Add(edge.TargetId);
                    }
                }

                foreach (var edge in
                         _index.GetIncomingEdges(nodeId))
                {
                    if (includedIds.Add(edge.SourceId))
                    {
                        next.Add(edge.SourceId);
                    }
                }
            }

            frontier = next;

            if (frontier.Count == 0)
            {
                break;
            }
        }

        var nodes =
            includedIds
                .Select(_index.FindNode)
                .Where(node => node is not null)
                .Cast<GraphNode>()
                .Select(node =>
                {
                    var resource =
                        node.Resource as InfrastructureResource;

                    return new TopologyNode
                    {
                        Id = node.Id,

                        ProviderId =
                            node.ProviderId,

                        AccountId =
                            resource?.AccountId ?? "",

                        DomainId =
                            node.DomainId,

                        ResourceType =
                            node.ResourceType,

                        DisplayName =
                            node.DisplayName,

                        NativeId =
                            resource?.NativeId ?? "",

                        State =
                            resource?.State ?? "",

                        Location =
                            resource?.Location ?? "",

                        AvailabilityZone =
                            resource?.AvailabilityZone ?? "",

                        Arn =
                            resource?.Arn ?? "",

                        Properties =
                            resource?.Properties ??
                            new Dictionary<string, string>(),

                        Tags =
                            resource?.Tags ??
                            new Dictionary<string, string>(),

                        IsSelected =
                            node.Id.Equals(
                                selectedResourceId,
                                StringComparison.OrdinalIgnoreCase)
                    };
                })
                .OrderByDescending(node =>
                    node.IsSelected)
                .ThenBy(node =>
                    node.DomainId)
                .ThenBy(node =>
                    node.DisplayName)
                .ToList();

        var edges =
            _index.Graph.Edges
                .Where(edge =>
                    includedIds.Contains(
                        edge.SourceId) &&
                    includedIds.Contains(
                        edge.TargetId))
                .Select(edge =>
                    new TopologyEdge
                    {
                        SourceId =
                            edge.SourceId,

                        TargetId =
                            edge.TargetId,

                        Relationship =
                            edge.Relationship,

                        Kind =
                            _semantics.ResolveKind(
                                edge.Relationship)
                    })
                .ToList();

        return new TopologyView
        {
            SelectedResourceId =
                selectedResourceId,

            Nodes = nodes,

            Edges = edges
        };
    }
}
