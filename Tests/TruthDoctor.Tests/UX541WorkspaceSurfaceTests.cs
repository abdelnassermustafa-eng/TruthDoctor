using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX541WorkspaceSurfaceTests
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

    private static XElement Named(
        XDocument document,
        string name) =>
        Assert.Single(
            document.Descendants().Where(element =>
                (string?)element.Attribute("Name") == name));

    private static XElement Style(
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
    public void ResourceListUsesSemanticSurface()
    {
        var list = Named(
            LoadWorkbench(), "ResourceListBox");

        Assert.Equal(
            "{DynamicResource TD.SurfaceBrush}",
            (string?)list.Attribute("Background"));
    }

    [Fact]
    public void SecondaryWorkspaceListUsesSemanticSurface()
    {
        var list = Named(
            LoadWorkbench(), "SecondaryWorkspaceList");

        Assert.Equal(
            "{DynamicResource TD.SurfaceBrush}",
            (string?)list.Attribute("Background"));
    }

    [Fact]
    public void WorkspaceTitlesUseReadableSemanticText()
    {
        var document = LoadWorkbench();

        foreach (var name in new[]
        {
            "ResourceTitleText",
            "SecondaryWorkspaceTitle"
        })
        {
            Assert.Equal(
                "{DynamicResource TD.TextPrimaryBrush}",
                (string?)Named(
                    document, name).Attribute("Foreground"));
        }
    }

    [Fact]
    public void DomainExpanderUsesReadableSemanticText()
    {
        var document = LoadWorkbench();

        var style = Style(
            document, "Expander.nav-expander");

        Assert.Equal(
            "{DynamicResource TD.TextPrimaryBrush}",
            SetterValue(style, "Foreground"));
    }

    [Fact]
    public void ExistingNavigationAndStatusSignalsRemain()
    {
        var document = LoadWorkbench();

        Assert.Equal(
            "{DynamicResource TD.CanvasBrush}",
            (string?)document.Root?.Attribute("Background"));

        foreach (var selector in new[]
        {
            "Button.nav-item",
            "Button.nav-item:pointerover",
            "Button.nav-item.active",
            "Button.nav-item.active:pointerover",
            "Button.nav-item:focus-visible"
        })
        {
            Assert.NotNull(Style(document, selector));
        }

        foreach (var name in new[]
        {
            "DashboardNavButton",
            "ResourcesNavButton",
            "OperationsNavButton",
            "TopologyNavButton",
            "HistoryNavButton",
            "ReportsNavButton",
            "SettingsNavButton",
            "DiscoveryStatusText",
            "RunDiscoveryButton",
            "StatusProviderText",
            "StatusApiText"
        })
        {
            Assert.NotNull(Named(document, name));
        }

        var discoveryButton = Named(
            document, "RunDiscoveryButton");

        Assert.Equal(
            "#2563EB",
            (string?)discoveryButton.Attribute("Background"));
    }
}
