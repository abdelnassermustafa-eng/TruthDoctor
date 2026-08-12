using System;

namespace TruthDoctor.Graph;

/// <summary>
/// Portable, provider-neutral snapshot of topology workspace settings.
/// </summary>
public sealed class TopologySavedView
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } =
        CurrentSchemaVersion;

    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }

    public string SelectedResourceId { get; init; } = "";

    public int Depth { get; init; } = 2;

    public TopologyLayoutMode LayoutMode { get; init; } =
        TopologyLayoutMode.Radial;

    /// <summary>
    /// Uses "all" for all domains. It never stores the live empty sentinel.
    /// </summary>
    public string SelectedDomainId { get; init; } =
        TopologySavedViewDomainCodec
            .AllDomainsStorageId;

    public string[] CollapsedDomainIds { get; init; } =
        [];

    public TopologyRelationshipFilterState
        RelationshipFilters { get; init; } =
            new();

    public double Zoom { get; init; } = 1.00;

    public TopologyScrollOffset ScrollOffset { get; init; } =
        new(0, 0);

    public bool IsMinimapVisible { get; init; } = true;

    public string SearchText { get; init; } = "";
}
