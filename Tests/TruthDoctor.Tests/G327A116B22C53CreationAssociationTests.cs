using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C53CreationAssociationTests
{
    private static readonly DateTimeOffset Created =
        new(2026, 11, 1, 10, 20, 30, TimeSpan.Zero);

    private static AwsDiscoveryResourceKey Key(
        string type,
        string id,
        string account = "111122223333",
        string region = "us-east-1",
        string service = "ec2:vpc")
        => new(
            new AwsDiscoveryScope(account, region, service),
            type, id);

    private static ResourceLifecycleEventEvidence Evidence(
        ResourceLifecycleEventKind kind =
            ResourceLifecycleEventKind.Created,
        ResourceLifecycleEvidenceQuality quality =
            ResourceLifecycleEvidenceQuality.ProviderReported,
        string source = "aws.cloudtrail")
        => new(
            kind, Created, quality, source, "event-001");

    private static ProviderResourceRecord Record(
        string type,
        string id,
        string provider = "aws",
        ResourceLifecycleEvidenceSet? lifecycle = null)
        => new(
            provider, id, type, id,
            LifecycleEvidence: lifecycle);

    [Fact]
    public void VpcCreationAssociatesWithExactScope()
    {
        var key = Key("virtual-network", "vpc-1");
        var evidence = Evidence();

        var result = AwsCreationEvidenceAssociation.Apply(
            Record("virtual-network", "vpc-1"),
            key,
            new AwsCreationEvidenceBinding(key, evidence));

        Assert.Same(
            evidence,
            result.LifecycleEvidence!.VerifiedCreation);
        Assert.Equal(
            Created,
            result.LifecycleEvidence.VerifiedCreation!.OccurredAt);
        Assert.Equal("aws", result.ProviderId);
    }

    [Fact]
    public void SubnetAndRouteTableAssociationsAreSupported()
    {
        var subnetKey = Key(
            "subnet", "subnet-1", service: "ec2:subnet");

        var routeKey = Key(
            "route-table", "rtb-1",
            service: "ec2:route-table");

        var subnet = AwsCreationEvidenceAssociation.Apply(
            Record("subnet", "subnet-1"),
            subnetKey,
            new AwsCreationEvidenceBinding(
                subnetKey, Evidence()));

        var route = AwsCreationEvidenceAssociation.Apply(
            Record("route-table", "rtb-1"),
            routeKey,
            new AwsCreationEvidenceBinding(
                routeKey, Evidence()));

        Assert.NotNull(
            subnet.LifecycleEvidence!.VerifiedCreation);
        Assert.NotNull(
            route.LifecycleEvidence!.VerifiedCreation);
    }

    [Fact]
    public void AccountRegionAndServiceMismatchesAreRejected()
    {
        var key = Key("virtual-network", "vpc-1");

        foreach (var wrong in new[]
        {
            Key("virtual-network", "vpc-1",
                account: "999999999999"),
            Key("virtual-network", "vpc-1",
                region: "us-west-2"),
            Key("virtual-network", "vpc-1",
                service: "ec2:subnet")
        })
        {
            Assert.Throws<ArgumentException>(
                () => AwsCreationEvidenceAssociation.Apply(
                    Record("virtual-network", "vpc-1"),
                    key,
                    new AwsCreationEvidenceBinding(
                        wrong, Evidence())));
        }
    }

    [Fact]
    public void WrongResourceIdentityIsRejected()
    {
        var key = Key("virtual-network", "vpc-1");

        Assert.Throws<ArgumentException>(
            () => AwsCreationEvidenceAssociation.Apply(
                Record("virtual-network", "vpc-other"),
                key,
                new AwsCreationEvidenceBinding(
                    key, Evidence())));

        Assert.Throws<ArgumentException>(
            () => AwsCreationEvidenceAssociation.Apply(
                Record("virtual-network", "vpc-1"),
                key,
                new AwsCreationEvidenceBinding(
                    Key("virtual-network", "vpc-2"),
                    Evidence())));
    }

    [Fact]
    public void ExistingActivityIsPreservedAndCreationCannotOverwrite()
    {
        var activity = Evidence(
            ResourceLifecycleEventKind.ConfigurationChanged);

        var existing = new ResourceLifecycleEvidenceSet(
            activities: [activity]);

        var key = Key("virtual-network", "vpc-1");
        var binding = new AwsCreationEvidenceBinding(
            key, Evidence());

        var result = AwsCreationEvidenceAssociation.Apply(
            Record("virtual-network", "vpc-1",
                lifecycle: existing),
            key, binding);

        Assert.Same(
            activity,
            Assert.Single(result.LifecycleEvidence!.Activities));
        Assert.Same(
            binding.Evidence,
            result.LifecycleEvidence.VerifiedCreation);

        Assert.Throws<InvalidOperationException>(
            () => AwsCreationEvidenceAssociation.Apply(
                result, key, binding));
    }

    [Fact]
    public void UnverifiedAndUnsupportedClaimsAreRejected()
    {
        var key = Key("subnet", "subnet-1");

        foreach (var invalid in new[]
        {
            Evidence(
                quality: ResourceLifecycleEvidenceQuality.Unverified),
            Evidence(
                ResourceLifecycleEventKind.Started),
            Evidence(source: "unknown-source")
        })
        {
            Assert.Throws<ArgumentException>(
                () => AwsCreationEvidenceAssociation.Apply(
                    Record("subnet", "subnet-1"),
                    key,
                    new AwsCreationEvidenceBinding(
                        key, invalid)));
        }

        Assert.Throws<ArgumentException>(
            () => AwsCreationEvidenceAssociation.Apply(
                Record("compute-instance", "i-1"),
                Key("compute-instance", "i-1"),
                new AwsCreationEvidenceBinding(
                    Key("compute-instance", "i-1"),
                    Evidence())));
    }

    [Fact]
    public void MissingClaimsAndNonAwsRecordsStayUnchanged()
    {
        var properties = new Dictionary<string, string>
        {
            ["LaunchTime"] = "2026-11-01T10:20:30Z"
        };

        var aws = new ProviderResourceRecord(
            "aws", "vpc-1", "virtual-network", "VPC",
            Properties: properties);

        var same = AwsCreationEvidenceAssociation.Apply(
            aws,
            Key("virtual-network", "vpc-1"),
            null);

        Assert.Same(aws, same);
        Assert.Null(same.LifecycleEvidence);

        var other = Record(
            "network-device", "router-1", "network-ssh");

        var unchanged = AwsCreationEvidenceAssociation.Apply(
            other,
            Key("network-device", "router-1"),
            null);

        Assert.Same(other, unchanged);
        Assert.Null(unchanged.LifecycleEvidence);
    }
}
