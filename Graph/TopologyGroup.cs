using System.Collections.Generic;

namespace TruthDoctor.Graph;

/// <summary>
/// Represents a provider-neutral collection of topology nodes that
/// belong to the same infrastructure domain.
/// </summary>
public sealed class TopologyGroup
{
    public string Id { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public IReadOnlyList<string> NodeIds { get; init; } =
        [];

    public int Count =>
        NodeIds.Count;
}
