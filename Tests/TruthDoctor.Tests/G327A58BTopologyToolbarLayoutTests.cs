using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A58BTopologyToolbarLayoutTests
{
    private static string ReadSource()
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
                return File.ReadAllText(path);

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TopologyCanvas.axaml");
    }

    [Fact]
    public void PrimaryTopologyToolbarRetainsNavigationAndMovesExport()

    {
        string source = ReadSource();
        XDocument document = XDocument.Parse(source);
        XNamespace a = "https://github.com/avaloniaui";

        var main = document.Descendants(a + "Grid")
            .Single(x =>
                (string?)x.Attribute("RowDefinitions")
                == "Auto,Auto,Auto,Auto,Auto,*");

        var top = main.Elements(a + "Border")
            .Single(x =>
                (string?)x.Attribute("Grid.Row") == "0");

        var toolbar = top.Elements(a + "WrapPanel").Single();

        Assert.Equal("Horizontal",
            (string?)toolbar.Attribute("Orientation"));

        var groups = toolbar.Elements(a + "StackPanel").ToArray();

        Assert.Equal(3, groups.Length);
        Assert.Equal(new[] { 3, 3, 5 },
            groups.Select(x => x.Elements().Count()).ToArray());

        Assert.All(groups, group =>
            Assert.Equal("Horizontal",
                (string?)group.Attribute("Orientation")));

        Assert.Equal(new[]
        {
            "TopologyBackButton",
            "TopologyForwardButton",
            "TopologyHomeButton"
        }, groups[0].Elements()
            .Select(x => (string?)x.Attribute("Name"))
            .ToArray());

        Assert.Equal("Depth",
            (string?)groups[1].Elements().First()
                .Attribute("Text"));

        Assert.Contains(groups[1].Descendants(),
            x => (string?)x.Attribute("Name")
                == "TopologyDepthComboBox");

        Assert.Equal("View",
            (string?)groups[2].Elements().First()
                .Attribute("Text"));

        Assert.Equal(new[]
        {
            "TopologyZoomOutButton",
            "TopologyResetZoomButton",
            "TopologyZoomInButton",
            "TopologyFitButton"
        }, groups[2].Elements(a + "Button")
            .Select(x => (string?)x.Attribute("Name"))
            .ToArray());

        var fit = groups[2].Elements(a + "Button")
            .Single(x => (string?)x.Attribute("Name")
                == "TopologyFitButton");

        Assert.Equal("#0F766E",
            (string?)fit.Attribute("Background"));

        Assert.DoesNotContain(toolbar.Descendants(),
            x => (string?)x.Attribute("Name")
                == "TopologyExportButton");

        var export = document.Descendants(a + "Button")
            .Single(x => (string?)x.Attribute("Name")
                == "TopologyExportButton");

        Assert.Equal("0",
            (string?)export.Attribute("Grid.Row"));
        Assert.Equal("2",
            (string?)export.Attribute("Grid.Column"));
        Assert.Equal("100",
            (string?)export.Attribute("Width"));
        Assert.Single(export.Descendants(a + "MenuFlyout"));
    }

    [Fact]
    public void PrimaryToolbarPreservesActionsAndControls()
    {
        string source = ReadSource();

        foreach (string name in new[]
        {
            "TopologyBackButton",
            "TopologyDepthComboBox",
            "TopologyScopeComboBox",
            "TopologyZoomOutButton",
            "TopologyResetZoomButton",
            "TopologyZoomInButton",
            "TopologyFitButton",
            "TopologyExportButton"
        })
        {
            Assert.Contains(name, source);
        }

        foreach (string handler in new[]
        {
            "TopologyBackButton_OnClick",
            "TopologyZoomOutButton_OnClick",
            "TopologyResetZoomButton_OnClick",
            "TopologyZoomInButton_OnClick",
            "TopologyFitButton_OnClick"
        })
        {
            Assert.Contains(handler, source);
        }
    }
}
