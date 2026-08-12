using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// Produces a domain-specific topology projection without introducing
/// provider or UI dependencies.
/// </summary>
public sealed class TopologyDomainFilter
{
    public const string AllDomains = "";

    public TopologyView Apply(
        TopologyView topology,
        string? domainId)
    {
        ArgumentNullException.ThrowIfNull(topology);

        var normalizedDomainId =
            NormalizeDomainId(domainId);

        if (normalizedDomainId.Length == 0)
        {
            return topology;
        }

        var nodes =
            topology.Nodes
                .Where(node =>
                    NormalizeNodeDomainId(
                        node.DomainId)
                    .Equals(
                        normalizedDomainId,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        var includedNodeIds =
            nodes
                .Select(node => node.Id)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        var edges =
            topology.Edges
                .Where(edge =>
                    includedNodeIds.Contains(
                        edge.SourceId) &&
                    includedNodeIds.Contains(
                        edge.TargetId))
                .ToList();

        var selectedResourceId =
            includedNodeIds.Contains(
                topology.SelectedResourceId)
                ? topology.SelectedResourceId
                : "";

        return new TopologyView
        {
            SelectedResourceId =
                selectedResourceId,

            Nodes =
                nodes,

            Edges =
                edges
        };
    }

    public IReadOnlyList<TopologyGroup>
        AvailableDomains(
            TopologyView topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        return new TopologyGroupingEngine()
            .GroupByDomain(topology);
    }

    private static string NormalizeDomainId(
        string? domainId)
    {
        return string.IsNullOrWhiteSpace(domainId)
            ? ""
            : domainId
                .Trim()
                .ToLowerInvariant();
    }

    private static string NormalizeNodeDomainId(
        string? domainId)
    {
        var normalized =
            NormalizeDomainId(domainId);

        return normalized.Length == 0
            ? "other"
            : normalized;
    }
}
