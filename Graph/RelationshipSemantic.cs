namespace TruthDoctor.Graph;

public sealed class RelationshipSemantic
{
    public RelationshipKind Kind { get; init; } =
        RelationshipKind.Unknown;

    public string CanonicalName { get; init; } =
        "related-to";

    public string ReverseName { get; init; } =
        "related-from";

    public bool IsDependency { get; init; }

    public bool IsContainment { get; init; }

    public bool IsConnectivity { get; init; }

    public bool IsSecurity { get; init; }

    public bool IsTrafficFlow { get; init; }
}
