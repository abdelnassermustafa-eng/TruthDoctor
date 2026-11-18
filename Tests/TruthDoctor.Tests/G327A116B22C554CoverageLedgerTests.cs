using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C554CoverageLedgerTests
{
    private const string Account = "123456789012";

    private static AwsDiscoveredResource Resource(
        string id,
        string type = "virtual-network",
        string service = "ec2:vpc",
        string account = Account,
        string region = "us-east-1")
    {
        var key = new AwsDiscoveryResourceKey(
            new AwsDiscoveryScope(account, region, service),
            type,
            id);

        return new AwsDiscoveredResource(key, id);
    }

    private static AwsDiscoveryInventorySnapshot Snapshot(
        IEnumerable<AwsDiscoveredResource> resources,
        bool complete = true)
    {
        var list = resources.ToArray();

        var coverage = list.Select(x =>
            x.Key.Scope).Distinct().Select(scope =>
                new AwsDiscoveryCoverage(
                    scope,
                    complete
                        ? AwsDiscoveryScanStatus.Success
                        : AwsDiscoveryScanStatus.Failed,
                    null)).ToArray();

        return new AwsDiscoveryInventorySnapshot(
            1, list, coverage);
    }

    [Fact]
    public void EmptyInventoryHasNoScheduledLookups()
    {
        var ledger = new AwsCloudTrailEnrichmentCoverageLedger(
            Snapshot(Array.Empty<AwsDiscoveredResource>()),
            Account);

        Assert.Empty(ledger.Entries);
        Assert.Empty(ledger.ScheduledKeys);
        Assert.Equal(0, ledger.Summary.Eligible);
    }

    [Fact]
    public void TwelveResourceBudgetMarksOverflowNotExamined()
    {
        var resources = Enumerable.Range(1, 15)
            .Select(index => Resource("vpc-" + index))
            .ToArray();

        var ledger = new AwsCloudTrailEnrichmentCoverageLedger(
            Snapshot(resources), Account);

        Assert.Equal(15, ledger.Summary.Eligible);
        Assert.Equal(12, ledger.Summary.Scheduled);
        Assert.Equal(12, ledger.Summary.Pending);
        Assert.Equal(3, ledger.Summary.NotExamined);

        Assert.Equal(
            "vpc-1",
            ledger.ScheduledKeys[0].ResourceId);

        Assert.Equal(
            "vpc-12",
            ledger.ScheduledKeys[11].ResourceId);

        Assert.All(
            ledger.Entries.Skip(12),
            entry => Assert.Equal(
                AwsCloudTrailCoverageStatus.NotExamined,
                entry.Status));
    }

    [Fact]
    public void OnlySupportedAccountScopedResourcesAreEligible()
    {
        var resources = new[]
        {
            Resource("vpc-1"),
            Resource("subnet-1", "subnet", "ec2:subnet"),
            Resource(
                "rtb-1", "route-table", "ec2:route-table"),
            Resource("i-1", "compute-instance", "ec2:instance"),
            Resource(
                "vpc-other", account: "999988887777"),
            Resource("vpc-global", region: "global"),
            Resource(
                "vpc-wrong-service", service: "ec2:other")
        };

        var ledger = new AwsCloudTrailEnrichmentCoverageLedger(
            Snapshot(resources), Account);

        Assert.Equal(3, ledger.Summary.Eligible);
        Assert.Equal(3, ledger.Summary.Scheduled);
        Assert.Equal(
            new[] { "vpc-1", "subnet-1", "rtb-1" },
            ledger.ScheduledKeys
                .Select(x => x.ResourceId).ToArray());
    }

    [Fact]
    public void IncompleteDiscoveryCannotCreateLedger()
    {
        var snapshot = Snapshot(
            new[] { Resource("vpc-1") },
            complete: false);

        Assert.Throws<InvalidOperationException>(
            () => new AwsCloudTrailEnrichmentCoverageLedger(
                snapshot, Account));
    }

    [Fact]
    public void OutcomesHaveIndependentAccountingStates()
    {
        var resources = new[]
        {
            Resource("vpc-1"),
            Resource("vpc-2"),
            Resource("vpc-3"),
            Resource("vpc-4")
        };

        var ledger = new AwsCloudTrailEnrichmentCoverageLedger(
            Snapshot(resources), Account, resourceBudget: 3);

        ledger.RecordOutcome(
            resources[0].Key,
            AwsCloudTrailEnrichmentStatus.EvidenceFound);

        ledger.RecordOutcome(
            resources[1].Key,
            AwsCloudTrailEnrichmentStatus.NoMatchingEvidence);

        ledger.RecordOutcome(
            resources[2].Key,
            AwsCloudTrailEnrichmentStatus.Unavailable);

        Assert.Equal(1, ledger.Summary.EvidenceFound);
        Assert.Equal(1, ledger.Summary.NoMatchingEvidence);
        Assert.Equal(1, ledger.Summary.Unavailable);
        Assert.Equal(1, ledger.Summary.NotExamined);
        Assert.Equal(0, ledger.Summary.Pending);
    }

    [Fact]
    public void DuplicateAndUnscheduledOutcomesAreRejected()
    {
        var resources = new[]
        {
            Resource("vpc-1"),
            Resource("vpc-2")
        };

        var ledger = new AwsCloudTrailEnrichmentCoverageLedger(
            Snapshot(resources), Account, resourceBudget: 1);

        ledger.RecordOutcome(
            resources[0].Key,
            AwsCloudTrailEnrichmentStatus.Unavailable);

        Assert.Throws<InvalidOperationException>(() =>
            ledger.RecordOutcome(
                resources[0].Key,
                AwsCloudTrailEnrichmentStatus.EvidenceFound));

        Assert.Throws<InvalidOperationException>(() =>
            ledger.RecordOutcome(
                resources[1].Key,
                AwsCloudTrailEnrichmentStatus.NoMatchingEvidence));

        Assert.Equal(1, ledger.Summary.Unavailable);
        Assert.Equal(1, ledger.Summary.NotExamined);
    }

    [Fact]
    public void WrongIdentityAndInvalidBudgetAreRejected()
    {
        var original = Resource("vpc-1");
        var snapshot = Snapshot(new[] { original });

        var ledger = new AwsCloudTrailEnrichmentCoverageLedger(
            snapshot, Account);

        Assert.Throws<ArgumentException>(() =>
            ledger.RecordOutcome(
                Resource(
                    "vpc-1",
                    account: "999988887777").Key,
                AwsCloudTrailEnrichmentStatus.EvidenceFound));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AwsCloudTrailEnrichmentCoverageLedger(
                snapshot, Account, resourceBudget: 13));

        Assert.Throws<InvalidOperationException>(() =>
            new AwsCloudTrailEnrichmentCoverageLedger(
                Snapshot(new[] { original, original }),
                Account));

        Assert.Equal(1, ledger.Summary.Pending);
    }
}
