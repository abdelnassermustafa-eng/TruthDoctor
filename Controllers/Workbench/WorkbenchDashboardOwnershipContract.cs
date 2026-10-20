using System;

namespace TruthDoctor.Controllers.Workbench;

public enum WorkbenchWorkspaceOwnershipKind
{
    Owned,
    SharedContext,
    NotOwned
}

public enum WorkbenchWorkspaceConcern
{
    InfrastructureOverview,
    DiscoveryInteraction,
    DiscoveryLifecyclePresentation,
    ScopePresentation,
    ResourceAndDomainCounts,
    DomainSummaryPresentation,
    DashboardResourceFiltering,
    HighLevelWarningAlertSummary,
    ResourceSelectionEntry,

    PlatformState,
    InfrastructureGraph,
    ProviderAccountLocationContext,
    DiscoveryLifecycleState,
    DiscoveryTimestamp,
    DiscoveryWarnings,
    SelectedResource,
    SelectedResourceContext,

    CanonicalResourceInventory,
    ResourceDetailSemantics,
    ResourceCapabilities,
    TopologyDependencyExploration,
    OperationsLifecycle,
    HistoryAuditEvidence,
    ReportDefinitionsAndRuns,
    SettingsAdministrationGovernance,
    ProviderCapabilityAdvertisement
}

public static class WorkbenchDashboardOwnershipContract
{
    public static WorkbenchWorkspaceOwnershipKind Classify(
        WorkbenchWorkspaceConcern concern)
    {
        return concern switch
        {
            WorkbenchWorkspaceConcern.InfrastructureOverview or
            WorkbenchWorkspaceConcern.DiscoveryInteraction or
            WorkbenchWorkspaceConcern.DiscoveryLifecyclePresentation or
            WorkbenchWorkspaceConcern.ScopePresentation or
            WorkbenchWorkspaceConcern.ResourceAndDomainCounts or
            WorkbenchWorkspaceConcern.DomainSummaryPresentation or
            WorkbenchWorkspaceConcern.DashboardResourceFiltering or
            WorkbenchWorkspaceConcern.HighLevelWarningAlertSummary or
            WorkbenchWorkspaceConcern.ResourceSelectionEntry
                => WorkbenchWorkspaceOwnershipKind.Owned,

            WorkbenchWorkspaceConcern.PlatformState or
            WorkbenchWorkspaceConcern.InfrastructureGraph or
            WorkbenchWorkspaceConcern.ProviderAccountLocationContext or
            WorkbenchWorkspaceConcern.DiscoveryLifecycleState or
            WorkbenchWorkspaceConcern.DiscoveryTimestamp or
            WorkbenchWorkspaceConcern.DiscoveryWarnings or
            WorkbenchWorkspaceConcern.SelectedResource or
            WorkbenchWorkspaceConcern.SelectedResourceContext
                => WorkbenchWorkspaceOwnershipKind.SharedContext,

            WorkbenchWorkspaceConcern.CanonicalResourceInventory or
            WorkbenchWorkspaceConcern.ResourceDetailSemantics or
            WorkbenchWorkspaceConcern.ResourceCapabilities or
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
