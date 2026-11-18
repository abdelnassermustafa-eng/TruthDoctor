using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A58D3TopologyResponsiveFiltersTests
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

    [Fact]
    public void AllNineRelationshipFiltersRemainVisibleInOneFlyoutRow()
    {
        XNamespace a = "https://github.com/avaloniaui";
        var doc = ReadTopology();

        var trigger = doc.Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyRelationshipFiltersButton");

        var flyout = trigger.Descendants(a + "Flyout").Single();

        Assert.Equal("BottomEdgeAlignedLeft",
            (string?)flyout.Attribute("Placement"));

        Assert.Empty(flyout.Descendants(a + "WrapPanel"));
        Assert.Empty(flyout.Descendants(a + "ScrollViewer"));

        var row = flyout.Descendants(a + "StackPanel")
            .Single(node =>
                (string?)node.Attribute("Orientation")
                == "Horizontal");

        string[] names =
        {
            "AllRelationshipFilter",
            "ContainmentFilter",
            "PlacementFilter",
            "DependencyFilter",
            "ConnectivityFilter",
            "SecurityFilter",
            "TrafficFilter",
            "AssociationFilter",
            "OtherFilter"
        };

        var checkboxes = row.Elements(a + "CheckBox").ToArray();

        Assert.Equal(9, checkboxes.Length);
        Assert.Equal(names,
            checkboxes.Select(node =>
                (string?)node.Attribute("Name")).ToArray());

        Assert.True(checkboxes
            .Select(node =>
                (string?)node.Attribute("Foreground"))
            .Distinct()
            .Count() >= 8);

        var reset = doc.Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Click")
                == "ResetRelationshipFilters_OnClick");

        Assert.False(reset.Ancestors(a + "Flyout").Any());
        Assert.Equal("1",
            (string?)reset.Attribute("Grid.Column"));

        Assert.Empty(flyout.Descendants(a + "TextBlock")
            .Where(node =>
                (string?)node.Attribute("Text")
                == "Filter relationship types"));
    }

    [Fact]
    public void RelationshipCountsAndResetRemainAccessible()
    {
        XNamespace a = "https://github.com/avaloniaui";
        var doc = ReadTopology();

        Assert.Single(doc.Descendants(a + "TextBlock")
            .Where(node =>
                (string?)node.Attribute("Name")
                == "RelationshipCountText"));

        Assert.Single(doc.Descendants(a + "Button")
            .Where(node =>
                (string?)node.Attribute("Click")
                == "ResetRelationshipFilters_OnClick"));
    }

    [Fact]
    public void SavedViewActionsAlignWithSearchNavigation()
    {
        XNamespace a = "https://github.com/avaloniaui";
        var doc = ReadTopology();

        var topologyGrid = doc.Descendants(a + "Grid")
            .Single(grid =>
                (string?)grid.Attribute("RowDefinitions")
                == "Auto,Auto,Auto,Auto,Auto,*");

        var border = topologyGrid.Elements(a + "Border")
            .Single(node =>
                (string?)node.Attribute("Grid.Row") == "2");

        var grid = border.Element(a + "Grid");
        Assert.NotNull(grid);

        Assert.Equal("90,209,0,100,100,*,100",
            (string?)grid.Attribute("ColumnDefinitions"));
        Assert.Equal(7, grid.Elements().Count());

        var selector = grid.Elements(a + "ComboBox")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologySavedViewComboBox");

        var status = grid.Elements(a + "TextBlock")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologySavedViewStatusText");

        Assert.Equal("1",
            (string?)selector.Attribute("Grid.Column"));
        Assert.Equal("5",
            (string?)status.Attribute("Grid.Column"));

        var search = doc.Descendants(a + "TextBox")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologySearchTextBox");

        var previous = doc.Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyPreviousMatchButton");

        var next = doc.Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyNextMatchButton");

        Assert.Equal("0,0,10,0",
            (string?)search.Attribute("Margin"));

        Assert.Equal("100",
            (string?)previous.Attribute("Width"));

        Assert.Equal("100",
            (string?)next.Attribute("Width"));

        Assert.Equal("0,0,10,0",
            (string?)previous.Attribute("Margin"));

        Assert.Equal("0,0,10,0",
            (string?)next.Attribute("Margin"));

        Assert.Equal("209",
            (string?)selector.Attribute("Width"));

        int previousX = 51 + 8 + 260 + 10;
        int loadX = 90 + 10 + 209 + 10 + 0 + 10;

        int nextX = previousX + 100 + 10;
        int manageX = loadX + 100 + 10;

        Assert.Equal(previousX, loadX);
        Assert.Equal(nextX, manageX);


        foreach (var item in new[]
        {
            new { Name = "TopologyLoadViewButton", Column = "3" },
            new { Name = "TopologyManageViewButton", Column = "4" },
            new { Name = "TopologySaveViewButton", Column = "6" }
        })
        {
            var button = grid.Elements(a + "Button")
                .Single(node =>
                    (string?)node.Attribute("Name") == item.Name);

            Assert.Equal(item.Column,
                (string?)button.Attribute("Grid.Column"));
            Assert.Equal("0",
                (string?)button.Attribute("Grid.Row"));
        }

        var save = grid.Elements(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologySaveViewButton");

        Assert.Equal("100", (string?)save.Attribute("Width"));
        Assert.Equal("32", (string?)save.Attribute("Height"));
        Assert.Equal("#0F766E",
            (string?)save.Attribute("Background"));

        var count = grid.Elements(a + "TextBlock")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologySavedViewCountText");

        Assert.Equal("False",
            (string?)count.Attribute("IsVisible"));
        Assert.Equal("1",
            (string?)count.Attribute("Grid.Row"));
    }
}
