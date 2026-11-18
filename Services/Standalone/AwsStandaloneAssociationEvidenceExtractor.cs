using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Verified AWS configuration association evidence.
/// Not an operational graph edge or observed traffic claim.
/// </summary>
public sealed record AwsStandaloneAssociationEvidence(
    string SourceResourceId,
    string TargetResourceId,
    string AssociationType,
    string PropertyName);

/// <summary>
/// Offline extraction of explicit subnet-to-route-table
/// association evidence from discovered AWS resources.
/// </summary>
public static class AwsStandaloneAssociationEvidenceExtractor
{
    private const string AssociationType =
        "subnet-route-table-association";

    private static readonly Regex SubnetAssociationPattern =
        new(
            @"^Associations\[\d+\]\.SubnetId$",
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

    public static IReadOnlyList<AwsStandaloneAssociationEvidence>
        Extract(IReadOnlyList<InfrastructureResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        var output =
            new List<AwsStandaloneAssociationEvidence>();

        var identities =
            new HashSet<(string SourceId, string TargetId)>();

        foreach (var table in resources)
        {
            if (!StringComparer.Ordinal.Equals(
                    table.ProviderId, "aws") ||
                !StringComparer.Ordinal.Equals(
                    table.ResourceType, "route-table"))
            {
                continue;
            }

            if (!table.Properties.TryGetValue(
                    "VpcId", out var tableVpcId) ||
                string.IsNullOrWhiteSpace(tableVpcId))
            {
                continue;
            }

            foreach (var property in table.Properties
                         .OrderBy(
                             item => item.Key,
                             StringComparer.Ordinal))
            {
                if (!SubnetAssociationPattern.IsMatch(
                        property.Key) ||
                    string.IsNullOrWhiteSpace(property.Value))
                {
                    continue;
                }

                var candidates = resources
                    .Where(candidate =>
                        StringComparer.Ordinal.Equals(
                            candidate.ProviderId, "aws") &&
                        StringComparer.Ordinal.Equals(
                            candidate.AccountId, table.AccountId) &&
                        StringComparer.Ordinal.Equals(
                            candidate.Location, table.Location) &&
                        StringComparer.Ordinal.Equals(
                            candidate.ResourceType, "subnet") &&
                        StringComparer.Ordinal.Equals(
                            candidate.NativeId, property.Value) &&
                        !StringComparer.Ordinal.Equals(
                            candidate.ResourceId, table.ResourceId) &&
                        candidate.Properties.TryGetValue(
                            "VpcId", out var subnetVpcId) &&
                        !string.IsNullOrWhiteSpace(subnetVpcId) &&
                        StringComparer.Ordinal.Equals(
                            subnetVpcId, tableVpcId))
                    .ToArray();

                if (candidates.Length != 1)
                {
                    continue;
                }

                var subnet = candidates[0];

                var identity = (
                    SourceId: subnet.ResourceId,
                    TargetId: table.ResourceId);

                if (!identities.Add(identity))
                {
                    continue;
                }

                output.Add(
                    new AwsStandaloneAssociationEvidence(
                        subnet.ResourceId,
                        table.ResourceId,
                        AssociationType,
                        property.Key));
            }
        }

        return output;
    }
}
