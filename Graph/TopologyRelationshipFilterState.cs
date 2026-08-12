namespace TruthDoctor.Graph;

/// <summary>
/// Provider-neutral relationship visibility stored with a topology view.
/// </summary>
public sealed class TopologyRelationshipFilterState
{
    public bool Containment { get; init; } = true;

    public bool Placement { get; init; } = true;

    public bool Dependency { get; init; } = true;

    public bool Connectivity { get; init; } = true;

    public bool Security { get; init; } = true;

    public bool Traffic { get; init; } = true;

    public bool Association { get; init; } = true;

    public bool Other { get; init; } = true;

    public bool AreAllEnabled =>
        Containment &&
        Placement &&
        Dependency &&
        Connectivity &&
        Security &&
        Traffic &&
        Association &&
        Other;
}
