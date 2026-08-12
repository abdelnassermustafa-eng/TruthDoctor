using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using TruthDoctor.Graph;
using TruthDoctor.Services.Visuals;

namespace TruthDoctor.Controls.Topology;

public partial class TopologyCanvas : UserControl
{
    private readonly RenderRegistry _renderRegistry =
        new([
            new AwsRenderContributor()
        ]);

    private readonly TopologyLayoutEngine _layoutEngine =
        new();

    private readonly TopologyDomainFilter _domainFilter =
        new();

    private readonly TopologyGroupCollapseEngine
        _groupCollapseEngine =
            new();

    private readonly TopologyGroupBoundsEngine
        _groupBoundsEngine =
            new();

    private readonly TopologyMinimapMapper _minimapMapper =
        new();

    private TopologyLayoutMode _layoutMode =
        TopologyLayoutMode.Radial;

    private const double CanvasWidth = 1600;
    private const double CanvasHeight = 1000;

    private const double MinimapWidth = 228;
    private const double MinimapHeight = 140;

    private const double TopologyNodeWidth = 160;
    private const double TopologyNodeHeight = 70;

    private const double MinimumZoom = 0.35;
    private const double MaximumZoom = 2.00;
    private const double ZoomStep = 0.15;

    private double _zoom = 1.00;

    private TopologyView _completeTopology =
        new();

    private TopologyView _domainTopology =
        new();

    private TopologyView _currentTopology =
        new();

    private readonly HashSet<string>
        _collapsedDomainIds =
            new(
                StringComparer.OrdinalIgnoreCase);

    private string _selectedDomainId =
        TopologyDomainFilter.AllDomains;

    private bool _isUpdatingDomainSelector;

    private bool _isRestoringSavedView;

    private long _savedViewRestoreVersion;

    private bool _isPanning;
    private Point _panStart;
    private Vector _panStartOffset;

    private bool _isMinimapPanning;

    private bool _isMinimapNavigationAvailable;

    private string _searchText = "";

    private List<TopologyNode> _searchMatches =
        [];

    private int _activeSearchMatchIndex = -1;

    private IReadOnlyDictionary<string, Point> _nodePositions =
        new Dictionary<string, Point>(
            StringComparer.OrdinalIgnoreCase);

    private bool _isRelationshipFocusEnabled;

    private bool _isPathSelectionMode;

    private string _pathSourceId = "";

    private GraphPathResult? _activePath;

    public event Action<TopologyNode>? NodeInvoked;

    public event Action? BackRequested;

    public event Action? ForwardRequested;

    public event Action? HomeRequested;

    public event Action<int>? DepthChanged;

    public event Action<string, string>? PathRequested;

    public TopologyCanvas()
    {
        InitializeComponent();

        TopologySurface.PointerWheelChanged +=
            TopologySurface_OnPointerWheelChanged;

        TopologySurface.PointerPressed +=
            TopologySurface_OnPointerPressed;

        TopologySurface.PointerMoved +=
            TopologySurface_OnPointerMoved;

        TopologySurface.PointerReleased +=
            TopologySurface_OnPointerReleased;

        TopologySurface.PointerCaptureLost +=
            TopologySurface_OnPointerCaptureLost;

        TopologyMinimapSurface.PointerPressed +=
            TopologyMinimapSurface_OnPointerPressed;

        TopologyMinimapSurface.PointerMoved +=
            TopologyMinimapSurface_OnPointerMoved;

        TopologyMinimapSurface.PointerReleased +=
            TopologyMinimapSurface_OnPointerReleased;

        TopologyMinimapSurface.PointerCaptureLost +=
            TopologyMinimapSurface_OnPointerCaptureLost;

        TopologyScrollViewer.ScrollChanged +=
            (_, _) =>
                UpdateMinimapViewport();

        TopologyScrollViewer.SizeChanged +=
            (_, _) =>
                UpdateMinimapViewport();

        TopologySearchTextBox.TextChanged +=
            TopologySearchTextBox_OnTextChanged;

        TopologySearchTextBox.KeyDown +=
            TopologySearchTextBox_OnKeyDown;

        TopologyLayoutComboBox.SelectionChanged +=
            TopologyLayoutComboBox_OnSelectionChanged;

        TopologyDomainComboBox.SelectionChanged +=
            TopologyDomainComboBox_OnSelectionChanged;

        ApplyZoom();
        UpdateSearchControls();
    }

