using System;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class
    WorkbenchSavedViewRestoreCoordinatorTests
{
    [Fact]
    public void ExistingResourceAndDomainAreCoordinated()
    {
        var fixture =
            CreateFixture(
                CreateNode(
                    "resource-1",
                    "networking"));

        var summary =
            fixture.Coordinator.Restore(
                CreateView(
                    selectedResourceId:
                        "resource-1",
                    selectedDomainId:
                        "networking",
                    depth: 3));

        Assert.False(
            summary.ResourceWasUnavailable);

        Assert.False(
            summary.DomainFellBackToAll);

        Assert.False(
            summary.UsedFallback);

        Assert.Equal(
            "resource-1",
            summary.Plan.SelectedResourceId);

        Assert.Equal(
            "networking",
            summary.Plan.LiveSelectedDomainId);

        Assert.Equal(
            3,
            fixture.Controller.Depth);

        Assert.Equal(
            "resource-1",
            fixture.State
                .SelectedResource?
                .ResourceId);
    }

    [Fact]
    public void MissingResourceIsClearedAndReported()
    {
        var fixture =
            CreateFixture(
                CreateNode(
                    "available-resource",
                    "networking"));

        fixture.Selection.SelectResource(
            CreateResource(
                "original",
                "networking"));

        var summary =
            fixture.Coordinator.Restore(
                CreateView(
                    selectedResourceId:
                        "missing-resource",
                    selectedDomainId:
                        "networking"));

        Assert.True(
            summary.ResourceWasUnavailable);

        Assert.True(
            summary.UsedFallback);

        Assert.Equal(
            "",
            summary.Plan.SelectedResourceId);

        Assert.Null(
            fixture.State.SelectedResource);
    }

    [Fact]
    public void MissingDomainFallsBackToAllAndIsReported()
    {
        var fixture =
            CreateFixture(
                CreateNode(
                    "resource-1",
                    "networking"));

        var summary =
            fixture.Coordinator.Restore(
                CreateView(
                    selectedResourceId:
                        "resource-1",
                    selectedDomainId:
                        "removed-domain"));

        Assert.True(
            summary.DomainFellBackToAll);

        Assert.True(
            summary.UsedFallback);

        Assert.Equal(
            TopologyDomainFilter.AllDomains,
            summary.Plan.LiveSelectedDomainId);
    }

    [Fact]
    public void StoredAllDomainIsNotReportedAsFallback()
    {
        var fixture =
            CreateFixture(
                CreateNode(
                    "resource-1",
                    "networking"));

        var summary =
            fixture.Coordinator.Restore(
                CreateView(
                    selectedResourceId:
                        "resource-1",
                    selectedDomainId:
                        TopologySavedViewDomainCodec
                            .AllDomainsStorageId));

        Assert.False(
            summary.DomainFellBackToAll);

        Assert.False(
            summary.UsedFallback);

        Assert.Equal(
            "",
            summary.Plan.LiveSelectedDomainId);
    }

    [Fact]
    public void CollapsedDomainsAreFilteredAndCounted()
    {
        var fixture =
            CreateFixture(
                CreateNode(
                    "resource-1",
                    "networking"),

                CreateNode(
                    "resource-2",
                    "compute"));

        var summary =
            fixture.Coordinator.Restore(
                CreateView(
                    selectedResourceId:
                        "resource-1",

                    selectedDomainId:
                        "networking",

                    collapsedDomainIds:
                    [
                        "networking",
                        "compute",
                        "removed-domain"
                    ]));

        Assert.Equal(
            ["compute", "networking"],
            summary.Plan.CollapsedDomainIds);

        Assert.Equal(
            1,
            summary.IgnoredCollapsedDomainCount);

        Assert.True(
            summary.UsedFallback);
    }

    [Fact]
    public void EmptyGraphProducesSafeFallbackState()
    {
        var fixture =
            CreateFixture();

        fixture.Selection.SelectResource(
            CreateResource(
                "original",
                "networking"));

        var summary =
            fixture.Coordinator.Restore(
                CreateView(
                    selectedResourceId:
                        "missing-resource",

                    selectedDomainId:
                        "missing-domain",

                    depth:
                        1));

        Assert.True(
            summary.ResourceWasUnavailable);

        Assert.True(
            summary.DomainFellBackToAll);

        Assert.Null(
            fixture.State.SelectedResource);

        Assert.Equal(
            1,
            fixture.Controller.Depth);
    }

    [Fact]
    public void BlankGraphDomainIsAvailableAsOther()
    {
        var fixture =
            CreateFixture(
                CreateNode(
                    "resource-1",
                    ""));

        var summary =
            fixture.Coordinator.Restore(
                CreateView(
                    selectedResourceId:
                        "resource-1",

                    selectedDomainId:
                        "other"));

        Assert.False(
            summary.DomainFellBackToAll);

        Assert.Equal(
            "other",
            summary.Plan.LiveSelectedDomainId);
    }

    [Fact]
    public void InvalidViewIsRejectedBeforeWorkbenchMutation()
    {
        var fixture =
            CreateFixture(
                CreateNode(
                    "resource-1",
                    "networking"));

        var original =
            CreateResource(
                "original",
                "networking");

        fixture.Selection.SelectResource(
            original);

        var invalid =
            CreateView(
                selectedResourceId:
                    "resource-1",

                selectedDomainId:
                    "networking",

                depth:
                    9);

        Assert.Throws<ArgumentException>(() =>
            fixture.Coordinator.Restore(
                invalid));

        Assert.Same(
            original,
            fixture.State.SelectedResource);

        Assert.Equal(
            2,
            fixture.Controller.Depth);
    }

    private static CoordinatorFixture CreateFixture(
        params GraphNode[] nodes)
    {
        var state =
            new WorkbenchState();

        var graph =
            new InfrastructureGraph();

        foreach (var node in nodes)
        {
            graph.AddNode(node);
        }

        state.SetInfrastructureGraph(graph);

        var selection =
            new WorkbenchSelectionController(
                state);

        var graphContext =
            new WorkbenchGraphContextController(
                state);

        var controller =
            new WorkbenchTopologyController(
                graphContext,
                selection,
                state);

        var coordinator =
            new WorkbenchSavedViewRestoreCoordinator(
                state,
                controller);

        return new CoordinatorFixture(
            state,
            selection,
            controller,
            coordinator);
    }

    private static TopologySavedView CreateView(
        string selectedResourceId,
        string selectedDomainId,
        int depth = 2,
        string[]? collapsedDomainIds = null)
    {
        var created =
            new DateTimeOffset(
                2026,
                8,
                12,
                10,
                0,
                0,
                TimeSpan.Zero);

        return new TopologySavedView
        {
            Id = "saved-view",
            Name = "Saved view",
            CreatedAtUtc = created,
            UpdatedAtUtc =
                created.AddMinutes(1),

            SelectedResourceId =
                selectedResourceId,

            Depth =
                depth,

            LayoutMode =
                TopologyLayoutMode.Domain,

            SelectedDomainId =
                selectedDomainId,

            CollapsedDomainIds =
                collapsedDomainIds ?? [],

            RelationshipFilters =
                new TopologyRelationshipFilterState(),

            Zoom = 1,

            ScrollOffset =
                new TopologyScrollOffset(0, 0),

            IsMinimapVisible = true,

            SearchText = ""
        };
    }

    private static GraphNode CreateNode(
        string id,
        string domainId)
    {
        return new GraphNode
        {
            Id = id,
            ProviderId = "test",
            DomainId = domainId,
            ResourceType = "test-resource",
            DisplayName = id,

            Resource =
                CreateResource(
                    id,
                    domainId)
        };
    }

    private static InfrastructureResource
        CreateResource(
            string id,
            string domainId)
    {
        return new InfrastructureResource
        {
            ResourceId = id,
            ProviderId = "test",
            AccountId = "test-account",
            DomainId = domainId,
            ResourceType = "test-resource",
            NativeId = id,
            DisplayName = id,
            Location = "test-location"
        };
    }

    private sealed record CoordinatorFixture(
        WorkbenchState State,
        WorkbenchSelectionController Selection,
        WorkbenchTopologyController Controller,
        WorkbenchSavedViewRestoreCoordinator Coordinator);
}
