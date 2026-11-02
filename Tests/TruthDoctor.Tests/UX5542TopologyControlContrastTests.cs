using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5542TopologyControlContrastTests
{
    private const string MarkupPath =
        "Controls/Topology/TopologyCanvas.axaml";

    private const string Primary =
        "{DynamicResource TD.TextPrimaryBrush}";

    private const string Surface =
        "{DynamicResource TD.SurfaceBrush}";

    private const string Border =
        "{DynamicResource TD.BorderBrush}";

    private static string Root(
        [CallerFilePath] string path = "")
    {
        return Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(path)!,
                "..", ".."));
    }

    private static XDocument Load()
    {
        return XDocument.Load(
            Path.Combine(Root(), MarkupPath));
    }

    private static XElement Named(
        XDocument document,
        string name)
    {
        return Assert.Single(
            document.Descendants()
                .Where(element =>
                    (string?)element.Attribute("Name") == name));
    }

    private static void AssertLightControl(
        XDocument document,
        string name,
        string tag)
    {
        var control = Named(document, name);

        Assert.Equal(tag, control.Name.LocalName);
        Assert.Equal(
            Surface,
            (string?)control.Attribute("Background"));
        Assert.Equal(
            Primary,
            (string?)control.Attribute("Foreground"));
        Assert.Equal(
            Border,
            (string?)control.Attribute("BorderBrush"));
    }

    [Fact]
    public void SearchFieldUsesReadableLightThemeControls()
    {
        var document = Load();

        AssertLightControl(
            document,
            "TopologySearchTextBox",
            "TextBox");

        Assert.Equal(
            "Search this topology...",
            (string?)Named(
                document,
                "TopologySearchTextBox")
                .Attribute("Watermark"));
    }

    [Fact]
    public void DomainAndLayoutSelectorsUseReadableLightTheme()
    {
        var document = Load();

        AssertLightControl(
            document,
            "TopologyDomainComboBox",
            "ComboBox");

        AssertLightControl(
            document,
            "TopologyLayoutComboBox",
            "ComboBox");
    }

    [Fact]
    public void SavedViewSelectorUsesReadableLightTheme()
    {
        var document = Load();

        AssertLightControl(
            document,
            "TopologySavedViewComboBox",
            "ComboBox");

        Assert.NotNull(
            Named(
                document,
                "TopologySavedViewComboBox")
                .Attribute("SelectionChanged"));
    }

    [Fact]
    public void SavedViewSecondaryActionsUseReadableLightTheme()
    {
        var document = Load();

        foreach (var name in new[]
        {
            "TopologyLoadViewButton",
            "TopologyManageViewButton"
        })
        {
            AssertLightControl(
                document,
                name,
                "Button");
        }

        Assert.Equal(
            "False",
            (string?)Named(
                document,
                "TopologyLoadViewButton")
                .Attribute("IsEnabled"));

        Assert.Equal(
            "False",
            (string?)Named(
                document,
                "TopologyManageViewButton")
                .Attribute("IsEnabled"));
    }

    [Fact]
    public void TopologyGraphAndExistingChromeRemainProtected()
    {
        var document = Load();

        Assert.NotNull(document.Root);

        var names = document.Descendants()
            .Attributes("Name")
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.Equal(50, names.Length);
        Assert.Equal(50, names.Distinct().Count());

        Assert.Equal(
            "Button",
            Named(
                document,
                "TopologyFitButton")
                .Name.LocalName);

        Assert.Equal(
            "Button",
            Named(
                document,
                "TopologyExportButton")
                .Name.LocalName);

        Assert.Equal(
            "TextBlock",
            Named(
                document,
                "TopologyExportStatusText")
                .Name.LocalName);

        Assert.Equal(
            Primary,
            (string?)Named(
                document,
                "TopologyExportStatusText")
                .Attribute("Foreground"));

        Assert.NotNull(
            Named(document, "TopologyMinimapPanel"));

        Assert.NotNull(
            Named(document, "TopologyMinimapViewport"));
    }
}
