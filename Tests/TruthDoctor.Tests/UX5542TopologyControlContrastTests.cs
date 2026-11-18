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

        // F6.2: Load and Manage use a local secondary
        // style instead of local color resource values.
        // Keep their disabled-state and readability
        // contracts explicit.
        foreach (string name in new[]
        {
            "TopologyLoadViewButton",
            "TopologyManageViewButton"
        })
        {
            var button = Named(document, name);

            Assert.Equal(
                "td-topology-secondary",
                (string?)button.Attribute("Classes"));

            Assert.Equal(
                "False",
                (string?)button.Attribute("IsEnabled"));

            Assert.Null(button.Attribute("Background"));
            Assert.Null(button.Attribute("Foreground"));
            Assert.Null(button.Attribute("BorderBrush"));
        }

        XNamespace a = "https://github.com/avaloniaui";

        var baseStyle = document.Descendants(a + "Style")
            .Single(style =>
                (string?)style.Attribute("Selector")
                == "Button.td-topology-secondary");

        var disabledStyle = document.Descendants(a + "Style")
            .Single(style =>
                (string?)style.Attribute("Selector")
                == "Button.td-topology-secondary:disabled");

        var normal = baseStyle.Elements(a + "Setter")
            .ToDictionary(
                setter =>
                    (string?)setter.Attribute("Property") ?? "",
                setter =>
                    (string?)setter.Attribute("Value") ?? "");

        var disabled = disabledStyle.Elements(a + "Setter")
            .ToDictionary(
                setter =>
                    (string?)setter.Attribute("Property") ?? "",
                setter =>
                    (string?)setter.Attribute("Value") ?? "");

        Assert.Equal("#F8FAFC", normal["Background"]);
        Assert.Equal("#0F172A", normal["Foreground"]);
        Assert.Equal("#CBD5E1", normal["BorderBrush"]);

        Assert.Equal("#E2E8F0", disabled["Background"]);
        Assert.Equal("#475569", disabled["Foreground"]);
        Assert.Equal("#CBD5E1", disabled["BorderBrush"]);
        Assert.Equal("1", disabled["Opacity"]);

        foreach (string selector in new[]
        {
            "Button.td-topology-secondary:pointerover",
            "Button.td-topology-secondary:pressed",
            "Button.td-topology-secondary:focus-visible",
            "Button.td-topology-secondary:disabled:pointerover",
            "Button.td-topology-secondary:disabled /template/ ContentPresenter#PART_ContentPresenter"
        })
        {
            Assert.Single(document.Descendants(a + "Style")
                .Where(style =>
                    (string?)style.Attribute("Selector")
                    == selector));
        }
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

        Assert.Equal(51, names.Length);

        // D5.8-F2: the one authorized new named control.
        Assert.Equal(
            "Button",
            Named(document,
                "TopologyRelationshipFiltersButton")
                .Name.LocalName);
        Assert.Equal(51, names.Distinct().Count());

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
