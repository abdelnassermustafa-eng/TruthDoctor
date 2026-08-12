using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologySavedViewCatalogTests
{
    [Fact]
    public void AddedViewCanBeFoundIgnoringIdCase()
    {
        var catalog =
            new TopologySavedViewCatalog();

        var view =
            CreateView(
                "one",
                "Primary view");

        catalog.Add(view);

        Assert.Same(
            view,
            catalog.Find("ONE"));
    }

    [Fact]
    public void DuplicateIdsAreRejectedIgnoringCase()
    {
        var catalog =
            new TopologySavedViewCatalog();

        catalog.Add(
            CreateView(
                "one",
                "First"));

        Assert.Throws<
            InvalidOperationException>(() =>
                catalog.Add(
                    CreateView(
                        "ONE",
                        "Second")));
    }

    [Fact]
    public void DuplicateNamesAreRejectedIgnoringCase()
    {
        var catalog =
            new TopologySavedViewCatalog();

        catalog.Add(
            CreateView(
                "one",
                "Network view"));

        Assert.Throws<
            InvalidOperationException>(() =>
                catalog.Add(
                    CreateView(
                        "two",
                        "NETWORK VIEW")));
    }

    [Fact]
    public void RenamePreservesWorkspaceState()
    {
        var catalog =
            new TopologySavedViewCatalog();

        var original =
            CreateView(
                "one",
                "Before");

        catalog.Add(original);

        var renamed =
            catalog.Rename(
                original.Id,
                "After",
                original.UpdatedAtUtc
                    .AddMinutes(5));

        Assert.Equal("After", renamed.Name);
        Assert.Equal(original.Id, renamed.Id);
        Assert.Equal(
            original.CreatedAtUtc,
            renamed.CreatedAtUtc);
        Assert.Equal(
            original.SelectedDomainId,
            renamed.SelectedDomainId);
        Assert.Equal(
            original.SelectedResourceId,
            renamed.SelectedResourceId);
        Assert.Equal(
            original.LayoutMode,
            renamed.LayoutMode);
        Assert.Equal(
            original.Zoom,
            renamed.Zoom);
        Assert.Equal(
            original.ScrollOffset,
            renamed.ScrollOffset);
        Assert.Equal(
            original.CollapsedDomainIds,
            renamed.CollapsedDomainIds);
    }

    [Fact]
    public void RenameRejectsAnotherViewsName()
    {
        var catalog =
            new TopologySavedViewCatalog();

        catalog.Add(
            CreateView(
                "one",
                "First"));

        catalog.Add(
            CreateView(
                "two",
                "Second"));

        Assert.Throws<
            InvalidOperationException>(() =>
                catalog.Rename(
                    "two",
                    "FIRST",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void DeleteReportsWhetherViewExisted()
    {
        var catalog =
            new TopologySavedViewCatalog();

        catalog.Add(
            CreateView(
                "one",
                "View"));

        Assert.True(
            catalog.Delete("ONE"));

        Assert.False(
            catalog.Delete("one"));

        Assert.Empty(catalog.All);
    }

    [Fact]
    public void ViewsAreOrderedByNameDeterministically()
    {
        var catalog =
            new TopologySavedViewCatalog();

        catalog.Add(
            CreateView(
                "three",
                "Zulu"));

        catalog.Add(
            CreateView(
                "one",
                "Alpha"));

        catalog.Add(
            CreateView(
                "two",
                "Bravo"));

        Assert.Equal(
            ["Alpha", "Bravo", "Zulu"],
            catalog.All.Select(view =>
                view.Name));
    }

    [Fact]
    public void ReplaceAllIsAtomicWhenReplacementIsInvalid()
    {
        var catalog =
            new TopologySavedViewCatalog();

        catalog.Add(
            CreateView(
                "existing",
                "Existing"));

        Assert.Throws<
            InvalidOperationException>(() =>
                catalog.ReplaceAll(
                [
                    CreateView(
                        "one",
                        "Duplicate"),

                    CreateView(
                        "two",
                        "DUPLICATE")
                ]));

        var remaining =
            Assert.Single(catalog.All);

        Assert.Equal(
            "existing",
            remaining.Id);
    }

    private static TopologySavedView CreateView(
        string id,
        string name)
    {
        var timestamp =
            new DateTimeOffset(
                2026,
                8,
                12,
                9,
                30,
                0,
                TimeSpan.Zero);

        return new TopologySavedView
        {
            Id = id,
            Name = name,

            CreatedAtUtc =
                timestamp,

            UpdatedAtUtc =
                timestamp,

            SelectedResourceId =
                "provider-neutral-resource",

            Depth = 2,

            LayoutMode =
                TopologyLayoutMode.Domain,

            SelectedDomainId =
                TopologySavedViewDomainCodec
                    .AllDomainsStorageId,

            CollapsedDomainIds =
            [
                "compute"
            ],

            RelationshipFilters =
                new TopologyRelationshipFilterState(),

            Zoom = 1.10,

            ScrollOffset =
                new TopologyScrollOffset(
                    120,
                    80),

            IsMinimapVisible = true,
            SearchText = "route"
        };
    }
}
