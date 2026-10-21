using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchWorkspaceVerificationMatrixTests
{
    private static readonly (string Workspace,
        WorkbenchGlobalSearchRoute Route,
        WorkbenchWorkspaceConcern PrimaryConcern)[]
        Expected =
        {
            ("dashboard", WorkbenchGlobalSearchRoute.Dashboard,
                WorkbenchWorkspaceConcern.InfrastructureOverview),
            ("resources", WorkbenchGlobalSearchRoute.Resources,
                WorkbenchWorkspaceConcern.CanonicalResourceInventory),
            ("topology", WorkbenchGlobalSearchRoute.Topology,
                WorkbenchWorkspaceConcern.TopologyDependencyExploration),
            ("operations", WorkbenchGlobalSearchRoute.Operations,
                WorkbenchWorkspaceConcern.OperationsLifecycle),
            ("history", WorkbenchGlobalSearchRoute.History,
                WorkbenchWorkspaceConcern.HistoryAuditEvidence),
            ("reports", WorkbenchGlobalSearchRoute.Reports,
                WorkbenchWorkspaceConcern.ReportDefinitionsAndRuns),
            ("settings", WorkbenchGlobalSearchRoute.Settings,
                WorkbenchWorkspaceConcern.SettingsAdministrationGovernance)
        };

    [Fact]
    public void MatrixCoversAllSevenWorkspacesExactlyOnce()
    {
        var actual = WorkbenchWorkspaceVerificationMatrix.Workspaces;

        Assert.Equal(7, actual.Count);
        Assert.Equal(7, actual.Distinct(
            StringComparer.OrdinalIgnoreCase).Count());

        Assert.Equal(
            Expected.Select(entry => entry.Workspace),
            actual);
    }

    [Fact]
    public void EveryWorkspaceHasItsExpectedRouteAndPrimaryOwner()
    {
        foreach (var entry in Expected)
        {
            var result =
                WorkbenchWorkspaceVerificationMatrix.Evaluate(
                    entry.Workspace,
                    entry.PrimaryConcern,
                    false,
                    true,
                    null);

            Assert.Equal(entry.Workspace, result.Workspace);
            Assert.Equal(entry.Route, result.SearchRoute);
            Assert.Equal(
                WorkbenchWorkspaceOwnershipKind.Owned,
                result.Ownership);
            Assert.Equal(
                WorkbenchWorkspacePresentationState.Ready,
                result.PresentationState);
        }
    }

    [Fact]
    public void EveryDefinedConcernUsesTheExistingOwnershipContract()
    {
        foreach (var entry in Expected)
        {
            foreach (var concern in
                Enum.GetValues<WorkbenchWorkspaceConcern>())
            {
                var result =
                    WorkbenchWorkspaceVerificationMatrix.Evaluate(
                        entry.Workspace,
                        concern,
                        false,
                        true,
                        null);

                Assert.True(Enum.IsDefined(result.Ownership));
                Assert.Equal(
                    ExpectedOwnership(entry.Workspace, concern),
                    result.Ownership);
            }
        }
    }

    [Fact]
    public void SharedPlatformStateIsNeverClaimedAsOwned()
    {
        foreach (var entry in Expected)
        {
            var result =
                WorkbenchWorkspaceVerificationMatrix.Evaluate(
                    entry.Workspace,
                    WorkbenchWorkspaceConcern.PlatformState,
                    false,
                    true,
                    null);

            Assert.Equal(
                WorkbenchWorkspaceOwnershipKind.SharedContext,
                result.Ownership);
        }
    }

    [Fact]
    public void EveryWorkspaceSupportsAllPresentationStates()
    {
        foreach (var entry in Expected)
        {
            var cases = new[]
            {
                (false, true, (string?)null,
                    WorkbenchWorkspacePresentationState.Ready),
                (false, false, (string?)null,
                    WorkbenchWorkspacePresentationState.Empty),
                (true, false, (string?)null,
                    WorkbenchWorkspacePresentationState.Loading),
                (true, true, "Failure",
                    WorkbenchWorkspacePresentationState.Error)
            };

            foreach (var item in cases)
            {
                var result =
                    WorkbenchWorkspaceVerificationMatrix.Evaluate(
                        entry.Workspace,
                        entry.PrimaryConcern,
                        item.Item1,
                        item.Item2,
                        item.Item3);

                Assert.Equal(item.Item4, result.PresentationState);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    public void UnsupportedWorkspaceIsRejected(string? workspace)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            WorkbenchWorkspaceVerificationMatrix.Evaluate(
                workspace!,
                WorkbenchWorkspaceConcern.PlatformState,
                false,
                true,
                null));
    }

    [Fact]
    public void UndefinedConcernIsRejected()
    {
        foreach (var entry in Expected)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorkbenchWorkspaceVerificationMatrix.Evaluate(
                    entry.Workspace,
                    (WorkbenchWorkspaceConcern)999,
                    false,
                    true,
                    null));
        }
    }

    [Fact]
    public void WorkspaceIdentityIsCaseInsensitiveAndCanonical()
    {
        var result =
            WorkbenchWorkspaceVerificationMatrix.Evaluate(
                "TOPOLOGY",
                WorkbenchWorkspaceConcern.TopologyDependencyExploration,
                false,
                true,
                null);

        Assert.Equal("topology", result.Workspace);
        Assert.Equal(
            WorkbenchGlobalSearchRoute.Topology,
            result.SearchRoute);
    }

    private static WorkbenchWorkspaceOwnershipKind ExpectedOwnership(
        string workspace,
        WorkbenchWorkspaceConcern concern)
    {
        return workspace switch
        {
            "dashboard" =>
                WorkbenchDashboardOwnershipContract.Classify(concern),
            "resources" =>
                WorkbenchResourcesOwnershipContract.Classify(concern),
            "topology" =>
                WorkbenchTopologyOwnershipContract.Classify(concern),
            "operations" =>
                WorkbenchOperationsOwnershipContract.Classify(concern),
            "history" =>
                WorkbenchHistoryOwnershipContract.Classify(concern),
            "reports" =>
                WorkbenchReportsOwnershipContract.Classify(concern),
            "settings" =>
                WorkbenchSettingsOwnershipContract.Classify(concern),
            _ => throw new ArgumentOutOfRangeException(
                nameof(workspace))
        };
    }
}
