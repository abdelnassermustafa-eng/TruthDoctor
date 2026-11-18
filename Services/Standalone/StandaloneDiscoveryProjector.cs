using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Projects a validated single-provider discovery snapshot into
/// the existing workbench platform-state model.
/// </summary>
/// <remarks>
/// This component does not authenticate, discover infrastructure,
/// manage credentials, call HTTP endpoints, or mutate workbench state.
/// Account, location, and domain context must be supplied explicitly.
/// </remarks>
public static class StandaloneDiscoveryProjector
{
    public static PlatformState Project(
        IReadOnlyList<ProviderResourceRecord> records,
        string accountId,
        string location,
        string domainId)
        => ProjectWithAwsDomains(records, accountId, location, domainId, false);

    public static PlatformState ProjectWithAwsDomains(
        IReadOnlyList<ProviderResourceRecord> records,
        string accountId,
        string location,
        string domainId,
        bool classifyAwsDomains = false)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        ArgumentException.ThrowIfNullOrWhiteSpace(domainId);

        if (records.Count == 0)
        {
            throw new ArgumentException(
                "A provider discovery snapshot is required.",
                nameof(records));
        }

        var providerId = records[0]?.ProviderId;

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        var identifiers = new HashSet<string>(
            StringComparer.Ordinal);

        var resources = new List<InfrastructureResource>(
            records.Count);

        var discoveredAt = DateTimeOffset.UtcNow;

        foreach (var record in records)
        {
            if (record is null)
            {
                throw new ArgumentException(
                    "Discovery records cannot contain null.",
                    nameof(records));
            }

            if (!StringComparer.Ordinal.Equals(
                providerId,
                record.ProviderId))
            {
                throw new ArgumentException(
                    "A discovery snapshot must belong to one provider.",
                    nameof(records));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(
                record.ResourceId);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                record.ResourceType);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                record.DisplayName);

            if (!identifiers.Add(record.ResourceId))
            {
                throw new ArgumentException(
                    "Duplicate resource identities are not allowed.",
                    nameof(records));
            }

            resources.Add(new InfrastructureResource
            {
                ProviderId = record.ProviderId,
                AccountId = accountId,
                DomainId =
                    classifyAwsDomains &&
                    StringComparer.OrdinalIgnoreCase.Equals(
                        providerId, "aws")
                        ? AwsResourceDomainClassifier.Classify(
                            record.ResourceType)
                        : domainId,
                ResourceId = record.ResourceId,
                ResourceType = record.ResourceType,
                DisplayName = record.DisplayName,
                NativeId = record.ResourceId,
                State = record.State,
                Location = location,
                AvailabilityZone = record.AvailabilityZone,
                Arn = record.Arn,
                Properties = record.Properties is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(
                        record.Properties),
                Tags = record.Tags is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(
                        record.Tags),
                LifecycleEvidence = record.LifecycleEvidence,
                DiscoveredAt = discoveredAt
            });
        }

        var domains = new List<InfrastructureDomain>();

        if (classifyAwsDomains &&
            StringComparer.OrdinalIgnoreCase.Equals(
                providerId, "aws"))
        {
            foreach (var group in resources.GroupBy(
                resource => resource.DomainId,
                StringComparer.OrdinalIgnoreCase))
            {
                domains.Add(new InfrastructureDomain
                {
                    Id = group.Key,
                    DisplayName =
                        AwsResourceDomainClassifier.DisplayName(
                            group.Key),
                    ResourceCount = group.Count()
                });
            }
        }
        else
        {
            domains.Add(new InfrastructureDomain
            {
                Id = domainId,
                DisplayName = domainId,
                ResourceCount = resources.Count
            });
        }

        return new PlatformState
        {
            Context = new PlatformContext
            {
                ProviderId = providerId!,
                AccountId = accountId,
                DefaultLocation = location,
                Locations = new List<string> { location }
            },
            Resources = resources,
            Relationships =
                StringComparer.OrdinalIgnoreCase.Equals(
                    providerId, "aws")
                    ? AwsStandaloneRelationshipBuilder.Build(resources)
                    : new List<InfrastructureRelationship>(),
            Domains = domains,
            DiscoveredAt = discoveredAt,
            TotalResourceCount = resources.Count
        };
    }
}
