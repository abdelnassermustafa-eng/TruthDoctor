using System;

namespace TruthDoctor.Controllers.Workbench;

/// <summary>
/// Defines the Operations workspace's responsibility boundaries.
/// Ownership identifies architectural responsibility, not proof
/// that operation execution or lifecycle services are implemented.
/// </summary>
public static class WorkbenchOperationsOwnershipContract
{
    public static WorkbenchWorkspaceOwnershipKind Classify(
        WorkbenchWorkspaceConcern concern)
    {
        return concern switch
        {
            // Operations owns the operational lifecycle contract.
            WorkbenchWorkspaceConcern.OperationsLifecycle
                => WorkbenchWorkspaceOwnershipKind.Owned,

            // Shared infrastructure and selection context remain
            // owned by the common workbench state.
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

            // Other workspaces and providers retain these concerns.
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
