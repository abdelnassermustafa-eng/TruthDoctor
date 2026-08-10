using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services;
using TruthDoctor.Services.Providers;
using TruthDoctor.Services.Visuals;
using TruthDoctor.ViewModels;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.State;

namespace TruthDoctor.Views.UniversalWorkbench;

public partial class UniversalWorkbenchWindow : Window
{
    private readonly DispatcherTimer _clockTimer;

    private readonly WorkbenchState _workbenchState =
        new();

    private readonly ProviderRegistry _providers =
        new();

    private readonly TruthDoctor.Services.Visuals.RenderRegistry _renderRegistry =
        new([
            new AwsRenderContributor()
        ]);

    private readonly WorkbenchNavigationController
        _navigation;

    private readonly WorkbenchResourceController
        _resources;

    private readonly WorkbenchSelectionController
        _selection;

    private readonly WorkbenchGraphContextController
        _graphContext;

    private readonly WorkbenchTopologyController
        _topology;

    private readonly WorkbenchStatusController
        _status;

    private readonly WorkbenchDetailsController
        _details;

    private readonly WorkbenchDiscoveryController
        _discovery;

    private PlatformState? _state;
    private List<InfrastructureResource> _visibleResources = [];

    public UniversalWorkbenchWindow()
    {
        _navigation =
            new WorkbenchNavigationController(
                _workbenchState);

        _resources =
            new WorkbenchResourceController(
                _workbenchState);

        _selection =
            new WorkbenchSelectionController(
                _workbenchState);

        _graphContext =
            new WorkbenchGraphContextController(
                _workbenchState);

        _topology =
            new WorkbenchTopologyController(
                _graphContext);

        _status =
            new WorkbenchStatusController(
                _workbenchState,
                _providers);

        _details =
            new WorkbenchDetailsController(
                _workbenchState,
                _renderRegistry);

        _discovery =
            new WorkbenchDiscoveryController(
                _workbenchState);

        InitializeComponent();

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        TopBar.ConnectRequested += async (_, _) =>
            await LoadStateAsync();

        TopBar.RefreshRequested += async (_, _) =>
            await LoadStateAsync(GetSelectedLocation());

        TopBar.DiscoverRequested += async (_, _) =>
            await LoadStateAsync(GetSelectedLocation());

        TopBar.OperateRequested += (_, _) =>
            NavigateToWorkspace(
                "operations");

        TopBar.ViewRequested += (_, view) =>
            NavigateToWorkspace(view);

        TopBar.SearchChanged += (_, search) =>
        {
            if (_workbenchState.CurrentView.Equals(
                    "resources",
                    StringComparison.OrdinalIgnoreCase))
            {
                SecondaryWorkspaceSearchTextBox.Text =
                    search;

                return;
            }

            ResourceSearchTextBox.Text = search;
            ApplyFilters();
        };

        TopBar.LocationChanged += async (_, location) =>
            await LoadStateAsync(location);

        TopBar.ExitRequested += (_, _) => Close();

        TopBar.AboutRequested += (_, _) =>
            ShowInformation(
                "TruthDoctor V2",
                "Universal Infrastructure State Management Workbench");

        TopBar.UserActionRequested += (_, action) =>
            HandleUserAction(action);

        Opened += async (_, _) => await LoadStateAsync();

        UpdateClock();
    }

    private async Task LoadStateAsync(
        string? region = null)
    {
        SetLoadingState();

        var succeeded =
            await _discovery.DiscoverAsync(region);

        if (!succeeded)
        {
            ApplyFailure(_workbenchState.LastError);
            return;
        }

        _state = _workbenchState.PlatformState;

        if (_state is null)
        {
            ApplyFailure(
                "Discovery completed without platform state.");
            return;
        }

        ApplyState(_state);
    }

