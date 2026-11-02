using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX53NavigationShellTests
{
    private static string SourcePath(
        [CallerFilePath] string path = "") => path;

    private static string RepositoryRoot =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(SourcePath())!,
            "..",
            ".."));

    private static XDocument LoadWorkbench() =>
        XDocument.Load(Path.Combine(
            RepositoryRoot,
            "Views",
            "UniversalWorkbench",
            "UniversalWorkbenchWindow.axaml"));

    private static XElement FindStyle(
        XDocument document,
        string selector) =>
        Assert.Single(
            document.Descendants().Where(element =>
                element.Name.LocalName == "Style" &&
                (string?)element.Attribute("Selector") ==
                    selector));

    private static string? SetterValue(
        XElement style,
        string property) =>
        Assert.Single(
            style.Elements().Where(element =>
                element.Name.LocalName == "Setter" &&
                (string?)element.Attribute("Property") ==
                    property))
        .Attribute("Value")?.Value;

    [Fact]
    public void WindowCanvasUsesSemanticBrush()
    {
        var document = LoadWorkbench();

        Assert.Equal(
            "{DynamicResource TD.CanvasBrush}",
            (string?)document.Root?.Attribute("Background"));
    }

    [Fact]
    public void NavigationSidebarUsesSemanticSurfaceAndBorder()
    {
        var document = LoadWorkbench();

        var sidebar = Assert.Single(
            document.Descendants().Where(element =>
                element.Name.LocalName == "Border" &&
                (string?)element.Attribute("Grid.Column") == "0"));

        Assert.Equal(
            "{DynamicResource TD.SurfaceBrush}",
            (string?)sidebar.Attribute("Background"));

        Assert.Equal(
            "{DynamicResource TD.BorderBrush}",
            (string?)sidebar.Attribute("BorderBrush"));
    }

    [Fact]
    public void NavigationDefaultAndHoverUseSemanticBrushes()
    {
        var document = LoadWorkbench();

        var normal = FindStyle(
            document, "Button.nav-item");

        var hover = FindStyle(
            document, "Button.nav-item:pointerover");

        Assert.Equal(
            "{DynamicResource TD.TextPrimaryBrush}",
            SetterValue(normal, "Foreground"));

        Assert.Equal(
            "{DynamicResource TD.HoverSurfaceBrush}",
            SetterValue(hover, "Background"));

        Assert.Equal(
            "{DynamicResource TD.TextPrimaryBrush}",
            SetterValue(hover, "Foreground"));

        Assert.Equal(
            "{DynamicResource TD.BorderBrush}",
            SetterValue(hover, "BorderBrush"));
    }

    [Fact]
    public void KeyboardFocusHasVisibleSemanticRing()
    {
        var document = LoadWorkbench();

        var focus = FindStyle(
            document, "Button.nav-item:focus-visible");

        Assert.Equal(
            "{DynamicResource TD.FocusRingBrush}",
            SetterValue(focus, "BorderBrush"));

        Assert.Equal(
            "2",
            SetterValue(focus, "BorderThickness"));
    }

    [Fact]
    public void NavigationStructureAndActiveSelectorRemain()
    {
        var document = LoadWorkbench();

        foreach (var name in new[]
        {
            "DashboardNavButton",
            "ResourcesNavButton",
            "OperationsNavButton",
            "TopologyNavButton",
            "HistoryNavButton",
            "ReportsNavButton",
            "SettingsNavButton"
        })
        {
            Assert.Single(
                document.Descendants().Where(element =>
                    (string?)element.Attribute("Name") == name));
        }

        var active = FindStyle(
            document, "Button.nav-item.active");

        Assert.Equal(
            "SemiBold",
            SetterValue(active, "FontWeight"));

        Assert.NotNull(SetterValue(active, "Background"));
        Assert.NotNull(SetterValue(active, "Foreground"));
    }
}
