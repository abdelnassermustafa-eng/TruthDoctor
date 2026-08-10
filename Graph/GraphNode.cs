namespace TruthDoctor.Graph;

public sealed class GraphNode
{
    public required string Id { get; init; }

    public required string ProviderId { get; init; }

    public required string DomainId { get; init; }

    public required string ResourceType { get; init; }

    public required string DisplayName { get; init; }

    public object? Resource { get; init; }
}
