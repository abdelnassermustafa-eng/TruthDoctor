using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using TruthDoctor.Services.Providers;
using TruthDoctor.Services.Visuals;
using TruthDoctor.ViewModels;

namespace TruthDoctor.Controllers.Workbench;

public sealed class WorkbenchDetailsController
{
    private readonly WorkbenchState _state;
    private readonly TruthDoctor.Services.Visuals.RenderRegistry
        _renderRegistry;

    public WorkbenchDetailsController(
        WorkbenchState state,
        TruthDoctor.Services.Visuals.RenderRegistry renderRegistry)
    {
        _state = state;
        _renderRegistry = renderRegistry;
    }

    public void RenderSelectedResource(
        StackPanel detailsPanel,
        TextBlock assistantStatus)
    {
        var resource = _state.SelectedResource;

        if (resource is null)
        {
            RenderEmpty(detailsPanel, assistantStatus);
            return;
        }

        Render(
            resource,
            detailsPanel,
            assistantStatus);
    }

    public void Render(
        InfrastructureResource resource,
        StackPanel detailsPanel,
        TextBlock assistantStatus)
    {
        detailsPanel.Children.Clear();

        var visual =
            _renderRegistry.Resolve(
                resource.ProviderId,
                resource.DomainId,
                resource.ResourceType,
                resource.IconKey,
                resource.AccentKey);

        detailsPanel.Children.Add(
            new TextBlock
            {
                Text =
                    $"{visual.Icon}  " +
                    resource.DisplayName,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brush("#F8FAFC"),
                TextWrapping = TextWrapping.Wrap
            });

        AddSectionTitle(
            detailsPanel,
            "Summary");

        AddDetail(
            detailsPanel,
            "Provider",
            resource.ProviderId);

        AddDetail(
            detailsPanel,
            "Account",
            resource.AccountId);

        AddDetail(
            detailsPanel,
            "Domain",
            resource.DomainId);

        AddDetail(
            detailsPanel,
            "Type",
            resource.ResourceType);

        AddDetail(
            detailsPanel,
            "Native ID",
            resource.NativeId);

        AddDetail(
            detailsPanel,
            "State",
            resource.State);

        AddDetail(
            detailsPanel,
            "Location",
            resource.Location);

        AddDetail(
            detailsPanel,
            "Availability Zone",
            resource.AvailabilityZone);

        AddDetail(
            detailsPanel,
            "ARN",
            resource.Arn);

        if (resource.Properties.Count > 0)
        {
            AddSeparator(detailsPanel);

            AddSectionTitle(
                detailsPanel,
                "Properties");

            foreach (var property in resource.Properties
                         .OrderBy(item => item.Key))
            {
                AddDetail(
                    detailsPanel,
                    property.Key,
                    property.Value);
            }
        }

        if (resource.Tags.Count > 0)
        {
            AddSeparator(detailsPanel);

            AddSectionTitle(
                detailsPanel,
                "Tags");

            foreach (var tag in resource.Tags
                         .OrderBy(item => item.Key))
            {
                AddDetail(
                    detailsPanel,
                    tag.Key,
                    tag.Value);
            }
        }

        if (resource.Relationships.Count > 0)
        {
            AddSeparator(detailsPanel);

            AddSectionTitle(
                detailsPanel,
                "Relationships");

            foreach (var relationship in resource.Relationships)
            {
                AddDetail(
                    detailsPanel,
                    relationship.Type,
                    string.IsNullOrWhiteSpace(
                        relationship.Description)
                        ? relationship.TargetResourceId
                        : relationship.Description);
            }
        }

        if (resource.Capabilities.Count > 0)
        {
            AddSeparator(detailsPanel);

            AddSectionTitle(
                detailsPanel,
                "Available Operations");

            foreach (var capability in resource.Capabilities)
            {
                detailsPanel.Children.Add(
                    new Button
                    {
                        Content = capability,
                        HorizontalContentAlignment =
                            HorizontalAlignment.Left,
                        Tag = capability
                    });
            }
        }

        assistantStatus.Text =
            $"Selected {resource.ResourceType}: " +
            $"{resource.DisplayName}.";
    }

    public void RenderEmpty(
        StackPanel detailsPanel,
        TextBlock assistantStatus)
    {
        detailsPanel.Children.Clear();

        detailsPanel.Children.Add(
            new TextBlock
            {
                Text =
                    "Select a resource to inspect its state, " +
                    "properties, relationships, tags, and " +
                    "available operations.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brush("#94A3B8")
            });

        assistantStatus.Text =
            "Waiting for a resource selection.";
    }

    private static void AddSectionTitle(
        StackPanel detailsPanel,
        string title)
    {
        detailsPanel.Children.Add(
            new TextBlock
            {
                Text = title,
                Margin = new Thickness(0, 5, 0, 2),
                FontWeight = FontWeight.SemiBold,
                Foreground = Brush("#F8FAFC")
            });
    }

    private static void AddSeparator(
        StackPanel detailsPanel)
    {
        detailsPanel.Children.Add(
            new Separator
            {
                Margin = new Thickness(0, 8)
            });
    }

    private static void AddDetail(
        StackPanel detailsPanel,
        string label,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var grid = new Grid
        {
            ColumnDefinitions =
                new ColumnDefinitions("120,*")
        };

        grid.Children.Add(
            new TextBlock
            {
                Text = label,
                Foreground = Brush("#94A3B8"),
                TextWrapping = TextWrapping.Wrap
            });

        var valueText = new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("#E2E8F0")
        };

        Grid.SetColumn(valueText, 1);

        grid.Children.Add(valueText);

        detailsPanel.Children.Add(grid);
    }

    private static IBrush Brush(string value)
    {
        return new SolidColorBrush(
            Color.Parse(value));
    }
}
