using System;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Defines the Resources workspace's responsibility boundaries.
/// Ownership describes workspace behavior, not exclusive ownership
/// of canonical platform state or infrastructure operations.
/// </summary>
public static class WorkbenchResourcesOwnershipContract
{
    public static WorkbenchWorkspaceOwnershipKind Classify(
        WorkbenchWorkspaceConcern concern)
    {
        return concern switch
        {
            // Resources owns inventory inspection and presentation.
            WorkbenchWorkspaceConcern.CanonicalResourceInventory or
            WorkbenchWorkspaceConcern.ResourceDetailSemantics or
            WorkbenchWorkspaceConcern.ResourceCapabilities
                => WorkbenchWorkspaceOwnershipKind.Owned,

            // These values belong to shared workbench context.
            WorkbenchWorkspaceConcern.PlatformState or
            WorkbenchWorkspaceConcern.InfrastructureGraph or
            WorkbenchWorkspaceConcern.ProviderAccountLocationContext or
            WorkbenchWorkspaceConcern.DiscoveryLifecycleState or
            WorkbenchWorkspaceConcern.DiscoveryTimestamp or
            WorkbenchWorkspaceConcern.DiscoveryWarnings or
            WorkbenchWorkspaceConcern.SelectedResource or
            WorkbenchWorkspaceConcern.SelectedResourceContext or
            WorkbenchWorkspaceConcern.ResourceSelectionEntry
                => WorkbenchWorkspaceOwnershipKind.SharedContext,

            // These responsibilities belong to other workspaces
            // or to provider and operations infrastructure.
            WorkbenchWorkspaceConcern.InfrastructureOverview or
            WorkbenchWorkspaceConcern.DiscoveryInteraction or
            WorkbenchWorkspaceConcern.DiscoveryLifecyclePresentation or
            WorkbenchWorkspaceConcern.ScopePresentation or
            WorkbenchWorkspaceConcern.ResourceAndDomainCounts or
            WorkbenchWorkspaceConcern.DomainSummaryPresentation or
            WorkbenchWorkspaceConcern.DashboardResourceFiltering or
            WorkbenchWorkspaceConcern.HighLevelWarningAlertSummary or
            WorkbenchWorkspaceConcern.TopologyDependencyExploration or
            WorkbenchWorkspaceConcern.OperationsLifecycle or
            WorkbenchWorkspaceConcern.HistoryAuditEvidence or
            WorkbenchWorkspaceConcern.ReportDefinitionsAndRuns or
            WorkbenchWorkspaceConcern.SettingsAdministrationGovernance or
            WorkbenchWorkspaceConcern.ProviderCapabilityAdvertisement
                => WorkbenchWorkspaceOwnershipKind.NotOwned,

            _ => throw new ArgumentOutOfRangeException(
                nameof(concern),
                concern,
                "Unknown workspace concern.")
        };
    }
}
