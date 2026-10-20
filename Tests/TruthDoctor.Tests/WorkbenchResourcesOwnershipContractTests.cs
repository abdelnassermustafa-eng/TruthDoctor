using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchResourcesOwnershipContractTests
{
    [Theory]
    [InlineData(WorkbenchWorkspaceConcern.CanonicalResourceInventory)]
    [InlineData(WorkbenchWorkspaceConcern.ResourceDetailSemantics)]
    [InlineData(WorkbenchWorkspaceConcern.ResourceCapabilities)]
    public void ResourceInspectionResponsibilitiesAreOwned(
        WorkbenchWorkspaceConcern concern)
    {
        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.Owned,
            WorkbenchResourcesOwnershipContract.Classify(concern));
    }

    [Theory]
    [InlineData(WorkbenchWorkspaceConcern.PlatformState)]
    [InlineData(WorkbenchWorkspaceConcern.InfrastructureGraph)]
    [InlineData(WorkbenchWorkspaceConcern.ProviderAccountLocationContext)]
    [InlineData(WorkbenchWorkspaceConcern.DiscoveryLifecycleState)]
    [InlineData(WorkbenchWorkspaceConcern.DiscoveryTimestamp)]
    [InlineData(WorkbenchWorkspaceConcern.DiscoveryWarnings)]
    [InlineData(WorkbenchWorkspaceConcern.SelectedResource)]
    [InlineData(WorkbenchWorkspaceConcern.SelectedResourceContext)]
    [InlineData(WorkbenchWorkspaceConcern.ResourceSelectionEntry)]
    public void WorkbenchContextRemainsShared(
        WorkbenchWorkspaceConcern concern)
    {
        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.SharedContext,
            WorkbenchResourcesOwnershipContract.Classify(concern));
    }

    [Theory]
    [InlineData(WorkbenchWorkspaceConcern.InfrastructureOverview)]
    [InlineData(WorkbenchWorkspaceConcern.DiscoveryInteraction)]
    [InlineData(WorkbenchWorkspaceConcern.DiscoveryLifecyclePresentation)]
    [InlineData(WorkbenchWorkspaceConcern.ScopePresentation)]
    [InlineData(WorkbenchWorkspaceConcern.ResourceAndDomainCounts)]
    [InlineData(WorkbenchWorkspaceConcern.DomainSummaryPresentation)]
    [InlineData(WorkbenchWorkspaceConcern.DashboardResourceFiltering)]
    [InlineData(WorkbenchWorkspaceConcern.HighLevelWarningAlertSummary)]
    [InlineData(WorkbenchWorkspaceConcern.TopologyDependencyExploration)]
    [InlineData(WorkbenchWorkspaceConcern.OperationsLifecycle)]
    [InlineData(WorkbenchWorkspaceConcern.HistoryAuditEvidence)]
    [InlineData(WorkbenchWorkspaceConcern.ReportDefinitionsAndRuns)]
    [InlineData(WorkbenchWorkspaceConcern.SettingsAdministrationGovernance)]
    [InlineData(WorkbenchWorkspaceConcern.ProviderCapabilityAdvertisement)]
    public void OtherWorkspaceResponsibilitiesAreNotOwned(
        WorkbenchWorkspaceConcern concern)
    {
        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.NotOwned,
            WorkbenchResourcesOwnershipContract.Classify(concern));
    }

    [Fact]
    public void EveryDefinedConcernHasAnExplicitClassification()
    {
        foreach (var concern in
                 Enum.GetValues<WorkbenchWorkspaceConcern>())
        {
            var classification =
                WorkbenchResourcesOwnershipContract.Classify(concern);

            Assert.True(
                Enum.IsDefined(classification),
                $"Concern {concern} has no valid ownership classification.");
        }
    }

    [Fact]
    public void UndefinedConcernThrowsRatherThanDefaulting()
    {
        var undefined =
            (WorkbenchWorkspaceConcern)999;

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                WorkbenchResourcesOwnershipContract.Classify(undefined));
    }
}
