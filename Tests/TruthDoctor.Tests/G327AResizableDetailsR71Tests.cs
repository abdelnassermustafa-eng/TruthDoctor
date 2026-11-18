using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AResizableDetailsR71Tests
{
    [Fact]
    public void ResourceDetailsHasResizableLeftBoundary()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        string? path = null;

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "Views", "UniversalWorkbench",
                "UniversalWorkbenchWindow.axaml");

            if (File.Exists(candidate))
            {
                path = candidate;
                break;
            }

            directory = directory.Parent;
        }

        Assert.NotNull(path);

        var xml = XDocument.Load(path!);
        var splitters = xml.Descendants()
            .Where(element =>
                element.Name.LocalName == "GridSplitter")
            .ToArray();

        var splitter = Assert.Single(splitters);
        Assert.Equal("2",
            (string?)splitter.Attribute("Grid.Column"));
        Assert.Equal("PreviousAndNext",
            (string?)splitter.Attribute("ResizeBehavior"));
        Assert.Equal("Columns",
            (string?)splitter.Attribute("ResizeDirection"));

        var columns = xml.Descendants()
            .Where(element =>
                element.Name.LocalName == "Grid.ColumnDefinitions")
            .Select(element => element.Elements().ToArray())
            .Single(items =>
                items.Length == 4
                && (string?)items[0].Attribute("Width") == "290"
                && (string?)items[2].Attribute("Width") == "8");

        Assert.Equal("320",
            (string?)columns[1].Attribute("MinWidth"));
        Assert.Equal("300",
            (string?)columns[3].Attribute("MinWidth"));

        var details = Assert.Single(
            xml.Descendants().Where(element =>
                element.Name.LocalName == "StackPanel"
                && (string?)element.Attribute("Name")
                    == "ResourceDetailsPanel"));

        var border = details.Ancestors()
            .First(element =>
                element.Name.LocalName == "Border"
                && element.Attribute("Grid.Column") != null);

        Assert.Equal("3",
            (string?)border.Attribute("Grid.Column"));
    }
}
