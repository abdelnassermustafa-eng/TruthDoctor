using System;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Describes the presentation state of an individual workspace.
/// This contract does not execute discovery or mutate shared state.
/// </summary>
public enum WorkbenchWorkspacePresentationState
{
    Ready,
    Empty,
    Loading,
    Error
}

/// <summary>
/// Classifies workspace presentation state from explicit inputs.
/// Content availability remains owned by each workspace.
/// </summary>
public static class WorkbenchWorkspacePresentationStateContract
{
    public static WorkbenchWorkspacePresentationState Classify(
        string workspace,
        bool isLoading,
        bool hasContent,
        string? errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);

        if (!IsRecognizedWorkspace(workspace))
        {
            throw new ArgumentException(
                "Unknown workspace.",
                nameof(workspace));
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            return WorkbenchWorkspacePresentationState.Error;
        }

        if (isLoading)
        {
            return WorkbenchWorkspacePresentationState.Loading;
        }

        return hasContent
            ? WorkbenchWorkspacePresentationState.Ready
            : WorkbenchWorkspacePresentationState.Empty;
    }

    private static bool IsRecognizedWorkspace(string workspace)
    {
        return workspace.Equals(
                   "dashboard",
                   StringComparison.OrdinalIgnoreCase)
               || workspace.Equals(
                   "resources",
                   StringComparison.OrdinalIgnoreCase)
               || workspace.Equals(
                   "topology",
                   StringComparison.OrdinalIgnoreCase)
               || workspace.Equals(
                   "operations",
                   StringComparison.OrdinalIgnoreCase)
               || workspace.Equals(
                   "history",
                   StringComparison.OrdinalIgnoreCase)
               || workspace.Equals(
                   "reports",
                   StringComparison.OrdinalIgnoreCase)
               || workspace.Equals(
                   "settings",
                   StringComparison.OrdinalIgnoreCase);
    }
}
