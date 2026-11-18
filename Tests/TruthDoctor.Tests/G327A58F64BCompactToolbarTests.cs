using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A58F64BCompactToolbarTests
{
    private static XDocument LoadTopology()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "Controls",
                "Topology",
                "TopologyCanvas.axaml");

            if (File.Exists(candidate))
                return XDocument.Load(candidate);

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TopologyCanvas.axaml");
    }

    [Fact]
    public void NavigationSearchAndClearPathHaveCompactContracts()
    {
        XNamespace a = "https://github.com/avaloniaui";
        XDocument document = LoadTopology();

        var navigation = new[]
        {
            new {
                Name = "TopologyBackButton",
                Content = "←",
                Description = "Back — previous topology view"
            },
            new {
                Name = "TopologyForwardButton",
                Content = "→",
                Description = "Forward — next topology view"
            },
            new {
                Name = "TopologyHomeButton",
                Content = "⌂",
                Description = "Home — topology overview"
            }
        };

        foreach (var expected in navigation)
        {
            XElement button = document
                .Descendants(a + "Button")
                .Single(node =>
                    (string?)node.Attribute("Name")
                    == expected.Name);

            Assert.Equal(
                expected.Content,
                (string?)button.Attribute("Content"));
            Assert.Equal(
                expected.Description,
                (string?)button.Attribute("ToolTip.Tip"));
            Assert.Equal(
                expected.Description,
                (string?)button.Attribute(
                    "AutomationProperties.Name"));
            Assert.Equal(
                "40", (string?)button.Attribute("Width"));
            Assert.Equal(
                "32", (string?)button.Attribute("Height"));
            Assert.Equal(
                "False",
                (string?)button.Attribute("IsEnabled"));

            Assert.NotNull(button.Attribute("Click"));
        }

        XElement find = document.Descendants(a + "TextBlock")
            .Single(node =>
                (string?)node.Attribute("Text") == "Find");

        Assert.Equal(
            "51",
            (string?)find.Attribute("Width"));
        Assert.Equal(
            "0,0,8,0", (string?)find.Attribute("Margin"));

        XElement clearPath = document
            .Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyClearPathButton");

        Assert.Equal(
            "32", (string?)clearPath.Attribute("Height"));
        Assert.Equal(
            "100",
            (string?)clearPath.Attribute("MinWidth"));
        Assert.Equal(
            "td-topology-teal td-topology-clear-path",
            (string?)clearPath.Attribute("Classes"));
        Assert.Equal(
            "False",
            (string?)clearPath.Attribute("IsEnabled"));


        Assert.Equal(
            "100",
            (string?)clearPath.Attribute("Width"));
        Assert.Equal(
            "#0F766E",
            (string?)clearPath.Attribute("Background"));
        Assert.Equal(
            "White",
            (string?)clearPath.Attribute("Foreground"));
        Assert.Equal(
            "#14B8A6",
            (string?)clearPath.Attribute("BorderBrush"));

        foreach (string name in new[]
        {
            "TopologyExportButton",
            "TopologyArrangeButton",
            "TopologySaveViewButton",
            "TopologyFitButton",
            "TopologyStartPathButton"
        })
        {
            XElement primary = document
                .Descendants(a + "Button")
                .Single(node =>
                    (string?)node.Attribute("Name") == name);

            Assert.Equal(
                "td-topology-teal",
                (string?)primary.Attribute("Classes"));
            Assert.Equal(
                "#0F766E",
                (string?)primary.Attribute("Background"));
        }
    }
}
