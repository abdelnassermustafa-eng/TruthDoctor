using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// Creates a provider-neutral topology projection in which selected
/// infrastructure domains are represented by summary nodes.
/// </summary>
public sealed class TopologyGroupCollapseEngine
{
    public const string SummaryResourceType =
        "domain-summary";

    private const string SummaryIdPrefix =
        "topology-domain-summary:";

    private readonly TopologyGroupingEngine _groupingEngine =
        new();

    public TopologyView Project(
        TopologyView topology,
        IEnumerable<string>? collapsedDomainIds)
    {
        ArgumentNullException.ThrowIfNull(topology);

        var requestedCollapsedIds =
            (collapsedDomainIds ?? [])
                .Where(domainId =>
                    !string.IsNullOrWhiteSpace(
                        domainId))
                .Select(NormalizeDomainId)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        if (requestedCollapsedIds.Count == 0 ||
            topology.Nodes.Count == 0)
        {
            return topology;
        }

        var groups =
            _groupingEngine.GroupByDomain(
                topology);

        var collapsedGroups =
            groups
                .Where(group =>
                    requestedCollapsedIds.Contains(
                        group.Id))
                .ToList();

        if (collapsedGroups.Count == 0)
        {
            return topology;
        }

        var collapsedGroupByNodeId =
            new Dictionary<string, TopologyGroup>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in collapsedGroups)
        {
            foreach (var nodeId in group.NodeIds)
            {
                collapsedGroupByNodeId[nodeId] =
                    group;
            }
        }

        var nodes =
            topology.Nodes
                .Where(node =>
                    !collapsedGroupByNodeId.ContainsKey(
                        node.Id))
                .ToList();

        nodes.AddRange(
            collapsedGroups.Select(group =>
                BuildSummaryNode(
                    group,
                    group.NodeIds.Contains(
                        topology.SelectedResourceId,
                        StringComparer.OrdinalIgnoreCase))));

        var projectedEdges =
            topology.Edges
                .Select(edge =>
                    ProjectEdge(
                        edge,
                        collapsedGroupByNodeId))
                .Where(edge =>
                    edge is not null)
                .Cast<TopologyEdge>()
                .GroupBy(
                    EdgeIdentity,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var first =
                        group.First();

                    return new TopologyEdge
                    {
                        SourceId =
                            first.SourceId,

                        TargetId =
                            first.TargetId,

                        Relationship =
                            first.Relationship,

                        Kind =
                            first.Kind,

                        Multiplicity =
                            group.Sum(edge =>
                                Math.Max(
                                    1,
                                    edge.Multiplicity))
                    };
                })
                .OrderBy(edge =>
                    edge.SourceId,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(edge =>
                    edge.TargetId,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(edge =>
                    edge.Relationship,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(edge =>
                    edge.Kind)
                .ToList();

        var projectedSelectedResourceId =
            collapsedGroupByNodeId.TryGetValue(
                topology.SelectedResourceId,
                out var selectedCollapsedGroup)
                ? SummaryNodeId(
                    selectedCollapsedGroup.Id)
                : topology.SelectedResourceId;

        return new TopologyView
        {
            SelectedResourceId =
                projectedSelectedResourceId,

            Nodes =
                nodes
                    .OrderBy(node =>
                        node.DomainId,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(node =>
                        node.DisplayName,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(node =>
                        node.Id,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList(),

            Edges =
                projectedEdges
        };
    }

    public static bool IsSummaryNode(
        TopologyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node.ResourceType.Equals(
            SummaryResourceType,
            StringComparison.OrdinalIgnoreCase);
    }

    public static string SummaryNodeId(
        string domainId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            domainId);

        return
            SummaryIdPrefix +
            NormalizeDomainId(domainId);
    }

    private static TopologyNode BuildSummaryNode(
        TopologyGroup group,
        bool isSelected)
    {
        return new TopologyNode
        {
            Id =
                SummaryNodeId(group.Id),

            ProviderId =
                "universal",

            DomainId =
                group.Id,

            ResourceType =
                SummaryResourceType,

            DisplayName =
                $"{group.DisplayName} · " +
                $"{group.Count}",

            NativeId =
                group.Id,

            State =
                "collapsed",

            Properties =
                new Dictionary<string, string>
                {
                    ["DomainId"] =
                        group.Id,

                    ["DomainName"] =
                        group.DisplayName,

                    ["ResourceCount"] =
                        group.Count.ToString(),

                    ["Collapsed"] =
                        bool.TrueString
                },

            IsSelected =
                isSelected
        };
    }

    private static TopologyEdge? ProjectEdge(
        TopologyEdge edge,
        IReadOnlyDictionary<
            string,
            TopologyGroup> collapsedGroupByNodeId)
    {
        var sourceGroup =
            collapsedGroupByNodeId.GetValueOrDefault(
                edge.SourceId);

        var targetGroup =
            collapsedGroupByNodeId.GetValueOrDefault(
                edge.TargetId);

        if (sourceGroup is not null &&
            targetGroup is not null &&
            sourceGroup.Id.Equals(
                targetGroup.Id,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var sourceId =
            sourceGroup is null
                ? edge.SourceId
                : SummaryNodeId(
                    sourceGroup.Id);

        var targetId =
            targetGroup is null
                ? edge.TargetId
                : SummaryNodeId(
                    targetGroup.Id);

        if (sourceId.Equals(
                targetId,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new TopologyEdge
        {
            SourceId =
                sourceId,

            TargetId =
                targetId,

            Relationship =
                edge.Relationship,

            Kind =
                edge.Kind,

            Multiplicity =
                Math.Max(
                    1,
                    edge.Multiplicity)
        };
    }

    private static string EdgeIdentity(
        TopologyEdge edge)
    {
        return
            $"{edge.SourceId}\u001F" +
            $"{edge.TargetId}\u001F" +
            $"{edge.Relationship}\u001F" +
            $"{edge.Kind}";
    }

    private static string NormalizeDomainId(
        string domainId)
    {
        return domainId
            .Trim()
            .ToLowerInvariant();
    }
}
