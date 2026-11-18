using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A58F62TopologySecondaryStatesTests
{
    private static XDocument ReadTopology()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string path = Path.Combine(
                directory.FullName,
                "Controls",
                "Topology",
                "TopologyCanvas.axaml");

            if (File.Exists(path))
            {
                return XDocument.Load(path);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TopologyCanvas.axaml");
    }

    [Fact]
    public void FiveSecondaryActionsPreserveDisabledAndInteractionContracts()
    {
        XNamespace a = "https://github.com/avaloniaui";
        XDocument doc = ReadTopology();

        string[] names =
        {
            "TopologyPreviousMatchButton",
            "TopologyNextMatchButton",
            "TopologyClearSearchButton",
            "TopologyLoadViewButton",
            "TopologyManageViewButton",
        };

        foreach (string name in names)
        {
            XElement button = doc.Descendants(a + "Button")
                .Single(element =>
                    (string?)element.Attribute("Name") == name);

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

        string[] selectors =
        {
            "Button.td-topology-secondary",
            "Button.td-topology-secondary:pointerover",
            "Button.td-topology-secondary:pressed",
            "Button.td-topology-secondary:focus-visible",
            "Button.td-topology-secondary:disabled",
            "Button.td-topology-secondary:disabled:pointerover",
            "Button.td-topology-secondary:disabled /template/ ContentPresenter#PART_ContentPresenter"
        };

        foreach (string selector in selectors)
        {
            Assert.Single(
                doc.Descendants(a + "Style")
                    .Where(style =>
                        (string?)style.Attribute("Selector")
                        == selector));
        }

        XElement disabled = doc.Descendants(a + "Style")
            .Single(style =>
                (string?)style.Attribute("Selector")
                == "Button.td-topology-secondary:disabled");

        var values = disabled.Elements(a + "Setter")
            .ToDictionary(
                item => (string?)item.Attribute("Property") ?? "",
                item => (string?)item.Attribute("Value") ?? "");

        Assert.Equal("#E2E8F0", values["Background"]);
        Assert.Equal("#475569", values["Foreground"]);
        Assert.Equal("1", values["Opacity"]);

        foreach (string name in new[]
        {
            "TopologyExportButton",
            "TopologyArrangeButton",
            "TopologySaveViewButton",
            "TopologyFitButton",
            "TopologyStartPathButton"
        })
        {
            XElement action = doc.Descendants(a + "Button")
                .Single(button =>
                    (string?)button.Attribute("Name") == name);

            Assert.Equal(
                "td-topology-teal",
                (string?)action.Attribute("Classes"));

            Assert.Equal(
                "#0F766E",
                (string?)action.Attribute("Background"));
        }

        XElement clearPath = doc.Descendants(a + "Button")
            .Single(button =>
                (string?)button.Attribute("Name")
                == "TopologyClearPathButton");

        Assert.Equal(
            "td-topology-teal td-topology-clear-path",
            (string?)clearPath.Attribute("Classes"));

        Assert.Equal(
            "False",
            (string?)clearPath.Attribute("IsEnabled"));

        Assert.Equal(
            "#0F766E",
            (string?)clearPath.Attribute("Background"));

        Assert.Equal(
            "White",
            (string?)clearPath.Attribute("Foreground"));

        Assert.Equal(
            "100",
            (string?)clearPath.Attribute("Width"));

        Assert.Equal(
            "32",
            (string?)clearPath.Attribute("Height"));


    }
}
