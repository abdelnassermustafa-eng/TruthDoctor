namespace TruthDoctor.Graph;

/// <summary>
/// Validated, provider-neutral instructions for restoring a saved topology
/// view. The selected domain uses the topology workspace's live
/// representation, where an empty value means all domains.
/// </summary>
public sealed class TopologySavedViewRestorePlan
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
