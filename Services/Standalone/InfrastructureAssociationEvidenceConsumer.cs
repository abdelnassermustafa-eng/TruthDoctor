using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Provider-neutral declaration of verified association evidence.
/// A provider adapter supplies these records after discovery.
/// </summary>
public sealed record InfrastructureAssociationEvidence(
    string SourceResourceId,
    string TargetResourceId,
    string AssociationType,
    string EvidenceKey);

/// <summary>
/// A validated, read-only association view.
/// Not an operational graph edge or traffic observation.
/// </summary>
public sealed record InfrastructureAssociationView(
    string SourceResourceId,
    string TargetResourceId,
    string ProviderId,
    string AccountId,
    string Location,
    string AssociationType,
    string EvidenceKey);

/// <summary>
/// Resolves provider-neutral association evidence against
/// discovered resources without modifying graph relationships.
/// </summary>
public static class InfrastructureAssociationEvidenceConsumer
{
    public static IReadOnlyList<InfrastructureAssociationView> Resolve(
        IReadOnlyList<InfrastructureResource> resources,
        IReadOnlyList<InfrastructureAssociationEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(evidence);

        var index = resources
            .GroupBy(resource => resource.ResourceId,
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal);

        var output = new List<InfrastructureAssociationView>();

        var seen = new HashSet<(
            string SourceId,
            string TargetId,
            string AssociationType)>();

        foreach (var item in evidence
                     .OrderBy(x => x.SourceResourceId,
                         StringComparer.Ordinal)
                     .ThenBy(x => x.TargetResourceId,
                         StringComparer.Ordinal)
                     .ThenBy(x => x.AssociationType,
                         StringComparer.Ordinal)
                     .ThenBy(x => x.EvidenceKey,
                         StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(item.SourceResourceId) ||
                string.IsNullOrWhiteSpace(item.TargetResourceId) ||
                string.IsNullOrWhiteSpace(item.AssociationType) ||
                string.IsNullOrWhiteSpace(item.EvidenceKey) ||
                StringComparer.Ordinal.Equals(
                    item.SourceResourceId,
                    item.TargetResourceId))
            {
                continue;
            }

            if (!index.TryGetValue(
                    item.SourceResourceId, out var sources) ||
                !index.TryGetValue(
                    item.TargetResourceId, out var targets) ||
                sources.Length != 1 ||
                targets.Length != 1)
            {
                continue;
            }

            var source = sources[0];
            var target = targets[0];

            if (string.IsNullOrWhiteSpace(source.ProviderId) ||
                string.IsNullOrWhiteSpace(source.AccountId) ||
                string.IsNullOrWhiteSpace(source.Location) ||
                !StringComparer.Ordinal.Equals(
                    source.ProviderId, target.ProviderId) ||
                !StringComparer.Ordinal.Equals(
                    source.AccountId, target.AccountId) ||
                !StringComparer.Ordinal.Equals(
                    source.Location, target.Location))
            {
                continue;
            }

            var identity = (
                item.SourceResourceId,
                item.TargetResourceId,
                item.AssociationType);

            if (!seen.Add(identity))
            {
                continue;
            }

            output.Add(new InfrastructureAssociationView(
                item.SourceResourceId,
                item.TargetResourceId,
                source.ProviderId,
                source.AccountId,
                source.Location,
                item.AssociationType,
                item.EvidenceKey));
        }

        return output;
    }
}
