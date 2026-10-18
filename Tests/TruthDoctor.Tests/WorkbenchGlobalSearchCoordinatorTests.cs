using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchGlobalSearchCoordinatorTests
{
    [Theory]
    [InlineData("dashboard", WorkbenchGlobalSearchRoute.Dashboard)]
    [InlineData("resources", WorkbenchGlobalSearchRoute.Resources)]
    [InlineData("topology", WorkbenchGlobalSearchRoute.Topology)]
    [InlineData("operations", WorkbenchGlobalSearchRoute.Operations)]
    [InlineData("history", WorkbenchGlobalSearchRoute.History)]
    [InlineData("reports", WorkbenchGlobalSearchRoute.Reports)]
    [InlineData("settings", WorkbenchGlobalSearchRoute.Settings)]
    public void Resolve_returns_expected_route_for_known_workspace(
        string currentView,
        WorkbenchGlobalSearchRoute expected)
    {
        var actual =
            WorkbenchGlobalSearchCoordinator.Resolve(
                currentView);

        Assert.Equal(
            expected,
            actual);
    }

    [Theory]
    [InlineData("DASHBOARD", WorkbenchGlobalSearchRoute.Dashboard)]
    [InlineData("Resources", WorkbenchGlobalSearchRoute.Resources)]
    [InlineData("ToPoLoGy", WorkbenchGlobalSearchRoute.Topology)]
    [InlineData("OPERATIONS", WorkbenchGlobalSearchRoute.Operations)]
    [InlineData("History", WorkbenchGlobalSearchRoute.History)]
    [InlineData("REPORTS", WorkbenchGlobalSearchRoute.Reports)]
    [InlineData("Settings", WorkbenchGlobalSearchRoute.Settings)]
    public void Resolve_is_case_insensitive(
        string currentView,
        WorkbenchGlobalSearchRoute expected)
    {
        var actual =
            WorkbenchGlobalSearchCoordinator.Resolve(
                currentView);

        Assert.Equal(
            expected,
            actual);
    }

    [Theory]
    [InlineData(" dashboard ", WorkbenchGlobalSearchRoute.Dashboard)]
    [InlineData(" resources ", WorkbenchGlobalSearchRoute.Resources)]
    [InlineData(" topology ", WorkbenchGlobalSearchRoute.Topology)]
    [InlineData(" operations ", WorkbenchGlobalSearchRoute.Operations)]
    [InlineData(" history ", WorkbenchGlobalSearchRoute.History)]
    [InlineData(" reports ", WorkbenchGlobalSearchRoute.Reports)]
    [InlineData(" settings ", WorkbenchGlobalSearchRoute.Settings)]
    public void Resolve_trims_workspace_name(
        string currentView,
        WorkbenchGlobalSearchRoute expected)
    {
        var actual =
            WorkbenchGlobalSearchCoordinator.Resolve(
                currentView);

        Assert.Equal(
            expected,
            actual);
    }

    [Theory]
    [InlineData("operations")]
    [InlineData("history")]
    [InlineData("reports")]
    [InlineData("settings")]
    [InlineData("future-workspace")]
    public void Resolve_never_routes_non_dashboard_workspace_to_dashboard(
        string currentView)
    {
        var actual =
            WorkbenchGlobalSearchCoordinator.Resolve(
                currentView);

        Assert.NotEqual(
            WorkbenchGlobalSearchRoute.Dashboard,
            actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData("future-workspace")]
    public void Resolve_returns_unknown_for_missing_or_unrecognized_workspace(
        string? currentView)
    {
        var actual =
            WorkbenchGlobalSearchCoordinator.Resolve(
                currentView);

        Assert.Equal(
            WorkbenchGlobalSearchRoute.Unknown,
            actual);
    }
}
