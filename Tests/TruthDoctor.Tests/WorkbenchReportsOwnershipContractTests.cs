using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchReportsOwnershipContractTests
{
    [Theory]
    [InlineData(WorkbenchWorkspaceConcern.ReportDefinitionsAndRuns)]
    public void ReportDefinitionsAndRunsResponsibilityIsOwned(
        WorkbenchWorkspaceConcern concern)
    {
        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.Owned,
            WorkbenchReportsOwnershipContract.Classify(concern));
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
    public void SharedWorkbenchContextRemainsShared(
        WorkbenchWorkspaceConcern concern)
    {
        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.SharedContext,
            WorkbenchReportsOwnershipContract.Classify(concern));
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
    [InlineData(WorkbenchWorkspaceConcern.CanonicalResourceInventory)]
    [InlineData(WorkbenchWorkspaceConcern.ResourceDetailSemantics)]
    [InlineData(WorkbenchWorkspaceConcern.ResourceCapabilities)]
    [InlineData(WorkbenchWorkspaceConcern.TopologyDependencyExploration)]
    [InlineData(WorkbenchWorkspaceConcern.OperationsLifecycle)]
    [InlineData(WorkbenchWorkspaceConcern.HistoryAuditEvidence)]
    [InlineData(WorkbenchWorkspaceConcern.SettingsAdministrationGovernance)]
    [InlineData(WorkbenchWorkspaceConcern.ProviderCapabilityAdvertisement)]
    public void OtherWorkspaceResponsibilitiesAreNotOwned(
        WorkbenchWorkspaceConcern concern)
    {
        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.NotOwned,
            WorkbenchReportsOwnershipContract.Classify(concern));
    }

    [Fact]
    public void EveryDefinedConcernHasAnExplicitClassification()
    {
        foreach (var concern in
                 Enum.GetValues<WorkbenchWorkspaceConcern>())
        {
            var classification =
                WorkbenchReportsOwnershipContract.Classify(concern);

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
                WorkbenchReportsOwnershipContract.Classify(undefined));
    }
}
