using System;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Resource-scoped creation evidence supplied by a trusted
/// upstream AWS discovery integration. The binding itself
/// does not authenticate a CloudTrail event.
/// </summary>
public sealed record AwsCreationEvidenceBinding(
    AwsDiscoveryResourceKey Key,
    ResourceLifecycleEventEvidence Evidence);

/// <summary>
/// Associates prevalidated AWS creation evidence with one
/// provider-neutral discovery record, without contacting AWS.
/// Exact scope and identity are mandatory.
/// </summary>
public static class AwsCreationEvidenceAssociation
{
    public static ProviderResourceRecord Apply(
        ProviderResourceRecord record,
        AwsDiscoveryResourceKey resourceKey,
        AwsCreationEvidenceBinding? binding)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(resourceKey);
        ArgumentNullException.ThrowIfNull(resourceKey.Scope);

        if (!StringComparer.Ordinal.Equals(
                record.ProviderId, "aws"))
        {
            if (binding is not null)
                throw new ArgumentException(
                    "AWS evidence cannot attach to another provider.",
                    nameof(binding));

            return record;
        }

        if (string.IsNullOrWhiteSpace(
                resourceKey.Scope.AccountId) ||
            string.IsNullOrWhiteSpace(
                resourceKey.Scope.Region) ||
            string.IsNullOrWhiteSpace(
                resourceKey.Scope.Service))
        {
            throw new ArgumentException(
                "AWS discovery scope is required.",
                nameof(resourceKey));
        }

        if (!StringComparer.Ordinal.Equals(
                resourceKey.ResourceId, record.ResourceId) ||
            !StringComparer.Ordinal.Equals(
                resourceKey.ResourceType, record.ResourceType))
        {
            throw new ArgumentException(
                "AWS discovery identity does not match record.",
                nameof(resourceKey));
        }

        if (binding is null)
            return record;

        ArgumentNullException.ThrowIfNull(binding.Key);
        ArgumentNullException.ThrowIfNull(binding.Key.Scope);
        ArgumentNullException.ThrowIfNull(binding.Evidence);

        // Record equality includes the account, Region,
        // service, resource type and resource identifier.
        if (binding.Key != resourceKey)
        {
            throw new ArgumentException(
                "Creation evidence scope or identity mismatch.",
                nameof(binding));
        }

        if (record.ResourceType is not
            ("virtual-network" or "subnet" or "route-table"))
        {
            throw new ArgumentException(
                "Unsupported resource type for AWS creation evidence.",
                nameof(record));
        }

        var evidence = binding.Evidence;

        if (evidence.Kind != ResourceLifecycleEventKind.Created ||
            !evidence.IsVerified ||
            !StringComparer.Ordinal.Equals(
                evidence.Source, "aws.cloudtrail") ||
            string.IsNullOrWhiteSpace(evidence.Reference))
        {
            throw new ArgumentException(
                "Invalid or unsupported creation evidence.",
                nameof(binding));
        }

        // Never silently overwrite another creation claim.
        if (record.LifecycleEvidence?.Creation is not null)
        {
            throw new InvalidOperationException(
                "A creation claim is already present.");
        }

        var lifecycle = new ResourceLifecycleEvidenceSet(
            creation: evidence,
            activities: record.LifecycleEvidence?.Activities);

        return record with { LifecycleEvidence = lifecycle };
    }
}
