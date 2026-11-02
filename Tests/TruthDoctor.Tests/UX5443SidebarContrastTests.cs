using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5443SidebarContrastTests
{
    private const string Surface =
        "{DynamicResource TD.SurfaceBrush}";

    private const string BorderBrush =
        "{DynamicResource TD.BorderBrush}";

    private const string Primary =
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

    private static XElement NearestBorder(XElement element) =>
        Assert.Single(element.Ancestors().Where(node =>
            node.Name.LocalName == "Border").Take(1));

    private static string Color(string raw)
    {
        return raw switch
        {
            Surface => "#FFFFFF",
            Primary => "#172B4D",
            _ => raw
        };
    }

    private static double Luminance(string hex)
    {
        Assert.Matches("^#[0-9a-fA-F]{6}$", hex);

        double Linear(int start)
        {
            var value = Convert.ToInt32(
                hex.Substring(start, 2), 16) / 255.0;

            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linear(1)
             + 0.7152 * Linear(3)
             + 0.0722 * Linear(5);
    }

    private static double Contrast(string first, string second)
    {
        var a = Luminance(Color(first));
        var b = Luminance(Color(second));

        return (Math.Max(a, b) + 0.05)
             / (Math.Min(a, b) + 0.05);
    }

    [Fact]
    public void InfrastructureDomainContainerUsesSemanticSurface()
    {
        var document = Workbench();
        var empty = Named(document, "EmptyDomainsText");
        var border = NearestBorder(empty);

        Assert.Equal(
            Surface,
            (string?)border.Attribute("Background"));

        Assert.Equal(
            BorderBrush,
            (string?)border.Attribute("BorderBrush"));
    }

    [Fact]
    public void EmptyDomainsTextUsesReadableSemanticForeground()
    {
        var document = Workbench();
        var empty = Named(document, "EmptyDomainsText");

        Assert.Equal(
            Primary,
            (string?)empty.Attribute("Foreground"));

        var border = NearestBorder(empty);

        Assert.True(
            Contrast(
                (string)empty.Attribute("Foreground")!,
                (string)border.Attribute("Background")!
            ) >= 4.5,
            "Empty-domain text must meet 4.5:1 contrast.");
    }

    [Fact]
    public void SidebarRefreshTextMeetsNormalTextContrast()
    {
        var document = Workbench();
        var refresh = Named(document, "SidebarRefreshText");
        var border = NearestBorder(refresh);

        var foreground = (string?)refresh.Attribute("Foreground");
        var background = (string?)border.Attribute("Background");

        Assert.NotNull(foreground);
        Assert.NotNull(background);

        Assert.True(
            Contrast(foreground!, background!) >= 4.5,
            "Sidebar refresh text must meet 4.5:1 contrast.");
    }

    [Fact]
    public void DiscoveryAssistantFooterAndNavigationRemainProtected()
    {
        var document = Workbench();

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
            "StatusProviderText",
            "StatusApiText",
            "ResourceListBox",
            "SecondaryWorkspaceList"
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

        Assert.Equal("#E2E8F0",
            (string?)Named(document, "SidebarProviderText")
                .Attribute("Foreground"));

        Assert.Equal("#0F172A",
            (string?)Named(document, "StatusApiText")
                .Attribute("Foreground"));

        Assert.Equal(Surface,
            (string?)Named(document, "ResourceListBox")
                .Attribute("Background"));

        Assert.Equal(Surface,
            (string?)Named(document, "SecondaryWorkspaceList")
                .Attribute("Background"));

        var discoveryCard = NearestBorder(
            Named(document, "DiscoveryStatusText"));

        Assert.Equal("#0D223B",
            (string?)discoveryCard.Attribute("Background"));

        Assert.Equal("#235181",
            (string?)discoveryCard.Attribute("BorderBrush"));

        var assistantCard = NearestBorder(
            Named(document, "AssistantStatusText"));

        Assert.Equal("#152B51",
            (string?)assistantCard.Attribute("Background"));

        Assert.Equal("#2563EB",
            (string?)assistantCard.Attribute("BorderBrush"));
    }
}
