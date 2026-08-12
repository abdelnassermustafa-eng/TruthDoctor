using System;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologySavedViewRestoreTests
{
    private readonly TopologySavedViewRestoreService
        _service =
            new();

    [Fact]
    public void ValidViewProducesCompleteRestorePlan()
    {
        var view =
            CreateView();

        var plan =
            _service.CreatePlan(
                view,
                ["resource-1", "resource-2"],
                ["compute", "networking"]);

        Assert.Equal(
            "resource-1",
            plan.SelectedResourceId);

        Assert.Equal(3, plan.Depth);

        Assert.Equal(
            TopologyLayoutMode.Domain,
            plan.LayoutMode);

        Assert.Equal(
            "networking",
            plan.LiveSelectedDomainId);

        Assert.Equal(
            ["compute", "networking"],
            plan.CollapsedDomainIds);

        Assert.False(
            plan.RelationshipFilters.Security);

        Assert.Equal(1.25, plan.Zoom);

        Assert.Equal(
            new TopologyScrollOffset(120, 80),
            plan.ScrollOffset);

        Assert.False(plan.IsMinimapVisible);

        Assert.Equal(
            "database",
            plan.SearchText);
    }

    [Fact]
    public void StoredAllDomainMapsToLiveEmptySentinel()
    {
        var view =
            CopyView(
                CreateView(),
                selectedDomainId:
                    TopologySavedViewDomainCodec
                        .AllDomainsStorageId);

        var plan =
            _service.CreatePlan(
                view,
                ["resource-1"],
                ["compute", "networking"]);

        Assert.Equal(
            TopologyDomainFilter.AllDomains,
            plan.LiveSelectedDomainId);

        Assert.Equal(
            "",
            plan.LiveSelectedDomainId);
    }

    [Fact]
    public void ExistingProviderNeutralDomainIsRestored()
    {
        var view =
            CopyView(
                CreateView(),
                selectedDomainId:
                    "future-provider-domain");

        var plan =
            _service.CreatePlan(
                view,
                ["resource-1"],
                ["future-provider-domain"]);

        Assert.Equal(
            "future-provider-domain",
            plan.LiveSelectedDomainId);
    }

    [Fact]
    public void MissingSelectedDomainFallsBackToAllDomains()
    {
        var view =
            CopyView(
                CreateView(),
                selectedDomainId:
                    "domain-no-longer-present");

        var plan =
            _service.CreatePlan(
                view,
                ["resource-1"],
                ["compute", "networking"]);

        Assert.Equal(
            TopologyDomainFilter.AllDomains,
            plan.LiveSelectedDomainId);
    }

    [Fact]
    public void MissingSelectedResourceIsCleared()
    {
        var plan =
            _service.CreatePlan(
                CreateView(),
                ["another-resource"],
                ["compute", "networking"]);

        Assert.Equal(
            "",
            plan.SelectedResourceId);
    }

    [Fact]
    public void UnavailableCollapsedDomainsAreIgnored()
    {
        var view =
            CopyView(
                CreateView(),
                collapsedDomainIds:
                [
                    "networking",
                    "removed-domain",
                    "compute"
                ]);

        var plan =
            _service.CreatePlan(
                view,
                ["resource-1"],
                ["networking", "compute"]);

        Assert.Equal(
            ["compute", "networking"],
            plan.CollapsedDomainIds);
    }

    [Fact]
    public void PlanDoesNotReuseMutableRelationshipFilter()
    {
        var view =
            CreateView();

        var plan =
            _service.CreatePlan(
                view,
                ["resource-1"],
                ["compute", "networking"]);

        Assert.NotSame(
            view.RelationshipFilters,
            plan.RelationshipFilters);

        Assert.Equal(
            view.RelationshipFilters.Security,
            plan.RelationshipFilters.Security);
    }

    [Fact]
    public void InvalidViewIsRejectedBeforePlanCreation()
    {
        var invalid =
            CopyView(
                CreateView(),
                depth: 9);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                _service.CreatePlan(
                    invalid,
                    ["resource-1"],
                    ["networking"]));

        Assert.Contains(
            "Depth must be between 1 and 3.",
            exception.Message);
    }

    private static TopologySavedView CreateView()
    {
        var created =
            new DateTimeOffset(
                2026,
                8,
                12,
                9,
                0,
                0,
                TimeSpan.Zero);

        return new TopologySavedView
        {
            Id = "view-1",
            Name = "Network investigation",
            CreatedAtUtc = created,
            UpdatedAtUtc =
                created.AddMinutes(5),

            SelectedResourceId =
                "resource-1",

            Depth = 3,

            LayoutMode =
                TopologyLayoutMode.Domain,

            SelectedDomainId =
                "networking",

            CollapsedDomainIds =
            [
                "networking",
                "compute"
            ],

            RelationshipFilters =
                new TopologyRelationshipFilterState
                {
                    Security = false
                },

            Zoom = 1.25,

            ScrollOffset =
                new TopologyScrollOffset(
                    120,
                    80),

            IsMinimapVisible = false,

            SearchText = "  database  "
        };
    }

    private static TopologySavedView CopyView(
        TopologySavedView source,
        int? depth = null,
        string? selectedDomainId = null,
        string[]? collapsedDomainIds = null)
    {
        return new TopologySavedView
        {
            SchemaVersion =
                source.SchemaVersion,

            Id =
                source.Id,

            Name =
                source.Name,

            CreatedAtUtc =
                source.CreatedAtUtc,

            UpdatedAtUtc =
                source.UpdatedAtUtc,

            SelectedResourceId =
                source.SelectedResourceId,

            Depth =
                depth ?? source.Depth,

            LayoutMode =
                source.LayoutMode,

            SelectedDomainId =
                selectedDomainId ??
                source.SelectedDomainId,

            CollapsedDomainIds =
                collapsedDomainIds ??
                source.CollapsedDomainIds,

            RelationshipFilters =
                source.RelationshipFilters,

            Zoom =
                source.Zoom,

            ScrollOffset =
                source.ScrollOffset,

            IsMinimapVisible =
                source.IsMinimapVisible,

            SearchText =
                source.SearchText
        };
    }
}
