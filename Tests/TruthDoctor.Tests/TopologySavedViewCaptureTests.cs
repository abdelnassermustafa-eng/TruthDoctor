using System;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologySavedViewCaptureTests
{
    private readonly TopologySavedViewCaptureService
        _service =
            new();

    [Fact]
    public void LiveAllDomainsIsCapturedAsPortableAll()
    {
        var view =
            Capture(
                CreateState(
                    liveDomainId:
                        TopologyDomainFilter.AllDomains));

        Assert.Equal(
            "all",
            view.SelectedDomainId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                view.SelectedDomainId));
    }

    [Fact]
    public void SpecificLiveDomainIsCapturedWithoutSentinel()
    {
        var view =
            Capture(
                CreateState(
                    liveDomainId:
                        "future-provider-domain"));

        Assert.Equal(
            "future-provider-domain",
            view.SelectedDomainId);
    }

    [Fact]
    public void CompleteWorkspaceStateIsCaptured()
    {
        var filters =
            new TopologyRelationshipFilterState
            {
                Security = false,
                Traffic = false
            };

        var state =
            new TopologySavedViewCaptureState
            {
                SelectedResourceId =
                    "resource-123",

                Depth = 3,

                LayoutMode =
                    TopologyLayoutMode.Network,

                LiveSelectedDomainId =
                    "networking",

                CollapsedDomainIds =
                [
                    "storage",
                    "compute"
                ],

                RelationshipFilters =
                    filters,

                Zoom = 1.40,

                ScrollOffset =
                    new TopologyScrollOffset(
                        320,
                        180),

                IsMinimapVisible = false,
                SearchText = "  route table  "
            };

        var view =
            Capture(state);

        Assert.Equal(
            "resource-123",
            view.SelectedResourceId);

        Assert.Equal(3, view.Depth);

        Assert.Equal(
            TopologyLayoutMode.Network,
            view.LayoutMode);

        Assert.Equal(
            "networking",
            view.SelectedDomainId);

        Assert.Equal(
            ["compute", "storage"],
            view.CollapsedDomainIds);

        Assert.Same(
            filters,
            view.RelationshipFilters);

        Assert.False(
            view.RelationshipFilters.Security);

        Assert.False(
            view.RelationshipFilters.Traffic);

        Assert.Equal(1.40, view.Zoom);

        Assert.Equal(
            new TopologyScrollOffset(
                320,
                180),
            view.ScrollOffset);

        Assert.False(
            view.IsMinimapVisible);

        Assert.Equal(
            "route table",
            view.SearchText);
    }

    [Fact]
    public void CaptureTrimsIdentityAndName()
    {
        var timestamp =
            Timestamp();

        var view =
            _service.Capture(
                "  saved-id  ",
                "  My topology  ",
                timestamp,
                timestamp,
                CreateState());

        Assert.Equal(
            "saved-id",
            view.Id);

        Assert.Equal(
            "My topology",
            view.Name);
    }

    [Fact]
    public void CaptureProducesValidatorApprovedView()
    {
        var view =
            Capture(
                CreateState());

        var errors =
            new TopologySavedViewValidator()
                .Validate(view);

        Assert.Empty(errors);
    }

    [Fact]
    public void InvalidCaptureMetadataIsRejected()
    {
        var timestamp =
            Timestamp();

        Assert.Throws<
            ArgumentException>(() =>
                _service.Capture(
                    "",
                    "",
                    timestamp,
                    timestamp,
                    CreateState()));
    }

    [Fact]
    public void InvalidLiveWorkspaceStateIsRejected()
    {
        var invalid =
            new TopologySavedViewCaptureState
            {
                Depth = 99,
                Zoom = -1
            };

        Assert.Throws<
            ArgumentException>(() =>
                Capture(invalid));
    }

    [Fact]
    public void CaptureDoesNotDependOnCloudProvider()
    {
        var state =
            CreateState(
                liveDomainId:
                    "future-cloud-quantum");

        var view =
            Capture(state);

        Assert.Equal(
            "future-cloud-quantum",
            view.SelectedDomainId);

        Assert.Equal(
            "provider-neutral-resource",
            view.SelectedResourceId);
    }

    private TopologySavedView Capture(
        TopologySavedViewCaptureState state)
    {
        var timestamp =
            Timestamp();

        return _service.Capture(
            "view-one",
            "View One",
            timestamp,
            timestamp.AddMinutes(1),
            state);
    }

    private static TopologySavedViewCaptureState
        CreateState(
            string liveDomainId =
                "networking")
    {
        return new TopologySavedViewCaptureState
        {
            SelectedResourceId =
                "provider-neutral-resource",

            Depth = 2,

            LayoutMode =
                TopologyLayoutMode.Domain,

            LiveSelectedDomainId =
                liveDomainId,

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

    private static DateTimeOffset Timestamp()
    {
        return new DateTimeOffset(
            2026,
            8,
            12,
            9,
            45,
            0,
            TimeSpan.Zero);
    }
}
