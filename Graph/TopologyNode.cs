using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class TopologyNode
{
    public string Id { get; init; } = "";

    public string ProviderId { get; init; } = "";

    public string AccountId { get; init; } = "";

    public string DomainId { get; init; } = "";

    public string ResourceType { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public string NativeId { get; init; } = "";

    public string State { get; init; } = "";

    public string Location { get; init; } = "";

    public string AvailabilityZone { get; init; } = "";

    public string Arn { get; init; } = "";

    public IReadOnlyDictionary<string, string> Properties
    { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> Tags
    { get; init; } =
        new Dictionary<string, string>();

    public bool IsSelected { get; init; }
}
