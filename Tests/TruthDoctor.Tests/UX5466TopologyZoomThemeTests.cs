using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5466TopologyZoomThemeTests
{
    private const string Surface =
        "{DynamicResource TD.SurfaceBrush}";

    private const string Border =
        "{DynamicResource TD.BorderBrush}";

    private const string Primary =
        "{DynamicResource TD.TextPrimaryBrush}";

    private static readonly string[] ZoomNames =
    {
        "TopologyZoomOutButton",
        "TopologyResetZoomButton",
        "TopologyZoomInButton"
    };

    private static XDocument Load(
        [CallerFilePath] string sourcePath = "")
    {
        string directory = Path.GetDirectoryName(
            sourcePath)!;

        string path = Path.GetFullPath(
            Path.Combine(
                directory,
                "..", "..",
                "Controls", "Topology",
                "TopologyCanvas.axaml"));

        return XDocument.Load(path);
    }

    private static XElement Named(
        XDocument document,
        string name)
    {
        return Assert.Single(
            document.Descendants()
                .Where(element =>
                    (string?)element.Attribute("Name")
                    == name));
    }

    [Fact]
    public void ZoomButtonsUseSemanticLightBackgrounds()
    {
        var document = Load();

        foreach (string name in ZoomNames)
        {
            var button = Named(document, name);

            Assert.Equal(
                Surface,
                (string?)button.Attribute("Background"));
        }
    }

    [Fact]
    public void ZoomButtonsUseSemanticTextAndBorders()
    {
        var document = Load();

        foreach (string name in ZoomNames)
        {
            var button = Named(document, name);

            Assert.Equal(
                Primary,
                (string?)button.Attribute("Foreground"));

            Assert.Equal(
                Border,
                (string?)button.Attribute("BorderBrush"));
        }
    }

    [Fact]
    public void ZoomControlsAndTopologyStructureRemainProtected()
    {
        var document = Load();

        var buttons = ZoomNames
            .Select(name => Named(document, name))
            .ToArray();

        Assert.All(buttons, button =>
        {
            Assert.Equal("Button", button.Name.LocalName);

            Assert.False(string.IsNullOrWhiteSpace(
                (string?)button.Attribute("Click")));
        });

        Assert.NotNull(buttons[0].Parent);

        Assert.True(ReferenceEquals(
            buttons[0].Parent, buttons[1].Parent));

        Assert.True(ReferenceEquals(
            buttons[1].Parent, buttons[2].Parent));

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
        Assert.Equal(
            names.Length,
            names.Distinct().Count());

        var eventNames = new HashSet<string>
        {
            "Click",
            "SelectionChanged",
            "TextChanged",
            "PointerPressed",
            "PointerReleased",
            "PointerMoved",
            "PointerWheelChanged",
            "DoubleTapped",
            "Tapped",
            "KeyDown",
            "KeyUp",
            "GotFocus",
            "LostFocus",
            "SizeChanged",
            "Loaded"
        };

        var events = document.Descendants()
            .Attributes()
            .Where(attribute =>
                eventNames.Contains(
                    attribute.Name.LocalName))
            .ToArray();

        Assert.Equal(33, events.Length);

        Assert.All(events, item =>
            Assert.False(
                string.IsNullOrWhiteSpace(
                    item.Value)));
    }
}
