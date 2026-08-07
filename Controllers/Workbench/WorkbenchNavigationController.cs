using System;
using System.Linq;
using Avalonia.Controls;
using TruthDoctor.State;

namespace TruthDoctor.Controllers.Workbench;

public sealed class WorkbenchNavigationController
{
    private readonly WorkbenchState _state;

    public WorkbenchNavigationController(
        WorkbenchState state)
    {
        _state = state;
    }

    public void ShowWorkspace(
        string view,
        ScrollViewer dashboardWorkspace,
        Border secondaryWorkspace,
        TextBlock title,
        TextBlock description,
        ListBox list)
    {
        _state.CurrentView = view;

        if (view.Equals(
            "dashboard",
            StringComparison.OrdinalIgnoreCase))
        {
            dashboardWorkspace.IsVisible = true;
            secondaryWorkspace.IsVisible = false;
            return;
        }

        dashboardWorkspace.IsVisible = false;
        secondaryWorkspace.IsVisible = true;

        list.Items.Clear();

        switch (view.ToLowerInvariant())
        {
            case "resources":

                title.Text = "Resources";

                description.Text =
                    "All resources in the current infrastructure state.";

                if (_state.PlatformState is not null)
                {
                    foreach (var resource in
                        _state.PlatformState.Resources
                            .OrderBy(x => x.DomainId)
                            .ThenBy(x => x.ResourceType)
                            .ThenBy(x => x.DisplayName))
                    {
                        list.Items.Add(
                            $"{resource.DomainId} · " +
                            $"{resource.ResourceType} · " +
                            $"{resource.DisplayName} · " +
                            $"{resource.State} · " +
                            $"{resource.Location}");
                    }
                }

                break;

            case "operations":

                title.Text = "Operations";

                description.Text =
                    "Infrastructure operations.";

                list.Items.Add(
                    "No live operations.");

                break;

            case "history":

                title.Text = "History";

                description.Text =
                    "Discovery history.";

                if (_state.PlatformState != null)
                {
                    list.Items.Add(
                        $"Last discovery: {_state.PlatformState.DiscoveredAt.LocalDateTime:g}");
                }

                break;

            case "reports":

                title.Text = "Reports";

                description.Text =
                    "Infrastructure reports.";

                if (_state.PlatformState != null)
                {
                    list.Items.Add(
                        $"Resources: {_state.PlatformState.TotalResourceCount}");

                    list.Items.Add(
                        $"Domains: {_state.PlatformState.Domains.Count}");
                }

                break;

            case "settings":

                title.Text = "Settings";

                description.Text =
                    "Workbench settings.";

                break;

            default:

                title.Text = view;

                description.Text =
                    "Workspace ready.";

                break;
        }
    }
}
