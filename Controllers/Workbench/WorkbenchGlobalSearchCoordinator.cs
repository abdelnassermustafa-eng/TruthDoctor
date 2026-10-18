using System;

namespace TruthDoctor.Controllers.Workbench;

public enum WorkbenchGlobalSearchRoute
{
    Unknown,
    Dashboard,
    Resources,
    Topology,
    Operations,
    History,
    Reports,
    Settings
}

public static class WorkbenchGlobalSearchCoordinator
{
    public static WorkbenchGlobalSearchRoute Resolve(
        string? currentView)
    {
        if (string.IsNullOrWhiteSpace(currentView))
        {
            return WorkbenchGlobalSearchRoute.Unknown;
        }

        var normalized =
            currentView.Trim();

        if (normalized.Equals(
            "dashboard",
            StringComparison.OrdinalIgnoreCase))
        {
            return WorkbenchGlobalSearchRoute.Dashboard;
        }

        if (normalized.Equals(
            "resources",
            StringComparison.OrdinalIgnoreCase))
        {
            return WorkbenchGlobalSearchRoute.Resources;
        }

        if (normalized.Equals(
            "topology",
            StringComparison.OrdinalIgnoreCase))
        {
            return WorkbenchGlobalSearchRoute.Topology;
        }

        if (normalized.Equals(
            "operations",
            StringComparison.OrdinalIgnoreCase))
        {
            return WorkbenchGlobalSearchRoute.Operations;
        }

        if (normalized.Equals(
            "history",
            StringComparison.OrdinalIgnoreCase))
        {
            return WorkbenchGlobalSearchRoute.History;
        }

        if (normalized.Equals(
            "reports",
            StringComparison.OrdinalIgnoreCase))
        {
            return WorkbenchGlobalSearchRoute.Reports;
        }

        if (normalized.Equals(
            "settings",
            StringComparison.OrdinalIgnoreCase))
        {
            return WorkbenchGlobalSearchRoute.Settings;
        }

        return WorkbenchGlobalSearchRoute.Unknown;
    }
}
