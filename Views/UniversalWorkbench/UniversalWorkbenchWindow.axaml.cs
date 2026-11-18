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
using TruthDoctor.Services.Standalone;
using TruthDoctor.Services.Visuals;
using TruthDoctor.ViewModels;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.State;
using TruthDoctor.Graph;

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

    private readonly WorkbenchCrossWorkspaceNavigationCoordinator
        _crossWorkspaceNavigation = new();

    private readonly WorkbenchStatusController
        _status;

    private readonly WorkbenchDetailsController
        _details;

    private readonly WorkbenchDiscoveryController
        _discovery;

    // The demo is strictly opt-in. Normal startup retains
    // the existing discovery controller.
    private readonly StandaloneLocalDemoSession? _localDemo =
        Environment.GetEnvironmentVariable(
            "TRUTHDOCTOR_STANDALONE_DEMO") == "1"
            ? new StandaloneLocalDemoSession()
            : null;

    // Real AWS discovery is strictly opt-in.
    // Normal desktop and demo modes remain unchanged.
    // Explicit, offline SG visual acceptance fixture.
    // This is not live AWS discovery.
    private readonly StandaloneSyntheticAwsSecurityGroupSession?
        _syntheticSgDemo =
        Environment.GetEnvironmentVariable(
            "TRUTHDOCTOR_SYNTHETIC_AWS_SG_DEMO") == "1"
            ? new StandaloneSyntheticAwsSecurityGroupSession()
            : null;

    private readonly StandaloneAwsDesktopSession? _awsSession =
        StandaloneAwsDesktopSession.FromEnvironment();

    // Explicit opt-in only; the in-process source remains
    // unconfigured until separately authorized and tested.
    private readonly bool _inProcessDiscoveryRequested =
        Environment.GetEnvironmentVariable(
            "TRUTHDOCTOR_INPROCESS_DISCOVERY") == "1";

    private readonly bool _inProcessLocalDemoRequested =
        Environment.GetEnvironmentVariable(
            "TRUTHDOCTOR_INPROCESS_LOCAL_DEMO") == "1";

    private readonly TopologySavedViewCatalogService
        _savedViews;

    private readonly TopologySavedViewStoreLoadResult
        _savedViewLoadResult;

    private PlatformState? _state;
    private List<InfrastructureResource> _visibleResources = [];
    private string _globalSearchText = "";

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
                _graphContext,
                _selection,
                _workbenchState);

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

        var savedViewStoragePath =
            new TopologySavedViewStoragePathResolver()
                .FilePath;

        _savedViews =
            new TopologySavedViewCatalogService(
                new TopologySavedViewFileStore(
                    savedViewStoragePath));

        _savedViewLoadResult =
            _savedViews.Load();

        UpdateSavedViewControls();

        TopologyWorkspace.SaveViewRequested +=
            async () =>
                await SaveCurrentTopologyViewAsync();

        TopologyWorkspace.LoadViewRequested +=
            LoadTopologySavedView;

        TopologyWorkspace.RenameViewRequested +=
            async savedViewId =>
                await RenameTopologySavedViewAsync(
                    savedViewId);

        TopologyWorkspace.DeleteViewRequested +=
            async savedViewId =>
                await DeleteTopologySavedViewAsync(
                    savedViewId);

        TopologyWorkspace.NodeInvoked += node =>
        {
            var graphNode =
                _workbenchState
                    .InfrastructureGraphIndex?
                    .FindNode(node.Id);

            if (graphNode?.Resource is not
                InfrastructureResource resource)
            {
                return;
            }

            if (_topology.NavigateTo(resource))
            {
                RenderTopologySelection();
            }
        };

        TopologyWorkspace.PathRequested +=
            (sourceId, targetId) =>
            {
                var result =
                    _graphContext.FindShortestPath(
                        sourceId,
                        targetId,
                        includeReverseRelationships: true);

                TopologyWorkspace.ShowPath(result);
            };

        TopologyWorkspace.BackRequested += () =>
        {
            if (_topology.GoBack())
            {
                RenderTopologySelection();
            }
        };

        TopologyWorkspace.ForwardRequested += () =>
        {
            if (_topology.GoForward())
            {
                RenderTopologySelection();
            }
        };

        TopologyWorkspace.HomeRequested += () =>
        {
            if (_topology.GoHome())
            {
                RenderTopologySelection();
            }
        };


        TopologyWorkspace.InfrastructureScopeChanged +=
            showFullInfrastructure =>
            {
                _showFullInfrastructureTopology =
                    showFullInfrastructure;

                RenderTopologySelection();
            };

        TopologyWorkspace.DepthChanged += depth =>
        {
            if (_topology.SetDepth(depth))
            {
                RenderTopologySelection();
            }
        };

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
            _globalSearchText = search;
            RouteGlobalSearch(search);
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

        Opened += async (_, _) =>
        {
            if (DesktopStartupDiscoveryPolicy.ShouldAutoDiscover(
                    localDemo: _localDemo is not null,
                    syntheticSecurityGroup: _syntheticSgDemo is not null,
                    awsStandalone: _awsSession is not null,
                    inProcess: _inProcessDiscoveryRequested))
            {
                await LoadStateAsync();
            }
        };

        UpdateClock();
    }

    private async Task LoadStateAsync(
        string? region = null)
    {
        bool uiLocalDemoSelected = DesktopManualDiscoveryPolicy.MayUseOfflineDemo(
            ProviderComboBox.SelectedIndex == 1);

        // A plain user action cannot silently contact the legacy HTTP backend.
        // Explicitly selected offline demo is routed to the existing in-process runner.
        SetLoadingState();

        DesktopDiscoveryRoute selectedRoute;

        try
        {
            selectedRoute = DesktopDiscoveryRouteSelector.Select(
                localDemo: _localDemo is not null,
                syntheticSecurityGroup: _syntheticSgDemo is not null,
                awsStandalone: _awsSession is not null,
                inProcess: _inProcessDiscoveryRequested || uiLocalDemoSelected);
        }
        catch (InvalidOperationException exception)
        {
            ApplyFailure(exception.Message);
            return;
        }


        if (selectedRoute == DesktopDiscoveryRoute.LegacyHttp &&
            !DesktopManualDiscoveryPolicy.MayUseLegacyHttp(
                true,
                Environment.GetEnvironmentVariable("TRUTHDOCTOR_LEGACY_HTTP_DISCOVERY") == "1"))
        {
            ApplyFailure("Select Local Demo (Offline) before discovery.");
            return;
        }

        // Do not fall back to HTTP when explicit in-process
        // discovery has no approved execution source.
        if (selectedRoute == DesktopDiscoveryRoute.InProcess)
        {

            // UI selection authorizes the offline demo independently.
            // Otherwise preserve the original environment secondary gate.
            if (!uiLocalDemoSelected)
            {
                if (!_inProcessLocalDemoRequested)
                {
                    ApplyFailure(
                        "In-process discovery is not configured.");
                    return;
                }
            }
        }

        bool succeeded;

        if (selectedRoute == DesktopDiscoveryRoute.LegacyHttp)
        {
            succeeded = await _discovery.DiscoverAsync(region);
        }
        else
        {
            try
            {
                if (selectedRoute != DesktopDiscoveryRoute.InProcess)
                {
                    _workbenchState.IsDiscovering = true;
                    _workbenchState.NotifyChanged();
                }
                _workbenchState.LastError = "";

                if (selectedRoute == DesktopDiscoveryRoute.InProcess)
                {
                    DesktopInProcessExecutionDecision coordinatorDecision =
                        await DesktopInProcessDiscoveryRunner.ExecuteAsync(
        _workbenchState,
        _inProcessDiscoveryRequested || uiLocalDemoSelected,
        _inProcessLocalDemoRequested || uiLocalDemoSelected,
        async cancellationToken =>
        {
            await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                _workbenchState,
                (workbench, location, sessionCancellationToken) =>
                                                new StandaloneLocalDemoSession().DiscoverAsync(
                                                    workbench,
                                                    location,
                                                    sessionCancellationToken),
                region,
                cancellationToken);
        },
        default);

                    if (coordinatorDecision ==
                        DesktopInProcessExecutionDecision.FailClosed)
                    {
                        throw new InvalidOperationException(
                            "In-process discovery is not configured.");
                    }

                    if (coordinatorDecision !=
                        DesktopInProcessExecutionDecision.OfflineLocalDemo)
                    {
                        throw new InvalidOperationException(
                            "Unexpected offline discovery authorization result.");
                    }
                }
                else if (_syntheticSgDemo is not null)
                {
                    await _syntheticSgDemo.DiscoverAsync(
                        _workbenchState);
                }
                else if (_awsSession is not null)
                {
                    await _awsSession.DiscoverAsync(
                        _workbenchState, region);
                }
                else
                {
                    await _localDemo!.DiscoverAsync(
                        _workbenchState, region);
                }

                if (selectedRoute != DesktopDiscoveryRoute.InProcess)
                {
                    _workbenchState.IsDiscovering = false;
                    _workbenchState.NotifyChanged();
                }
                succeeded = true;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                _workbenchState.SetFailure(exception.Message);
                succeeded = false;
            }
        }

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

        if (uiLocalDemoSelected)
        {
            ProviderComboBox.Items.Clear();
            ProviderComboBox.Items.Add("Not connected");
            ProviderComboBox.Items.Add("Local Demo (Offline)");
            ProviderComboBox.SelectedIndex = 1;
            StatusApiText.Text = "Offline demo - no API";
        }


        if (_localDemo is not null)
        {
            ProviderComboBox.Items.Clear();
            ProviderComboBox.Items.Add(
                "Local Demo (mock, offline)");
            ProviderComboBox.SelectedIndex = 0;

            StatusApiText.Text = "Offline demo - no API";
        }

        if (_syntheticSgDemo is not null)
        {
            ProviderComboBox.Items.Clear();
            ProviderComboBox.Items.Add(
                "Synthetic AWS Security Groups (OFFLINE)");
            ProviderComboBox.SelectedIndex = 0;
            StatusApiText.Text =
                "Synthetic SG fixture - no AWS API";
        }
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

        // Offline discovery does not imply cloud authentication.
        if (string.Equals(
                state.Context.DefaultLocation,
                "local-demo",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                state.Context.DefaultLocation,
                "synthetic-offline",
                StringComparison.OrdinalIgnoreCase))
        {
            TopBar.SetOfflineFixtureStatus();
            StatusApiText.Text = "Offline fixture - no cloud API";
            StatusApiText.Foreground = Brush("#0F766E");
        }

        // The standalone AWS CLI provider is authenticated through
        // its own discovery session. Do not report an HTTP backend
        // health result that was never independently checked.
        if (_awsSession is not null &&
            string.Equals(
                state.Context.ProviderId,
                "aws",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                state.Context.DefaultLocation,
                "synthetic-offline",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                state.Context.DefaultLocation,
                "local-demo",
                StringComparison.OrdinalIgnoreCase))
        {
            TopBar.SetStandaloneProviderConnected();

            StatusApiText.Text = "AWS CLI: Connected";
            StatusApiText.Foreground = Brush("#15803D");
        }

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



    // Sorting is presentation state, never discovery state.
    private int? _primarySortColumn;
    private bool _primarySortDescending;
    private int? _secondarySortColumn;
    private bool _secondarySortDescending;

    private void OnPrimarySortHeaderClick(int column)
    {
        _primarySortDescending =
            _primarySortColumn == column &&
            !_primarySortDescending;

        _primarySortColumn = column;
        RenderResources();
    }

    private void OnSecondarySortHeaderClick(int column)
    {
        _secondarySortDescending =
            _secondarySortColumn == column &&
            !_secondarySortDescending;

        _secondarySortColumn = column;
        RenderSecondaryResources();
    }

    private static DateTimeOffset? NativeCreationSortTime(
        InfrastructureResource resource)
    {
        if (resource.LifecycleEvidence?.VerifiedCreation
            is { } verified)
        {
            return verified.OccurredAt;
        }

        return resource.Properties.TryGetValue(
                   "NativeLifecycle.CreationTimestamp",
                   out var raw) &&
               DateTimeOffset.TryParse(
                   raw,
                   System.Globalization.CultureInfo.InvariantCulture,
                   System.Globalization.DateTimeStyles.RoundtripKind,
                   out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? SortDate(
        InfrastructureResource resource,
        int field)
    {
        return field switch
        {
            9 => NativeCreationSortTime(resource),
            10 => resource.LifecycleEvidence?
                .LastVerifiedActivity?.OccurredAt,
            11 => resource.DiscoveredAt == default
                ? null
                : resource.DiscoveredAt,
            _ => null
        };
    }

    private static int CompareResourceRows(
        InfrastructureResource left,
        InfrastructureResource right,
        int column,
        bool primary,
        bool descending)
    {
        var field = primary
            ? column switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                4 => 6,
                5 => 5,
                6 => 9,
                _ => -1
            }
            : column;

        int comparison;

        // Missing dates remain last in both directions.
        if (field is 9 or 10 or 11)
        {
            var leftTime = SortDate(left, field);
            var rightTime = SortDate(right, field);

            if (!leftTime.HasValue)
                return rightTime.HasValue ? 1 : 0;

            if (!rightTime.HasValue)
                return -1;

            comparison =
                leftTime.Value.CompareTo(rightTime.Value);
        }
        else if (field == 8)
        {
            comparison = left.Relationships.Count.CompareTo(
                right.Relationships.Count);
        }
        else
        {
            string? leftValue = field switch
            {
                0 => left.DisplayName,
                1 => left.NativeId,
                2 => left.ResourceType,
                3 => left.ProviderId,
                4 => left.AccountId,
                5 => left.Location,
                6 => left.State,
                7 => left.AvailabilityZone,
                _ => null
            };

            string? rightValue = field switch
            {
                0 => right.DisplayName,
                1 => right.NativeId,
                2 => right.ResourceType,
                3 => right.ProviderId,
                4 => right.AccountId,
                5 => right.Location,
                6 => right.State,
                7 => right.AvailabilityZone,
                _ => null
            };

            comparison =
                StringComparer.OrdinalIgnoreCase.Compare(
                    leftValue, rightValue);
        }

        return descending ? -comparison : comparison;
    }

    private static List<InfrastructureResource>
        SortResourceRows(
            IEnumerable<InfrastructureResource> input,
            int? column,
            bool descending,
            bool primary)
    {
        if (!column.HasValue)
            return input.ToList();

        return input
            .OrderBy(
                resource => resource,
                Comparer<InfrastructureResource>.Create(
                    (left, right) =>
                        CompareResourceRows(
                            left,
                            right,
                            column.Value,
                            primary,
                            descending)))
            .ToList();
    }

    private static Button MakeSortHeaderButton(
        string title,
        int column,
        int? selectedColumn,
        bool descending,
        Action<int> onClick)
    {
        var indicator = selectedColumn == column
            ? (descending ? " ▼" : " ▲")
            : " ↕";

        var button = new Button
        {
            Content = title + indicator,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#172B4D"),
            Background = Brush("#F8FAFC"),
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment =
                HorizontalAlignment.Left,
            Padding = new Thickness(5, 3, 5, 3)
        };

        ToolTip.SetTip(
            button,
            "Sort " + title +
            " ascending or descending");

        button.Click += (_, _) =>
            onClick(column);

        return button;
    }

    private ListBoxItem CreatePrimaryLifecycleHeader()
    {
        var columns = new[]
        {
            "", "Resource", "Resource ID",
            "Type", "State", "Location", "Created At"
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(
                "42,210,210,145,110,105,290"),
            Margin = new Thickness(4)
        };

        for (int index = 0; index < columns.Length; index++)
        {
            if (index == 0)
                continue;

            var sortButton = MakeSortHeaderButton(
                columns[index],
                index,
                _primarySortColumn,
                _primarySortDescending,
                OnPrimarySortHeaderClick);

            Grid.SetColumn(sortButton, index);
            grid.Children.Add(sortButton);
        }

        return new ListBoxItem
        {
            Content = grid,
            IsEnabled = true,
            Padding = new Thickness(8),
            HorizontalContentAlignment =
                HorizontalAlignment.Left
        };
    }

    private void RenderResources()
    {
        ResourceListBox.Items.Clear();

        ResourceListBox.Items.Add(
            CreatePrimaryLifecycleHeader());

        foreach (var resource in SortResourceRows(
                     _visibleResources,
                     _primarySortColumn,
                     _primarySortDescending,
                     true))
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
                new ColumnDefinitions("42,210,210,145,110,105,290"),
            Margin = new Thickness(4)
        };

        grid.Children.Add(new TextBlock
        {
            Text = visual.Icon,
            FontSize = 18,
            Foreground = Brush(visual.Foreground),
            VerticalAlignment = VerticalAlignment.Center
        });

        var resourceName = new TextBlock
        {
            Text = resource.DisplayName,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#172B4D"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(5, 0, 10, 0)
        };

        ToolTip.SetTip(resourceName, resource.DisplayName);
        Grid.SetColumn(resourceName, 1);
        grid.Children.Add(resourceName);

        var resourceId = new TextBlock
        {
            Text = resource.NativeId,
            Foreground = Brush("#526581"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(5, 0, 10, 0)
        };

        ToolTip.SetTip(resourceId, resource.NativeId);
        Grid.SetColumn(resourceId, 2);
        grid.Children.Add(resourceId);

        var type = new TextBlock
        {
            Text = resource.ResourceType,
            Foreground = Brush("#526581"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        Grid.SetColumn(type, 3);
        grid.Children.Add(type);

        var state = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(resource.State)
                ? "Observed"
                : $"● {resource.State}",
            Foreground = StateBrush(resource.State),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(state, 4);
        grid.Children.Add(state);

        var location = new TextBlock
        {
            Text = resource.Location,
            Foreground = Brush("#526581"),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(location, 5);
        grid.Children.Add(location);


        var lifecyclePresentation =
            ResolveCreatedAtPresentation(resource);

        var createdAtCell = new TextBlock
        {
            Text = lifecyclePresentation.Value,
            Foreground = Brush("#526581"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(5, 0, 10, 0)
        };

        ToolTip.SetTip(
            createdAtCell,
            lifecyclePresentation.Tooltip);

        Grid.SetColumn(createdAtCell, 6);
        grid.Children.Add(createdAtCell);

        return new ListBoxItem
        {
            Content = grid,
            Tag = resource,
            Padding = new Thickness(8),
            HorizontalContentAlignment =
                HorizontalAlignment.Left
        };
    }

    private static Grid CreateInventoryColumns()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(
                "220,220,145,90,155,105,110,120,105,330,270,175"),
            Margin = new Thickness(4)
        };
    }

    private static TextBlock CreateInventoryCell(
        string? value,
        int column,
        bool heading = false)
    {
        var text = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value)
                ? "—"
                : value,
            FontWeight = heading
                ? FontWeight.SemiBold
                : FontWeight.Normal,
            Foreground = Brush(
                heading ? "#172B4D" : "#526581"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 3, 7, 3)
        };

        ToolTip.SetTip(text, value ?? "");

        Grid.SetColumn(text, column);

        return text;
    }

    private ListBoxItem CreateResourceInventoryHeader()
    {
        var grid = CreateInventoryColumns();

        var columns = new[]
        {
            "Resource Name",
            "Resource ID",
            "Type",
            "Provider",
            "Account / Project",
            "Region",
            "Status",
            "Availability Zone",
            "Relationships",
            "Created At",
            "Last Known Activity",
            "Discovered At"
        };

        for (var index = 0; index < columns.Length; index++)
        {
            var sortButton = MakeSortHeaderButton(
                columns[index],
                index,
                _secondarySortColumn,
                _secondarySortDescending,
                OnSecondarySortHeaderClick);

            Grid.SetColumn(sortButton, index);
            grid.Children.Add(sortButton);
        }

        return new ListBoxItem
        {
            Content = grid,
            IsEnabled = false,
            Padding = new Thickness(8),
            HorizontalContentAlignment =
                HorizontalAlignment.Left
        };
    }


    private static (string Value, string Tooltip)
        ResolveCreatedAtPresentation(
            InfrastructureResource resource)
    {
        if (resource.LifecycleEvidence?.VerifiedCreation
            is { } evidence)
        {
            var value =
                evidence.OccurredAt.ToLocalTime().ToString(
                    "yyyy-MM-dd HH:mm:ss zzz",
                    System.Globalization.CultureInfo.InvariantCulture);

            return (
                value,
                "Verified creation evidence; source: " +
                evidence.Source + "; time: " + value);
        }

        if (resource.Properties is { } properties &&
            properties.TryGetValue(
                "NativeLifecycle.CreationTimestamp",
                out var raw) &&
            properties.TryGetValue(
                "NativeLifecycle.CreationLabel",
                out var label) &&
            properties.TryGetValue(
                "NativeLifecycle.CreationSource",
                out var source) &&
            (label == "Launched" || label == "Created") &&
            !string.IsNullOrWhiteSpace(source) &&
            System.DateTimeOffset.TryParse(
                raw,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var time))
        {
            var formatted = time.ToLocalTime().ToString(
                "yyyy-MM-dd HH:mm:ss zzz",
                System.Globalization.CultureInfo.InvariantCulture);

            return (
                label + ": " + formatted,
                "Native provider timestamp; source: " +
                source + "; not audit-verified; time: " +
                formatted);
        }

        return (
            "Not available",
            "No verified creation evidence or supported " +
            "native creation/launch timestamp.");
    }

private ListBoxItem CreateResourceInventoryRow(
        InfrastructureResource resource)
    {
        var grid = CreateInventoryColumns();

        var createdAtPresentation =
            ResolveCreatedAtPresentation(resource);

        var fields = new[]
        {
            resource.DisplayName,
            resource.NativeId,
            resource.ResourceType,
            resource.ProviderId,
            resource.AccountId,
            resource.Location,
            string.IsNullOrWhiteSpace(resource.State)
                ? "Observed"
                : resource.State,
            resource.AvailabilityZone,
            resource.Relationships.Count.ToString(),
            // Original creation is not currently available
            // as verified provider-neutral metadata.
            createdAtPresentation.Value,
            // No verified lifecycle event contract yet.
            // Never infer events from current resource state.
            resource.LifecycleEvidence?.LastVerifiedActivity?.OccurredAt is { } lastActivityAt
                ? lastActivityAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "Not available",
            resource.DiscoveredAt == default
                ? "Not available"
                : resource.DiscoveredAt.LocalDateTime.ToString(
                    "yyyy-MM-dd HH:mm")
        };

        for (var index = 0; index < fields.Length; index++)
        {
            var cell = CreateInventoryCell(
                fields[index],
                index);

            if (index == 0)
            {
                cell.FontWeight = FontWeight.SemiBold;
                cell.Foreground = Brush("#172B4D");
            }

            if (index == 6)
            {
                cell.Foreground =
                    StateBrush(resource.State);
            }

            if (index == 9)
            {
                ToolTip.SetTip(
                    cell,
                    createdAtPresentation.Tooltip);
            }
            else if (index == 10)
            {
                ToolTip.SetTip(
                    cell,
                    resource.LifecycleEvidence?.LastVerifiedActivity is { } activityEvidence
                        ? "Verified activity evidence (" +
                          activityEvidence.Source + "): " +
                          activityEvidence.OccurredAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz")
                        : "No verified activity evidence available");
            }
            else if (index == 11 &&
                     resource.DiscoveredAt != default)
            {
                ToolTip.SetTip(
                    cell,
                    "Discovery timestamp (local display): " +
                    resource.DiscoveredAt.ToLocalTime().ToString(
                        "yyyy-MM-dd HH:mm:ss zzz"));
            }

            grid.Children.Add(cell);
        }

        return new ListBoxItem
        {
            Content = grid,
            Tag = resource,
            Padding = new Thickness(8),
            HorizontalContentAlignment =
                HorizontalAlignment.Left
        };
    }

    private bool _updatingSecondaryResourceFilters;

    private void PopulateSecondaryResourceFilters()
    {
        if (_state is null)
        {
            return;
        }

        var oldDomain =
            SecondaryDomainFilterComboBox.SelectedItem?.ToString();

        var oldState =
            SecondaryStateFilterComboBox.SelectedItem?.ToString();

        _updatingSecondaryResourceFilters = true;

        try
        {
            SecondaryDomainFilterComboBox.Items.Clear();
            SecondaryDomainFilterComboBox.Items.Add("All domains");

            foreach (var domain in _state.Resources
                         .Select(resource => resource.DomainId)
                         .Where(value => !string.IsNullOrWhiteSpace(value))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(value => value))
            {
                SecondaryDomainFilterComboBox.Items.Add(domain);
            }

            SecondaryStateFilterComboBox.Items.Clear();
            SecondaryStateFilterComboBox.Items.Add("All states");

            foreach (var state in _state.Resources
                         .Select(resource =>
                             string.IsNullOrWhiteSpace(resource.State)
                                 ? "Observed"
                                 : resource.State)
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(value => value))
            {
                SecondaryStateFilterComboBox.Items.Add(state);
            }

            var domainIndex = 0;
            for (var index = 1;
                 index < SecondaryDomainFilterComboBox.ItemCount;
                 index++)
            {
                if (string.Equals(
                    SecondaryDomainFilterComboBox.Items[index]?.ToString(),
                    oldDomain,
                    StringComparison.OrdinalIgnoreCase))
                {
                    domainIndex = index;
                    break;
                }
            }

            var stateIndex = 0;
            for (var index = 1;
                 index < SecondaryStateFilterComboBox.ItemCount;
                 index++)
            {
                if (string.Equals(
                    SecondaryStateFilterComboBox.Items[index]?.ToString(),
                    oldState,
                    StringComparison.OrdinalIgnoreCase))
                {
                    stateIndex = index;
                    break;
                }
            }

            SecondaryDomainFilterComboBox.SelectedIndex = domainIndex;
            SecondaryStateFilterComboBox.SelectedIndex = stateIndex;
        }
        finally
        {
            _updatingSecondaryResourceFilters = false;
        }
    }

    private void SecondaryResourceFilter_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_updatingSecondaryResourceFilters || _state is null)
        {
            return;
        }

        if (_workbenchState.CurrentView.Equals(
                "resources",
                StringComparison.OrdinalIgnoreCase))
        {
            RenderSecondaryResources();
        }
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

        // Initialize choices only when the filter controls are empty.
        // Never rebuild ComboBox.Items during SelectionChanged.
        if (SecondaryDomainFilterComboBox.ItemCount == 0 ||
            SecondaryStateFilterComboBox.ItemCount == 0)
        {
            PopulateSecondaryResourceFilters();
        }

        var search =
            SecondaryWorkspaceSearchTextBox.Text?.Trim() ?? "";

        var selectedDomain =
            SecondaryDomainFilterComboBox.SelectedIndex > 0
                ? SecondaryDomainFilterComboBox.SelectedItem?.ToString()
                : null;

        var selectedState =
            SecondaryStateFilterComboBox.SelectedIndex > 0
                ? SecondaryStateFilterComboBox.SelectedItem?.ToString()
                : null;

        var resources =
            _state.Resources
                .Where(resource =>
                    InfrastructureResourceSearch.Matches(
                        resource,
                        search))
                .Where(resource =>
                    selectedDomain is null ||
                    string.Equals(
                        resource.DomainId,
                        selectedDomain,
                        StringComparison.OrdinalIgnoreCase))
                .Where(resource =>
                    selectedState is null ||
                    string.Equals(
                        string.IsNullOrWhiteSpace(resource.State)
                            ? "Observed"
                            : resource.State,
                        selectedState,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(resource =>
                    resource.DomainId)
                .ThenBy(resource =>
                    resource.ResourceType)
                .ThenBy(resource =>
                    resource.DisplayName)
                .ToList();

        SecondaryWorkspaceInventoryHeaderContent.Content =
            CreateResourceInventoryHeader().Content;

        foreach (var resource in SortResourceRows(
                     resources,
                     _secondarySortColumn,
                     _secondarySortDescending,
                     false))
        {
            SecondaryWorkspaceList.Items.Add(
                CreateResourceInventoryRow(resource));
        }

        SecondaryWorkspaceDescription.Text =
            $"{resources.Count} of " +
            $"{_state.TotalResourceCount} resources";
    }

    private ListBoxItem CreateOperationsSearchRow(
        WorkbenchOperationsSearchMatch match)
    {
        var resource =
            match.Resource;

        var panel =
            new StackPanel
            {
                Spacing = 3
            };

        panel.Children.Add(
            new TextBlock
            {
                Text =
                    match.DisplayText,

                FontWeight =
                    FontWeight.SemiBold,

                Foreground =
                    Brush("#172B4D")
            });

        var context =
            match.Kind ==
            WorkbenchOperationsSearchMatchKind.Capability
                ? resource is null
                    ? "Capability"
                    : $"Capability · {resource.DisplayName}"
                : resource is null
                    ? match.Kind.ToString()
                    : $"{match.Kind} · " +
                      $"{resource.ResourceType} · " +
                      $"{resource.State}";

        panel.Children.Add(
            new TextBlock
            {
                Text =
                    context,

                FontSize =
                    10,

                Foreground =
                    Brush("#526581")
            });

        return new ListBoxItem
        {
            Content =
                panel,

            Tag =
                resource,

            Padding =
                new Thickness(8)
        };
    }

    private void RenderOperationsSearch(
        string? searchText)
    {
        SecondaryWorkspaceList.Items.Clear();

        if (_state is null)
        {
            SecondaryWorkspaceDescription.Text =
                "No infrastructure state is loaded.";

            return;
        }

        var results =
            WorkbenchOperationsSearch.Search(
                _state.Resources,
                searchText);

        foreach (var result in results)
        {
            SecondaryWorkspaceList.Items.Add(
                CreateOperationsSearchRow(
                    result));
        }

        SecondaryWorkspaceDescription.Text =
            string.IsNullOrWhiteSpace(searchText)
                ? results.Count == 1
                    ? "1 searchable resource"
                    : $"{results.Count} searchable resources"
                : results.Count == 1
                    ? "1 operations search result"
                    : $"{results.Count} operations search results";
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

    private async Task SaveCurrentTopologyViewAsync()
    {
        var dialog =
            new TruthDoctor.Views.Dialogs
                .TopologySavedViewNameDialog(
                    "Save topology view",
                    "Enter a unique name for the current topology workspace.",
                    "Save");

        var name =
            await dialog.ShowDialog<string?>(
                this);

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            var timestamp =
                DateTimeOffset.UtcNow;

            var view =
                TopologyWorkspace.CaptureSavedView(
                    Guid.NewGuid().ToString("N"),
                    name,
                    timestamp,
                    timestamp);

            _savedViews.Add(
                view);

            UpdateSavedViewControls(
                view.Id,
                $"Saved '{view.Name}'.");
        }
        catch (Exception exception)
        {
            UpdateSavedViewControls(
                message:
                    $"Unable to save view: {exception.Message}",
                isError: true);
        }
    }

    private async Task DeleteTopologySavedViewAsync(
        string savedViewId)
    {
        if (string.IsNullOrWhiteSpace(
                savedViewId))
        {
            UpdateSavedViewControls(
                message:
                    "Select a saved view before deleting.",
                isError: true);

            return;
        }

        var normalizedId =
            savedViewId.Trim();

        var existing =
            _savedViews.Find(
                normalizedId);

        if (existing is null)
        {
            UpdateSavedViewControls(
                message:
                    "The selected saved view is no longer available.",
                isError: true);

            return;
        }

        var dialog =
            new ConfirmDialog(
                $"Delete saved view '{existing.Name}'?\n\n" +
                "The displayed topology will remain open.");

        await dialog.ShowDialog(
            this);

        if (!dialog.Result)
        {
            UpdateSavedViewControls(
                existing.Id,
                $"Deletion cancelled for '{existing.Name}'.");

            return;
        }

        try
        {
            var deleted =
                _savedViews.Delete(
                    existing.Id);

            if (!deleted)
            {
                UpdateSavedViewControls(
                    message:
                        "The selected saved view is no longer available.",
                    isError: true);

                return;
            }

            UpdateSavedViewControls(
                message:
                    $"Deleted saved view '{existing.Name}'. " +
                    "Displayed topology remains open.");
        }
        catch (Exception exception)
        {
            UpdateSavedViewControls(
                existing.Id,
                $"Unable to delete view: {exception.Message}",
                isError: true);
        }
    }

    private async Task RenameTopologySavedViewAsync(
        string savedViewId)
    {
        if (string.IsNullOrWhiteSpace(
                savedViewId))
        {
            UpdateSavedViewControls(
                message:
                    "Select a saved view before renaming.",
                isError: true);

            return;
        }

        var normalizedId =
            savedViewId.Trim();

        var existing =
            _savedViews.Find(
                normalizedId);

        if (existing is null)
        {
            UpdateSavedViewControls(
                message:
                    "The selected saved view is no longer available.",
                isError: true);

            return;
        }

        var dialog =
            new TruthDoctor.Views.Dialogs
                .TopologySavedViewNameDialog(
                    "Rename topology view",
                    "Enter a new unique name for this saved topology view.",
                    "Rename",
                    existing.Name);

        var newName =
            await dialog.ShowDialog<string?>(
                this);

        if (string.IsNullOrWhiteSpace(
                newName))
        {
            return;
        }

        try
        {
            var renamed =
                _savedViews.Rename(
                    existing.Id,
                    newName,
                    DateTimeOffset.UtcNow);

            UpdateSavedViewControls(
                renamed.Id,
                $"Renamed saved view to '{renamed.Name}'.");
        }
        catch (Exception exception)
        {
            UpdateSavedViewControls(
                existing.Id,
                $"Unable to rename view: {exception.Message}",
                isError: true);
        }
    }

    private void LoadTopologySavedView(
        string savedViewId)
    {
        if (string.IsNullOrWhiteSpace(
                savedViewId))
        {
            UpdateSavedViewControls(
                message:
                    "Select a saved view before loading.",
                isError: true);

            return;
        }

        var normalizedId =
            savedViewId.Trim();

        var view =
            _savedViews.Find(
                normalizedId);

        if (view is null)
        {
            UpdateSavedViewControls(
                message:
                    "The selected saved view is no longer available.",
                isError: true);

            return;
        }

        try
        {
            var summary =
                RestoreTopologySavedView(
                    view);

            UpdateSavedViewControls(
                view.Id,
                BuildSavedViewLoadMessage(
                    view,
                    summary));
        }
        catch (Exception exception)
        {
            UpdateSavedViewControls(
                view.Id,
                $"Unable to load view: {exception.Message}",
                isError: true);
        }
    }

    private static string BuildSavedViewLoadMessage(
        TopologySavedView view,
        TopologySavedViewWorkbenchRestoreSummary summary)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(summary);

        if (!summary.UsedFallback)
        {
            return $"Loaded '{view.Name}'.";
        }

        var adjustments =
            new List<string>();

        if (summary.ResourceWasUnavailable)
        {
            adjustments.Add(
                "selected resource was unavailable");
        }

        if (summary.DomainFellBackToAll)
        {
            adjustments.Add(
                "domain changed to All domains");
        }

        if (summary.IgnoredCollapsedDomainCount > 0)
        {
            var count =
                summary.IgnoredCollapsedDomainCount;

            adjustments.Add(
                count == 1
                    ? "1 unavailable collapsed domain was ignored"
                    : $"{count} unavailable collapsed domains were ignored");
        }

        return
            $"Loaded '{view.Name}' with adjustments: " +
            $"{string.Join("; ", adjustments)}.";
    }

    private void UpdateSavedViewControls(
        string? selectedViewId = null,
        string? message = null,
        bool isError = false)
    {
        TopologyWorkspace.SetSavedViews(
            _savedViews.All,
            selectedViewId);

        UpdateSavedViewStatus(
            message,
            isError);
    }

    private void UpdateSavedViewStatus(
        string? message = null,
        bool isError = false)
    {
        var count =
            _savedViews.All.Count;

        var status =
            message ??
            (
                count == 0
                    ? "No saved views"
                    : count == 1
                        ? "1 saved view"
                        : $"{count} saved views"
            );

        TopologyWorkspace.SetSavedViewStatus(
            count,
            status,
            isError);
    }

    public IReadOnlyList<TopologySavedView>
        SavedTopologyViews =>
            _savedViews.All;

    public TopologySavedViewStoreLoadResult
        SavedTopologyViewLoadResult =>
            _savedViewLoadResult;

    public string TopologySavedViewStoragePath =>
        new TopologySavedViewStoragePathResolver()
            .FilePath;


    public TopologySavedViewWorkbenchRestoreSummary
        RestoreTopologySavedView(
            TopologySavedView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        var summary =
            new WorkbenchSavedViewRestoreCoordinator(
                _workbenchState,
                _topology)
            .Restore(view);

        // Saved-view restoration establishes its own Topology
        // session. Synchronize the remembered selection without
        // requesting another history reset on workspace return.
        _crossWorkspaceNavigation
            .SynchronizeRestoredTopologySession(
                _workbenchState.SelectedResource);

        TopologyWorkspace
            .PrepareSavedViewVisualState(
                summary.Plan);

        RenderTopologySelection();

        return summary;
    }

    private bool _showFullInfrastructureTopology;

    private void RenderTopologySelection()
    {
        _details.RenderSelectedResource(
            ResourceDetailsPanel,
            AssistantStatusText);

        TopologyWorkspace.Render(
            _showFullInfrastructureTopology
                ? _graphContext.BuildFullInfrastructureTopology()
                : _topology.Current);

        TopologyWorkspace.SetNavigationState(
            _topology.CanGoBack,
            _topology.CanGoForward,
            _topology.CanGoHome);
    }

    private void NavigateToWorkspace(
        string view)
    {
        if (_workbenchState.CurrentView.Equals(
                "topology",
                StringComparison.OrdinalIgnoreCase))
        {
            // Capture topology-local selection changes before leaving.
            _crossWorkspaceNavigation.EnterWorkspace(
                "topology",
                _workbenchState.SelectedResource);
        }

        var beginTopologySession =
            _crossWorkspaceNavigation.EnterWorkspace(
                view,
                _workbenchState.SelectedResource);

        SetActivePrimaryNavigation(view);

        if (view.Equals(
                "topology",
                StringComparison.OrdinalIgnoreCase))
        {
            if (beginTopologySession)
            {
                _topology.BeginSession();
            }

            RenderTopologySelection();
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

        SecondaryWorkspaceResourcesFilters.IsVisible =
            isResources;

        SecondaryWorkspaceInventoryHeader.IsVisible =
            isResources;

        if (isResources)
        {
            RenderSecondaryResources();
        }

        RouteGlobalSearch(
            _globalSearchText);
    }

    private void RouteGlobalSearch(
        string search)
    {
        var route =
            WorkbenchGlobalSearchCoordinator.Resolve(
                _workbenchState.CurrentView);

        switch (route)
        {
            case WorkbenchGlobalSearchRoute.Dashboard:

                ResourceSearchTextBox.Text = search;
                ApplyFilters();
                return;

            case WorkbenchGlobalSearchRoute.Resources:

                SecondaryWorkspaceSearchTextBox.Text =
                    search;

                return;

            case WorkbenchGlobalSearchRoute.Topology:

                var resources =
                    _workbenchState.PlatformState?.Resources;

                if (resources is null ||
                    resources.Count == 0)
                {
                    TopologyWorkspace.SetSearchText(
                        search);

                    return;
                }

                var resource =
                    InfrastructureResourceSearchResolver
                        .ResolveUnique(
                            resources,
                            search);

                if (resource is not null)
                {
                    _topology.NavigateTo(resource);

                    RenderTopologySelection();
                }

                TopologyWorkspace.SetSearchText(
                    search);

                return;

            case WorkbenchGlobalSearchRoute.Operations:

                RenderOperationsSearch(
                    search);

                return;

            case WorkbenchGlobalSearchRoute.History:
            case WorkbenchGlobalSearchRoute.Reports:
            case WorkbenchGlobalSearchRoute.Settings:
            case WorkbenchGlobalSearchRoute.Unknown:
            default:
                return;
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
