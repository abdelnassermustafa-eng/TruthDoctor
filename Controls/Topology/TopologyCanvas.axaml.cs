using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using TruthDoctor.Graph;
using TruthDoctor.Services.Visuals;

namespace TruthDoctor.Controls.Topology;

public partial class TopologyCanvas : UserControl
{
    private readonly RenderRegistry _renderRegistry =
        new([
            new AwsRenderContributor()
        ]);

    public TopologyCanvas()
    {
        InitializeComponent();
    }

    public void Render(
        TopologyView topology)
    {
        RootCanvas.Children.Clear();

        if (topology.Nodes.Count == 0)
        {
            RenderEmpty();
            return;
        }

        var positions =
            LayoutNodes(topology);

        RenderEdges(
            topology,
            positions);

        RenderNodes(
            topology,
            positions);
    }

    private Dictionary<string, Point> LayoutNodes(
        TopologyView topology)
    {
        var positions =
            new Dictionary<string, Point>(
                StringComparer.OrdinalIgnoreCase);

        var selected =
            topology.Nodes.FirstOrDefault(
                node => node.IsSelected);

        if (selected is not null)
        {
            positions[selected.Id] =
                new Point(
                    700,
                    420);
        }

        var others =
            topology.Nodes
                .Where(node => !node.IsSelected)
                .ToList();

        if (others.Count == 0)
        {
            return positions;
        }

        const double radius = 310;

        for (var index = 0;
             index < others.Count;
             index++)
        {
            var angle =
                2 *
                Math.PI *
                index /
                others.Count;

            var x =
                700 +
                Math.Cos(angle) *
                radius;

            var y =
                420 +
                Math.Sin(angle) *
                radius;

            positions[others[index].Id] =
                new Point(x, y);
        }

        return positions;
    }

    private void RenderEdges(
        TopologyView topology,
        IReadOnlyDictionary<string, Point> positions)
    {
        foreach (var edge in topology.Edges)
        {
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
                    edgeColor);

            var line =
                new Line
                {
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    Stroke = edgeBrush,
                    StrokeThickness = 2
                };

            RootCanvas.Children.Add(line);

            RenderArrowHead(
                startPoint,
                endPoint,
                edgeBrush);

            RenderRelationshipLabel(
                edge,
                startPoint,
                endPoint,
                edgeBrush);
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
        Point startPoint,
        Point endPoint,
        IBrush edgeBrush)
    {
        var midpointX =
            (startPoint.X + endPoint.X) / 2;

        var midpointY =
            (startPoint.Y + endPoint.Y) / 2;

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

        var offsetX = 0.0;
        var offsetY = -12.0;

        if (length >= 1)
        {
            offsetX =
                (-deltaY / length) * 10;

            offsetY =
                (deltaX / length) * 10;
        }

        var estimatedWidth =
            Math.Max(
                64,
                (edge.Relationship.Length * 6.5) + 16);

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

                Child =
                    new TextBlock
                    {
                        Text =
                            edge.Relationship,

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

            var panel =
                new StackPanel
                {
                    Spacing = 4
                };

            panel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"{visual.Icon}  " +
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

            border.Child = panel;

            border.SetValue(
                ToolTip.TipProperty,
                BuildNodeToolTip(node));

            border.SetValue(
                ToolTip.ShowDelayProperty,
                300);

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
