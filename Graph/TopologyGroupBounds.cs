namespace TruthDoctor.Graph;

/// <summary>
/// Describes the provider-neutral visual boundary surrounding one
/// topology domain group.
/// </summary>
public sealed class TopologyGroupBounds
{
    public string GroupId { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public int NodeCount { get; init; }

    public double X { get; init; }

    public double Y { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }
}
