using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5453BackendStatusContrastTests
{
    private const string DarkStatusSurface = "#0B1728";

    private const string SemanticSurface =
        "{DynamicResource TD.SurfaceBrush}";

    private static XDocument TopBar(
        [CallerFilePath] string sourcePath = "")
    {
        string sourceDirectory =
            Path.GetDirectoryName(sourcePath)!;

        string path = Path.GetFullPath(Path.Combine(
            sourceDirectory,
            "..", "..",
            "Controls", "Workbench",
            "WorkbenchTopBar.axaml"));

        return XDocument.Load(path);
    }

    private static XElement Named(
        XDocument document, string name)
    {
        return Assert.Single(
            document.Descendants().Where(element =>
                (string?)element.Attribute("Name") == name));
    }

    private static double Luminance(string color)
    {
        Assert.Matches("^#[0-9a-fA-F]{6}$", color);

        double Channel(int start)
        {
            double value = Convert.ToInt32(
                color.Substring(start, 2), 16) / 255.0;

            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(1)
             + 0.7152 * Channel(3)
             + 0.0722 * Channel(5);
    }

    private static double Contrast(string a, string b)
    {
        double first = Luminance(a);
        double second = Luminance(b);

        return (Math.Max(first, second) + 0.05)
             / (Math.Min(first, second) + 0.05);
    }

    [Fact]
    public void BackendStatusUsesLocalContrastSafeSurface()
    {
        var document = TopBar();
        var status = Named(document, "BackendStatusText");

        Assert.Equal(
            DarkStatusSurface,
            (string?)status.Attribute("Background"));

        string foreground =
            (string?)status.Attribute("Foreground") ?? "";

        string background =
            (string?)status.Attribute("Background") ?? "";

        Assert.True(
            Contrast(foreground, background) >= 4.5,
            "Backend status must meet normal-text contrast.");
    }

    [Fact]
    public void TopBarAndBackendStatusContractsRemainPreserved()
    {
        var document = TopBar();

        var status = Named(document, "BackendStatusText");
        var indicator = Named(document, "BackendIndicator");

        Assert.Equal(
            "#FDE68A",
            (string?)status.Attribute("Foreground"));

        Assert.Equal(
            "Backend unknown",
            (string?)status.Attribute("Text"));

        Assert.Equal(
            "#FBBF24",
            (string?)indicator.Attribute("Background"));

        var lowerToolbar = Assert.Single(
            document.Descendants().Where(element =>
                element.Name.LocalName == "Border"
                && (string?)element.Attribute("Grid.Row") == "1"));

        Assert.Equal(
            SemanticSurface,
            (string?)lowerToolbar.Attribute("Background"));

        var names = document.Descendants()
            .Attributes("Name")
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.Equal(7, names.Length);

        var eventNames = new[]
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
                eventNames.Contains(attribute.Name.LocalName))
            .ToArray();

        Assert.Equal(28, handlers.Length);

        Assert.Equal(
            "GlobalSearchTextBox_OnTextChanged",
            (string?)Named(document, "GlobalSearchTextBox")
                .Attribute("TextChanged"));

        Assert.Equal(
            "UserButton_OnClick",
            (string?)Named(document, "UserButton")
                .Attribute("Click"));
    }
}
