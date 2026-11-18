using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// The status of one eligible AWS resource during
/// optional CloudTrail creation-date enrichment.
/// </summary>
public enum AwsCloudTrailCoverageStatus
{
    NotExamined,
    Pending,
    EvidenceFound,
    NoMatchingEvidence,
    Unavailable
}

/// <summary>
/// Accounting only. Evidence verification and attachment
/// remain the responsibility of the existing authenticated
/// lookup and creation-evidence association contracts.
/// </summary>
public sealed record AwsCloudTrailCoverageEntry(
    AwsDiscoveryResourceKey Key,
    AwsCloudTrailCoverageStatus Status);

public sealed record AwsCloudTrailCoverageSummary(
    int Eligible,
    int Scheduled,
    int NotExamined,
    int Pending,
    int EvidenceFound,
    int NoMatchingEvidence,
    int Unavailable);

/// <summary>
/// A deterministic, offline-only enrichment coverage ledger.
///
/// Eligible resources are selected in discovery snapshot order.
/// The first budgeted resources are scheduled for lookup.
/// Remaining eligible resources are NotExamined.
///
/// The ledger does not query AWS and cannot manufacture
/// creation evidence. It tracks outcomes supplied by
/// the authenticated lookup integration.
/// </summary>
public sealed class AwsCloudTrailEnrichmentCoverageLedger
{
    public const int DefaultResourceBudget = 12;

    private readonly List<AwsDiscoveryResourceKey> _order = new();

    private readonly Dictionary<
        AwsDiscoveryResourceKey,
        AwsCloudTrailCoverageStatus> _statuses = new();

    private readonly IReadOnlyList<
        AwsDiscoveryResourceKey> _scheduled;

    public AwsCloudTrailEnrichmentCoverageLedger(
        AwsDiscoveryInventorySnapshot snapshot,
        string authenticatedAccountId,
        int resourceBudget = DefaultResourceBudget)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrWhiteSpace(authenticatedAccountId) ||
            authenticatedAccountId.Length != 12 ||
            !authenticatedAccountId.All(char.IsAsciiDigit))
        {
            throw new ArgumentException(
                "Authenticated AWS account identity is invalid.",
                nameof(authenticatedAccountId));
        }

        if (resourceBudget < 0 ||
            resourceBudget > DefaultResourceBudget)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resourceBudget));
        }

        if (snapshot.Resources is null ||
            snapshot.Coverage is null ||
            snapshot.Coverage.Any(coverage =>
                coverage is null ||
                coverage.Scope is null ||
                coverage.Status != AwsDiscoveryScanStatus.Success))
        {
            throw new InvalidOperationException(
                "CloudTrail accounting requires complete discovery.");
        }

        var scheduled =
            new List<AwsDiscoveryResourceKey>();

        foreach (AwsDiscoveredResource resource
            in snapshot.Resources)
        {
            if (resource is null ||
                resource.Key is null ||
                resource.Key.Scope is null)
            {
                throw new InvalidOperationException(
                    "Discovered resource identity is invalid.");
            }

            AwsDiscoveryResourceKey key = resource.Key;

            if (!Eligible(key, authenticatedAccountId))
                continue;

            if (_statuses.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    "Duplicate AWS discovery resource identity.");
            }

            _order.Add(key);

            bool selected =
                scheduled.Count < resourceBudget;

            _statuses.Add(
                key,
                selected
                    ? AwsCloudTrailCoverageStatus.Pending
                    : AwsCloudTrailCoverageStatus.NotExamined);

            if (selected)
                scheduled.Add(key);
        }

        _scheduled = scheduled.AsReadOnly();
    }

    public IReadOnlyList<AwsDiscoveryResourceKey> ScheduledKeys
        => _scheduled;

    public IReadOnlyList<AwsCloudTrailCoverageEntry> Entries
        => _order.Select(key =>
            new AwsCloudTrailCoverageEntry(
                key, _statuses[key])).ToArray();

    public AwsCloudTrailCoverageSummary Summary
    {
        get
        {
            int Count(AwsCloudTrailCoverageStatus status)
                => _statuses.Values.Count(
                    value => value == status);

            return new AwsCloudTrailCoverageSummary(
                Eligible: _statuses.Count,
                Scheduled: _scheduled.Count,
                NotExamined: Count(
                    AwsCloudTrailCoverageStatus.NotExamined),
                Pending: Count(
                    AwsCloudTrailCoverageStatus.Pending),
                EvidenceFound: Count(
                    AwsCloudTrailCoverageStatus.EvidenceFound),
                NoMatchingEvidence: Count(
                    AwsCloudTrailCoverageStatus.NoMatchingEvidence),
                Unavailable: Count(
                    AwsCloudTrailCoverageStatus.Unavailable));
        }
    }

    /// <summary>
    /// Records a trusted lookup outcome, not evidence itself.
    /// The caller must validate and associate evidence separately.
    /// </summary>
    public void RecordOutcome(
        AwsDiscoveryResourceKey key,
        AwsCloudTrailEnrichmentStatus outcome)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_statuses.TryGetValue(
                key, out AwsCloudTrailCoverageStatus current))
        {
            throw new ArgumentException(
                "Resource is not eligible for this coverage ledger.",
                nameof(key));
        }

        if (current != AwsCloudTrailCoverageStatus.Pending)
        {
            throw new InvalidOperationException(
                "Resource was not pending or already has an outcome.");
        }

        AwsCloudTrailCoverageStatus final = outcome switch
        {
            AwsCloudTrailEnrichmentStatus.EvidenceFound =>
                AwsCloudTrailCoverageStatus.EvidenceFound,

            AwsCloudTrailEnrichmentStatus.NoMatchingEvidence =>
                AwsCloudTrailCoverageStatus.NoMatchingEvidence,

            AwsCloudTrailEnrichmentStatus.Unavailable =>
                AwsCloudTrailCoverageStatus.Unavailable,

            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome))
        };

        _statuses[key] = final;
    }

    private static bool Eligible(
        AwsDiscoveryResourceKey key,
        string authenticatedAccountId)
    {
        AwsDiscoveryScope scope = key.Scope;

        if (!StringComparer.Ordinal.Equals(
                scope.AccountId, authenticatedAccountId) ||
            string.IsNullOrWhiteSpace(scope.Region) ||
            StringComparer.Ordinal.Equals(
                scope.Region, "global"))
        {
            return false;
        }

        return (key.ResourceType, scope.Service) switch
        {
            ("virtual-network", "ec2:vpc") => true,
            ("subnet", "ec2:subnet") => true,
            ("route-table", "ec2:route-table") => true,
            _ => false
        };
    }
}