    private void ApplyState(PlatformState state)
    {

        var provider =
            _providers.Resolve(state.Context.ProviderId);

        ProviderComboBox.Items.Clear();
        ProviderComboBox.Items.Add(
            $"{ProviderVisualFactory.ResolveIcon(provider.ProviderVisual.IconKey)}  " +
            provider.ProviderVisual.DisplayName);
        ProviderComboBox.SelectedIndex = 0;

        AccountComboBox.Items.Clear();
        AccountComboBox.Items.Add(
            string.IsNullOrWhiteSpace(state.Context.AccountName)
                ? state.Context.AccountId
                : $"{state.Context.AccountName} ({state.Context.AccountId})");
        AccountComboBox.SelectedIndex = 0;

        LocationComboBox.Items.Clear();

        foreach (var location in state.Context.Locations)
        {
            LocationComboBox.Items.Add(location);
        }

        if (LocationComboBox.ItemCount == 0 &&
            !string.IsNullOrWhiteSpace(
                state.Context.DefaultLocation))
        {
            LocationComboBox.Items.Add(
                state.Context.DefaultLocation);
        }

        var selectedIndex = state.Context.Locations
            .FindIndex(location =>
                string.Equals(
                    location,
                    state.Context.DefaultLocation,
                    StringComparison.OrdinalIgnoreCase));

        LocationComboBox.SelectedIndex =
            selectedIndex >= 0 ? selectedIndex : 0;

        BuildDomainNavigation(state.Domains);
        BuildDomainCards(state.Domains);
        BuildFilters(state);

        ResourceTitleText.Text =
            $"Discovered Resources ({state.TotalResourceCount})";

        LastDiscoveryText.Text =
            $"Last discovery: {state.DiscoveredAt.LocalDateTime:g}";

        DiscoveryStatusText.Text =
            state.Warnings.Count == 0
                ? "● Discovery completed"
                : $"● Completed with {state.Warnings.Count} warning(s)";

        DiscoveryStatusText.Foreground =
            state.Warnings.Count == 0
                ? Brush("#4ADE80")
                : Brush("#FBBF24");

        SidebarRefreshText.Text =
            $"Last refresh: {state.DiscoveredAt.LocalDateTime:t}";

        StatusAccountText.Text =
            $"Account: {state.Context.AccountName}";

        StatusLocationText.Text =
            $"Location: {state.Context.DefaultLocation}";

        StatusDiscoveryText.Text =
            $"Discovery: {state.TotalResourceCount} resources";

        StatusApiText.Text = "API: Online";
        StatusApiText.Foreground = Brush("#15803D");

        AssistantStatusText.Text =
            $"Infrastructure state loaded: {state.TotalResourceCount} resources across {state.Domains.Count} domains.";

        _status.ApplyLoaded(
            state,
            TopBar,
            RunDiscoveryButton,
            DiscoveryStatusText,
            SidebarProviderText,
            SidebarRefreshText,
            LastDiscoveryText,
            StatusProviderText,
            StatusAccountText,
            StatusLocationText,
            StatusDiscoveryText,
            StatusApiText,
            AssistantStatusText);

        ApplyFilters();
    }

    private void BuildDomainNavigation(
        IEnumerable<InfrastructureDomain> domains)
    {
        DomainNavigationPanel.Children.Clear();

        var providerId =
            _workbenchState.PlatformState?.Context.ProviderId ?? "";

        foreach (var domain in domains)
        {
            var visual =
                _renderRegistry.Resolve(
                    providerId,
                    domain.Id,
                    "",
                    domain.IconKey,
                    domain.AccentKey);

            var button = new Button
            {
                Content =
                    $"{visual.Icon}   " +
                    $"{domain.DisplayName} ({domain.ResourceCount})",
                HorizontalContentAlignment =
                    HorizontalAlignment.Left,
                Padding = new Thickness(10, 8),
                Tag = domain.Id
            };

            button.Classes.Add("nav-item");
            button.Classes.Add("domain-item");
            button.Click += DomainNavigationButton_OnClick;

            DomainNavigationPanel.Children.Add(button);
        }

        EmptyDomainsText.IsVisible =
            !DomainNavigationPanel.Children.Any();
    }

