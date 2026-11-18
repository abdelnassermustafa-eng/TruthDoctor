using System;
using System.Text.Json;
using Xunit;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Tests;

public sealed class G327A116B22BLifecycleEvidenceContractTests
{
    private static ResourceLifecycleEventEvidence Event(
        ResourceLifecycleEventKind kind,
        int day,
        ResourceLifecycleEvidenceQuality quality =
            ResourceLifecycleEvidenceQuality.ProviderReported)
        => new(
            kind,
            new DateTimeOffset(
                2026, 11, day, 10, 30, 0, TimeSpan.Zero),
            quality,
            "provider-api",
            "event-reference");

    [Fact]
    public void CreationAndDiscoveryAreIndependent()
    {
        var creation = Event(
            ResourceLifecycleEventKind.Created, 10);

        var resource = new InfrastructureResource
        {
            DiscoveredAt = new DateTimeOffset(
                2026, 11, 16, 12, 0, 0, TimeSpan.Zero),
            LifecycleEvidence =
                new ResourceLifecycleEvidenceSet(creation)
        };

        Assert.Same(
            creation,
            resource.LifecycleEvidence!.VerifiedCreation);

        Assert.NotEqual(
            creation.OccurredAt,
            resource.DiscoveredAt);
    }

    [Fact]
    public void UnverifiedCreationCannotBePresentedAsVerified()
    {
        var creation = Event(
            ResourceLifecycleEventKind.Created,
            10,
            ResourceLifecycleEvidenceQuality.Unverified);

        var evidence = new ResourceLifecycleEvidenceSet(creation);

        Assert.Null(evidence.VerifiedCreation);
        Assert.Same(creation, evidence.Creation);
    }

    [Fact]
    public void LatestVerifiedActivityExcludesUnverifiedEvents()
    {
        var started = Event(
            ResourceLifecycleEventKind.Started, 12);

        var stopped = Event(
            ResourceLifecycleEventKind.Stopped, 13);

        var unverifiedRestart = Event(
            ResourceLifecycleEventKind.Restarted,
            15,
            ResourceLifecycleEvidenceQuality.Unverified);

        var evidence = new ResourceLifecycleEvidenceSet(
            activities: new[]
            {
                unverifiedRestart,
                started,
                stopped
            });

        Assert.Same(
            stopped,
            evidence.LastVerifiedActivity);
    }

    [Fact]
    public void MissingEvidenceDoesNotInventEvents()
    {
        var evidence = new ResourceLifecycleEvidenceSet();

        Assert.Null(evidence.VerifiedCreation);
        Assert.Null(evidence.LastVerifiedActivity);
        Assert.Empty(evidence.Activities);
    }

    [Fact]
    public void EvidenceRequiresTimestampAndSource()
    {
        Assert.Throws<ArgumentException>(() =>
            new ResourceLifecycleEventEvidence(
                ResourceLifecycleEventKind.Started,
                default,
                ResourceLifecycleEvidenceQuality.ProviderReported,
                "provider-api"));

        Assert.Throws<ArgumentException>(() =>
            new ResourceLifecycleEventEvidence(
                ResourceLifecycleEventKind.Started,
                DateTimeOffset.UtcNow,
                ResourceLifecycleEvidenceQuality.ProviderReported,
                " "));
    }

    [Fact]
    public void EventKindsAndOptionalJsonRemainCorrect()
    {
        var creation = Event(
            ResourceLifecycleEventKind.Created, 10);

        var started = Event(
            ResourceLifecycleEventKind.Started, 12);

        Assert.Throws<ArgumentException>(() =>
            new ResourceLifecycleEvidenceSet(
                creation: started));

        Assert.Throws<ArgumentException>(() =>
            new ResourceLifecycleEvidenceSet(
                activities: new[] { creation }));

        var resource = new InfrastructureResource();

        var json = JsonSerializer.Serialize(resource);

        Assert.DoesNotContain(
            "\"lifecycleEvidence\"",
            json,
            StringComparison.OrdinalIgnoreCase);
    }
}
