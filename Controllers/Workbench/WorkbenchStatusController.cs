using Avalonia.Controls;
using Avalonia.Media;
using TruthDoctor.Controls.Workbench;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using TruthDoctor.Services.Providers;

namespace TruthDoctor.Controllers.Workbench;

public sealed class WorkbenchStatusController
{
    private readonly WorkbenchState _state;
    private readonly ProviderRegistry _providers;

    public WorkbenchStatusController(
        WorkbenchState state,
        ProviderRegistry providers)
    {
        _state = state;
        _providers = providers;
    }

    public void ApplyLoading(
        Button discoveryButton,
        TextBlock discoveryStatus,
        TextBlock statusDiscovery,
        TextBlock statusApi)
    {
        discoveryButton.IsEnabled = false;
        discoveryButton.Content = "Discovering...";

        discoveryStatus.Text = "● Discovering";
        discoveryStatus.Foreground = Brush("#60A5FA");

        statusDiscovery.Text = "Discovery: Running";
        statusApi.Text = "API: Connecting";
        statusApi.Foreground = Brush("#92400E");
    }

    public void ApplyLoaded(
        PlatformState platformState,
        WorkbenchTopBar topBar,
        Button discoveryButton,
        TextBlock discoveryStatus,
        TextBlock sidebarProvider,
        TextBlock sidebarRefresh,
        TextBlock lastDiscovery,
        TextBlock statusProvider,
        TextBlock statusAccount,
        TextBlock statusLocation,
        TextBlock statusDiscovery,
        TextBlock statusApi,
        TextBlock assistantStatus)
    {
        discoveryButton.IsEnabled = true;
        discoveryButton.Content = "▶  Run Discovery";

        var provider =
            _providers.Resolve(
                platformState.Context.ProviderId);

        topBar.SetContext(
            provider.ProviderVisual,
            platformState.Context.AccountName,
            platformState.Context.AccountId,
            platformState.Context.Locations,
            _state.SelectedLocation,
            platformState.Context.IdentityArn);

        discoveryStatus.Text =
            platformState.Warnings.Count == 0
                ? "● Discovery completed"
                : $"● Completed with {platformState.Warnings.Count} warning(s)";

        discoveryStatus.Foreground =
            platformState.Warnings.Count == 0
                ? Brush("#4ADE80")
                : Brush("#FBBF24");

        sidebarProvider.Text =
            $"{ProviderVisualFactory.ResolveIcon(provider.ProviderVisual.IconKey)}  " +
            $"Provider: {provider.ProviderVisual.DisplayName}";

        sidebarProvider.Foreground =
            Brush(
                ProviderVisualFactory.ResolveForeground(
                    provider.ProviderVisual.AccentKey));

        sidebarRefresh.Text =
            $"Last refresh: {platformState.DiscoveredAt.LocalDateTime:t}";

        lastDiscovery.Text =
            $"Last discovery: {platformState.DiscoveredAt.LocalDateTime:g}";

        statusProvider.Text =
            $"Provider: {provider.ProviderVisual.DisplayName}";

        statusProvider.Foreground =
            Brush(
                ProviderVisualFactory.ResolveBorder(
                    provider.ProviderVisual.AccentKey));

        statusAccount.Text =
            $"Account: {DisplayAccount(platformState)}";

        statusLocation.Text =
            $"Location: {_state.SelectedLocation}";

        statusDiscovery.Text =
            $"Discovery: {platformState.TotalResourceCount} resources";

        statusApi.Text = "API: Online";
        statusApi.Foreground = Brush("#15803D");

        assistantStatus.Text =
            $"Infrastructure state loaded: " +
            $"{platformState.TotalResourceCount} resources across " +
            $"{platformState.Domains.Count} domains.";
    }

    public void ApplyFailure(
        string message,
        WorkbenchTopBar topBar,
        Button discoveryButton,
        TextBlock discoveryStatus,
        TextBlock statusDiscovery,
        TextBlock statusApi,
        TextBlock assistantStatus)
    {
        discoveryButton.IsEnabled = true;
        discoveryButton.Content = "▶  Run Discovery";

        discoveryStatus.Text = "● Discovery failed";
        discoveryStatus.Foreground = Brush("#F87171");

        statusDiscovery.Text = "Discovery: Failed";

        statusApi.Text = "API: Offline";
        statusApi.Foreground = Brush("#B91C1C");

        assistantStatus.Text = message;

        topBar.SetDisconnected();
    }

    public void ApplyLocation(
        string location,
        TextBlock statusLocation)
    {
        _state.SelectedLocation = location;
        statusLocation.Text = $"Location: {location}";
    }

    private static string DisplayAccount(
        PlatformState platformState)
    {
        return string.IsNullOrWhiteSpace(
            platformState.Context.AccountName)
            ? platformState.Context.AccountId
            : platformState.Context.AccountName;
    }

    private static IBrush Brush(string value)
    {
        return new SolidColorBrush(
            Color.Parse(value));
    }
}
