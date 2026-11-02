using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX543WorkbenchPanelTests
{
    private const string Surface =
        "{DynamicResource TD.SurfaceBrush}";

    private const string BorderBrush =
        "{DynamicResource TD.BorderBrush}";

    private const string PrimaryText =
        "{DynamicResource TD.TextPrimaryBrush}";

    private static string SourcePath(
        [CallerFilePath] string path = "") => path;

    private static XDocument Workbench() =>
        XDocument.Load(Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(SourcePath())!,
            "..", "..", "Views", "UniversalWorkbench",
            "UniversalWorkbenchWindow.axaml")));

    private static XElement Named(
        XDocument document, string name) =>
        Assert.Single(document.Descendants().Where(node =>
            (string?)node.Attribute("Name") == name));

    private static XElement NearestBorder(XElement node) =>
        Assert.Single(node.Ancestors().Where(element =>
            element.Name.LocalName == "Border").Take(1));

    private static void AssertSemanticPanel(XElement border)
    {
        Assert.Equal(Surface,
            (string?)border.Attribute("Background"));

        Assert.Equal(BorderBrush,
            (string?)border.Attribute("BorderBrush"));
    }

    [Fact]
    public void ProviderSelectorPanelUsesSemanticSurfaceAndBorder()
    {
        var document = Workbench();
        var provider = Named(document, "ProviderComboBox");

        AssertSemanticPanel(NearestBorder(provider));
    }

    [Fact]
    public void DiscoveredResourcesPanelUsesSemanticSurfaceAndBorder()
    {
        var document = Workbench();
        var list = Named(document, "ResourceListBox");

        AssertSemanticPanel(NearestBorder(list));
    }

    [Fact]
    public void SecondaryWorkspacePanelUsesSemanticSurfaceAndBorder()
    {
        AssertSemanticPanel(
            Named(Workbench(), "SecondaryWorkspace"));
    }

    [Fact]
    public void ResourceDetailsCardUsesSemanticSurfaceAndBorder()
    {
        var document = Workbench();
        var details = Named(document, "ResourceDetailsPanel");

        AssertSemanticPanel(NearestBorder(details));
    }

    [Fact]
    public void DetailsRailUsesSemanticSurfaceAndReadableHeading()
    {
        var document = Workbench();
        var details = Named(document, "ResourceDetailsPanel");

        var rail = Assert.Single(
            details.Ancestors().Where(element =>
                element.Name.LocalName == "Border" &&
                (string?)element.Attribute("Grid.Column") == "2"));

        AssertSemanticPanel(rail);

        var heading = Assert.Single(
            rail.Descendants().Where(element =>
                element.Name.LocalName == "TextBlock" &&
                (string?)element.Attribute("Text") ==
                    "Resource Details"));

        Assert.Equal(PrimaryText,
            (string?)heading.Attribute("Foreground"));
    }

    [Fact]
    public void NavigationDiscoveryAssistantAndFooterRemainProtected()
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
            "DiscoveryStatusText",
            "AssistantStatusText",
            "RunDiscoveryButton",
            "StatusProviderText",
            "StatusApiText"
        })
        {
            Assert.NotNull(Named(document, name));
        }

        Assert.Equal("#2563EB",
            (string?)Named(document, "RunDiscoveryButton")
                .Attribute("Background"));

        Assert.Equal("#FBBF24",
            (string?)Named(document, "DiscoveryStatusText")
                .Attribute("Foreground"));

        Assert.Equal("#BFDBFE",
            (string?)Named(document, "AssistantStatusText")
                .Attribute("Foreground"));

        Assert.Equal("#0F172A",
            (string?)Named(document, "StatusApiText")
                .Attribute("Foreground"));

        Assert.Equal(
            "{DynamicResource TD.SurfaceBrush}",
            (string?)Named(document, "ResourceListBox")
                .Attribute("Background"));
    }
}
