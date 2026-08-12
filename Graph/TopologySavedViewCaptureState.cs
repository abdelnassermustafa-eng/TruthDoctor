using System;

namespace TruthDoctor.Graph;

/// <summary>
/// Provider-neutral values read from the live topology workspace.
/// The domain ID here uses the topology's live representation.
/// </summary>
public sealed class TopologySavedViewCaptureState
{
    public string SelectedResourceId { get; init; } = "";

    public int Depth { get; init; } = 2;

    public TopologyLayoutMode LayoutMode { get; init; } =
        TopologyLayoutMode.Radial;

    public string LiveSelectedDomainId { get; init; } =
        TopologyDomainFilter.AllDomains;

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
