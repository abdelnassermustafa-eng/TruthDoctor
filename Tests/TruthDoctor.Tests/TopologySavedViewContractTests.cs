using System;
using System.Text.Json;
using TruthDoctor.Graph;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class TopologySavedViewContractTests
{
    private readonly TopologySavedViewValidator
        _validator =
            new();

    private readonly TopologySavedViewSerializer
        _serializer =
            new();

    [Fact]
    public void LiveAllDomainsUsesPortableStorageValue()
    {
        var stored =
            TopologySavedViewDomainCodec
                .ToStorage(
                    TopologyDomainFilter.AllDomains);

        Assert.Equal(
            "all",
            stored);

        Assert.False(
            string.IsNullOrWhiteSpace(stored));
    }

    [Fact]
    public void PortableAllDomainsRestoresLiveSentinel()
    {
        var live =
            TopologySavedViewDomainCodec
                .ToLive("ALL");

        Assert.Equal(
            TopologyDomainFilter.AllDomains,
            live);

        Assert.Equal("", live);
    }

    [Fact]
    public void SpecificDomainRoundTripsWithoutSentinelCollision()
    {
        const string domainId =
            "future-provider-domain";

        var stored =
            TopologySavedViewDomainCodec
                .ToStorage(
                    $"  {domainId}  ");

        var live =
            TopologySavedViewDomainCodec
                .ToLive(stored);

        Assert.Equal(domainId, stored);
        Assert.Equal(domainId, live);
    }

    [Fact]
    public void BlankStoredDomainIsRejected()
    {
        Assert.Throws<
            ArgumentException>(() =>
                TopologySavedViewDomainCodec
                    .ToLive("   "));
    }

    [Fact]
    public void DefaultContractStoresNonBlankAllDomains()
    {
        var view =
            new TopologySavedView();

        Assert.Equal(
            "all",
            view.SelectedDomainId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                view.SelectedDomainId));
    }

    [Fact]
    public void ValidProviderNeutralViewPassesValidation()
    {
        var errors =
            _validator.Validate(
                CreateView());

        Assert.Empty(errors);
    }

    [Fact]
    public void MissingStoredDomainFailsValidation()
    {
        var source =
            CreateView();

        var invalid =
            Copy(
                source,
                selectedDomainId: "");

        var error =
            Assert.Single(
                _validator.Validate(invalid),
                message =>
                    message.Contains(
                        "SelectedDomainId",
                        StringComparison.Ordinal));

        Assert.Contains(
            "required",
            error,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonRoundTripPreservesWorkspaceContract()
    {
        var source =
            CreateView();

        var json =
            _serializer.Serialize(source);

        var restored =
            _serializer.Deserialize(json);

        Assert.Equal(source.Id, restored.Id);
        Assert.Equal(source.Name, restored.Name);
        Assert.Equal(
            source.SelectedResourceId,
            restored.SelectedResourceId);
        Assert.Equal(
            source.SelectedDomainId,
            restored.SelectedDomainId);
        Assert.Equal(
            source.LayoutMode,
            restored.LayoutMode);
        Assert.Equal(
            source.ScrollOffset,
            restored.ScrollOffset);
        Assert.Equal(
            source.CollapsedDomainIds,
            restored.CollapsedDomainIds);
    }

    [Fact]
    public void JsonStoresAllDomainsAsExplicitText()
    {
        var view =
            Copy(
                CreateView(),
                selectedDomainId:
                    TopologySavedViewDomainCodec
                        .AllDomainsStorageId);

        var json =
            _serializer.Serialize(view);

        Assert.Contains(
            "\"selectedDomainId\": \"all\"",
            json,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "\"selectedDomainId\": \"\"",
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedJsonIsRejected()
    {
        Assert.Throws<JsonException>(() =>
            _serializer.Deserialize(
                "{ not-json }"));
    }

    [Fact]
    public void UnknownLayoutModeIsRejectedDuringParsing()
    {
        var json =
            _serializer
                .Serialize(
                    CreateView())
                .Replace(
                    "\"domain\"",
                    "\"future-layout\"",
                    StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            _serializer.Deserialize(json));
    }

    [Fact]
    public void RelationshipFilterAllStateIsDerived()
    {
        var all =
            new TopologyRelationshipFilterState();

        var partial =
            new TopologyRelationshipFilterState
            {
                Security = false
            };

        Assert.True(all.AreAllEnabled);
        Assert.False(partial.AreAllEnabled);
    }

    private static TopologySavedView CreateView()
    {
        var timestamp =
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
            Id = "network-overview",
            Name = "Network Overview",
            CreatedAtUtc = timestamp,
            UpdatedAtUtc =
                timestamp.AddMinutes(5),

            SelectedResourceId =
                "provider-neutral-resource",

            Depth = 2,

            LayoutMode =
                TopologyLayoutMode.Domain,

            SelectedDomainId =
                "future-provider-networking",

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

    private static TopologySavedView Copy(
        TopologySavedView source,
        string? selectedDomainId = null)
    {
        return new TopologySavedView
        {
            SchemaVersion =
                source.SchemaVersion,

            Id = source.Id,
            Name = source.Name,

            CreatedAtUtc =
                source.CreatedAtUtc,

            UpdatedAtUtc =
                source.UpdatedAtUtc,

            SelectedResourceId =
                source.SelectedResourceId,

            Depth = source.Depth,

            LayoutMode =
                source.LayoutMode,

            SelectedDomainId =
                selectedDomainId ??
                source.SelectedDomainId,

            CollapsedDomainIds =
                [.. source.CollapsedDomainIds],

            RelationshipFilters =
                source.RelationshipFilters,

            Zoom = source.Zoom,

            ScrollOffset =
                source.ScrollOffset,

            IsMinimapVisible =
                source.IsMinimapVisible,

            SearchText =
                source.SearchText
        };
    }
}
