using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A58F2CompactRelationshipFiltersTests
{
    private static XDocument ReadSource()
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
    public void RelationshipFiltersLiveInOneButtonFlyout()
    {
        XNamespace a = "https://github.com/avaloniaui";
        var doc = ReadSource();

        var button = doc.Descendants(a + "Button")
            .Single(x =>
                (string?)x.Attribute("Name")
                == "TopologyRelationshipFiltersButton");

        var flyout = button.Descendants(a + "Flyout")
            .Single();

        var filters = flyout.Descendants(a + "CheckBox")
            .ToList();

        Assert.Equal(9, filters.Count);
        Assert.Equal(
            9,
            filters.Select(x =>
                (string?)x.Attribute("Name")).Distinct().Count());

        Assert.Empty(flyout.Descendants(a + "Button")
            .Where(x =>
                (string?)x.Attribute("Click")
                == "ResetRelationshipFilters_OnClick"));

        var reset = doc.Descendants(a + "Button")
            .Single(x =>
                (string?)x.Attribute("Click")
                == "ResetRelationshipFilters_OnClick");

        Assert.False(reset.Ancestors(a + "Flyout").Any());
    }

    [Fact]
    public void LiveCountRemainsOutsideFlyoutAndColorsDiffer()
    {
        XNamespace a = "https://github.com/avaloniaui";
        var doc = ReadSource();

        var counter = doc.Descendants(a + "TextBlock")
            .Single(x =>
                (string?)x.Attribute("Name")
                == "RelationshipCountText");

        Assert.False(counter.Ancestors(a + "Flyout").Any());

        var filters = doc.Descendants(a + "CheckBox")
            .Where(x =>
                ((string?)x.Attribute("Name"))?
                    .EndsWith("Filter",
                        StringComparison.Ordinal) == true)
            .ToList();

        Assert.Equal(9, filters.Count);

        Assert.True(
            filters.Select(x =>
                (string?)x.Attribute("Foreground"))
                .Distinct().Count() >= 8);
    }

    [Fact]
    public void F4ToolbarAlignmentAndOneLineFilters()
    {
        XNamespace a = "https://github.com/avaloniaui";
        var doc = ReadSource();

        var trigger = doc.Descendants(a + "Button")
            .Single(x =>
                (string?)x.Attribute("Name")
                == "TopologyRelationshipFiltersButton");

        var flyout = trigger.Descendants(a + "Flyout")
            .Single();

        Assert.Equal("BottomEdgeAlignedLeft",
            (string?)flyout.Attribute("Placement"));
        Assert.Empty(flyout.Descendants(a + "ScrollViewer"));
        Assert.Empty(flyout.Descendants(a + "WrapPanel"));

        var row = flyout.Descendants(a + "StackPanel")
            .Single(x => (string?)x.Attribute("Orientation")
                == "Horizontal");

        Assert.Equal(new[]
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
        }, row.Elements(a + "CheckBox")
            .Select(x => (string?)x.Attribute("Name"))
            .ToArray());

        foreach (string name in new[]
        {
            "TopologyExportButton",
            "TopologyArrangeButton",
            "TopologySaveViewButton"
        })
        {
            var button = doc.Descendants(a + "Button")
                .Single(x => (string?)x.Attribute("Name")
                    == name);

            Assert.Equal("100",
                (string?)button.Attribute("Width"));
            Assert.Equal("32",
                (string?)button.Attribute("Height"));
            Assert.Equal("#0F766E",
                (string?)button.Attribute("Background"));
        }

        foreach (string name in new[]
        {
            "TopologyFitButton",
            "TopologyStartPathButton"
        })
        {
            var button = doc.Descendants(a + "Button")
                .Single(x => (string?)x.Attribute("Name")
                    == name);

            Assert.Equal("#0F766E",
                (string?)button.Attribute("Background"));
        }

        var reset = doc.Descendants(a + "Button")
            .Single(x => (string?)x.Attribute("Click")
                == "ResetRelationshipFilters_OnClick");

        Assert.False(reset.Ancestors(a + "Flyout").Any());
    }


    [Fact]
    public void F53RelationshipFlyoutUsesWideScopedPresenter()
    {
        XNamespace a = "https://github.com/avaloniaui";
        var doc = ReadSource();

        var trigger = doc.Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyRelationshipFiltersButton");

        var flyout = trigger.Descendants(a + "Flyout")
            .Single();

        Assert.Equal(
            "td-relationship-wide",
            (string?)flyout.Attribute(
                "FlyoutPresenterClasses"));

        Assert.Equal(
            "BottomEdgeAlignedLeft",
            (string?)flyout.Attribute("Placement"));

        var style = doc.Descendants(a + "Style")
            .Single(node =>
                (string?)node.Attribute("Selector")
                == "FlyoutPresenter.td-relationship-wide");

        var setter = style.Elements(a + "Setter").Single();

        Assert.Equal(
            "MaxWidth",
            (string?)setter.Attribute("Property"));

        Assert.Equal(
            "1200",
            (string?)setter.Attribute("Value"));

        Assert.Empty(flyout.Descendants(a + "ScrollViewer"));
        Assert.Empty(flyout.Descendants(a + "WrapPanel"));

        var row = flyout.Descendants(a + "StackPanel")
            .Single(node =>
                (string?)node.Attribute("Orientation")
                == "Horizontal");

        Assert.Equal(
            new[]
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
            },
            row.Elements(a + "CheckBox")
                .Select(node =>
                    (string?)node.Attribute("Name"))
                .ToArray());

        Assert.True(
            row.Elements(a + "CheckBox")
                .Select(node =>
                    (string?)node.Attribute("Foreground"))
                .Distinct()
                .Count() >= 8);

        var reset = doc.Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Click")
                == "ResetRelationshipFilters_OnClick");

        Assert.False(
            reset.Ancestors(a + "Flyout").Any());
    }

}
