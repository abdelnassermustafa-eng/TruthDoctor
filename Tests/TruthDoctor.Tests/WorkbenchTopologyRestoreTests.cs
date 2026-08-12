using System;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchTopologyRestoreTests
{
    [Fact]
    public void RestoresSelectionAndDepthTogether()
    {
        var fixture =
            CreateFixture();

        var node =
            CreateNode("resource-2");

        var result =
            fixture.Controller
                .RestoreSavedViewSelection(
                    CreatePlan(
                        "resource-2",
                        depth: 3),
                    node);

        Assert.True(result.Changed);
        Assert.True(result.SelectionChanged);
        Assert.True(result.DepthChanged);
        Assert.True(result.HasSelectedResource);

        Assert.Equal(
            "resource-2",
            result.SelectedResourceId);

        Assert.Equal(3, result.Depth);
        Assert.Equal(3, fixture.Controller.Depth);

        Assert.Same(
            node.Resource,
            fixture.State.SelectedResource);
    }

    [Fact]
    public void RestoreClearsNavigationHistory()
    {
        var fixture =
            CreateFixture();

        var first =
            CreateNode("resource-1");

        var second =
            CreateNode("resource-2");

        fixture.Selection.SelectResource(
            ResourceOf(first));

        fixture.Controller.BeginSession();

        Assert.True(
            fixture.Controller.NavigateTo(
                ResourceOf(second)));

        Assert.True(
            fixture.Controller.CanGoBack);

        fixture.Controller
            .RestoreSavedViewSelection(
                CreatePlan("resource-1"),
                first);

        Assert.False(
            fixture.Controller.CanGoBack);

        Assert.False(
            fixture.Controller.CanGoForward);

        Assert.False(
            fixture.Controller.CanGoHome);
    }

    [Fact]
    public void EmptyResourceIdClearsCurrentSelection()
    {
        var fixture =
            CreateFixture();

        fixture.Selection.SelectResource(
            CreateResource("resource-1"));

        var result =
            fixture.Controller
                .RestoreSavedViewSelection(
                    CreatePlan(""),
                    selectedNode: null);

        Assert.True(result.SelectionChanged);

        Assert.False(
            result.HasSelectedResource);

        Assert.Null(
            fixture.State.SelectedResource);
    }

    [Fact]
    public void IdenticalStateReportsNoChange()
    {
        var fixture =
            CreateFixture();

        var node =
            CreateNode("resource-1");

        fixture.Selection.SelectResource(
            ResourceOf(node));

        fixture.Controller.SetDepth(3);

        var result =
            fixture.Controller
                .RestoreSavedViewSelection(
                    CreatePlan(
                        "resource-1",
                        depth: 3),
                    node);

        Assert.False(result.Changed);
        Assert.False(result.SelectionChanged);
        Assert.False(result.DepthChanged);
    }

    [Fact]
    public void MissingResolvedNodeIsRejectedBeforeMutation()
    {
        var fixture =
            CreateFixture();

        var original =
            CreateResource("original");

        fixture.Selection.SelectResource(
            original);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                fixture.Controller
                    .RestoreSavedViewSelection(
                        CreatePlan(
                            "missing",
                            depth: 3),
                        selectedNode: null));

        Assert.Contains(
            "could not be resolved",
            exception.Message);

        Assert.Same(
            original,
            fixture.State.SelectedResource);

        Assert.Equal(
            2,
            fixture.Controller.Depth);
    }

    [Fact]
    public void MismatchedGraphNodeIsRejectedBeforeMutation()
    {
        var fixture =
            CreateFixture();

        var original =
            CreateResource("original");

        fixture.Selection.SelectResource(
            original);

        Assert.Throws<ArgumentException>(() =>
            fixture.Controller
                .RestoreSavedViewSelection(
                    CreatePlan(
                        "expected-resource",
                        depth: 3),
                    CreateNode(
                        "different-resource")));

        Assert.Same(
            original,
            fixture.State.SelectedResource);

        Assert.Equal(
            2,
            fixture.Controller.Depth);
    }

    [Fact]
    public void InvalidDepthIsRejectedBeforeMutation()
    {
        var fixture =
            CreateFixture();

        var original =
            CreateResource("original");

        fixture.Selection.SelectResource(
            original);

        var invalidPlan =
            CreatePlan(
                "resource-1",
                depth: 9);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            fixture.Controller
                .RestoreSavedViewSelection(
                    invalidPlan,
                    CreateNode("resource-1")));

        Assert.Same(
            original,
            fixture.State.SelectedResource);

        Assert.Equal(
            2,
            fixture.Controller.Depth);
    }

    [Fact]
    public void NullPlanIsRejected()
    {
        var fixture =
            CreateFixture();

        Assert.Throws<ArgumentNullException>(() =>
            fixture.Controller
                .RestoreSavedViewSelection(
                    null!,
                    selectedNode: null));
    }

    private static ControllerFixture CreateFixture()
    {
        var state =
            new WorkbenchState();

        var selection =
            new WorkbenchSelectionController(
                state);

        var graph =
            new WorkbenchGraphContextController(
                state);

        var controller =
            new WorkbenchTopologyController(
                graph,
                selection,
                state);

        return new ControllerFixture(
            state,
            selection,
            controller);
    }

    private static TopologySavedViewRestorePlan
        CreatePlan(
            string selectedResourceId,
            int depth = 2)
    {
        return new TopologySavedViewRestorePlan
        {
            SelectedResourceId =
                selectedResourceId,

            Depth =
                depth
        };
    }

    private static GraphNode CreateNode(
        string id)
    {
        return new GraphNode
        {
            Id = id,
            ProviderId = "test",
            DomainId = "networking",
            ResourceType = "test-resource",
            DisplayName = id,
            Resource =
                CreateResource(id)
        };
    }

    private static InfrastructureResource ResourceOf(
        GraphNode node)
    {
        if (node.Resource is not
            InfrastructureResource resource)
        {
            throw new InvalidOperationException(
                "Test graph node has no infrastructure resource.");
        }

        return resource;
    }

    private static InfrastructureResource
        CreateResource(
            string id)
    {
        return new InfrastructureResource
        {
            ResourceId = id,
            ProviderId = "test",
            AccountId = "test-account",
            DomainId = "networking",
            ResourceType = "test-resource",
            NativeId = id,
            DisplayName = id,
            Location = "test-location"
        };
    }

    private sealed record ControllerFixture(
        WorkbenchState State,
        WorkbenchSelectionController Selection,
        WorkbenchTopologyController Controller);
}
