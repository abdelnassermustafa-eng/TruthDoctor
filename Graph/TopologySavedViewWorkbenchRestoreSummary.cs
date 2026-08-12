namespace TruthDoctor.Graph;

/// <summary>
/// Reports the coordinated Workbench portion of saved-view restoration,
/// including safe fallbacks caused by changed infrastructure.
/// </summary>
public sealed class TopologySavedViewWorkbenchRestoreSummary
{
    public TopologySavedViewRestorePlan Plan { get; init; } =
        new();

    public TopologySavedViewWorkbenchRestoreResult
        WorkbenchResult { get; init; } =
            new();

    public bool ResourceWasUnavailable { get; init; }

    public bool DomainFellBackToAll { get; init; }

    public int IgnoredCollapsedDomainCount { get; init; }

    public bool UsedFallback =>
        ResourceWasUnavailable ||
        DomainFellBackToAll ||
        IgnoredCollapsedDomainCount > 0;
}
