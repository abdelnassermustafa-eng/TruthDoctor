using System;
using System.Collections.Generic;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchSearchVerificationMatrixTests
{
    private static readonly
        (string Workspace, WorkbenchGlobalSearchRoute Route)[]
        VerificationMatrix =
        {
            (
                "dashboard",
                WorkbenchGlobalSearchRoute.Dashboard
            ),
            (
                "resources",
                WorkbenchGlobalSearchRoute.Resources
            ),
            (
                "topology",
                WorkbenchGlobalSearchRoute.Topology
            ),
            (
                "operations",
                WorkbenchGlobalSearchRoute.Operations
            ),
            (
                "history",
                WorkbenchGlobalSearchRoute.History
            ),
            (
                "reports",
                WorkbenchGlobalSearchRoute.Reports
            ),
            (
                "settings",
                WorkbenchGlobalSearchRoute.Settings
            )
        };

    [Theory]
    [InlineData(
        "dashboard",
        WorkbenchGlobalSearchRoute.Dashboard)]
    [InlineData(
        "resources",
        WorkbenchGlobalSearchRoute.Resources)]
    [InlineData(
        "topology",
        WorkbenchGlobalSearchRoute.Topology)]
    [InlineData(
        "operations",
        WorkbenchGlobalSearchRoute.Operations)]
    [InlineData(
        "history",
        WorkbenchGlobalSearchRoute.History)]
    [InlineData(
        "reports",
        WorkbenchGlobalSearchRoute.Reports)]
    [InlineData(
        "settings",
        WorkbenchGlobalSearchRoute.Settings)]
    public void EveryPrimaryWorkspaceResolvesToItsOwnedSearchRoute(
        string workspace,
        WorkbenchGlobalSearchRoute expectedRoute)
    {
        var actualRoute =
            WorkbenchGlobalSearchCoordinator.Resolve(
                workspace);

        Assert.Equal(
            expectedRoute,
            actualRoute);
    }

    [Fact]
    public void VerificationMatrixCoversEveryRecognizedRouteExactlyOnce()
    {
        var routes =
            new HashSet<WorkbenchGlobalSearchRoute>();

        foreach (var entry in VerificationMatrix)
        {
            Assert.NotEqual(
                WorkbenchGlobalSearchRoute.Unknown,
                entry.Route);

            Assert.True(
                routes.Add(
                    entry.Route),
                $"Duplicate verification route: {entry.Route}");
        }

        foreach (var route in
                 Enum.GetValues<WorkbenchGlobalSearchRoute>())
        {
            if (route ==
                WorkbenchGlobalSearchRoute.Unknown)
            {
                continue;
            }

            Assert.Contains(
                route,
                routes);
        }

        Assert.Equal(
            Enum.GetValues<WorkbenchGlobalSearchRoute>().Length - 1,
            routes.Count);
    }

    [Fact]
    public void VerificationMatrixWorkspaceNamesAreUnique()
    {
        var workspaces =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var entry in VerificationMatrix)
        {
            Assert.True(
                workspaces.Add(
                    entry.Workspace),
                $"Duplicate verification workspace: {entry.Workspace}");
        }

        Assert.Equal(
            VerificationMatrix.Length,
            workspaces.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData("future-workspace")]
    public void MissingOrUnsupportedWorkspaceRemainsUnknown(
        string? workspace)
    {
        var actualRoute =
            WorkbenchGlobalSearchCoordinator.Resolve(
                workspace);

        Assert.Equal(
            WorkbenchGlobalSearchRoute.Unknown,
            actualRoute);
    }

    [Theory]
    [InlineData("operations")]
    [InlineData("history")]
    [InlineData("reports")]
    [InlineData("settings")]
    [InlineData("future-workspace")]
    public void NonDashboardWorkspaceNeverFallsBackToDashboard(
        string workspace)
    {
        var actualRoute =
            WorkbenchGlobalSearchCoordinator.Resolve(
                workspace);

        Assert.NotEqual(
            WorkbenchGlobalSearchRoute.Dashboard,
            actualRoute);
    }

    [Fact]
    public void
        CanonicalResourceSearchSupportsDashboardAndResourcesRepresentativeQueries()
    {
        var resource =
            CreateVerificationResource(
                "resource-1",
                "payments-api");

        Assert.True(
            InfrastructureResourceSearch.Matches(
                resource,
                "  PAYMENTS-API  "));

        Assert.False(
            InfrastructureResourceSearch.Matches(
                resource,
                "does-not-exist"));

        Assert.True(
            InfrastructureResourceSearch.Matches(
                resource,
                "   "));
    }

    [Fact]
    public void
        TopologyResolverSupportsRepresentativeUniqueAndNoMatchQueries()
    {
        var expected =
            CreateVerificationResource(
                "resource-1",
                "CloudWatch-Metrics-Lab");

        var unrelated =
            CreateVerificationResource(
                "resource-2",
                "lab-key");

        var resources =
            new[]
            {
                expected,
                unrelated
            };

        var resolved =
            InfrastructureResourceSearchResolver
                .ResolveUnique(
                    resources,
                    "  CLOUDWATCH-METRICS-LAB  ");

        Assert.Same(
            expected,
            resolved);

        Assert.Null(
            InfrastructureResourceSearchResolver
                .ResolveUnique(
                    resources,
                    "does-not-exist"));

        Assert.Null(
            InfrastructureResourceSearchResolver
                .ResolveUnique(
                    resources,
                    "   "));
    }

    [Fact]
    public void
        OperationsSearchSupportsRepresentativeResourceCapabilityAndNoMatchQueries()
    {
        var resource =
            CreateVerificationResource(
                "resource-1",
                "payments-api");

        var resourceMatch =
            WorkbenchOperationsSearch.Match(
                resource,
                "  PAYMENTS-API  ");

        Assert.NotNull(
            resourceMatch);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Resource,
            resourceMatch.Kind);

        var capabilityMatch =
            WorkbenchOperationsSearch.Match(
                resource,
                "  RESTART  ");

        Assert.NotNull(
            capabilityMatch);

        Assert.Equal(
            WorkbenchOperationsSearchMatchKind.Capability,
            capabilityMatch.Kind);

        Assert.Equal(
            "Restart",
            capabilityMatch.DisplayText);

        Assert.Null(
            WorkbenchOperationsSearch.Match(
                resource,
                "does-not-exist"));
    }

    [Fact]
    public void
        HistorySearchContractSupportsRepresentativePositiveBlankAndNegativeQueries()
    {
        var document =
            new WorkbenchHistorySearchDocument
            {
                Kind =
                    WorkbenchHistorySearchDocumentKind.History,

                DisplayText =
                    "Production restart history",

                Actor =
                    "operator@example.com",

                Action =
                    "restart",

                Target =
                    "payments-api"
            };

        Assert.True(
            WorkbenchHistorySearch.Matches(
                document,
                "  RESTART  "));

        Assert.True(
            WorkbenchHistorySearch.Matches(
                document,
                "   "));

        Assert.False(
            WorkbenchHistorySearch.Matches(
                document,
                "does-not-exist"));
    }

    [Fact]
    public void
        ReportsSearchContractSupportsRepresentativePositiveBlankAndNegativeQueries()
    {
        var document =
            new WorkbenchReportsSearchDocument
            {
                Kind =
                    WorkbenchReportsSearchDocumentKind.ReportDefinition,

                DisplayText =
                    "Weekly infrastructure report",

                Report =
                    "weekly-infrastructure",

                Category =
                    "infrastructure",

                Resource =
                    "payments-api"
            };

        Assert.True(
            WorkbenchReportsSearch.Matches(
                document,
                "  WEEKLY-INFRASTRUCTURE  "));

        Assert.True(
            WorkbenchReportsSearch.Matches(
                document,
                "   "));

        Assert.False(
            WorkbenchReportsSearch.Matches(
                document,
                "does-not-exist"));
    }

    [Fact]
    public void
        SettingsSearchContractSupportsRepresentativePositiveBlankAndNegativeQueries()
    {
        var document =
            new WorkbenchSettingsSearchDocument
            {
                Kind =
                    WorkbenchSettingsSearchDocumentKind.Administration,

                DisplayText =
                    "Production administration settings",

                Provider =
                    "aws",

                CredentialReference =
                    "aws-production-readonly",

                Policy =
                    "production-protection"
            };

        Assert.True(
            WorkbenchSettingsSearch.Matches(
                document,
                "  AWS-PRODUCTION-READONLY  "));

        Assert.True(
            WorkbenchSettingsSearch.Matches(
                document,
                "   "));

        Assert.False(
            WorkbenchSettingsSearch.Matches(
                document,
                "does-not-exist"));
    }

    [Fact]
    public void RepresentativeCollectionSearchDoesNotMutateInputCollection()
    {
        var first =
            CreateVerificationResource(
                "resource-1",
                "payments-api");

        var second =
            CreateVerificationResource(
                "resource-2",
                "orders-api");

        var resources =
            new List<InfrastructureResource>
            {
                first,
                second
            };

        _ = WorkbenchOperationsSearch.Search(
            resources,
            "payments-api");

        Assert.Equal(
            2,
            resources.Count);

        Assert.Same(
            first,
            resources[0]);

        Assert.Same(
            second,
            resources[1]);

        Assert.Equal(
            "payments-api",
            resources[0].DisplayName);

        Assert.Equal(
            "orders-api",
            resources[1].DisplayName);
    }

    private static InfrastructureResource
        CreateVerificationResource(
            string resourceId,
            string displayName)
    {
        return new InfrastructureResource
        {
            ProviderId =
                "aws",

            AccountId =
                "123456789012",

            DomainId =
                "compute",

            ResourceType =
                "ec2-instance",

            ResourceId =
                resourceId,

            NativeId =
                resourceId,

            DisplayName =
                displayName,

            State =
                "running",

            Location =
                "us-east-1",

            Properties =
                new Dictionary<string, string>
                {
                    ["PrivateIpAddress"] =
                        "10.0.1.10"
                },

            Tags =
                new Dictionary<string, string>
                {
                    ["Environment"] =
                        "Production"
                },

            Capabilities =
            [
                "Start",
                "Stop",
                "Restart"
            ]
        };
    }

}
