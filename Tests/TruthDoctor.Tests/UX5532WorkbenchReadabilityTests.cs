using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX5532WorkbenchReadabilityTests
{
    private const string WindowCode =
        "Views/UniversalWorkbench/UniversalWorkbenchWindow.axaml.cs";

    private const string DetailsCode =
        "Controllers/Workbench/WorkbenchDetailsController.cs";

    private const string WindowMarkup =
        "Views/UniversalWorkbench/UniversalWorkbenchWindow.axaml";

    private static string RepositoryRoot(
        [CallerFilePath] string source = "")
    {
        return Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(source)!,
                "..", ".."));
    }

    private static string Read(string path)
    {
        return File.ReadAllText(
            Path.Combine(RepositoryRoot(), path));
    }

    private static string Between(
        string text,
        string first,
        string second)
    {
        var begin = text.IndexOf(
            first, StringComparison.Ordinal);

        Assert.True(
            begin >= 0, $"Missing method anchor: {first}");

        var end = text.IndexOf(
            second, begin + first.Length,
            StringComparison.Ordinal);

        Assert.True(
            end > begin, $"Missing next boundary: {second}");

        return text[begin..end];
    }

    [Fact]
    public void ResourceRowsUseReadableLightForegrounds()
    {
        var code = Read(WindowCode);

        var method = Between(
            code,
            "private ListBoxItem CreateResourceRow(",
            "private void RenderSecondaryResources(");

        Assert.Contains("Brush(\"#172B4D\")", method);
        Assert.Contains("Brush(\"#526581\")", method);

        Assert.DoesNotContain("Brush(\"#F8FAFC\")", method);
        Assert.DoesNotContain("Brush(\"#94A3B8\")", method);
        Assert.DoesNotContain("Brush(\"#CBD5E1\")", method);

        Assert.Contains("StateBrush(resource.State)", method);
        Assert.Contains("Tag = resource", method);
    }

    [Fact]
    public void OperationsRowsUseReadableLightForegrounds()
    {
        var code = Read(WindowCode);

        var method = Between(
            code,
            "private ListBoxItem CreateOperationsSearchRow(",
            "private void RenderOperationsSearch(");

        Assert.Contains("Brush(\"#172B4D\")", method);
        Assert.Contains("Brush(\"#526581\")", method);

        Assert.DoesNotContain("Brush(\"#F8FAFC\")", method);
        Assert.DoesNotContain("Brush(\"#94A3B8\")", method);

        Assert.Contains("Tag =", method);
        Assert.Contains("resource", method);
    }

    [Fact]
    public void ResourceDetailsUseReadableLightForegrounds()
    {
        var code = Read(DetailsCode);

        Assert.Contains("Brush(\"#172B4D\")", code);
        Assert.Contains("Brush(\"#526581\")", code);

        Assert.DoesNotContain("Brush(\"#F8FAFC\")", code);
        Assert.DoesNotContain("Brush(\"#94A3B8\")", code);
        Assert.DoesNotContain("Brush(\"#E2E8F0\")", code);

        Assert.Contains("private static void AddSectionTitle(", code);
        Assert.Contains("private static void AddDetail(", code);
        Assert.Contains("detailsPanel.Children.Add(grid)", code);
    }

    [Fact]
    public void DomainNavigationUsesSemanticPrimaryText()
    {
        var document = XDocument.Parse(Read(WindowMarkup));

        var style = Assert.Single(
            document.Descendants().Where(element =>
                element.Name.LocalName == "Style" &&
                (string?)element.Attribute("Selector") ==
                "Button.domain-item"));

        var foreground = Assert.Single(
            style.Elements().Where(element =>
                element.Name.LocalName == "Setter" &&
                (string?)element.Attribute("Property") ==
                "Foreground"));

        Assert.Equal(
            "{DynamicResource TD.TextPrimaryBrush}",
            (string?)foreground.Attribute("Value"));
    }

    [Fact]
    public void DomainCardRenderingAndTopologyRemainProtected()
    {
        var code = Read(WindowCode);

        var cards = Between(
            code,
            "var card = new Border",
            "private void BuildFilters(");

        Assert.Contains(
            "Background = Brush(visual.Background)", cards);
        Assert.Contains(
            "BorderBrush = Brush(visual.Border)", cards);
        Assert.Contains(
            "Foreground = Brush(visual.Foreground)", cards);
        Assert.Contains(
            "Foreground = Brush(\"#FFFFFF\")", cards);

        var markup = Read(WindowMarkup);
        Assert.Contains(
            "Name=\"TopologyWorkspace\"", markup);

        Assert.True(
            File.Exists(Path.Combine(
                RepositoryRoot(),
                "Controls/Topology/TopologyCanvas.axaml")));
    }
}
