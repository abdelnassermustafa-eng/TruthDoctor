using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A58F65DomainLayoutAlignmentTests
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
            {
                return XDocument.Load(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TopologyCanvas.axaml");
    }

    [Fact]
    public void DomainMatchesSearchWidthAndLayoutAlignsRight()
    {
        XNamespace a = "https://github.com/avaloniaui";
        XDocument document = LoadTopology();

        XElement domain = document.Descendants(a + "ComboBox")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyDomainComboBox");

        XElement search = document.Descendants(a + "TextBox")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologySearchTextBox");

        Assert.Equal(
            "260",
            (string?)domain.Attribute("Width"));

        Assert.Equal(
            (string?)search.Attribute("Width"),
            (string?)domain.Attribute("Width"));

        XElement layout = document.Descendants(a + "ComboBox")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyLayoutComboBox");

        XElement group = layout.Ancestors(a + "StackPanel")
            .First();

        Assert.Equal(
            "1",
            (string?)group.Attribute("Grid.Row"));

        Assert.Equal(
            "1",
            (string?)group.Attribute("Grid.Column"));

        Assert.Equal(
            "Horizontal",
            (string?)group.Attribute("Orientation"));

        Assert.Equal(
            "Right",
            (string?)group.Attribute("HorizontalAlignment"));

        Assert.Equal(
            "0,0,12,0",
            (string?)group.Attribute("Margin"));

        Assert.Equal(
            "136",
            (string?)layout.Attribute("Width"));

        foreach (string name in new[]
        {
            "TopologyExportButton",
            "TopologyArrangeButton",
            "TopologySaveViewButton",
            "TopologyClearPathButton"
        })
        {
            XElement button = document.Descendants(a + "Button")
                .Single(node =>
                    (string?)node.Attribute("Name") == name);

            Assert.Contains(
                "td-topology-teal",
                (string?)button.Attribute("Classes") ?? "");

            Assert.Equal(
                "100",
                (string?)button.Attribute("Width"));
        }

        XElement clearPath = document.Descendants(a + "Button")
            .Single(node =>
                (string?)node.Attribute("Name")
                == "TopologyClearPathButton");

        Assert.Equal(
            "False",
            (string?)clearPath.Attribute("IsEnabled"));
    }
}
