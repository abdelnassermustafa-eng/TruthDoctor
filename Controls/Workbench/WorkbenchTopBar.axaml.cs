using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using TruthDoctor.Services.Providers;

namespace TruthDoctor.Controls.Workbench;

public partial class WorkbenchTopBar : UserControl
{
    private bool _updatingContext;

    public event EventHandler? ConnectRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? DiscoverRequested;
    public event EventHandler? OperateRequested;
    public event EventHandler<string>? ViewRequested;
    public event EventHandler<string>? SearchChanged;
    public event EventHandler<string>? LocationChanged;
    public event EventHandler? ExitRequested;
    public event EventHandler? AboutRequested;
    public event EventHandler<string>? UserActionRequested;

    public WorkbenchTopBar()
    {
        InitializeComponent();
    }

    public void SetContext(
        ProviderRenderDescriptor providerVisual,
        string accountName,
        string accountId,
        IReadOnlyList<string> locations,
        string selectedLocation,
        string identity)
    {
        _updatingContext = true;

        ProviderAccountComboBox.Items.Clear();

        var accountDisplay =
            string.IsNullOrWhiteSpace(accountName)
                ? accountId
                : accountName;

        ProviderAccountComboBox.Items.Add(
            $"{ProviderVisualFactory.ResolveIcon(providerVisual.IconKey)}  " +
            $"{providerVisual.DisplayName} · " +
            $"{accountDisplay} ({accountId})");

        ProviderAccountComboBox.SelectedIndex = 0;

        LocationComboBox.Items.Clear();

        foreach (var location in locations)
        {
            LocationComboBox.Items.Add(location);
        }

        var selectedIndex = -1;

        for (var index = 0;
             index < LocationComboBox.ItemCount;
             index++)
        {
            if (string.Equals(
                    LocationComboBox.Items[index]?.ToString(),
                    selectedLocation,
                    StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = index;
                break;
            }
        }

        LocationComboBox.SelectedIndex =
            selectedIndex >= 0
                ? selectedIndex
                : LocationComboBox.ItemCount > 0 ? 0 : -1;

        var userDisplay = GetIdentityDisplay(identity);

        UserButton.Content = $"{userDisplay}  ▾";
        UserIdentityMenuItem.Header = identity;

        BackendIndicator.Background =
            new SolidColorBrush(Color.Parse("#22C55E"));

        BackendStatusText.Text = "Backend connected";
        BackendStatusText.Foreground =
            new SolidColorBrush(Color.Parse("#86EFAC"));

        _updatingContext = false;
    }

    public void SetDisconnected()
    {
        BackendIndicator.Background =
            new SolidColorBrush(Color.Parse("#F87171"));

        BackendStatusText.Text = "Backend offline";
        BackendStatusText.Foreground =
            new SolidColorBrush(Color.Parse("#FCA5A5"));
    }

    private void ConnectButton_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        ConnectRequested?.Invoke(this, EventArgs.Empty);

    private void RefreshButton_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        RefreshRequested?.Invoke(this, EventArgs.Empty);

    private void DiscoverButton_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        DiscoverRequested?.Invoke(this, EventArgs.Empty);

    private void OperateButton_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        OperateRequested?.Invoke(this, EventArgs.Empty);

    private void ConnectMenuItem_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        ConnectRequested?.Invoke(this, EventArgs.Empty);

    private void RefreshMenuItem_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        RefreshRequested?.Invoke(this, EventArgs.Empty);

    private void DiscoverMenuItem_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        DiscoverRequested?.Invoke(this, EventArgs.Empty);

    private void ExitMenuItem_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

    private void AboutMenuItem_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        AboutRequested?.Invoke(this, EventArgs.Empty);

    private void ViewMenuItem_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Tag is string view)
        {
            ViewRequested?.Invoke(this, view);
        }
    }

    private void GlobalSearchTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs e) =>
        SearchChanged?.Invoke(
            this,
            GlobalSearchTextBox.Text ?? "");

    private void ProviderAccountComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_updatingContext)
        {
            return;
        }
    }

    private void LocationComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_updatingContext ||
            LocationComboBox.SelectedItem is null)
        {
            return;
        }

        LocationChanged?.Invoke(
            this,
            LocationComboBox.SelectedItem.ToString() ?? "");
    }

    private void UserButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
    }

    private void UserMenuItem_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Tag is string action)
        {
            UserActionRequested?.Invoke(this, action);
        }
    }

    private static string GetIdentityDisplay(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            return "User";
        }

        var value = identity;

        var slash = value.LastIndexOf('/');

        if (slash >= 0 && slash < value.Length - 1)
        {
            value = value[(slash + 1)..];
        }

        var colon = value.LastIndexOf(':');

        if (colon >= 0 && colon < value.Length - 1)
        {
            value = value[(colon + 1)..];
        }

        return value;
    }
}
