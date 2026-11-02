using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX54612TopologyStaticLabelContrastTests
{
    private const string Primary =
        "{DynamicResource TD.TextPrimaryBrush}";

    private const string Surface =
        "{DynamicResource TD.SurfaceBrush}";

    private static XDocument Load(
        [CallerFilePath] string sourceFile = "")
    {
        string directory = Path.GetDirectoryName(sourceFile)!;

        string path = Path.GetFullPath(
            Path.Combine(
                directory,
                "..", "..",
                "Controls", "Topology",
                "TopologyCanvas.axaml"));

        return XDocument.Load(path);
    }

    private static XElement Label(
        XDocument document,
        string text)
    {
        return Assert.Single(
            document.Descendants()
                .Where(element =>
                    element.Name.LocalName == "TextBlock"
                    && (string?)element.Attribute("Text") == text));
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

    [Fact]
    public void DepthUsesSemanticPrimaryText()
    {
        var label = Label(Load(), "Depth");

        Assert.Equal(
            Primary,
            (string?)label.Attribute("Foreground"));
    }

    [Fact]
    public void ViewUsesSemanticPrimaryText()
    {
        var label = Label(Load(), "View");

        Assert.Equal(
            Primary,
            (string?)label.Attribute("Foreground"));
    }

    [Fact]
    public void ToolbarAndDynamicExportStatusRemainProtected()
    {
        var document = Load();

        var depth = Label(document, "Depth");
        var view = Label(document, "View");

        Assert.NotNull(depth.Parent);
        Assert.Same(depth.Parent, view.Parent);

        var toolbar = depth.Ancestors()
            .FirstOrDefault(element =>
                element.Name.LocalName == "Border"
                && (string?)element.Attribute("Background")
                    == Surface);

        Assert.NotNull(toolbar);
        Assert.Contains(view, toolbar!.Descendants());

        var export = Named(
            document,
            "TopologyExportStatusText");

        Assert.Equal(
            "#7F9AB8",
            (string?)export.Attribute("Foreground"));

        foreach (string name in new[]
        {
            "TopologyBackButton",
            "TopologyForwardButton",
            "TopologyHomeButton",
            "TopologyZoomOutButton",
            "TopologyResetZoomButton",
            "TopologyZoomInButton"
        })
        {
            var button = Named(document, name);

            Assert.Equal(
                "Button",
                button.Name.LocalName);

            Assert.Equal(
                Primary,
                (string?)button.Attribute("Foreground"));

            Assert.False(string.IsNullOrWhiteSpace(
                (string?)button.Attribute("Click")));
        }

        Assert.NotNull(
            Named(document, "TopologyFitButton"));

        Assert.NotNull(
            Named(document, "TopologyExportButton"));

        Assert.NotNull(
            Named(document, "TopologyMinimapPanel"));

        Assert.NotNull(
            Named(document, "TopologyMinimapViewport"));

        var names = document.Descendants()
            .Attributes("Name")
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.Equal(50, names.Length);
        Assert.Equal(50, names.Distinct().Count());

        // Full event preservation is independently checked
        // against the published AXAML in the GREEN audit.
    }
}
