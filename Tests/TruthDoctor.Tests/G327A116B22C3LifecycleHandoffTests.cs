using System;
using System.Collections.Generic;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A116B22C3LifecycleHandoffTests
{
    private static readonly DateTimeOffset Created =
        new(2025, 1, 14, 9, 30, 0, TimeSpan.Zero);

    private static ResourceLifecycleEventEvidence Event(
        ResourceLifecycleEventKind kind,
        DateTimeOffset occurredAt,
        ResourceLifecycleEvidenceQuality quality =
            ResourceLifecycleEvidenceQuality.ProviderReported)
        => new(
            kind,
            occurredAt,
            quality,
            "synthetic-provider-api",
            "fixture-event",
            "fixture-actor");

    private static InfrastructureResource Project(
        ProviderResourceRecord record)
        => Assert.Single(
            StandaloneDiscoveryProjector.Project(
                [record],
                "test-account",
                "test-location",
                "infrastructure").Resources);

    [Fact]
    public void ExistingPositionalRecordPreservesAbsentEvidence()
    {
        var record = new ProviderResourceRecord(
            "aws", "i-1", "compute-instance",
            "Example", "running", "zone-1",
            "arn:synthetic",
            new Dictionary<string, string>
            {
                ["InstanceType"] = "t3.micro"
            },
            new Dictionary<string, string>
            {
                ["Environment"] = "test"
            });

        var resource = Project(record);

        Assert.Null(record.LifecycleEvidence);
        Assert.Null(resource.LifecycleEvidence);
        Assert.Equal("i-1", resource.NativeId);
        Assert.Equal("running", resource.State);
        Assert.Equal(
            "t3.micro",
            resource.Properties["InstanceType"]);
        Assert.Equal(
            "test",
            resource.Tags["Environment"]);
    }

    [Fact]
    public void VerifiedCreationPassesThroughWithoutDateSubstitution()
    {
        var creation = Event(
            ResourceLifecycleEventKind.Created, Created);

        var evidence = new ResourceLifecycleEvidenceSet(
            creation: creation);

        var resource = Project(
            new ProviderResourceRecord(
                "aws", "i-2", "compute-instance",
                "Example",
                LifecycleEvidence: evidence));

        Assert.Same(evidence, resource.LifecycleEvidence);
        Assert.Same(
            creation,
            resource.LifecycleEvidence!.VerifiedCreation);
        Assert.Equal(
            Created,
            resource.LifecycleEvidence.VerifiedCreation!.OccurredAt);
        Assert.NotEqual(
            Created,
            resource.DiscoveredAt);
    }

    [Fact]
    public void ActivityHandoffPreservesQualityAndProvenance()
    {
        var verified = Event(
            ResourceLifecycleEventKind.Started,
            new DateTimeOffset(
                2025, 1, 15, 10, 0, 0, TimeSpan.Zero));

        var unverified = Event(
            ResourceLifecycleEventKind.Restarted,
            new DateTimeOffset(
                2025, 1, 16, 10, 0, 0, TimeSpan.Zero),
            ResourceLifecycleEvidenceQuality.Unverified);

        var evidence = new ResourceLifecycleEvidenceSet(
            activities: [unverified, verified]);

        var resource = Project(
            new ProviderResourceRecord(
                "aws", "i-3", "compute-instance",
                "Example",
                LifecycleEvidence: evidence));

        var last = resource.LifecycleEvidence!
            .LastVerifiedActivity;

        Assert.Same(verified, last);
        Assert.Equal(
            "synthetic-provider-api",
            last!.Source);
        Assert.Equal("fixture-event", last.Reference);
        Assert.Equal("fixture-actor", last.Actor);
        Assert.Equal(
            ResourceLifecycleEvidenceQuality.ProviderReported,
            last.Quality);
    }

    [Fact]
    public void NonAwsProviderUsesSameLifecycleHandoff()
    {
        var creation = Event(
            ResourceLifecycleEventKind.Created, Created);

        var evidence = new ResourceLifecycleEvidenceSet(
            creation: creation);

        var resource = Project(
            new ProviderResourceRecord(
                "network-ssh",
                "router-1",
                "network-device",
                "Lab Router",
                LifecycleEvidence: evidence));

        Assert.Equal("network-ssh", resource.ProviderId);
        Assert.Same(evidence, resource.LifecycleEvidence);
        Assert.Same(
            creation,
            resource.LifecycleEvidence!.VerifiedCreation);
    }

    [Fact]
    public void ArbitraryPropertiesDoNotBecomeLifecycleClaims()
    {
        var properties = new Dictionary<string, string>
        {
            ["LaunchTime"] = "2025-01-15T10:00:00Z",
            ["CreatedAt"] = "2025-01-14T09:30:00Z"
        };

        var resource = Project(
            new ProviderResourceRecord(
                "aws", "i-4", "compute-instance",
                "Example",
                Properties: properties));

        Assert.Null(resource.LifecycleEvidence);
        Assert.Equal(
            "2025-01-15T10:00:00Z",
            resource.Properties["LaunchTime"]);
        Assert.Equal(
            "2025-01-14T09:30:00Z",
            resource.Properties["CreatedAt"]);
    }
}
