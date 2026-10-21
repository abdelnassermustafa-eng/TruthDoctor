using System;
using System.Collections.Generic;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Declarative snapshot of existing workspace contracts.
/// This does not prove runtime capability implementation.
/// </summary>
public sealed record WorkbenchWorkspaceVerificationResult(
    string Workspace,
    WorkbenchGlobalSearchRoute SearchRoute,
    WorkbenchWorkspaceOwnershipKind Ownership,
    WorkbenchWorkspacePresentationState PresentationState);

/// <summary>
/// Combines existing ownership, search-routing, and presentation
/// contracts without executing navigation or mutating shared state.
/// </summary>
public static class WorkbenchWorkspaceVerificationMatrix
{
    private static readonly IReadOnlyList<string> WorkspaceNames =
        Array.AsReadOnly(new[]
        {
            "dashboard",
            "resources",
            "topology",
            "operations",
            "history",
            "reports",
            "settings"
        });

    public static IReadOnlyList<string> Workspaces => WorkspaceNames;

    public static WorkbenchWorkspaceVerificationResult Evaluate(
        string workspace,
        WorkbenchWorkspaceConcern concern,
        bool isLoading,
        bool hasContent,
        string? errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);

        var route = WorkbenchGlobalSearchCoordinator.Resolve(workspace);

        if (route == WorkbenchGlobalSearchRoute.Unknown)
        {
            throw new ArgumentException(
                "Unknown workspace.",
                nameof(workspace));
        }

        var canonicalWorkspace = route.ToString().ToLowerInvariant();

        var ownership = route switch
        {
            WorkbenchGlobalSearchRoute.Dashboard =>
                WorkbenchDashboardOwnershipContract.Classify(concern),

            WorkbenchGlobalSearchRoute.Resources =>
                WorkbenchResourcesOwnershipContract.Classify(concern),

            WorkbenchGlobalSearchRoute.Topology =>
                WorkbenchTopologyOwnershipContract.Classify(concern),

            WorkbenchGlobalSearchRoute.Operations =>
                WorkbenchOperationsOwnershipContract.Classify(concern),

            WorkbenchGlobalSearchRoute.History =>
                WorkbenchHistoryOwnershipContract.Classify(concern),

            WorkbenchGlobalSearchRoute.Reports =>
                WorkbenchReportsOwnershipContract.Classify(concern),

            WorkbenchGlobalSearchRoute.Settings =>
                WorkbenchSettingsOwnershipContract.Classify(concern),

            _ => throw new ArgumentOutOfRangeException(
                nameof(workspace),
                workspace,
                "Unsupported workspace route.")
        };

        var presentation =
            WorkbenchWorkspacePresentationStateContract.Classify(
                canonicalWorkspace,
                isLoading,
                hasContent,
                errorMessage);

        return new WorkbenchWorkspaceVerificationResult(
            canonicalWorkspace,
            route,
            ownership,
            presentation);
    }
}
