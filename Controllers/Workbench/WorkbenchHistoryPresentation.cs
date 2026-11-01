using System;
using System.Collections.Generic;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Read-only History workspace presentation derived exclusively
/// from explicitly supplied search documents and state inputs.
/// No historical events are discovered, persisted, or verified here.
/// </summary>
public sealed class WorkbenchHistoryPresentation
{
    public WorkbenchWorkspacePresentationState State { get; }

    public string Title { get; }

    public string Description { get; }

    public IReadOnlyList<WorkbenchHistorySearchDocument> Documents { get; }

    private WorkbenchHistoryPresentation(
        WorkbenchWorkspacePresentationState state,
        string description,
        IReadOnlyList<WorkbenchHistorySearchDocument> documents)
    {
        State = state;
        Title = "History";
        Description = description;
        Documents = documents;
    }

    public static WorkbenchHistoryPresentation Create(
        IEnumerable<WorkbenchHistorySearchDocument> documents,
        string? searchText = null,
        bool isLoading = false,
        string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var filtered = WorkbenchHistorySearch.Search(
            documents,
            searchText);

        var state =
            WorkbenchWorkspacePresentationStateContract.Classify(
                "history",
                isLoading,
                filtered.Count > 0,
                errorMessage);

        string description = state switch
        {
            WorkbenchWorkspacePresentationState.Error =>
                errorMessage!.Trim(),

            WorkbenchWorkspacePresentationState.Loading =>
                "Operation history is loading.",

            WorkbenchWorkspacePresentationState.Empty =>
                "No operation history records are currently loaded.",

            WorkbenchWorkspacePresentationState.Ready =>
                filtered.Count == 1
                    ? "1 operation history record available."
                    : $"{filtered.Count} operation history records available.",

            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown History presentation state.")
        };

        return new WorkbenchHistoryPresentation(
            state,
            description,
            new List<WorkbenchHistorySearchDocument>(filtered).AsReadOnly());
    }
}