    private void TopologyZoomOutButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        SetZoom(
            _zoom - ZoomStep);
    }

    private void TopologyZoomInButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        SetZoom(
            _zoom + ZoomStep);
    }

    private void TopologyResetZoomButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        SetZoom(1.00);
    }

    private void TopologyFitButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        FitTopologyToViewport();
    }

    private void TopologyArrangeButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        ArrangeCurrentTopology();
    }

    private void RefreshDomainSelector()
    {
        var domains =
            _domainFilter.AvailableDomains(
                _completeTopology);

        var selectedStillExists =
            string.IsNullOrWhiteSpace(
                _selectedDomainId) ||
            domains.Any(domain =>
                domain.Id.Equals(
                    _selectedDomainId,
                    StringComparison.OrdinalIgnoreCase));

        if (!selectedStillExists)
        {
            _selectedDomainId =
                TopologyDomainFilter.AllDomains;
        }

        _isUpdatingDomainSelector =
            true;

        try
        {
            TopologyDomainComboBox.Items.Clear();

            TopologyDomainComboBox.Items.Add(
                new ComboBoxItem
                {
                    Content =
                        $"All domains · " +
                        $"{_completeTopology.Nodes.Count}",

                    Tag =
                        TopologyDomainFilter.AllDomains
                });

            foreach (var domain in domains)
            {
                TopologyDomainComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content =
                            $"{domain.DisplayName} · " +
                            $"{domain.Count}",

                        Tag =
                            domain.Id
                    });
            }

            var selectedIndex = 0;

            for (var index = 1;
                 index <
                 TopologyDomainComboBox.Items.Count;
                 index++)
            {
                if (TopologyDomainComboBox.Items[index] is
                        ComboBoxItem item &&
                    string.Equals(
                        item.Tag?.ToString(),
                        _selectedDomainId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex =
                        index;

                    break;
                }
            }

            TopologyDomainComboBox.SelectedIndex =
                selectedIndex;

            TopologyDomainComboBox.IsEnabled =
                domains.Count > 0;
        }
        finally
        {
            _isUpdatingDomainSelector =
                false;
        }
    }

    private void ApplySelectedDomain()
    {
        _domainTopology =
            _domainFilter.Apply(
                _completeTopology,
                _selectedDomainId);

        var availableDomainIds =
            _domainFilter
                .AvailableDomains(
                    _domainTopology)
                .Select(group =>
                    group.Id)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        _collapsedDomainIds.IntersectWith(
            availableDomainIds);

        _currentTopology =
            _groupCollapseEngine.Project(
                _domainTopology,
                _collapsedDomainIds);
    }

    private TopologyGroup? FindDomainGroup(
        string domainId)
    {
        return _domainFilter
            .AvailableDomains(
                _domainTopology)
            .FirstOrDefault(group =>
                group.Id.Equals(
                    domainId,
                    StringComparison.OrdinalIgnoreCase));
    }

    private bool IsDomainCollapsedInProjection(
        string domainId)
    {
        return _currentTopology.Nodes.Any(node =>
            TopologyGroupCollapseEngine
                .IsSummaryNode(node) &&
            node.DomainId.Equals(
                domainId,
                StringComparison.OrdinalIgnoreCase));
    }

    private void ToggleDomainGroup(
        string domainId)
    {
        if (!_collapsedDomainIds.Remove(
                domainId))
        {
            _collapsedDomainIds.Add(
                domainId);
        }

        ApplySelectedDomain();
        ResetStateForDomainChange();

        RefreshSearchMatches(
            preserveActiveMatch: false);

        TopologyStartPathButton.IsEnabled =
            CanStartPathFromCurrentSelection();

        RenderCurrentTopology();
        FitTopologyToViewport();

        TopologyScrollViewer.Offset =
            new Vector(0, 0);

        var group =
            FindDomainGroup(domainId);

        var action =
            IsDomainCollapsedInProjection(
                domainId)
                ? "Collapsed"
                : "Expanded";

        SetExportStatus(
            $"{action}: " +
            $"{group?.DisplayName ?? domainId}");
    }

    private void ExpandSummaryNode(
        TopologyNode node)
    {
        if (!TopologyGroupCollapseEngine
                .IsSummaryNode(node))
        {
            return;
        }

        _collapsedDomainIds.Remove(
            node.DomainId);

        ApplySelectedDomain();
        ResetStateForDomainChange();

        RefreshSearchMatches(
            preserveActiveMatch: false);

        TopologyStartPathButton.IsEnabled =
            CanStartPathFromCurrentSelection();

        RenderCurrentTopology();
        FitTopologyToViewport();

        TopologyScrollViewer.Offset =
            new Vector(0, 0);

        SetExportStatus(
            $"Expanded: {node.DomainId}");
    }

    private void TopologyDomainComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs eventArgs)
    {
        if (_isUpdatingDomainSelector ||
            sender is not ComboBox comboBox ||
            comboBox.SelectedItem is not
                ComboBoxItem item)
        {
            return;
        }

        var selectedDomainId =
            item.Tag?.ToString() ??
            TopologyDomainFilter.AllDomains;

        if (selectedDomainId.Equals(
                _selectedDomainId,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _selectedDomainId =
            selectedDomainId;

        ApplySelectedDomain();
        ResetStateForDomainChange();

        RefreshSearchMatches(
            preserveActiveMatch: false);

        TopologyStartPathButton.IsEnabled =
            CanStartPathFromCurrentSelection();

        RenderCurrentTopology();
        FitTopologyToViewport();

        TopologyScrollViewer.Offset =
            new Vector(0, 0);

    }

    private void ResetStateForDomainChange()
    {
        _isRelationshipFocusEnabled =
            false;

        _isPathSelectionMode =
            false;

        _pathSourceId =
            "";

        _activePath =
            null;

        ClearPathDetails();

        TopologyStartPathButton.Content =
            "Start path";

        TopologyClearPathButton.IsEnabled =
            false;

        TopologyPathStatusText.Text =
            "Center a source resource, then start a path";
    }

    private void TopologyLayoutComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs eventArgs)
    {
        if (_isRestoringSavedView ||
            sender is not ComboBox comboBox ||
            comboBox.SelectedItem is not
                ComboBoxItem item ||
            !Enum.TryParse<TopologyLayoutMode>(
                item.Tag?.ToString(),
                ignoreCase: true,
                out var mode))
        {
            return;
        }

        _layoutMode =
            mode;

        ArrangeCurrentTopology();
    }

    private void ArrangeCurrentTopology()
    {
        if (_currentTopology.Nodes.Count == 0)
        {
            return;
        }

        RenderCurrentTopology();
        FitTopologyToViewport();

        TopologyScrollViewer.Offset =
            new Vector(0, 0);

        SetExportStatus(
            $"Layout: {LayoutDisplayName()}");
    }

    private void FitTopologyToViewport()
    {
        var availableWidth =
            Math.Max(
                1,
                TopologyScrollViewer.Bounds.Width - 32);

        var availableHeight =
            Math.Max(
                1,
                TopologyScrollViewer.Bounds.Height - 32);

        var fitZoom =
            Math.Min(
                availableWidth / CanvasWidth,
                availableHeight / CanvasHeight);

        SetZoom(
            fitZoom);
    }

    private string LayoutDisplayName()
    {
        return _layoutMode switch
        {
            TopologyLayoutMode.Hierarchical =>
                "Hierarchy",

            TopologyLayoutMode.Network =>
                "Network",

            TopologyLayoutMode.Domain =>
                "Domains",

            _ =>
                "Radial"
        };
    }

    private async void TopologyExportPngMenuItem_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        await ExportPngAsync();
    }

    private async void TopologyExportJsonMenuItem_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        await ExportJsonAsync();
    }

    private async Task ExportPngAsync()
    {
        if (_currentTopology.Nodes.Count == 0)
        {
            SetExportStatus(
                "Nothing to export.",
                isError: true);

            return;
        }

        var file =
            await RequestExportFileAsync(
                "Save topology snapshot",
                "png",
                new FilePickerFileType(
                    "PNG image")
                {
                    Patterns = ["*.png"],
                    MimeTypes = ["image/png"],
                    AppleUniformTypeIdentifiers =
                        ["public.png"]
                });

        if (file is null)
        {
            SetExportStatus(
                "PNG export cancelled.");

            return;
        }

        TopologyExportButton.IsEnabled = false;

        try
        {
            var previousTransform =
                RootCanvas.RenderTransform;

            try
            {
                RootCanvas.RenderTransform = null;

                RootCanvas.Measure(
                    new Size(
                        CanvasWidth,
                        CanvasHeight));

                RootCanvas.Arrange(
                    new Rect(
                        0,
                        0,
                        CanvasWidth,
                        CanvasHeight));

                using var bitmap =
                    new RenderTargetBitmap(
                        new PixelSize(
                            (int)CanvasWidth,
                            (int)CanvasHeight),
                        new Vector(96, 96));

                bitmap.Render(
                    RootCanvas);

                await using var stream =
                    await file.OpenWriteAsync();

                bitmap.Save(stream);
            }
            finally
            {
                RootCanvas.RenderTransform =
                    previousTransform;

                ApplyZoom();
            }

            SetExportStatus(
                $"Saved {file.Name}");
        }
        catch (Exception exception)
        {
            SetExportStatus(
                $"PNG export failed: " +
                $"{exception.Message}",
                isError: true);
        }
        finally
        {
            TopologyExportButton.IsEnabled = true;
        }
    }

    private async Task ExportJsonAsync()
    {
        if (_currentTopology.Nodes.Count == 0)
        {
            SetExportStatus(
                "Nothing to export.",
                isError: true);

            return;
        }

        var file =
            await RequestExportFileAsync(
                "Save topology data",
                "json",
                new FilePickerFileType(
                    "JSON document")
                {
                    Patterns = ["*.json"],
                    MimeTypes =
                        ["application/json"],
                    AppleUniformTypeIdentifiers =
                        ["public.json"]
                });

        if (file is null)
        {
            SetExportStatus(
                "JSON export cancelled.");

            return;
        }

        TopologyExportButton.IsEnabled = false;

        try
        {
            var visibleEdges =
                CurrentVisibleEdges();

            var exportDocument =
                new
                {
                    schema =
                        "truthdoctor.topology.v1",

                    exportedAtUtc =
                        DateTimeOffset.UtcNow,

                    projection =
                        new
                        {
                            selectedResourceId =
                                _currentTopology
                                    .SelectedResourceId,

                            depth =
                                CurrentTopologyDepth(),

                            layout =
                                _layoutMode
                                    .ToString()
                                    .ToLowerInvariant(),

                            domain =
                                string.IsNullOrWhiteSpace(
                                    _selectedDomainId)
                                    ? "all"
                                    : _selectedDomainId,

                            collapsedDomains =
                                _collapsedDomainIds
                                    .OrderBy(domainId =>
                                        domainId,
                                        StringComparer
                                            .OrdinalIgnoreCase)
                                    .ToList(),

                            zoom =
                                _zoom,

                            nodeCount =
                                _currentTopology
                                    .Nodes.Count,

                            visibleEdgeCount =
                                visibleEdges.Count,

                            totalProjectedEdgeCount =
                                _currentTopology
                                    .Edges.Count,

                            relationshipFilters =
                                CurrentRelationshipFilters(),

                            relationshipFocusEnabled =
                                HasRelationshipFocus,

                            focusedResourceId =
                                HasRelationshipFocus
                                    ? FocusedNodeId
                                    : null
                        },

                    nodes =
                        _currentTopology.Nodes
                            .Select(node =>
                                new
                                {
                                    node.Id,
                                    node.ProviderId,
                                    node.AccountId,
                                    node.DomainId,
                                    node.ResourceType,
                                    node.DisplayName,
                                    node.NativeId,
                                    node.State,
                                    node.Location,
                                    node.AvailabilityZone,
                                    node.Arn,
                                    node.Properties,
                                    node.Tags,
                                    node.IsSelected,

                                    isPathNode =
                                        IsActivePathNode(
                                            node),

                                    pathRole =
                                        PathRole(node)
                                })
                            .ToList(),

                    relationships =
                        visibleEdges
                            .Select(edge =>
                                new
                                {
                                    edge.SourceId,
                                    edge.TargetId,
                                    edge.Relationship,

                                    kind =
                                        edge.Kind.ToString(),

                                    edge.Multiplicity,

                                    isPathEdge =
                                        IsActivePathEdge(
                                            edge),

                                    isFocusedEdge =
                                        IsFocusedRelationshipEdge(
                                            edge)
                                })
                            .ToList(),

                    activePath =
                        BuildPathExport()
                };

            var bytes =
                JsonSerializer.SerializeToUtf8Bytes(
                    exportDocument,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            await using var stream =
                await file.OpenWriteAsync();

            await stream.WriteAsync(bytes);

            SetExportStatus(
                $"Saved {file.Name}");
        }
        catch (Exception exception)
        {
            SetExportStatus(
                $"JSON export failed: " +
                $"{exception.Message}",
                isError: true);
        }
        finally
        {
            TopologyExportButton.IsEnabled = true;
        }
    }

    private async Task<IStorageFile?>
        RequestExportFileAsync(
            string title,
            string extension,
            FilePickerFileType fileType)
    {
        var topLevel =
            TopLevel.GetTopLevel(this);

        if (topLevel is null ||
            !topLevel.StorageProvider.CanSave)
        {
            SetExportStatus(
                "Save-location selection is unavailable.",
                isError: true);

            return null;
        }

        try
        {
            return await topLevel
                .StorageProvider
                .SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = title,

                        SuggestedFileName =
                            $"truthdoctor-topology-" +
                            $"{DateTime.Now:yyyyMMdd-HHmmss}." +
                            extension,

                        DefaultExtension =
                            extension,

                        FileTypeChoices =
                            [fileType]
                    });
        }
        catch (Exception exception)
        {
            SetExportStatus(
                $"Save dialog failed: " +
                $"{exception.Message}",
                isError: true);

            return null;
        }
    }

    private List<TopologyEdge>
        CurrentVisibleEdges()
    {
        return _currentTopology.Edges
            .Where(edge =>
                IsRelationshipVisible(
                    edge.Kind) ||
                IsActivePathEdge(edge))
            .ToList();
    }

    private int CurrentTopologyDepth()
    {
        if (TopologyDepthComboBox.SelectedItem is
                ComboBoxItem item &&
            int.TryParse(
                item.Tag?.ToString(),
                out var depth))
        {
            return depth;
        }

        return 1;
    }

    private object CurrentRelationshipFilters()
    {
        return new
        {
            all =
                AllRelationshipFilter.IsChecked ==
                true,

            containment =
                ContainmentFilter.IsChecked ==
                true,

            placement =
                PlacementFilter.IsChecked ==
                true,

            dependency =
                DependencyFilter.IsChecked ==
                true,

            connectivity =
                ConnectivityFilter.IsChecked ==
                true,

            security =
                SecurityFilter.IsChecked ==
                true,

            traffic =
                TrafficFilter.IsChecked ==
                true,

            association =
                AssociationFilter.IsChecked ==
                true,

            other =
                OtherFilter.IsChecked ==
                true
        };
    }

    private object? BuildPathExport()
    {
        if (!HasActivePath)
        {
            return null;
        }

        return new
        {
            _activePath!.SourceId,
            _activePath.TargetId,
            _activePath.HopCount,

            nodes =
                _activePath.Nodes
                    .Select(node =>
                        new
                        {
                            node.Id,
                            node.DisplayName,
                            node.ResourceType
                        })
                    .ToList(),

            relationships =
                _activePath.Edges
                    .Select(edge =>
                        new
                        {
                            edge.SourceId,
                            edge.TargetId,
                            edge.Relationship
                        })
                    .ToList()
        };
    }

    private string? PathRole(
        TopologyNode node)
    {
        if (!HasActivePath ||
            !IsActivePathNode(node))
        {
            return null;
        }

        if (IsPathStartNode(node))
        {
            return "start";
        }

        if (IsPathEndNode(node))
        {
            return "end";
        }

        return $"hop-{PathNodePosition(node)}";
    }

    private void SetExportStatus(
        string message,
        bool isError = false)
    {
        TopologyExportStatusText.Text =
            message;

        TopologyExportStatusText.Foreground =
            new SolidColorBrush(
                Color.Parse(
                    isError
                        ? "#FCA5A5"
                        : "#86EFAC"));
    }


    private void TopologyHideMinimapButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        TopologyMinimapPanel.IsVisible =
            false;

        TopologyShowMinimapButton.IsVisible =
            true;
    }

    private void TopologyShowMinimapButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        TopologyMinimapPanel.IsVisible =
            true;

        TopologyShowMinimapButton.IsVisible =
            false;

        RenderMinimap();
    }

    private void RenderMinimap()
    {
        TopologyMinimapContent.Children.Clear();

        if (_currentTopology.Nodes.Count == 0)
        {
            UpdateMinimapViewport();
            return;
        }

        var scaleX =
            MinimapWidth / CanvasWidth;

        var scaleY =
            MinimapHeight / CanvasHeight;

        RenderMinimapDomainGroups(
            _nodePositions,
            scaleX,
            scaleY);

        var visibleEdges =
            CurrentVisibleEdges();

        foreach (var edge in visibleEdges)
        {
            if (!_nodePositions.TryGetValue(
                    edge.SourceId,
                    out var source) ||
                !_nodePositions.TryGetValue(
                    edge.TargetId,
                    out var target))
            {
                continue;
            }

            var isPathEdge =
                IsActivePathEdge(edge);

            var isFocusedEdge =
                IsFocusedRelationshipEdge(edge);

            var line =
                new Line
                {
                    StartPoint =
                        new Point(
                            (
                                source.X +
                                TopologyNodeWidth / 2
                            ) *
                            scaleX,
                            (
                                source.Y +
                                TopologyNodeHeight / 2
                            ) *
                            scaleY),

                    EndPoint =
                        new Point(
                            (
                                target.X +
                                TopologyNodeWidth / 2
                            ) *
                            scaleX,
                            (
                                target.Y +
                                TopologyNodeHeight / 2
                            ) *
                            scaleY),

                    Stroke =
                        new SolidColorBrush(
                            Color.Parse(
                                isPathEdge
                                    ? "#FACC15"
                                    : isFocusedEdge
                                        ? "#A78BFA"
                                        : "#64748B")),

                    StrokeThickness =
                        isPathEdge ||
                        isFocusedEdge
                            ? 2
                            : 0.8,

                    Opacity =
                        HasActivePath
                            ? (
                                isPathEdge
                                    ? 1
                                    : 0.16
                            )
                            : HasRelationshipFocus
                                ? (
                                    isFocusedEdge
                                        ? 1
                                        : 0.16
                                )
                                : 0.72,

                    IsHitTestVisible =
                        false
                };

            TopologyMinimapContent.Children.Add(
                line);
        }

        foreach (var node in
                 _currentTopology.Nodes)
        {
            if (!_nodePositions.TryGetValue(
                    node.Id,
                    out var position))
            {
                continue;
            }

            var isSelected =
                node.Id.Equals(
                    _currentTopology.SelectedResourceId,
                    StringComparison.OrdinalIgnoreCase);

            var isPathNode =
                IsActivePathNode(node);

            var isDirect =
                IsDirectFocusNeighbor(node);

            var miniature =
                new Rectangle
                {
                    Width =
                        Math.Max(
                            5,
                            TopologyNodeWidth *
                            scaleX),

                    Height =
                        Math.Max(
                            4,
                            TopologyNodeHeight *
                            scaleY),

                    RadiusX = 2,
                    RadiusY = 2,

                    Fill =
                        new SolidColorBrush(
                            Color.Parse("#49358A")),

                    Stroke =
                        new SolidColorBrush(
                            Color.Parse(
                                isPathNode
                                    ? "#FACC15"
                                    : isSelected
                                        ? "#FFFFFF"
                                        : isDirect
                                            ? "#22D3EE"
                                            : "#8B5CF6")),

                    StrokeThickness =
                        isPathNode ||
                        isSelected
                            ? 1.8
                            : 0.8,

                    Opacity =
                        ShouldKeepNodeVivid(node)
                            ? 0.96
                            : 0.20,

                    IsHitTestVisible =
                        false
                };

            Canvas.SetLeft(
                miniature,
                position.X * scaleX);

            Canvas.SetTop(
                miniature,
                position.Y * scaleY);

            TopologyMinimapContent.Children.Add(
                miniature);
        }

        UpdateMinimapViewport();
    }

    private void UpdateMinimapViewport()
    {
        if (TopologyMinimapViewport is null ||
            TopologyScrollViewer is null)
        {
            return;
        }

        var viewportWidth =
            Math.Max(
                1,
                TopologyScrollViewer.Bounds.Width -
                24);

        var viewportHeight =
            Math.Max(
                1,
                TopologyScrollViewer.Bounds.Height -
                24);

        var projection =
            _minimapMapper.ProjectViewport(
                CanvasWidth,
                CanvasHeight,
                _zoom,
                viewportWidth,
                viewportHeight,
                TopologyScrollViewer.Offset.X,
                TopologyScrollViewer.Offset.Y,
                MinimapWidth,
                MinimapHeight);

        _isMinimapNavigationAvailable =
            CanvasWidth * _zoom >
                viewportWidth + 1 ||
            CanvasHeight * _zoom >
                viewportHeight + 1;

        var viewportRectangleWidth =
            Math.Max(
                4,
                projection.Width - 2);

        var viewportRectangleHeight =
            Math.Max(
                4,
                projection.Height - 2);

        var viewportRectangleX =
            Math.Clamp(
                projection.X + 1,
                1,
                Math.Max(
                    1,
                    MinimapWidth -
                    viewportRectangleWidth -
                    1));

        var viewportRectangleY =
            Math.Clamp(
                projection.Y + 1,
                1,
                Math.Max(
                    1,
                    MinimapHeight -
                    viewportRectangleHeight -
                    1));

        TopologyMinimapViewport.Width =
            viewportRectangleWidth;

        TopologyMinimapViewport.Height =
            viewportRectangleHeight;

        Canvas.SetLeft(
            TopologyMinimapViewport,
            viewportRectangleX);

        Canvas.SetTop(
            TopologyMinimapViewport,
            viewportRectangleY);

        TopologyMinimapSurface.Cursor =
            new Cursor(
                !_isMinimapNavigationAvailable
                    ? StandardCursorType.Arrow
                    : _isMinimapPanning
                        ? StandardCursorType.SizeAll
                        : StandardCursorType.Hand);
    }

    private void TopologyMinimapSurface_OnPointerPressed(
        object? sender,
        PointerPressedEventArgs eventArgs)
    {
        var point =
            eventArgs.GetCurrentPoint(
                TopologyMinimapSurface);

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (!_isMinimapNavigationAvailable)
        {
            TopologyMinimapSurface.Cursor =
                new Cursor(
                    StandardCursorType.Arrow);

            eventArgs.Handled =
                true;

            return;
        }

        _isMinimapPanning =
            true;

        eventArgs.Pointer.Capture(
            TopologyMinimapSurface);

        TopologyMinimapSurface.Cursor =
            new Cursor(
                StandardCursorType.SizeAll);

        NavigateFromMinimap(
            eventArgs.GetPosition(
                TopologyMinimapSurface));

        eventArgs.Handled =
            true;
    }

    private void TopologyMinimapSurface_OnPointerMoved(
        object? sender,
        PointerEventArgs eventArgs)
    {
        if (!_isMinimapPanning)
        {
            return;
        }

        NavigateFromMinimap(
            eventArgs.GetPosition(
                TopologyMinimapSurface));

        eventArgs.Handled =
            true;
    }

    private void TopologyMinimapSurface_OnPointerReleased(
        object? sender,
        PointerReleasedEventArgs eventArgs)
    {
        EndMinimapPanning(
            eventArgs.Pointer);

        eventArgs.Handled =
            true;
    }

    private void TopologyMinimapSurface_OnPointerCaptureLost(
        object? sender,
        PointerCaptureLostEventArgs eventArgs)
    {
        _isMinimapPanning =
            false;

        TopologyMinimapSurface.Cursor =
            new Cursor(
                _isMinimapNavigationAvailable
                    ? StandardCursorType.Hand
                    : StandardCursorType.Arrow);
    }

    private void EndMinimapPanning(
        IPointer pointer)
    {
        if (!_isMinimapPanning)
        {
            return;
        }

        _isMinimapPanning =
            false;

        pointer.Capture(null);

        TopologyMinimapSurface.Cursor =
            new Cursor(
                _isMinimapNavigationAvailable
                    ? StandardCursorType.Hand
                    : StandardCursorType.Arrow);
    }

    private void NavigateFromMinimap(
        Point position)
    {
        if (!_isMinimapNavigationAvailable)
        {
            return;
        }

        var viewportWidth =
            Math.Max(
                1,
                TopologyScrollViewer.Bounds.Width -
                24);

        var viewportHeight =
            Math.Max(
                1,
                TopologyScrollViewer.Bounds.Height -
                24);

        var offset =
            _minimapMapper.NavigateTo(
                position.X,
                position.Y,
                CanvasWidth,
                CanvasHeight,
                _zoom,
                viewportWidth,
                viewportHeight,
                MinimapWidth,
                MinimapHeight);

        TopologyScrollViewer.Offset =
            new Vector(
                offset.X,
                offset.Y);

        UpdateMinimapViewport();
    }

    private void TopologySurface_OnPointerWheelChanged(
        object? sender,
        PointerWheelEventArgs eventArgs)
    {
        if (Math.Abs(eventArgs.Delta.Y) < 0.01)
        {
            return;
        }

        var pointerPosition =
            eventArgs.GetPosition(
                TopologyScrollViewer);

        var previousZoom =
            _zoom;

        SetZoom(
            _zoom +
            (
                eventArgs.Delta.Y > 0
                    ? ZoomStep
                    : -ZoomStep
            ));

        if (Math.Abs(_zoom - previousZoom) < 0.001)
        {
            eventArgs.Handled = true;
            return;
        }

        var previousOffset =
            TopologyScrollViewer.Offset;

        var zoomRatio =
            _zoom / previousZoom;

        var targetOffsetX =
            (
                previousOffset.X +
                pointerPosition.X
            ) *
            zoomRatio -
            pointerPosition.X;

        var targetOffsetY =
            (
                previousOffset.Y +
                pointerPosition.Y
            ) *
            zoomRatio -
            pointerPosition.Y;

        Dispatcher.UIThread.Post(
            () =>
            {
                TopologyScrollViewer.Offset =
                    new Vector(
                        Math.Max(
                            0,
                            targetOffsetX),
                        Math.Max(
                            0,
                            targetOffsetY));
            });

        eventArgs.Handled = true;
    }

    private void TopologySurface_OnPointerPressed(
        object? sender,
        PointerPressedEventArgs eventArgs)
    {
        var pointerPoint =
            eventArgs.GetCurrentPoint(
                TopologySurface);

        if (!pointerPoint.Properties
                .IsLeftButtonPressed)
        {
            return;
        }

        _isPanning = true;

        _panStart =
            eventArgs.GetPosition(
                TopologyScrollViewer);

        _panStartOffset =
            TopologyScrollViewer.Offset;

        eventArgs.Pointer.Capture(
            TopologySurface);

        TopologySurface.Cursor =
            new Cursor(
                StandardCursorType.SizeAll);

        eventArgs.Handled = true;
    }

    private void TopologySurface_OnPointerMoved(
        object? sender,
        PointerEventArgs eventArgs)
    {
        if (!_isPanning)
        {
            return;
        }

        var currentPosition =
            eventArgs.GetPosition(
                TopologyScrollViewer);

        var deltaX =
            currentPosition.X -
            _panStart.X;

        var deltaY =
            currentPosition.Y -
            _panStart.Y;

        TopologyScrollViewer.Offset =
            new Vector(
                Math.Max(
                    0,
                    _panStartOffset.X -
                    deltaX),
                Math.Max(
                    0,
                    _panStartOffset.Y -
                    deltaY));

        eventArgs.Handled = true;
    }

    private void TopologySurface_OnPointerReleased(
        object? sender,
        PointerReleasedEventArgs eventArgs)
    {
        EndPanning(
            eventArgs.Pointer);

        eventArgs.Handled = true;
    }

    private void TopologySurface_OnPointerCaptureLost(
        object? sender,
        PointerCaptureLostEventArgs eventArgs)
    {
        _isPanning = false;
        TopologySurface.Cursor = null;
    }

    private void EndPanning(
        IPointer pointer)
    {
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;

        pointer.Capture(null);

        TopologySurface.Cursor = null;
    }

    private void SetZoom(
        double zoom)
    {
        _zoom =
            Math.Clamp(
                zoom,
                MinimumZoom,
                MaximumZoom);

        ApplyZoom();
    }

    private void ApplyZoom()
    {
        RootCanvas.RenderTransformOrigin =
            new RelativePoint(
                0,
                0,
                RelativeUnit.Relative);

        RootCanvas.RenderTransform =
            new ScaleTransform
            {
                ScaleX = _zoom,
                ScaleY = _zoom
            };

        TopologySurface.Width =
            CanvasWidth * _zoom;

        TopologySurface.Height =
            CanvasHeight * _zoom;

        UpdateMinimapViewport();

        TopologyResetZoomButton.Content =
            $"{_zoom:P0}";

        TopologyZoomOutButton.IsEnabled =
            _zoom > MinimumZoom;

        TopologyZoomInButton.IsEnabled =
            _zoom < MaximumZoom;
    }

    public void PrepareSavedViewVisualState(
        TopologySavedViewRestorePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var restoreVersion =
            ++_savedViewRestoreVersion;

        _isRestoringSavedView =
            true;

        try
        {
            _layoutMode =
                plan.LayoutMode;

            for (var index = 0;
                 index <
                 TopologyLayoutComboBox.Items.Count;
                 index++)
            {
                if (TopologyLayoutComboBox.Items[index] is
                        ComboBoxItem item &&
                    string.Equals(
                        item.Tag?.ToString(),
                        _layoutMode.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    TopologyLayoutComboBox.SelectedIndex =
                        index;

                    break;
                }
            }

            _selectedDomainId =
                plan.LiveSelectedDomainId;

            _collapsedDomainIds.Clear();

            foreach (var domainId in
                     plan.CollapsedDomainIds)
            {
                _collapsedDomainIds.Add(
                    domainId);
            }

            var filters =
                plan.RelationshipFilters;

            ContainmentFilter.IsChecked =
                filters.Containment;

            PlacementFilter.IsChecked =
                filters.Placement;

            DependencyFilter.IsChecked =
                filters.Dependency;

            ConnectivityFilter.IsChecked =
                filters.Connectivity;

            SecurityFilter.IsChecked =
                filters.Security;

            TrafficFilter.IsChecked =
                filters.Traffic;

            AssociationFilter.IsChecked =
                filters.Association;

            OtherFilter.IsChecked =
                filters.Other;

            AllRelationshipFilter.IsChecked =
                RelationshipFilters.All(filter =>
                    filter.IsChecked == true);

            _searchText =
                plan.SearchText.Trim();

            TopologySearchTextBox.Text =
                _searchText;

            TopologyMinimapPanel.IsVisible =
                plan.IsMinimapVisible;

            TopologyShowMinimapButton.IsVisible =
                !plan.IsMinimapVisible;

            SetZoom(
                plan.Zoom);

            ResetStateForDomainChange();
        }
        finally
        {
            _isRestoringSavedView =
                false;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                if (restoreVersion !=
                    _savedViewRestoreVersion)
                {
                    return;
                }

                TopologyScrollViewer.Offset =
                    new Vector(
                        Math.Max(
                            0,
                            plan.ScrollOffset.X),

                        Math.Max(
                            0,
                            plan.ScrollOffset.Y));

                UpdateMinimapViewport();
            });
    }


    public TopologySavedView CaptureSavedView(
        string id,
        string name,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        var state =
            new TopologySavedViewCaptureState
            {
                SelectedResourceId =
                    _currentTopology
                        .SelectedResourceId,

                Depth =
                    CurrentTopologyDepth(),

                LayoutMode =
                    _layoutMode,

                LiveSelectedDomainId =
                    _selectedDomainId,

                CollapsedDomainIds =
                    _collapsedDomainIds
                        .OrderBy(
                            domainId => domainId,
                            StringComparer.OrdinalIgnoreCase)
                        .ToArray(),

                RelationshipFilters =
                    new TopologyRelationshipFilterState
                    {
                        Containment =
                            ContainmentFilter.IsChecked ==
                            true,

                        Placement =
                            PlacementFilter.IsChecked ==
                            true,

                        Dependency =
                            DependencyFilter.IsChecked ==
                            true,

                        Connectivity =
                            ConnectivityFilter.IsChecked ==
                            true,

                        Security =
                            SecurityFilter.IsChecked ==
                            true,

                        Traffic =
                            TrafficFilter.IsChecked ==
                            true,

                        Association =
                            AssociationFilter.IsChecked ==
                            true,

                        Other =
                            OtherFilter.IsChecked ==
                            true
                    },

                Zoom =
                    _zoom,

                ScrollOffset =
                    new TopologyScrollOffset(
                        TopologyScrollViewer
                            .Offset.X,

                        TopologyScrollViewer
                            .Offset.Y),

                IsMinimapVisible =
                    TopologyMinimapPanel
                        .IsVisible,

                SearchText =
                    _searchText
            };

        return new TopologySavedViewCaptureService()
            .Capture(
                id,
                name,
                createdAtUtc,
                updatedAtUtc,
                state);
    }

    public void SetNavigationState(
        bool canGoBack,
        bool canGoForward,
        bool canGoHome)
    {
        TopologyBackButton.IsEnabled =
            canGoBack;

        TopologyForwardButton.IsEnabled =
            canGoForward;

        TopologyHomeButton.IsEnabled =
            canGoHome;
    }

    private void TopologyBackButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        BackRequested?.Invoke();
    }

    private void TopologyForwardButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        ForwardRequested?.Invoke();
    }

    private void TopologyHomeButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        HomeRequested?.Invoke();
    }

    private void TopologyDepthComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs eventArgs)
    {
        if (sender is not ComboBox comboBox ||
            comboBox.SelectedItem is not
                ComboBoxItem item ||
            !int.TryParse(
                item.Tag?.ToString(),
                out var depth))
        {
            return;
        }

        DepthChanged?.Invoke(
            depth);
    }


    private bool CanStartPathFromCurrentSelection()
    {
        if (string.IsNullOrWhiteSpace(
                _currentTopology.SelectedResourceId))
        {
            return false;
        }

        var selectedNode =
            _currentTopology.Nodes.FirstOrDefault(node =>
                node.Id.Equals(
                    _currentTopology.SelectedResourceId,
                    StringComparison.OrdinalIgnoreCase));

        return selectedNode is not null &&
               !TopologyGroupCollapseEngine
                   .IsSummaryNode(selectedNode);
    }

    private void TopologyStartPathButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        var sourceId =
            _currentTopology.SelectedResourceId;

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            TopologyPathStatusText.Text =
                "No source resource is selected.";

            return;
        }

        _isRelationshipFocusEnabled = false;
        _activePath = null;
        ClearPathDetails();
        _isPathSelectionMode = true;
        _pathSourceId = sourceId;

        var source =
            _currentTopology.Nodes.FirstOrDefault(node =>
                node.Id.Equals(
                    sourceId,
                    StringComparison.OrdinalIgnoreCase));

        TopologyPathStatusText.Text =
            $"Source: " +
            $"{source?.DisplayName ?? sourceId} · " +
            "click a destination resource";

        TopologyStartPathButton.Content =
            "Selecting…";

        TopologyClearPathButton.IsEnabled =
            true;

        RenderCurrentTopology();
    }

    private void TopologyClearPathButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        ClearPath();
    }

    public void ShowPath(
        GraphPathResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _isPathSelectionMode = false;
        _pathSourceId = "";
        _activePath = result;

        TopologyStartPathButton.Content =
            "Start path";

        TopologyClearPathButton.IsEnabled =
            true;

        if (!result.Found)
        {
            ClearPathDetails();

            TopologyPathStatusText.Text =
                "No path exists between the selected resources.";

            RenderCurrentTopology();
            return;
        }

        RenderPathDetails(result);

        var route =
            string.Join(
                "  →  ",
                result.Nodes.Select(node =>
                    string.IsNullOrWhiteSpace(
                        node.DisplayName)
                        ? node.Id
                        : node.DisplayName));

        var projectedNodeIds =
            _currentTopology.Nodes
                .Select(node => node.Id)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        var visiblePathNodeCount =
            result.Nodes.Count(node =>
                projectedNodeIds.Contains(node.Id));

        var projectionStatus =
            visiblePathNodeCount ==
            result.Nodes.Count
                ? ""
                : $" · {visiblePathNodeCount}/" +
                  $"{result.Nodes.Count} path nodes visible; " +
                  "increase depth to display the full route";

        TopologyPathStatusText.Text =
            $"{result.HopCount} " +
            (
                result.HopCount == 1
                    ? "hop"
                    : "hops"
            ) +
            $" · {route}" +
            projectionStatus;

        RenderCurrentTopology();
    }

    private void ClearPath()
    {
        _isPathSelectionMode = false;
        _pathSourceId = "";
        _activePath = null;

        ClearPathDetails();

        TopologyStartPathButton.Content =
            "Start path";

        TopologyClearPathButton.IsEnabled =
            false;

        TopologyPathStatusText.Text =
            "Center a source resource, then start a path";

        RenderCurrentTopology();
    }

    private void ClearPathDetails()
    {
        TopologyPathDetailsPanel.Children.Clear();
        TopologyPathDetailsSummaryText.Text = "";
        TopologyPathDetailsCard.IsVisible = false;
    }

    private void RenderPathDetails(
        GraphPathResult result)
    {
        ClearPathDetails();

        if (!result.Found ||
            result.Nodes.Count == 0)
        {
            return;
        }

        var sourceName =
            PathNodeName(result.Nodes[0]);

        var destinationName =
            PathNodeName(result.Nodes[^1]);

        TopologyPathDetailsSummaryText.Text =
            $"{sourceName}  →  {destinationName}  ·  " +
            $"{result.HopCount} " +
            (
                result.HopCount == 1
                    ? "hop"
                    : "hops"
            );

        for (var index = 0;
             index < result.Nodes.Count;
             index++)
        {
            var node =
                result.Nodes[index];

            var role =
                index == 0
                    ? "START"
                    : index == result.Nodes.Count - 1
                        ? "END"
                        : $"HOP {index}";

            var accent =
                index == 0
                    ? "#34D399"
                    : index == result.Nodes.Count - 1
                        ? "#F472B6"
                        : "#FBBF24";

            TopologyPathDetailsPanel.Children.Add(
                BuildPathNodeButton(
                    node,
                    role,
                    accent));

            if (index >= result.Edges.Count ||
                index + 1 >= result.Nodes.Count)
            {
                continue;
            }

            TopologyPathDetailsPanel.Children.Add(
                BuildPathRelationshipRow(
                    result.Nodes[index],
                    result.Nodes[index + 1],
                    result.Edges[index]));
        }

        TopologyPathDetailsCard.IsVisible = true;
    }

    private Button BuildPathNodeButton(
        GraphNode node,
        string role,
        string accent)
    {
        var roleText =
            new TextBlock
            {
                Text = role,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground =
                    new SolidColorBrush(
                        Color.Parse(accent))
            };

        var nameText =
            new TextBlock
            {
                Text = PathNodeName(node),
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#F8FAFC")),
                TextWrapping = TextWrapping.Wrap
            };

        var typeText =
            new TextBlock
            {
                Text =
                    $"{node.DomainId} · " +
                    $"{node.ResourceType}",
                FontSize = 11,
                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#8EA6C2")),
                TextWrapping = TextWrapping.Wrap
            };

        var content =
            new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    roleText,
                    nameText,
                    typeText
                }
            };

        var button =
            new Button
            {
                Content = content,
                Padding = new Thickness(10, 7),
                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment =
                    Avalonia.Layout.HorizontalAlignment.Left,
                Background =
                    new SolidColorBrush(
                        Color.Parse("#142740")),
                BorderBrush =
                    new SolidColorBrush(
                        Color.Parse(accent)),
                BorderThickness =
                    new Thickness(1),
                Cursor =
                    new Cursor(
                        StandardCursorType.Hand)
            };

        button.Click +=
            (_, _) =>
            {
                NodeInvoked?.Invoke(
                    new TopologyNode
                    {
                        Id = node.Id,
                        ProviderId = node.ProviderId,
                        DomainId = node.DomainId,
                        ResourceType = node.ResourceType,
                        DisplayName = node.DisplayName,
                        IsSelected =
                            node.Id.Equals(
                                _currentTopology
                                    .SelectedResourceId,
                                StringComparison
                                    .OrdinalIgnoreCase)
                    });
            };

        return button;
    }

    private static Control BuildPathRelationshipRow(
        GraphNode current,
        GraphNode next,
        GraphEdge edge)
    {
        var followsStoredDirection =
            edge.SourceId.Equals(
                current.Id,
                StringComparison.OrdinalIgnoreCase) &&
            edge.TargetId.Equals(
                next.Id,
                StringComparison.OrdinalIgnoreCase);

        var directionText =
            followsStoredDirection
                ? "forward"
                : "reverse traversal";

        var directionSymbol =
            followsStoredDirection
                ? "↓"
                : "↙";

        return new Border
        {
            Margin = new Thickness(18, 0, 0, 0),
            Padding = new Thickness(8, 3),
            BorderBrush =
                new SolidColorBrush(
                    Color.Parse(
                        followsStoredDirection
                            ? "#38BDF8"
                            : "#FBBF24")),
            BorderThickness =
                new Thickness(2, 0, 0, 0),
            Child =
                new TextBlock
                {
                    Text =
                        $"{directionSymbol}  " +
                        $"{edge.Relationship}  ·  " +
                        directionText,
                    FontSize = 11,
                    FontWeight =
                        FontWeight.SemiBold,
                    Foreground =
                        new SolidColorBrush(
                            Color.Parse("#C7D7EA")),
                    TextWrapping =
                        TextWrapping.Wrap
                }
        };
    }

    private static string PathNodeName(
        GraphNode node)
    {
        return string.IsNullOrWhiteSpace(
                node.DisplayName)
            ? node.Id
            : node.DisplayName;
    }

    public void Render(
        TopologyView topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _completeTopology =
            topology;

        RefreshDomainSelector();
        ApplySelectedDomain();

        TopologyStartPathButton.IsEnabled =
            CanStartPathFromCurrentSelection();

        RefreshSearchMatches(
            preserveActiveMatch: true);

        RenderCurrentTopology();
    }

    private void RenderCurrentTopology()
    {
        RootCanvas.Children.Clear();

        var topology =
            _currentTopology;

        if (topology.Nodes.Count == 0)
        {
            RelationshipCountText.Text =
                "0 nodes · 0/0 edges";

            RenderEmpty();
            RenderMinimap();
            return;
        }

        var visibleEdges =
            topology.Edges
                .Where(edge =>
                    IsRelationshipVisible(
                        edge.Kind) ||
                    IsActivePathEdge(edge))
                .ToList();

        var visibleTopology =
            new TopologyView
            {
                SelectedResourceId =
                    topology.SelectedResourceId,

                Nodes =
                    topology.Nodes,

                Edges =
                    visibleEdges
            };

        RelationshipCountText.Text =
            $"{topology.Nodes.Count} nodes · " +
            $"{visibleEdges.Count}/" +
            $"{topology.Edges.Count} edges";

        var positions =
            LayoutNodes(topology);

        _nodePositions =
            positions;

        RenderDomainGroups(
            topology,
            positions);

        RenderEdges(
            visibleTopology,
            positions);

        RenderNodes(
            topology,
            positions);

        RenderMinimap();
    }

    private bool IsRelationshipVisible(
        RelationshipKind kind)
    {
        return kind switch
        {
            RelationshipKind.Contains or
            RelationshipKind.MemberOf =>
                ContainmentFilter.IsChecked == true,

            RelationshipKind.AttachedTo or
            RelationshipKind.HostedOn =>
                PlacementFilter.IsChecked == true,

            RelationshipKind.DependsOn or
            RelationshipKind.Uses =>
                DependencyFilter.IsChecked == true,

            RelationshipKind.ConnectedTo or
            RelationshipKind.RoutesThrough =>
                ConnectivityFilter.IsChecked == true,

            RelationshipKind.SecuredBy =>
                SecurityFilter.IsChecked == true,

            RelationshipKind.Serves or
            RelationshipKind.Targets =>
                TrafficFilter.IsChecked == true,

            RelationshipKind.AssociatedWith =>
                AssociationFilter.IsChecked == true,

            _ =>
                OtherFilter.IsChecked == true
        };
    }

    private IReadOnlyList<CheckBox>
        RelationshipFilters =>
        [
            ContainmentFilter,
            PlacementFilter,
            DependencyFilter,
            ConnectivityFilter,
            SecurityFilter,
            TrafficFilter,
            AssociationFilter,
            OtherFilter
        ];

    private void AllRelationshipFilter_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        SetAllRelationshipFilters(
            AllRelationshipFilter.IsChecked == true);

        RenderCurrentTopology();
    }

    private void RelationshipFilter_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        AllRelationshipFilter.IsChecked =
            RelationshipFilters.All(
                filter =>
                    filter.IsChecked == true);

        RenderCurrentTopology();
    }

    private void ResetRelationshipFilters_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        AllRelationshipFilter.IsChecked =
            true;

        SetAllRelationshipFilters(true);

        _isRelationshipFocusEnabled = false;

        _isPathSelectionMode = false;
        _pathSourceId = "";
        _activePath = null;

        ClearPathDetails();

        TopologyStartPathButton.Content =
            "Start path";

        TopologyClearPathButton.IsEnabled =
            false;

        TopologyPathStatusText.Text =
            "Center a source resource, then start a path";

        RenderCurrentTopology();
    }

    private void SetAllRelationshipFilters(
        bool isEnabled)
    {
        foreach (var filter in
                 RelationshipFilters)
        {
            filter.IsChecked =
                isEnabled;
        }
    }

    private Dictionary<string, Point> LayoutNodes(
        TopologyView topology)
    {
        return _layoutEngine
            .Arrange(
                topology,
                _layoutMode,
                CanvasWidth,
                CanvasHeight)
            .ToDictionary(
                item => item.Key,
                item =>
                    new Point(
                        item.Value.X,
                        item.Value.Y),
                StringComparer.OrdinalIgnoreCase);
    }


    private void RenderDomainGroups(
        TopologyView topology,
        IReadOnlyDictionary<string, Point> positions)
    {
        if (_layoutMode !=
            TopologyLayoutMode.Domain)
        {
            return;
        }

        foreach (var bounds in
                 CalculateDomainGroupBounds(
                     topology,
                     positions))
        {
            var colors =
                DomainGroupColors(
                    bounds.GroupId);

            var boundary =
                new Border
                {
                    Width =
                        bounds.Width,

                    Height =
                        bounds.Height,

                    Background =
                        new SolidColorBrush(
                            Color.Parse(
                                colors.Fill)),

                    BorderBrush =
                        new SolidColorBrush(
                            Color.Parse(
                                colors.Stroke)),

                    BorderThickness =
                        new Thickness(1.5),

                    CornerRadius =
                        new CornerRadius(14),

                    Opacity = 0.82,

                    IsHitTestVisible =
                        false
                };

            Canvas.SetLeft(
                boundary,
                bounds.X);

            Canvas.SetTop(
                boundary,
                bounds.Y);

            RootCanvas.Children.Add(
                boundary);

            var group =
                FindDomainGroup(
                    bounds.GroupId);

            var displayName =
                group?.DisplayName ??
                bounds.DisplayName;

            var resourceCount =
                group?.Count ??
                bounds.NodeCount;

            var isCollapsed =
                IsDomainCollapsedInProjection(
                    bounds.GroupId);

            var headerText =
                isCollapsed
                    ? $"▸  {displayName} · " +
                      $"{resourceCount} · Expand"
                    : $"▾  {displayName} · " +
                      $"{resourceCount} · Collapse";

            var groupId =
                bounds.GroupId;

            var header =
                new Button
                {
                    Content =
                        headerText,

                    Padding =
                        new Thickness(
                            10,
                            4),

                    Background =
                        new SolidColorBrush(
                            Color.Parse("#F20B1728")),

                    Foreground =
                        new SolidColorBrush(
                            Color.Parse(
                                colors.Text)),

                    BorderBrush =
                        new SolidColorBrush(
                            Color.Parse(
                                colors.Stroke)),

                    BorderThickness =
                        new Thickness(1),

                    CornerRadius =
                        new CornerRadius(7),

                    FontSize = 11,

                    FontWeight =
                        FontWeight.SemiBold,

                    IsEnabled =
                        true,

                    Cursor =
                        new Cursor(
                            StandardCursorType.Hand)
                };

            header.SetValue(
                ToolTip.TipProperty,
                isCollapsed
                    ? $"Expand {displayName}"
                    : $"Collapse {displayName}");

            header.Click +=
                (_, _) =>
                    ToggleDomainGroup(
                        groupId);

            Canvas.SetLeft(
                header,
                bounds.X + 12);

            Canvas.SetTop(
                header,
                bounds.Y + 8);

            RootCanvas.Children.Add(
                header);
        }
    }

    private void RenderMinimapDomainGroups(
        IReadOnlyDictionary<string, Point> positions,
        double scaleX,
        double scaleY)
    {
        if (_layoutMode !=
            TopologyLayoutMode.Domain)
        {
            return;
        }

        foreach (var bounds in
                 CalculateDomainGroupBounds(
                     _currentTopology,
                     positions))
        {
            var colors =
                DomainGroupColors(
                    bounds.GroupId);

            var miniatureBoundary =
                new Border
                {
                    Width =
                        Math.Max(
                            3,
                            bounds.Width *
                            scaleX),

                    Height =
                        Math.Max(
                            3,
                            bounds.Height *
                            scaleY),

                    Background =
                        new SolidColorBrush(
                            Color.Parse(
                                colors.Fill)),

                    BorderBrush =
                        new SolidColorBrush(
                            Color.Parse(
                                colors.Stroke)),

                    BorderThickness =
                        new Thickness(0.8),

                    CornerRadius =
                        new CornerRadius(2),

                    Opacity = 0.75,

                    IsHitTestVisible =
                        false
                };

            Canvas.SetLeft(
                miniatureBoundary,
                bounds.X * scaleX);

            Canvas.SetTop(
                miniatureBoundary,
                bounds.Y * scaleY);

            TopologyMinimapContent.Children.Add(
                miniatureBoundary);
        }
    }

    private IReadOnlyList<TopologyGroupBounds>
        CalculateDomainGroupBounds(
        TopologyView topology,
        IReadOnlyDictionary<string, Point> positions)
    {
        var layoutPositions =
            positions.ToDictionary(
                item =>
                    item.Key,

                item =>
                    new TopologyLayoutPosition(
                        item.Value.X,
                        item.Value.Y),

                StringComparer.OrdinalIgnoreCase);

        return _groupBoundsEngine.Calculate(
            topology,
            layoutPositions,
            TopologyNodeWidth,
            TopologyNodeHeight);
    }

    private static (
        string Stroke,
        string Fill,
        string Text)
        DomainGroupColors(
        string groupId)
    {
        return groupId
            .Trim()
            .ToLowerInvariant() switch
        {
            "compute" =>
                (
                    "#38BDF8",
                    "#121E40AF",
                    "#7DD3FC"
                ),

            "networking" =>
                (
                    "#A78BFA",
                    "#122E1A47",
                    "#C4B5FD"
                ),

            "load-balancing" =>
                (
                    "#2DD4BF",
                    "#1213453F",
                    "#5EEAD4"
                ),

            "storage" =>
                (
                    "#F59E0B",
                    "#12451A03",
                    "#FCD34D"
                ),

            "identity" or
            "security" or
            "identity-security" =>
                (
                    "#FB7185",
                    "#124C0519",
                    "#FDA4AF"
                ),

            "database" or
            "databases" =>
                (
                    "#34D399",
                    "#12064E3B",
                    "#6EE7B7"
                ),

            "management" or
            "operations" =>
                (
                    "#60A5FA",
                    "#121E3A8A",
                    "#93C5FD"
                ),

            _ =>
                StableDomainGroupColors(
                    groupId)
        };
    }

    private static (
        string Stroke,
        string Fill,
        string Text)
        StableDomainGroupColors(
        string groupId)
    {
        var palette =
            new[]
            {
                (
                    Stroke: "#C084FC",
                    Fill: "#123B0764",
                    Text: "#D8B4FE"
                ),

                (
                    Stroke: "#22D3EE",
                    Fill: "#12164E63",
                    Text: "#67E8F9"
                ),

                (
                    Stroke: "#F472B6",
                    Fill: "#12500724",
                    Text: "#F9A8D4"
                ),

                (
                    Stroke: "#A3E635",
                    Fill: "#123A4D0A",
                    Text: "#BEF264"
                ),

                (
                    Stroke: "#FB923C",
                    Fill: "#12431907",
                    Text: "#FDBA74"
                )
            };

        var stableValue = 0;

        foreach (var character in
                 groupId.ToLowerInvariant())
        {
            stableValue =
                (
                    stableValue *
                    31 +
                    character
                ) &
                0x7FFFFFFF;
        }

        return palette[
            stableValue %
            palette.Length];
    }

    private void RenderEdges(
        TopologyView topology,
        IReadOnlyDictionary<string, Point> positions)
    {
        for (var edgeIndex = 0;
             edgeIndex < topology.Edges.Count;
             edgeIndex++)
        {
            var edge =
                topology.Edges[edgeIndex];

            var isPathEdge =
                IsActivePathEdge(edge);

            var isFocusedEdge =
                IsFocusedRelationshipEdge(edge);

            var edgeOpacity =
                HasActivePath
                    ? (
                        isPathEdge
                            ? 1.00
                            : 0.025
                    )
                    : (
                        HasRelationshipFocus &&
                        !isFocusedEdge
                            ? 0.04
                            : 1.00
                    );

            if (!positions.TryGetValue(
                    edge.SourceId,
                    out var source))
            {
                continue;
            }

            if (!positions.TryGetValue(
                    edge.TargetId,
                    out var target))
            {
                continue;
            }

            var sourceCenter =
                new Point(
                    source.X + 80,
                    source.Y + 35);

            var targetCenter =
                new Point(
                    target.X + 80,
                    target.Y + 35);

            var startPoint =
                FindNodeBoundaryPoint(
                    sourceCenter,
                    targetCenter);

            var endPoint =
                FindNodeBoundaryPoint(
                    targetCenter,
                    sourceCenter);

            var edgeColor =
                GetRelationshipColor(
                    edge.Kind);

            var edgeBrush =
                new SolidColorBrush(
                    edgeColor,
                    edgeOpacity);

            var line =
                new Line
                {
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    Stroke = edgeBrush,
                    StrokeThickness =
                        isPathEdge
                            ? 6
                            : isFocusedEdge
                                ? 5
                                : 2
                };

            RootCanvas.Children.Add(line);

            RenderArrowHead(
                startPoint,
                endPoint,
                edgeBrush);

            RenderRelationshipLabel(
                edge,
                edgeIndex,
                startPoint,
                endPoint,
                edgeBrush,
                edgeOpacity);
        }
    }

    private void RenderArrowHead(
        Point startPoint,
        Point endPoint,
        IBrush edgeBrush)
    {
        var deltaX =
            endPoint.X -
            startPoint.X;

        var deltaY =
            endPoint.Y -
            startPoint.Y;

        var length =
            Math.Sqrt(
                (deltaX * deltaX) +
                (deltaY * deltaY));

        if (length < 1)
        {
            return;
        }

        var directionX =
            deltaX / length;

        var directionY =
            deltaY / length;

        const double arrowLength = 12;
        const double arrowWidth = 6;

        var arrowBase =
            new Point(
                endPoint.X -
                (directionX * arrowLength),
                endPoint.Y -
                (directionY * arrowLength));

        var perpendicularX =
            -directionY;

        var perpendicularY =
            directionX;

        var leftPoint =
            new Point(
                arrowBase.X +
                (perpendicularX * arrowWidth),
                arrowBase.Y +
                (perpendicularY * arrowWidth));

        var rightPoint =
            new Point(
                arrowBase.X -
                (perpendicularX * arrowWidth),
                arrowBase.Y -
                (perpendicularY * arrowWidth));

        RootCanvas.Children.Add(
            new Line
            {
                StartPoint = leftPoint,
                EndPoint = endPoint,
                Stroke = edgeBrush,
                StrokeThickness = 2
            });

        RootCanvas.Children.Add(
            new Line
            {
                StartPoint = rightPoint,
                EndPoint = endPoint,
                Stroke = edgeBrush,
                StrokeThickness = 2
            });
    }

    private void RenderRelationshipLabel(
        TopologyEdge edge,
        int edgeIndex,
        Point startPoint,
        Point endPoint,
        IBrush edgeBrush,
        double edgeOpacity)
    {
        var relationshipLabel =
            edge.Multiplicity > 1
                ? $"{edge.Relationship} ×" +
                  $"{edge.Multiplicity}"
                : edge.Relationship;

        var labelFraction =
            edgeIndex % 3 switch
            {
                0 => 0.40,
                1 => 0.50,
                _ => 0.60
            };

        var midpointX =
            startPoint.X +
            ((endPoint.X - startPoint.X) *
             labelFraction);

        var midpointY =
            startPoint.Y +
            ((endPoint.Y - startPoint.Y) *
             labelFraction);

        var deltaX =
            endPoint.X -
            startPoint.X;

        var deltaY =
            endPoint.Y -
            startPoint.Y;

        var length =
            Math.Sqrt(
                (deltaX * deltaX) +
                (deltaY * deltaY));

        var laneOffset =
            edgeIndex % 4 switch
            {
                0 => 12.0,
                1 => -12.0,
                2 => 22.0,
                _ => -22.0
            };

        var offsetX = 0.0;
        var offsetY = -12.0;

        if (length >= 1)
        {
            offsetX =
                (-deltaY / length) *
                laneOffset;

            offsetY =
                (deltaX / length) *
                laneOffset;
        }

        var estimatedWidth =
            Math.Max(
                64,
                (relationshipLabel.Length * 6.5) + 16);

        var label =
            new Border
            {
                MinWidth = 64,
                MaxWidth = 180,

                Padding =
                    new Thickness(
                        7,
                        3),

                CornerRadius =
                    new CornerRadius(6),

                Background =
                    new SolidColorBrush(
                        Color.Parse("#E60B1220")),

                BorderBrush = edgeBrush,

                BorderThickness =
                    new Thickness(1),

                Opacity =
                    edgeOpacity,

                Child =
                    new TextBlock
                    {
                        Text =
                            relationshipLabel,

                        FontSize = 11,

                        FontWeight =
                            FontWeight.SemiBold,

                        Foreground =
                            new SolidColorBrush(
                                Color.Parse("#E2E8F0")),

                        TextWrapping =
                            TextWrapping.Wrap,

                        TextAlignment =
                            TextAlignment.Center
                    }
            };

        Canvas.SetLeft(
            label,
            midpointX +
            offsetX -
            (estimatedWidth / 2));

        Canvas.SetTop(
            label,
            midpointY +
            offsetY -
            12);

        RootCanvas.Children.Add(label);
    }

    private static Point FindNodeBoundaryPoint(
        Point nodeCenter,
        Point otherCenter)
    {
        var deltaX =
            otherCenter.X -
            nodeCenter.X;

        var deltaY =
            otherCenter.Y -
            nodeCenter.Y;

        if (Math.Abs(deltaX) < 0.001 &&
            Math.Abs(deltaY) < 0.001)
        {
            return nodeCenter;
        }

        const double halfWidth = 84;
        const double halfHeight = 39;

        var horizontalScale =
            Math.Abs(deltaX) < 0.001
                ? double.PositiveInfinity
                : halfWidth /
                  Math.Abs(deltaX);

        var verticalScale =
            Math.Abs(deltaY) < 0.001
                ? double.PositiveInfinity
                : halfHeight /
                  Math.Abs(deltaY);

        var scale =
            Math.Min(
                horizontalScale,
                verticalScale);

        return new Point(
            nodeCenter.X +
            (deltaX * scale),
            nodeCenter.Y +
            (deltaY * scale));
    }

    private static Color GetRelationshipColor(
        RelationshipKind kind)
    {
        return kind switch
        {
            RelationshipKind.Contains or
            RelationshipKind.MemberOf =>
                Color.Parse("#A78BFA"),

            RelationshipKind.AttachedTo or
            RelationshipKind.HostedOn =>
                Color.Parse("#60A5FA"),

            RelationshipKind.DependsOn or
            RelationshipKind.Uses =>
                Color.Parse("#FBBF24"),

            RelationshipKind.ConnectedTo or
            RelationshipKind.RoutesThrough =>
                Color.Parse("#22D3EE"),

            RelationshipKind.SecuredBy =>
                Color.Parse("#FB7185"),

            RelationshipKind.Serves or
            RelationshipKind.Targets =>
                Color.Parse("#34D399"),

            RelationshipKind.AssociatedWith =>
                Color.Parse("#C084FC"),

            _ =>
                Color.Parse("#64748B")
        };
    }


    private void TopologySearchTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs eventArgs)
    {
        if (_isRestoringSavedView ||
            sender is not TextBox textBox)
        {
            return;
        }

        _searchText =
            textBox.Text?.Trim() ?? "";

        RefreshSearchMatches(
            preserveActiveMatch: false);

        RenderCurrentTopology();

        if (_searchMatches.Count > 0)
        {
            FocusActiveSearchMatch();
        }
    }

    private void TopologySearchTextBox_OnKeyDown(
        object? sender,
        KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Enter ||
            _searchMatches.Count == 0)
        {
            return;
        }

        MoveToSearchMatch(
            eventArgs.KeyModifiers.HasFlag(
                KeyModifiers.Shift)
                ? -1
                : 1);

        eventArgs.Handled = true;
    }

    private void TopologyPreviousMatchButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        MoveToSearchMatch(-1);
    }

    private void TopologyNextMatchButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        MoveToSearchMatch(1);
    }

    private void TopologyClearSearchButton_OnClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        TopologySearchTextBox.Text = "";
        TopologySearchTextBox.Focus();
    }

    private void MoveToSearchMatch(
        int direction)
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        _activeSearchMatchIndex =
            (
                _activeSearchMatchIndex +
                direction +
                _searchMatches.Count
            ) %
            _searchMatches.Count;

        UpdateSearchControls();
        RenderCurrentTopology();
        FocusActiveSearchMatch();
    }

    private void RefreshSearchMatches(
        bool preserveActiveMatch)
    {
        var previousActiveId =
            preserveActiveMatch &&
            _activeSearchMatchIndex >= 0 &&
            _activeSearchMatchIndex < _searchMatches.Count
                ? _searchMatches[
                    _activeSearchMatchIndex].Id
                : "";

        if (string.IsNullOrWhiteSpace(
                _searchText))
        {
            _searchMatches.Clear();
            _activeSearchMatchIndex = -1;
            UpdateSearchControls();
            return;
        }

        _searchMatches =
            _currentTopology.Nodes
                .Where(node =>
                    MatchesTopologySearch(
                        node,
                        _searchText))
                .OrderBy(node =>
                    node.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(node =>
                    node.NativeId,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        _activeSearchMatchIndex =
            string.IsNullOrWhiteSpace(
                previousActiveId)
                ? (
                    _searchMatches.Count > 0
                        ? 0
                        : -1
                )
                : _searchMatches.FindIndex(node =>
                    node.Id.Equals(
                        previousActiveId,
                        StringComparison.OrdinalIgnoreCase));

        if (_activeSearchMatchIndex < 0 &&
            _searchMatches.Count > 0)
        {
            _activeSearchMatchIndex = 0;
        }

        UpdateSearchControls();
    }

    private void UpdateSearchControls()
    {
        var hasSearch =
            !string.IsNullOrWhiteSpace(
                _searchText);

        var hasMatches =
            _searchMatches.Count > 0;

        TopologyPreviousMatchButton.IsEnabled =
            hasMatches;

        TopologyNextMatchButton.IsEnabled =
            hasMatches;

        TopologyClearSearchButton.IsEnabled =
            hasSearch;

        TopologySearchStatusText.Text =
            !hasSearch
                ? "Search current graph"
                : !hasMatches
                    ? "No matches"
                    : $"{_activeSearchMatchIndex + 1} " +
                      $"of {_searchMatches.Count}";
    }

    private void FocusActiveSearchMatch()
    {
        if (_activeSearchMatchIndex < 0 ||
            _activeSearchMatchIndex >=
                _searchMatches.Count)
        {
            return;
        }

        var activeNode =
            _searchMatches[
                _activeSearchMatchIndex];

        if (!_nodePositions.TryGetValue(
                activeNode.Id,
                out var position))
        {
            return;
        }

        var nodeCenterX =
            (
                position.X +
                80
            ) *
            _zoom;

        var nodeCenterY =
            (
                position.Y +
                35
            ) *
            _zoom;

        Dispatcher.UIThread.Post(
            () =>
            {
                var viewportWidth =
                    Math.Max(
                        1,
                        TopologyScrollViewer.Bounds.Width);

                var viewportHeight =
                    Math.Max(
                        1,
                        TopologyScrollViewer.Bounds.Height);

                TopologyScrollViewer.Offset =
                    new Vector(
                        Math.Max(
                            0,
                            nodeCenterX -
                            viewportWidth / 2),
                        Math.Max(
                            0,
                            nodeCenterY -
                            viewportHeight / 2));
            },
            DispatcherPriority.Loaded);
    }

    private bool IsSearchMatch(
        TopologyNode node)
    {
        return _searchMatches.Any(match =>
            match.Id.Equals(
                node.Id,
                StringComparison.OrdinalIgnoreCase));
    }

    private bool IsActiveSearchMatch(
        TopologyNode node)
    {
        return _activeSearchMatchIndex >= 0 &&
               _activeSearchMatchIndex <
                   _searchMatches.Count &&
               _searchMatches[
                       _activeSearchMatchIndex]
                   .Id.Equals(
                       node.Id,
                       StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesTopologySearch(
        TopologyNode node,
        string search)
    {
        return ContainsSearch(
                   node.DisplayName,
                   search) ||
               ContainsSearch(
                   node.NativeId,
                   search) ||
               ContainsSearch(
                   node.ResourceType,
                   search) ||
               ContainsSearch(
                   node.DomainId,
                   search) ||
               ContainsSearch(
                   node.ProviderId,
                   search) ||
               ContainsSearch(
                   node.AccountId,
                   search) ||
               ContainsSearch(
                   node.State,
                   search) ||
               ContainsSearch(
                   node.Location,
                   search) ||
               ContainsSearch(
                   node.AvailabilityZone,
                   search) ||
               ContainsSearch(
                   node.Arn,
                   search) ||
               node.Properties.Any(item =>
                   ContainsSearch(
                       item.Key,
                       search) ||
                   ContainsSearch(
                       item.Value,
                       search)) ||
               node.Tags.Any(item =>
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



    private bool HasActivePath =>
        _activePath?.Found == true &&
        _activePath.Nodes.Count > 0;

    private bool IsActivePathNode(
        TopologyNode node)
    {
        return HasActivePath &&
               _activePath!.Nodes.Any(pathNode =>
                   pathNode.Id.Equals(
                       node.Id,
                       StringComparison.OrdinalIgnoreCase));
    }

    private bool IsPathStartNode(
        TopologyNode node)
    {
        return HasActivePath &&
               node.Id.Equals(
                   _activePath!.SourceId,
                   StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPathEndNode(
        TopologyNode node)
    {
        return HasActivePath &&
               node.Id.Equals(
                   _activePath!.TargetId,
                   StringComparison.OrdinalIgnoreCase);
    }

    private int PathNodePosition(
        TopologyNode node)
    {
        if (!HasActivePath)
        {
            return -1;
        }

        for (var index = 0;
             index < _activePath!.Nodes.Count;
             index++)
        {
            if (_activePath.Nodes[index].Id.Equals(
                    node.Id,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private bool IsActivePathEdge(
        TopologyEdge edge)
    {
        return HasActivePath &&
               _activePath!.Edges.Any(pathEdge =>
                   pathEdge.SourceId.Equals(
                       edge.SourceId,
                       StringComparison.OrdinalIgnoreCase) &&
                   pathEdge.TargetId.Equals(
                       edge.TargetId,
                       StringComparison.OrdinalIgnoreCase) &&
                   pathEdge.Relationship.Equals(
                       edge.Relationship,
                       StringComparison.OrdinalIgnoreCase));
    }

    private bool HasRelationshipFocus =>
        !HasActivePath &&
        _isRelationshipFocusEnabled &&
        !string.IsNullOrWhiteSpace(
            _currentTopology.SelectedResourceId);

    private string FocusedNodeId =>
        HasRelationshipFocus
            ? _currentTopology.SelectedResourceId
            : "";

    private bool IsFocusedRelationshipEdge(
        TopologyEdge edge)
    {
        if (!HasRelationshipFocus)
        {
            return false;
        }

        return edge.SourceId.Equals(
                   FocusedNodeId,
                   StringComparison.OrdinalIgnoreCase) ||
               edge.TargetId.Equals(
                   FocusedNodeId,
                   StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDirectFocusNeighbor(
        TopologyNode node)
    {
        if (!HasRelationshipFocus ||
            node.Id.Equals(
                FocusedNodeId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return _currentTopology.Edges
            .Where(edge =>
                IsRelationshipVisible(edge.Kind))
            .Any(edge =>
                IsFocusedRelationshipEdge(edge) &&
                (
                    edge.SourceId.Equals(
                        node.Id,
                        StringComparison.OrdinalIgnoreCase) ||
                    edge.TargetId.Equals(
                        node.Id,
                        StringComparison.OrdinalIgnoreCase)
                ));
    }

    private bool ShouldKeepNodeVivid(
        TopologyNode node)
    {
        if (HasActivePath)
        {
            return IsActivePathNode(node);
        }

        if (!HasRelationshipFocus)
        {
            return true;
        }

        return node.Id.Equals(
                   FocusedNodeId,
                   StringComparison.OrdinalIgnoreCase) ||
               IsDirectFocusNeighbor(node) ||
               IsSearchMatch(node);
    }

    private void RenderNodes(
        TopologyView topology,
        IReadOnlyDictionary<string, Point> positions)
    {
        foreach (var node in topology.Nodes)
        {
            if (!positions.TryGetValue(
                    node.Id,
                    out var position))
            {
                continue;
            }

            var visual =
                _renderRegistry.Resolve(
                    node.ProviderId,
                    node.DomainId,
                    node.ResourceType);

            var border =
                new Border
                {
                    Width = 160,
                    MinHeight = 70,

                    CornerRadius =
                        new CornerRadius(8),

                    Padding =
                        new Thickness(10),

                    Background =
                        new SolidColorBrush(
                            Color.Parse(
                                visual.Background)),

                    BorderBrush =
                        new SolidColorBrush(
                            Color.Parse(
                                node.IsSelected
                                    ? "#F8FAFC"
                                    : visual.Border)),

                    BorderThickness =
                        new Thickness(
                            node.IsSelected
                                ? 3
                                : 1)
                };

            var isSummaryNode =
                TopologyGroupCollapseEngine
                    .IsSummaryNode(node);

            border.Cursor =
                new Cursor(
                    StandardCursorType.Hand);

            if (isSummaryNode)
            {
                var colors =
                    DomainGroupColors(
                        node.DomainId);

                border.Background =
                    new SolidColorBrush(
                        Color.Parse("#F21B1538"));

                border.BorderBrush =
                    new SolidColorBrush(
                        Color.Parse(
                            colors.Stroke));

                border.BorderThickness =
                    new Thickness(3);
            }

            var panel =
                new StackPanel
                {
                    Spacing = 4
                };

            if (isSummaryNode)
            {
                panel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            "▸  COLLAPSED DOMAIN",

                        FontSize = 10,

                        FontWeight =
                            FontWeight.Bold,

                        Foreground =
                            new SolidColorBrush(
                                Color.Parse("#22D3EE"))
                    });
            }

            panel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"{(isSummaryNode ? "◫" : visual.Icon)}  " +
                        node.DisplayName,

                    FontWeight =
                        FontWeight.SemiBold,

                    Foreground =
                        new SolidColorBrush(
                            Color.Parse(
                                visual.Foreground)),

                    TextWrapping =
                        TextWrapping.Wrap
                });

            panel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"{node.DomainId} · " +
                        node.ResourceType,

                    FontSize = 11,

                    Foreground =
                        new SolidColorBrush(
                            Color.Parse("#94A3B8")),

                    TextWrapping =
                        TextWrapping.Wrap
                });

            border.Opacity =
                ShouldKeepNodeVivid(node)
                    ? 1.00
                    : 0.08;

            if (HasActivePath &&
                IsActivePathNode(node))
            {
                var pathColor =
                    IsPathStartNode(node)
                        ? "#34D399"
                        : IsPathEndNode(node)
                            ? "#F472B6"
                            : "#FBBF24";

                border.BorderBrush =
                    new SolidColorBrush(
                        Color.Parse(pathColor));

                border.BorderThickness =
                    new Thickness(
                        IsPathStartNode(node) ||
                        IsPathEndNode(node)
                            ? 5
                            : 4);
            }
            else if (HasRelationshipFocus)
            {
                if (node.Id.Equals(
                        FocusedNodeId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    border.BorderBrush =
                        new SolidColorBrush(
                            Color.Parse("#FDE047"));

                    border.BorderThickness =
                        new Thickness(5);
                }
                else if (IsDirectFocusNeighbor(node))
                {
                    border.BorderBrush =
                        new SolidColorBrush(
                            Color.Parse("#38BDF8"));

                    border.BorderThickness =
                        new Thickness(3);
                }
            }

            if (!HasActivePath &&
                IsSearchMatch(node) &&
                !node.Id.Equals(
                    FocusedNodeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                border.BorderBrush =
                    new SolidColorBrush(
                        Color.Parse(
                            IsActiveSearchMatch(node)
                                ? "#FDE047"
                                : "#38BDF8"));

                border.BorderThickness =
                    new Thickness(
                        IsActiveSearchMatch(node)
                            ? 4
                            : 3);
            }

            if (HasActivePath &&
                IsActivePathNode(node))
            {
                var pathPosition =
                    PathNodePosition(node);

                var pathLabel =
                    IsPathStartNode(node)
                        ? "●  START"
                        : IsPathEndNode(node)
                            ? "●  END"
                            : $"●  PATH {pathPosition + 1}";

                var pathColor =
                    IsPathStartNode(node)
                        ? "#34D399"
                        : IsPathEndNode(node)
                            ? "#F472B6"
                            : "#FBBF24";

                panel.Children.Insert(
                    0,
                    new TextBlock
                    {
                        Text = pathLabel,
                        FontSize = 11,
                        FontWeight =
                            FontWeight.Bold,
                        Foreground =
                            new SolidColorBrush(
                                Color.Parse(pathColor))
                    });
            }
            else if (HasRelationshipFocus &&
                node.Id.Equals(
                    FocusedNodeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                panel.Children.Insert(
                    0,
                    new TextBlock
                    {
                        Text = "●  FOCUS",
                        FontSize = 11,
                        FontWeight =
                            FontWeight.Bold,
                        Foreground =
                            new SolidColorBrush(
                                Color.Parse("#FDE047"))
                    });
            }
            else if (IsDirectFocusNeighbor(node))
            {
                panel.Children.Insert(
                    0,
                    new TextBlock
                    {
                        Text = "●  DIRECT",
                        FontSize = 10,
                        FontWeight =
                            FontWeight.SemiBold,
                        Foreground =
                            new SolidColorBrush(
                                Color.Parse("#38BDF8"))
                    });
            }

            border.Child = panel;

            border.SetValue(
                ToolTip.TipProperty,
                BuildNodeToolTip(node));

            border.SetValue(
                ToolTip.ShowDelayProperty,
                300);

            border.PointerPressed +=
                (_, eventArgs) =>
                {
                    if (isSummaryNode)
                    {
                        ExpandSummaryNode(node);
                        eventArgs.Handled = true;
                        return;
                    }

                    if (_isPathSelectionMode)
                    {
                        if (node.Id.Equals(
                                _pathSourceId,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            TopologyPathStatusText.Text =
                                "Choose a destination different " +
                                "from the source.";
                        }
                        else
                        {
                            PathRequested?.Invoke(
                                _pathSourceId,
                                node.Id);
                        }

                        eventArgs.Handled = true;
                        return;
                    }

                    _activePath = null;
                    ClearPathDetails();
                    TopologyClearPathButton.IsEnabled = false;

                    TopologyPathStatusText.Text =
                        "Center a source resource, " +
                        "then start a path";

                    _isRelationshipFocusEnabled = true;

                    NodeInvoked?.Invoke(node);
                    RenderCurrentTopology();

                    eventArgs.Handled = true;
                };

            Canvas.SetLeft(
                border,
                position.X);

            Canvas.SetTop(
                border,
                position.Y);

            RootCanvas.Children.Add(border);
        }
    }

    private static Control BuildNodeToolTip(
        TopologyNode node)
    {
        var root =
            new Border
            {
                MinWidth = 300,
                MaxWidth = 440,

                Padding =
                    new Thickness(14),

                CornerRadius =
                    new CornerRadius(8),

                Background =
                    new SolidColorBrush(
                        Color.Parse("#0B1220")),

                BorderBrush =
                    new SolidColorBrush(
                        Color.Parse("#475569")),

                BorderThickness =
                    new Thickness(1)
            };

        var content =
            new StackPanel
            {
                Spacing = 6
            };

        content.Children.Add(
            new TextBlock
            {
                Text = node.DisplayName,
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#F8FAFC")),
                TextWrapping = TextWrapping.Wrap
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"{node.DomainId} · " +
                    node.ResourceType,

                FontSize = 11,

                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#A78BFA"))
            });

        AddToolTipDetail(
            content,
            "Provider",
            node.ProviderId);

        AddToolTipDetail(
            content,
            "Account",
            node.AccountId);

        AddToolTipDetail(
            content,
            "Region",
            node.Location);

        AddToolTipDetail(
            content,
            "Availability Zone",
            node.AvailabilityZone);

        AddToolTipDetail(
            content,
            "State",
            node.State);

        AddToolTipDetail(
            content,
            "Native ID",
            node.NativeId);

        AddToolTipDetail(
            content,
            "ARN",
            node.Arn);

        if (node.Properties.Count > 0)
        {
            AddToolTipSection(
                content,
                "Properties");

            foreach (var property in
                     node.Properties
                         .OrderBy(pair => pair.Key)
                         .Take(12))
            {
                AddToolTipDetail(
                    content,
                    property.Key,
                    property.Value);
            }

            if (node.Properties.Count > 12)
            {
                AddToolTipMuted(
                    content,
                    $"+ {node.Properties.Count - 12} more properties");
            }
        }

        if (node.Tags.Count > 0)
        {
            AddToolTipSection(
                content,
                "Tags");

            foreach (var tag in
                     node.Tags
                         .OrderBy(pair => pair.Key)
                         .Take(10))
            {
                AddToolTipDetail(
                    content,
                    tag.Key,
                    tag.Value);
            }

            if (node.Tags.Count > 10)
            {
                AddToolTipMuted(
                    content,
                    $"+ {node.Tags.Count - 10} more tags");
            }
        }

        root.Child = content;

        return root;
    }

    private static void AddToolTipSection(
        StackPanel panel,
        string title)
    {
        panel.Children.Add(
            new Border
            {
                Margin =
                    new Thickness(0, 5, 0, 2),

                BorderBrush =
                    new SolidColorBrush(
                        Color.Parse("#334155")),

                BorderThickness =
                    new Thickness(0, 1, 0, 0),

                Padding =
                    new Thickness(0, 7, 0, 0),

                Child =
                    new TextBlock
                    {
                        Text = title,

                        FontWeight =
                            FontWeight.SemiBold,

                        Foreground =
                            new SolidColorBrush(
                                Color.Parse("#E2E8F0"))
                    }
            });
    }

    private static void AddToolTipDetail(
        StackPanel panel,
        string label,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var row =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions(
                        "110,*")
            };

        row.Children.Add(
            new TextBlock
            {
                Text = label,

                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#94A3B8")),

                FontSize = 11,

                Margin =
                    new Thickness(0, 1, 10, 1)
            });

        var valueText =
            new TextBlock
            {
                Text = value,

                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#F8FAFC")),

                FontSize = 11,

                TextWrapping =
                    TextWrapping.Wrap,

                Margin =
                    new Thickness(0, 1, 0, 1)
            };

        Grid.SetColumn(
            valueText,
            1);

        row.Children.Add(
            valueText);

        panel.Children.Add(row);
    }

    private static void AddToolTipMuted(
        StackPanel panel,
        string text)
    {
        panel.Children.Add(
            new TextBlock
            {
                Text = text,

                FontSize = 10,

                FontStyle =
                    FontStyle.Italic,

                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#64748B"))
            });
    }

    private void RenderEmpty()
    {
        var message =
            new TextBlock
            {
                Text =
                    "Select a resource to explore its infrastructure topology.",

                FontSize = 16,

                Foreground =
                    new SolidColorBrush(
                        Color.Parse("#94A3B8"))
            };

        Canvas.SetLeft(
            message,
            80);

        Canvas.SetTop(
            message,
            80);

        RootCanvas.Children.Add(message);
    }
}