    private void BuildDomainCards(
        IEnumerable<InfrastructureDomain> domains)
    {
        DomainCardsControl.Items.Clear();

        var providerId =
            _workbenchState.PlatformState?.Context.ProviderId ?? "";

        foreach (var domain in domains)
        {
            var visual =
                _renderRegistry.Resolve(
                    providerId,
                    domain.Id,
                    "",
                    domain.IconKey,
                    domain.AccentKey);

            var card = new Border
            {
                Width = 210,
                Margin = new Thickness(5),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(10),
                Background = Brush(visual.Background),
                BorderBrush = Brush(visual.Border),
                BorderThickness = new Thickness(1),
                Tag = domain.Id
            };

            var stack = new StackPanel
            {
                Spacing = 5
            };

            stack.Children.Add(new TextBlock
            {
                Text = visual.Icon,
                FontSize = 25,
                Foreground = Brush(visual.Foreground)
            });

            stack.Children.Add(new TextBlock
            {
                Text = domain.DisplayName,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brush("#F8FAFC")
            });

            stack.Children.Add(new TextBlock
            {
                Text = domain.ResourceCount.ToString(),
                FontSize = 27,
                FontWeight = FontWeight.Bold,
                Foreground = Brush("#FFFFFF")
            });

            stack.Children.Add(new TextBlock
            {
                Text = string.Join(
                    ", ",
                    domain.ResourceTypes),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brush(visual.Foreground)
            });

            card.Child = stack;
            DomainCardsControl.Items.Add(card);
        }
    }

    private void BuildFilters(PlatformState state)
    {
        DomainFilterComboBox.Items.Clear();
        DomainFilterComboBox.Items.Add("All domains");

        foreach (var domain in state.Domains)
        {
            DomainFilterComboBox.Items.Add(
                domain.DisplayName);
        }

        DomainFilterComboBox.SelectedIndex = 0;

        StateFilterComboBox.Items.Clear();
        StateFilterComboBox.Items.Add("All states");

        foreach (var resourceState in state.Resources
                     .Select(resource => resource.State)
                     .Where(value =>
                         !string.IsNullOrWhiteSpace(value))
                     .Distinct(
                         StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value))
        {
            StateFilterComboBox.Items.Add(resourceState);
        }

        StateFilterComboBox.SelectedIndex = 0;
    }

    private void ApplyFilters()
    {
        _visibleResources = _resources.ApplyFilters(
                ResourceSearchTextBox.Text,
                DomainFilterComboBox.SelectedItem?.ToString(),
                StateFilterComboBox.SelectedItem?.ToString())
            .ToList();

        RenderResources();
    }

    private void RenderResources()
    {
        ResourceListBox.Items.Clear();

        foreach (var resource in _visibleResources)
        {
            ResourceListBox.Items.Add(
                CreateResourceRow(resource));
        }

        ResourceTitleText.Text =
            $"Discovered Resources ({_visibleResources.Count})";
    }

