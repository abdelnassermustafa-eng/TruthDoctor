using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5462TopologyNavigationThemeTests
{
    private const string Surface =
        "{DynamicResource TD.SurfaceBrush}";

    private const string Border =
        "{DynamicResource TD.BorderBrush}";

    private const string Primary =
        "{DynamicResource TD.TextPrimaryBrush}";

    private static XDocument LoadTopology(
        [CallerFilePath] string sourcePath = "")
    {
        string directory =
            Path.GetDirectoryName(sourcePath)!;

        string path = Path.GetFullPath(Path.Combine(
            directory, "..", "..",
            "Controls", "Topology",
            "TopologyCanvas.axaml"));

        return XDocument.Load(path);
    }

    private static XElement Named(
        XDocument document, string name)
    {
        return Assert.Single(
            document.Descendants().Where(element =>
                (string?)element.Attribute("Name") == name));
    }

    private static string[] NavigationNames() =>
        new[]
        {
            "TopologyBackButton",
            "TopologyForwardButton",
            "TopologyHomeButton"
        };

    private static XElement NavigationToolbar(
        XDocument document)
    {
        var back = Named(document, "TopologyBackButton");
        var forward = Named(document, "TopologyForwardButton");
        var home = Named(document, "TopologyHomeButton");

        var shared = back.Ancestors()
            .Where(element =>
                element.Name.LocalName == "Border")
            .FirstOrDefault(element =>
                forward.Ancestors().Contains(element) &&
                home.Ancestors().Contains(element));

        Assert.NotNull(shared);
        return shared!;
    }

    [Fact]
    public void NavigationToolbarUsesSemanticLightSurface()
    {
        var toolbar = NavigationToolbar(LoadTopology());

        Assert.Equal(
            Surface,
            (string?)toolbar.Attribute("Background"));

        Assert.Equal(
            Border,
            (string?)toolbar.Attribute("BorderBrush"));
    }

    [Fact]
    public void NavigationButtonsUseSemanticLightPalette()
    {
        var document = LoadTopology();

        foreach (string name in NavigationNames())
        {
            var button = Named(document, name);

            Assert.Equal(
                Surface,
                (string?)button.Attribute("Background"));

            Assert.Equal(
                Primary,
                (string?)button.Attribute("Foreground"));

            Assert.Equal(
                Border,
                (string?)button.Attribute("BorderBrush"));
        }
    }

    [Fact]
    public void TopologyControlsAndInteractionsRemainProtected()
    {
        var document = LoadTopology();

        var expectedHandlers = new Dictionary<string, string>
        {
            ["TopologyBackButton"] =
                "TopologyBackButton_OnClick",
            ["TopologyForwardButton"] =
                "TopologyForwardButton_OnClick",
            ["TopologyHomeButton"] =
                "TopologyHomeButton_OnClick"
        };

        foreach (var entry in expectedHandlers)
        {
            var button = Named(document, entry.Key);

            Assert.Equal(
                entry.Value,
                (string?)button.Attribute("Click"));
        }

        var namedControls = document.Descendants()
            .Attributes("Name")
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.Equal(50, namedControls.Length);
        Assert.Equal(
            namedControls.Length,
            namedControls.Distinct().Count());

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

        Assert.All(events, attribute =>
            Assert.False(
                string.IsNullOrWhiteSpace(
                    attribute.Value)));

        Assert.NotNull(
            Named(document, "TopologySurface"));

        Assert.NotNull(
            Named(document, "RootCanvas"));

        Assert.NotNull(
            Named(document, "TopologyMinimapPanel"));

        Assert.NotNull(
            Named(document, "TopologyMinimapViewport"));
    }
}
