using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5452TopBarThemeTests
{
    private const string Surface =
        "{DynamicResource TD.SurfaceBrush}";

    private const string BorderBrush =
        "{DynamicResource TD.BorderBrush}";

    private const string Primary =
        "{DynamicResource TD.TextPrimaryBrush}";

    private static string SourcePath(
        [CallerFilePath] string path = "") => path;

    private static XDocument TopBar()
    {
        string directory =
            Path.GetDirectoryName(SourcePath())!;

        string path = Path.GetFullPath(Path.Combine(
            directory,
            "..", "..",
            "Controls", "Workbench",
            "WorkbenchTopBar.axaml"));

        return XDocument.Load(path);
    }

    private static string LocalName(XElement element) =>
        element.Name.LocalName;

    private static XElement TopBarRow(
        XDocument document, string row)
    {
        return Assert.Single(
            document.Descendants().Where(element =>
                LocalName(element) == "Border" &&
                (string?)element.Attribute("Grid.Row") == row));
    }

    private static XElement Named(
        XDocument document, string name)
    {
        return Assert.Single(
            document.Descendants().Where(element =>
                (string?)element.Attribute("Name") == name));
    }

    [Fact]
    public void UpperMenuStripUsesSemanticSurfaceAndBorder()
    {
        var row = TopBarRow(TopBar(), "0");

        Assert.Equal(
            Surface,
            (string?)row.Attribute("Background"));

        Assert.Equal(
            BorderBrush,
            (string?)row.Attribute("BorderBrush"));
    }

    [Fact]
    public void LowerToolbarStripUsesSemanticSurfaceAndBorder()
    {
        var row = TopBarRow(TopBar(), "1");

        Assert.Equal(
            Surface,
            (string?)row.Attribute("Background"));

        Assert.Equal(
            BorderBrush,
            (string?)row.Attribute("BorderBrush"));
    }

    [Fact]
    public void ProductBrandUsesV3AndSemanticPrimaryText()
    {
        var document = TopBar();

        var brand = Assert.Single(
            document.Descendants().Where(element =>
                LocalName(element) == "TextBlock" &&
                ((string?)element.Attribute("Text"))
                    ?.Contains("TruthDoctor") == true));

        Assert.Equal(
            "◆  TruthDoctor V3",
            (string?)brand.Attribute("Text"));

        Assert.Equal(
            Primary,
            (string?)brand.Attribute("Foreground"));
    }

    [Fact]
    public void MainMenuUsesSemanticPrimaryForeground()
    {
        var menu = Assert.Single(
            TopBar().Descendants().Where(element =>
                LocalName(element) == "Menu"));

        Assert.Equal(
            "Transparent",
            (string?)menu.Attribute("Background"));

        Assert.Equal(
            Primary,
            (string?)menu.Attribute("Foreground"));
    }

    [Fact]
    public void BackendStatusAndInteractionContractsRemainPreserved()
    {
        var document = TopBar();

        var expectedNames = new HashSet<string>
        {
            "GlobalSearchTextBox",
            "BackendIndicator",
            "BackendStatusText",
            "ProviderAccountComboBox",
            "LocationComboBox",
            "UserButton",
            "UserIdentityMenuItem"
        };

        var actualNames = document.Descendants()
            .Attributes("Name")
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.Equal(7, actualNames.Length);
        Assert.True(
            expectedNames.SetEquals(actualNames),
            "Top bar named controls must remain unchanged.");

        var backend = Named(document, "BackendIndicator");
        var status = Named(document, "BackendStatusText");

        Assert.Equal(
            "#FBBF24",
            (string?)backend.Attribute("Background"));

        Assert.Equal(
            "#FDE68A",
            (string?)status.Attribute("Foreground"));

        Assert.Equal(
            "Backend unknown",
            (string?)status.Attribute("Text"));

        Assert.Equal(
            "GlobalSearchTextBox_OnTextChanged",
            (string?)Named(
                document, "GlobalSearchTextBox")
                .Attribute("TextChanged"));

        Assert.Equal(
            "ProviderAccountComboBox_OnSelectionChanged",
            (string?)Named(
                document, "ProviderAccountComboBox")
                .Attribute("SelectionChanged"));

        Assert.Equal(
            "LocationComboBox_OnSelectionChanged",
            (string?)Named(
                document, "LocationComboBox")
                .Attribute("SelectionChanged"));

        Assert.Equal(
            "UserButton_OnClick",
            (string?)Named(
                document, "UserButton")
                .Attribute("Click"));

        var events = new HashSet<string>
        {
            "Click",
            "SelectionChanged",
            "TextChanged",
            "PointerPressed",
            "PointerReleased",
            "DoubleTapped",
            "KeyDown",
            "KeyUp",
            "Tapped",
            "GotFocus",
            "LostFocus"
        };

        var handlers = document.Descendants()
            .Attributes()
            .Where(attribute =>
                events.Contains(attribute.Name.LocalName))
            .ToArray();

        Assert.Equal(28, handlers.Length);

        foreach (var handler in handlers)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(handler.Value));
        }

        var menu = Assert.Single(
            document.Descendants().Where(element =>
                LocalName(element) == "Menu"));

        var menuHeaders = menu.Elements()
            .Where(element => LocalName(element) == "MenuItem")
            .Select(element =>
                (string?)element.Attribute("Header"))
            .ToArray();

        Assert.Equal(
            new[]
            {
                "File", "View", "Discovery",
                "Operations", "Tools", "Window", "Help"
            },
            menuHeaders);

        Assert.Equal(
            "Run Discovery",
            (string?)document.Descendants()
                .Single(element =>
                    LocalName(element) == "MenuItem" &&
                    (string?)element.Attribute("Header")
                        == "Run Discovery")
                .Attribute("Header"));

        Assert.Equal(
            "Search resources, operations, IDs, and tags...",
            (string?)Named(
                document, "GlobalSearchTextBox")
                .Attribute("Watermark"));
    }
}
