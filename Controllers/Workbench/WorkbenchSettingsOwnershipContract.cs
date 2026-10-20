using System;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Defines the Settings workspace's responsibility boundaries.
/// Ownership establishes responsibility for administration
/// and governance, not proof that enforcement is implemented.
/// </summary>
public static class WorkbenchSettingsOwnershipContract
{
    public static WorkbenchWorkspaceOwnershipKind Classify(
        WorkbenchWorkspaceConcern concern)
    {
        return concern switch
        {
            // Settings owns administration and governance concerns.
            WorkbenchWorkspaceConcern.SettingsAdministrationGovernance
                => WorkbenchWorkspaceOwnershipKind.Owned,

            // Shared platform, graph, discovery, and selection context.
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

            // Responsibilities assigned to other workspaces
            // or provider infrastructure.
            WorkbenchWorkspaceConcern.InfrastructureOverview or
            WorkbenchWorkspaceConcern.DiscoveryInteraction or
            WorkbenchWorkspaceConcern.DiscoveryLifecyclePresentation or
            WorkbenchWorkspaceConcern.ScopePresentation or
            WorkbenchWorkspaceConcern.ResourceAndDomainCounts or
            WorkbenchWorkspaceConcern.DomainSummaryPresentation or
            WorkbenchWorkspaceConcern.DashboardResourceFiltering or
            WorkbenchWorkspaceConcern.HighLevelWarningAlertSummary or
            WorkbenchWorkspaceConcern.CanonicalResourceInventory or
            WorkbenchWorkspaceConcern.ResourceDetailSemantics or
            WorkbenchWorkspaceConcern.ResourceCapabilities or
            WorkbenchWorkspaceConcern.TopologyDependencyExploration or
            WorkbenchWorkspaceConcern.OperationsLifecycle or
            WorkbenchWorkspaceConcern.HistoryAuditEvidence or
            WorkbenchWorkspaceConcern.ReportDefinitionsAndRuns or
            WorkbenchWorkspaceConcern.ProviderCapabilityAdvertisement
                => WorkbenchWorkspaceOwnershipKind.NotOwned,

            _ => throw new ArgumentOutOfRangeException(
                nameof(concern),
                concern,
                "Unknown workspace concern.")
        };
    }
}
