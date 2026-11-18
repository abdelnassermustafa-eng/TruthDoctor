using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A58D2ResponsiveTopologyControlsTests
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
                return XDocument.Load(path);

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TopologyCanvas.axaml");
    }

    [Theory]
    [InlineData("0", 6)]
    [InlineData("1", 5)]
    public void SearchAndDomainRowsPreserveControlsWithRightActions(
        string row, int expectedControls)
    {
        XDocument document = ReadTopology();
        XNamespace a = "https://github.com/avaloniaui";

        var topologyGrid = document.Descendants(a + "Grid")
            .Single(grid =>
                (string?)grid.Attribute("RowDefinitions")
                == "Auto,Auto,Auto,Auto,Auto,*");

        var searchDomainBorder = topologyGrid.Elements(a + "Border")
            .Single(border =>
                (string?)border.Attribute("Grid.Row") == "1");

        var commands = searchDomainBorder.Element(a + "Grid");
        Assert.NotNull(commands);

        Assert.Equal("Auto,Auto",
            (string?)commands.Attribute("RowDefinitions"));
        Assert.Equal("Auto,*,Auto",
            (string?)commands.Attribute("ColumnDefinitions"));

        if (row == "0")
        {
            Assert.Equal(6, expectedControls);

            var search = commands.Elements(a + "WrapPanel")
                .Single(panel =>
                    (string?)panel.Attribute("Grid.Row") == "0");

            Assert.Equal("Horizontal",
                (string?)search.Attribute("Orientation"));
            Assert.Equal(expectedControls, search.Elements().Count());

            foreach (string name in new[]
            {
                "TopologySearchTextBox",
                "TopologyPreviousMatchButton",
                "TopologyNextMatchButton",
                "TopologySearchStatusText",
                "TopologyClearSearchButton"
            })
            {
                Assert.Contains(search.Elements(),
                    item => (string?)item.Attribute("Name") == name);
            }

            Assert.Contains(search.Elements(),
                item => (string?)item.Attribute("Text") == "Find");

            var export = commands.Elements(a + "Button")
                .Single(button =>
                    (string?)button.Attribute("Name")
                    == "TopologyExportButton");

            Assert.Equal("0",
                (string?)export.Attribute("Grid.Row"));
            Assert.Equal("2",
                (string?)export.Attribute("Grid.Column"));
            Assert.Equal("100",
                (string?)export.Attribute("Width"));
        }
        else
        {
            Assert.Equal("1", row);
            Assert.Equal(5, expectedControls);

            var domain = commands.Elements(a + "StackPanel")
                .Single(panel =>
                    (string?)panel.Attribute("Grid.Column") == "0");

            var layout = commands.Elements(a + "StackPanel")
                .Single(panel =>
                    (string?)panel.Attribute("Grid.Column") == "1");

            Assert.Equal("1",
                (string?)domain.Attribute("Grid.Row"));
            Assert.Equal("1",
                (string?)layout.Attribute("Grid.Row"));

            Assert.Equal("Horizontal",
                (string?)domain.Attribute("Orientation"));
            Assert.Equal("Horizontal",
                (string?)layout.Attribute("Orientation"));

            Assert.Equal(2, domain.Elements().Count());
            Assert.Equal(2, layout.Elements().Count());

            Assert.Contains(domain.Elements(),
                item => (string?)item.Attribute("Name")
                    == "TopologyDomainComboBox");
            Assert.Contains(domain.Elements(),
                item => (string?)item.Attribute("Text")
                    == "Domain");

            Assert.Contains(layout.Elements(),
                item => (string?)item.Attribute("Name")
                    == "TopologyLayoutComboBox");
            Assert.Contains(layout.Elements(),
                item => (string?)item.Attribute("Text")
                    == "Layout");

            var arrange = commands.Elements(a + "Button")
                .Single(button =>
                    (string?)button.Attribute("Name")
                    == "TopologyArrangeButton");

            Assert.Equal("1",
                (string?)arrange.Attribute("Grid.Row"));
            Assert.Equal("2",
                (string?)arrange.Attribute("Grid.Column"));
            Assert.Equal("100",
                (string?)arrange.Attribute("Width"));

            Assert.Equal(expectedControls,
                domain.Elements().Count()
                + layout.Elements().Count() + 1);
        }
    }
}
