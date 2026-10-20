using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchDashboardOwnershipContractTests
{
    [Theory]
    [InlineData(
        WorkbenchWorkspaceConcern.InfrastructureOverview)]
    [InlineData(
        WorkbenchWorkspaceConcern.DiscoveryInteraction)]
    [InlineData(
        WorkbenchWorkspaceConcern.DiscoveryLifecyclePresentation)]
    [InlineData(
        WorkbenchWorkspaceConcern.ScopePresentation)]
    [InlineData(
        WorkbenchWorkspaceConcern.ResourceAndDomainCounts)]
    [InlineData(
        WorkbenchWorkspaceConcern.DomainSummaryPresentation)]
    [InlineData(
        WorkbenchWorkspaceConcern.DashboardResourceFiltering)]
    [InlineData(
        WorkbenchWorkspaceConcern.HighLevelWarningAlertSummary)]
    [InlineData(
        WorkbenchWorkspaceConcern.ResourceSelectionEntry)]
    public void MissionControlResponsibilitiesAreOwned(
        WorkbenchWorkspaceConcern concern)
    {
        var actual =
            WorkbenchDashboardOwnershipContract.Classify(
                concern);

        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.Owned,
            actual);
    }

    [Theory]
    [InlineData(
        WorkbenchWorkspaceConcern.PlatformState)]
    [InlineData(
        WorkbenchWorkspaceConcern.InfrastructureGraph)]
    [InlineData(
        WorkbenchWorkspaceConcern.ProviderAccountLocationContext)]
    [InlineData(
        WorkbenchWorkspaceConcern.DiscoveryLifecycleState)]
    [InlineData(
        WorkbenchWorkspaceConcern.DiscoveryTimestamp)]
    [InlineData(
        WorkbenchWorkspaceConcern.DiscoveryWarnings)]
    [InlineData(
        WorkbenchWorkspaceConcern.SelectedResource)]
    [InlineData(
        WorkbenchWorkspaceConcern.SelectedResourceContext)]
    public void SharedWorkbenchContextRemainsShared(
        WorkbenchWorkspaceConcern concern)
    {
        var actual =
            WorkbenchDashboardOwnershipContract.Classify(
                concern);

        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.SharedContext,
            actual);
    }

    [Theory]
    [InlineData(
        WorkbenchWorkspaceConcern.CanonicalResourceInventory)]
    [InlineData(
        WorkbenchWorkspaceConcern.ResourceDetailSemantics)]
    [InlineData(
        WorkbenchWorkspaceConcern.ResourceCapabilities)]
    [InlineData(
        WorkbenchWorkspaceConcern.TopologyDependencyExploration)]
    [InlineData(
        WorkbenchWorkspaceConcern.OperationsLifecycle)]
    [InlineData(
        WorkbenchWorkspaceConcern.HistoryAuditEvidence)]
    [InlineData(
        WorkbenchWorkspaceConcern.ReportDefinitionsAndRuns)]
    [InlineData(
        WorkbenchWorkspaceConcern.SettingsAdministrationGovernance)]
    [InlineData(
        WorkbenchWorkspaceConcern.ProviderCapabilityAdvertisement)]
    public void DownstreamResponsibilitiesAreNotOwned(
        WorkbenchWorkspaceConcern concern)
    {
        var actual =
            WorkbenchDashboardOwnershipContract.Classify(
                concern);

        Assert.Equal(
            WorkbenchWorkspaceOwnershipKind.NotOwned,
            actual);
    }

    [Fact]
    public void EveryDefinedConcernHasAnExplicitClassification()
    {
        foreach (var concern in
                 Enum.GetValues<WorkbenchWorkspaceConcern>())
        {
            var classification =
                WorkbenchDashboardOwnershipContract.Classify(
                    concern);

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
                WorkbenchDashboardOwnershipContract.Classify(
                    undefined));
    }
}
