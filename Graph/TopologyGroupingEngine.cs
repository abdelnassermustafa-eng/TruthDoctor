using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

/// <summary>
/// Creates deterministic provider-neutral topology groups.
///
/// Groups are derived only from universal topology fields. The
/// grouping engine contains no provider or UI dependencies.
/// </summary>
public sealed class TopologyGroupingEngine
{
    private const string OtherGroupId = "other";

    public IReadOnlyList<TopologyGroup> GroupByDomain(
        TopologyView topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        return topology.Nodes
            .GroupBy(
                node =>
                    NormalizeDomainId(
                        node.DomainId),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group =>
                group.Key,
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
                new TopologyGroup
                {
                    Id =
                        group.Key,

                    DisplayName =
                        BuildDisplayName(
                            group.Key),

                    NodeIds =
                        group
                            .OrderBy(node =>
                                node.DisplayName,
                                StringComparer.OrdinalIgnoreCase)
                            .ThenBy(node =>
                                node.Id,
                                StringComparer.OrdinalIgnoreCase)
                            .Select(node =>
                                node.Id)
                            .ToList()
                })
            .ToList();
    }

    public TopologyGroup? FindGroup(
        TopologyView topology,
        string nodeId)
    {
        ArgumentNullException.ThrowIfNull(topology);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            nodeId);

        return GroupByDomain(topology)
            .FirstOrDefault(group =>
                group.NodeIds.Contains(
                    nodeId,
                    StringComparer.OrdinalIgnoreCase));
    }

    private static string NormalizeDomainId(
        string? domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId))
        {
            return OtherGroupId;
        }

        return domainId
            .Trim()
            .ToLowerInvariant();
    }

    private static string BuildDisplayName(
        string domainId)
    {
        if (domainId.Equals(
                OtherGroupId,
                StringComparison.OrdinalIgnoreCase))
        {
            return "Other";
        }

        var words =
            domainId
                .Split(
                    ['-', '_'],
                    StringSplitOptions
                        .RemoveEmptyEntries);

        return string.Join(
            " ",
            words.Select(word =>
                char.ToUpperInvariant(word[0]) +
                word[1..].ToLowerInvariant()));
    }
}
