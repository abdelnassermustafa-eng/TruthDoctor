using System;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Defines the Topology workspace's responsibility boundaries.
/// Ownership describes topology exploration and presentation,
/// not exclusive ownership of shared graph or resource selection state.
/// </summary>
public static class WorkbenchTopologyOwnershipContract
{
    public static WorkbenchWorkspaceOwnershipKind Classify(
        WorkbenchWorkspaceConcern concern)
    {
        return concern switch
        {
            // Topology owns relationship exploration and its presentation.
            WorkbenchWorkspaceConcern.TopologyDependencyExploration
                => WorkbenchWorkspaceOwnershipKind.Owned,

            // Graph, discovery, and selection context remain shared.
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
            WorkbenchWorkspaceConcern.CanonicalResourceInventory or
            WorkbenchWorkspaceConcern.ResourceDetailSemantics or
            WorkbenchWorkspaceConcern.ResourceCapabilities or
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
