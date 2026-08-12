namespace TruthDoctor.Graph;

/// <summary>
/// Describes the Workbench-owned portion of a saved-view restoration.
/// </summary>
public sealed class TopologySavedViewWorkbenchRestoreResult
{
    public bool Changed =>
        SelectionChanged ||
        DepthChanged;

    public bool SelectionChanged { get; init; }

    public bool DepthChanged { get; init; }

    public bool HasSelectedResource { get; init; }

    public string SelectedResourceId { get; init; } = "";

    public int Depth { get; init; }
}
