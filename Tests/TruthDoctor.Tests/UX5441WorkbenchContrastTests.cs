using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5441WorkbenchContrastTests
{
    private const string Primary =
        "{DynamicResource TD.TextPrimaryBrush}";

    private static string SourcePath(
        [CallerFilePath] string path = "") => path;

    private static XDocument Workbench() =>
        XDocument.Load(Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(SourcePath())!,
            "..",
            "..",
            "Views",
            "UniversalWorkbench",
            "UniversalWorkbenchWindow.axaml")));

    private static XElement Named(
        XDocument document,
        string name) =>
        Assert.Single(
            document.Descendants().Where(element =>
                (string?)element.Attribute("Name") == name));

    private static XElement Text(
        XDocument document,
        string value) =>
        Assert.Single(
            document.Descendants().Where(element =>
                element.Name.LocalName == "TextBlock" &&
                (string?)element.Attribute("Text") == value));

    private static void AssertReadable(XElement element) =>
        Assert.Equal(
            Primary,
            (string?)element.Attribute("Foreground"));

    [Fact]
    public void DashboardHeadingUsesReadableSemanticText()
    {
        AssertReadable(Text(
            Workbench(), "Infrastructure Overview"));
    }

    [Fact]
    public void DashboardSummaryAndDiscoveryTextUseSemanticText()
    {
        var document = Workbench();

        AssertReadable(Text(
            document, "Universal infrastructure state"));

        AssertReadable(Named(
            document, "LastDiscoveryText"));
    }

    [Fact]
    public void ProviderSelectionLabelsUseSemanticText()
    {
        var document = Workbench();

        foreach (var label in new[]
        {
            "Provider",
            "Account / Project",
            "Location"
        })
        {
            AssertReadable(Text(document, label));
        }
    }

    [Fact]
    public void SecondaryWorkspaceDescriptionUsesSemanticText()
    {
        AssertReadable(Named(
            Workbench(),
            "SecondaryWorkspaceDescription"));
    }

    [Fact]
    public void ResourceDetailsGuidanceUsesSemanticText()
    {
        AssertReadable(Text(
            Workbench(),
            "Select a resource to inspect its state, properties, " +
            "relationships, and available operations."));
    }

    [Fact]
    public void NavigationAndStatusContractsRemainPreserved()
    {
        var document = Workbench();

        Assert.Equal(
            "{DynamicResource TD.CanvasBrush}",
            (string?)document.Root?.Attribute("Background"));

        foreach (var name in new[]
        {
            "DashboardNavButton",
            "ResourcesNavButton",
            "OperationsNavButton",
            "TopologyNavButton",
            "HistoryNavButton",
            "ReportsNavButton",
            "SettingsNavButton",
            "RunDiscoveryButton",
            "DiscoveryStatusText",
            "SidebarProviderText",
            "SidebarRefreshText",
            "AssistantStatusText",
            "StatusApiText",
            "ResourceListBox",
            "SecondaryWorkspaceList"
        })
        {
            Assert.NotNull(Named(document, name));
        }

        Assert.Equal(
            "#2563EB",
            (string?)Named(document, "RunDiscoveryButton")
                .Attribute("Background"));

        Assert.Equal(
            "#FBBF24",
            (string?)Named(document, "DiscoveryStatusText")
                .Attribute("Foreground"));

        Assert.Equal(
            "#BFDBFE",
            (string?)Named(document, "AssistantStatusText")
                .Attribute("Foreground"));

        Assert.Equal(
            "#0F172A",
            (string?)Named(document, "StatusApiText")
                .Attribute("Foreground"));

        Assert.Equal(
            "{DynamicResource TD.SurfaceBrush}",
            (string?)Named(document, "ResourceListBox")
                .Attribute("Background"));

        Assert.Equal(
            "{DynamicResource TD.SurfaceBrush}",
            (string?)Named(document, "SecondaryWorkspaceList")
                .Attribute("Background"));
    }
}