    private ListBoxItem CreateResourceRow(
        InfrastructureResource resource)
    {
        var visual =
            _renderRegistry.Resolve(
                resource.ProviderId,
                resource.DomainId,
                resource.ResourceType,
                resource.IconKey,
                resource.AccentKey);

        var grid = new Grid
        {
            ColumnDefinitions =
                new ColumnDefinitions("42,2*,1.2*,1*,1*"),
            Margin = new Thickness(4)
        };

        grid.Children.Add(new TextBlock
        {
            Text = visual.Icon,
            FontSize = 18,
            Foreground = Brush(visual.Foreground),
            VerticalAlignment = VerticalAlignment.Center
        });

        var identity = new StackPanel
        {
            Spacing = 2
        };

        identity.Children.Add(new TextBlock
        {
            Text = resource.DisplayName,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#F8FAFC")
        });

        identity.Children.Add(new TextBlock
        {
            Text = resource.NativeId,
            FontSize = 10,
            Foreground = Brush("#94A3B8")
        });

        Grid.SetColumn(identity, 1);
        grid.Children.Add(identity);

        var type = new TextBlock
        {
            Text = resource.ResourceType,
            Foreground = Brush("#CBD5E1"),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(type, 2);
        grid.Children.Add(type);

        var state = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(resource.State)
                ? "Observed"
                : $"● {resource.State}",
            Foreground = StateBrush(resource.State),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(state, 3);
        grid.Children.Add(state);

        var location = new TextBlock
        {
            Text = resource.Location,
            Foreground = Brush("#CBD5E1"),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(location, 4);
        grid.Children.Add(location);

        return new ListBoxItem
        {
            Content = grid,
            Tag = resource,
            Padding = new Thickness(8)
        };
    }

    private void RenderSecondaryResources()
    {
        SecondaryWorkspaceList.Items.Clear();

        if (_state is null)
        {
            SecondaryWorkspaceDescription.Text =
                "No infrastructure state is loaded.";

            return;
        }

        var search =
            SecondaryWorkspaceSearchTextBox.Text?.Trim() ?? "";

        var resources =
            _state.Resources
                .Where(resource =>
                    MatchesResourceSearch(
                        resource,
                        search))
                .OrderBy(resource =>
                    resource.DomainId)
                .ThenBy(resource =>
                    resource.ResourceType)
                .ThenBy(resource =>
                    resource.DisplayName)
                .ToList();

        foreach (var resource in resources)
        {
            SecondaryWorkspaceList.Items.Add(
                CreateResourceRow(resource));
        }

        SecondaryWorkspaceDescription.Text =
            $"{resources.Count} of " +
            $"{_state.TotalResourceCount} resources";
    }

    private static bool MatchesResourceSearch(
        InfrastructureResource resource,
        string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return ContainsSearch(
                   resource.DisplayName,
                   search) ||
               ContainsSearch(
                   resource.NativeId,
                   search) ||
               ContainsSearch(
                   resource.ResourceId,
                   search) ||
               ContainsSearch(
                   resource.ResourceType,
                   search) ||
               ContainsSearch(
                   resource.DomainId,
                   search) ||
               ContainsSearch(
                   resource.State,
                   search) ||
               ContainsSearch(
                   resource.Location,
                   search) ||
               ContainsSearch(
                   resource.AccountId,
                   search) ||
               resource.Properties.Any(item =>
                   ContainsSearch(
                       item.Key,
                       search) ||
                   ContainsSearch(
                       item.Value,
                       search)) ||
               resource.Tags.Any(item =>
                   ContainsSearch(
                       item.Key,
                       search) ||
                   ContainsSearch(
                       item.Value,
                       search));
    }

    private static bool ContainsSearch(
        string? value,
        string search)
    {
        return value?.Contains(
                   search,
                   StringComparison.OrdinalIgnoreCase)
               == true;
    }

    private void SetLoadingState()
    {
        _status.ApplyLoading(
            RunDiscoveryButton,
            DiscoveryStatusText,
            StatusDiscoveryText,
            StatusApiText);
    }

    private void ApplyFailure(string message)
    {
        _status.ApplyFailure(
            message,
            TopBar,
            RunDiscoveryButton,
            DiscoveryStatusText,
            StatusDiscoveryText,
            StatusApiText,
            AssistantStatusText);
    }

    private async void RunDiscoveryButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var region =
            LocationComboBox.SelectedItem?.ToString();

        await LoadStateAsync(
            string.IsNullOrWhiteSpace(region) ||
            region == "No location"
                ? null
                : region);

        RunDiscoveryButton.IsEnabled = true;
        RunDiscoveryButton.Content = "▶  Run Discovery";
    }

    private void DomainNavigationButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not string domainId ||
            _state is null)
        {
            return;
        }

        var domain = _state.Domains.FirstOrDefault(
            item => item.Id.Equals(
                domainId,
                StringComparison.OrdinalIgnoreCase));

        if (domain is null)
        {
            return;
        }

        SetActiveDomainNavigation(button);

        _selection.SelectDomain(domain.Id);

        DomainFilterComboBox.SelectedItem =
            domain.DisplayName;

