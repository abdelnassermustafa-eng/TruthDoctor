using System;
using System.Collections.Generic;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryProjectionHardeningTests
{
    private static ProviderResourceRecord Resource(
        string providerId = "mock-provider",
        string resourceId = "resource-001",
        string resourceType = "virtual-machine",
        string displayName = "Sample Resource") =>
        new(providerId, resourceId, resourceType, displayName);

    private static PlatformState Project(
        IReadOnlyList<ProviderResourceRecord> records) =>
        StandaloneDiscoveryProjector.Project(
            records,
            "account-001",
            "location-001",
            "compute");

    [Fact]
    public void NullSnapshotIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => Project(null!));
    }

    [Fact]
    public void EmptySnapshotIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Project(Array.Empty<ProviderResourceRecord>()));
    }

    [Theory]
    [InlineData("", "location-001", "compute")]
    [InlineData("account-001", "", "compute")]
    [InlineData("account-001", "location-001", "")]
    public void MissingExplicitContextIsRejected(
        string accountId,
        string location,
        string domainId)
    {
        Assert.Throws<ArgumentException>(
            () => StandaloneDiscoveryProjector.Project(
                [Resource()],
                accountId,
                location,
                domainId));
    }

    [Fact]
    public void MixedProvidersAreRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Project([
                Resource(),
                Resource(
                    providerId: "other-provider",
                    resourceId: "resource-002")
            ]));
    }

    [Fact]
    public void DuplicateResourceIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Project([
                Resource(),
                Resource(
                    resourceId: "resource-001",
                    displayName: "Another Resource")
            ]));
    }

    [Theory]
    [InlineData("", "resource-001", "virtual-machine", "Sample")]
    [InlineData("mock-provider", "", "virtual-machine", "Sample")]
    [InlineData("mock-provider", "resource-001", "", "Sample")]
    [InlineData("mock-provider", "resource-001", "virtual-machine", "")]
    public void MissingResourceFieldsAreRejected(
        string providerId,
        string resourceId,
        string resourceType,
        string displayName)
    {
        Assert.Throws<ArgumentException>(
            () => Project([
                Resource(
                    providerId,
                    resourceId,
                    resourceType,
                    displayName)
            ]));
    }

    [Fact]
    public void NullRecordWithinSnapshotIsRejected()
    {
        IReadOnlyList<ProviderResourceRecord> records =
            new ProviderResourceRecord[]
            {
                Resource(),
                null!
            };

        Assert.Throws<ArgumentException>(
            () => Project(records));
    }

    [Fact]
    public void FailedProjectionDoesNotMutateWorkbenchState()
    {
        PlatformState original = Project([Resource()]);
        var graph = new InfrastructureGraphBuilder()
            .Build(original);

        var state = new WorkbenchState();
        state.SetPlatformState(original);
        state.SetInfrastructureGraph(graph);

        Assert.Throws<ArgumentException>(
            () => Project([
                Resource(),
                Resource(
                    providerId: "other-provider",
                    resourceId: "resource-002")
            ]));

        Assert.Same(original, state.PlatformState);
        Assert.Same(graph, state.InfrastructureGraph);
        Assert.Single(state.VisibleResources);
        Assert.Equal(
            "resource-001",
            state.VisibleResources[0].ResourceId);
    }

    [Fact]
    public void ValidProjectionProvidesDomainAndTimestamp()
    {
        PlatformState platform = Project([Resource()]);
        InfrastructureResource resource =
            Assert.Single(platform.Resources);

        InfrastructureDomain domain =
            Assert.Single(platform.Domains);

        Assert.Equal("compute", domain.Id);
        Assert.Equal(1, domain.ResourceCount);
        Assert.Equal(1, platform.TotalResourceCount);

        Assert.NotEqual(
            default(DateTimeOffset),
            platform.DiscoveredAt);
        Assert.Equal(
            platform.DiscoveredAt,
            resource.DiscoveredAt);

        Assert.Contains(
            "location-001",
            platform.Context.Locations);
    }
}
