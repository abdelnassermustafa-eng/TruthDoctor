using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class ImpactAnalysisResult
{
    public string ResourceId { get; init; } = "";

    public int DirectDependentCount { get; init; }

    public int TotalAffectedCount { get; init; }

    public IReadOnlyList<GraphNode> DirectDependents { get; init; } =
        [];

    public IReadOnlyList<GraphNode> AffectedResources { get; init; } =
        [];

    public IReadOnlyDictionary<string, int> AffectedByDomain { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> AffectedByResourceType
    { get; init; } =
        new Dictionary<string, int>();
}