        ApplyFilters();
    }

    private void ResourceListBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        SelectResourceFromList(
            ResourceListBox);
    }

    private void SecondaryWorkspaceList_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        SelectResourceFromList(
            SecondaryWorkspaceList);
    }

    private void SelectResourceFromList(
        ListBox list)
    {
        if (list.SelectedItem is not ListBoxItem item ||
            item.Tag is not InfrastructureResource resource)
        {
            return;
        }

        _selection.SelectResource(resource);

        _details.RenderSelectedResource(
            ResourceDetailsPanel,
            AssistantStatusText);
    }

    private void SecondaryWorkspaceSearchTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (_workbenchState.CurrentView.Equals(
                "resources",
                StringComparison.OrdinalIgnoreCase))
        {
            RenderSecondaryResources();
        }
    }

    private void ResourceSearchTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void FilterComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private async void LocationComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_state is null ||
            LocationComboBox.SelectedItem is not string region ||
            string.IsNullOrWhiteSpace(region))
        {
            return;
        }

        _selection.SelectLocation(region);

        _status.ApplyLocation(
            region,
            StatusLocationText);

        await Task.CompletedTask;
    }

    private void UpdateClock()
    {
        CurrentTimeText.Text =
            DateTime.Now.ToString("h:mm:ss tt");
    }


    private static IBrush StateBrush(string? state)
    {
        return state?.ToLowerInvariant() switch
        {
            "running" or
            "available" or
            "active" or
            "healthy" or
            "in-use" =>
                Brush("#4ADE80"),

            "pending" or
            "creating" or
            "updating" =>
                Brush("#FBBF24"),

            "failed" or
            "error" or
            "unhealthy" or
            "terminated" =>
                Brush("#F87171"),

            _ => Brush("#CBD5E1")
        };
    }

    private static IBrush Brush(string value)
    {
        return new SolidColorBrush(Color.Parse(value));
    }

    private string? GetSelectedLocation()
    {
        return LocationComboBox.SelectedItem?.ToString();
    }

    private void NavigateToWorkspace(
        string view)
    {
        SetActivePrimaryNavigation(view);
        if (view.Equals(
                "topology",
                StringComparison.OrdinalIgnoreCase))
        {
            var topology =
                _topology.Current;

            TopologyWorkspace.Render(
                topology);
        }

        _navigation.ShowWorkspace(
            view,
            DashboardWorkspace,
            TopologyWorkspace,
            SecondaryWorkspace,
            SecondaryWorkspaceTitle,
            SecondaryWorkspaceDescription,
            SecondaryWorkspaceList);

        var isResources =
            view.Equals(
                "resources",
                StringComparison.OrdinalIgnoreCase);

        SecondaryWorkspaceSearchTextBox.IsVisible =
            isResources;

        if (isResources)
        {
            RenderSecondaryResources();
        }
    }

    private void SetActivePrimaryNavigation(
        string view)
    {
        var primaryButtons = new[]
        {
            DashboardNavButton,
            ResourcesNavButton,
            OperationsNavButton,
            TopologyNavButton,
            HistoryNavButton,
            ReportsNavButton,
            SettingsNavButton
        };

        foreach (var button in primaryButtons)
        {
            var isActive =
                button.Tag is string tag &&
                tag.Equals(
                    view,
                    StringComparison.OrdinalIgnoreCase);

            SetActiveClass(
                button,
                isActive);
        }

        foreach (var child in
                 DomainNavigationPanel.Children.OfType<Button>())
        {
            SetActiveClass(
                child,
                false);
        }
    }

    private void SetActiveDomainNavigation(
        Button selectedButton)
    {
        foreach (var button in new[]
                 {
                     DashboardNavButton,
                     ResourcesNavButton,
                     OperationsNavButton,
                     TopologyNavButton,
                     HistoryNavButton,
                     ReportsNavButton,
                     SettingsNavButton
                 })
        {
            SetActiveClass(
                button,
                false);
        }

        foreach (var child in
                 DomainNavigationPanel.Children.OfType<Button>())
        {
            SetActiveClass(
                child,
                ReferenceEquals(
                    child,
                    selectedButton));
        }
    }

    private static void SetActiveClass(
        Button button,
        bool isActive)
    {
        if (isActive)
        {
            if (!button.Classes.Contains("active"))
            {
                button.Classes.Add("active");
            }

            return;
        }

        button.Classes.Remove("active");
    }

    private void NavigationButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is string view)
        {
            NavigateToWorkspace(view);
        }
    }


    private void HandleUserAction(string action)
    {
        if (action.Equals(
                "exit",
                StringComparison.OrdinalIgnoreCase))
        {
            Close();
            return;
        }

        NavigateToWorkspace(action);
    }

    private void ShowInformation(
        string title,
        string message)
    {
        SecondaryWorkspaceTitle.Text = title;
        SecondaryWorkspaceDescription.Text = message;
        SecondaryWorkspaceList.Items.Clear();
        DashboardWorkspace.IsVisible = false;
        SecondaryWorkspace.IsVisible = true;
    }
}


internal static class ListExtensions
{
    public static int FindIndex<T>(
        this IReadOnlyList<T> source,
        Func<T, bool> predicate)
    {
        for (var index = 0; index < source.Count; index++)
        {
            if (predicate(source[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
