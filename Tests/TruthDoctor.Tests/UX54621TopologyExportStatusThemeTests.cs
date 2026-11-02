using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX54621TopologyExportStatusThemeTests
{
    private const string Primary =
        "{DynamicResource TD.TextPrimaryBrush}";

    private static string Root(
        [CallerFilePath] string source = "")
    {
        return Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(source)!,
                "..", ".."));
    }

    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(Root(), relative));

    private static XElement ExportLabel()
    {
        var document = XDocument.Parse(
            Read("Controls/Topology/TopologyCanvas.axaml"));

        return Assert.Single(
            document.Descendants().Where(e =>
                e.Name.LocalName == "TextBlock" &&
                (string?)e.Attribute("Name") ==
                "TopologyExportStatusText"));
    }

    private static string StatusMethod()
    {
        var code = Read(
            "Controls/Topology/TopologyCanvas.axaml.cs");

        const string signature = "private void SetExportStatus(";
        const string nextMethod =
            "private void TopologyHideMinimapButton_OnClick(";

        var start = code.IndexOf(
            signature, StringComparison.Ordinal);
        Assert.True(start >= 0, "Status method missing");

        var end = code.IndexOf(
            nextMethod, start, StringComparison.Ordinal);
        Assert.True(end > start, "Status method boundary missing");

        return code[start..end];
    }

    [Fact]
    public void InitialStatusUsesSemanticPrimaryText()
    {
        Assert.Equal(
            Primary,
            (string?)ExportLabel().Attribute("Foreground"));
    }

    [Fact]
    public void NormalStatusResolvesSemanticSuccessBrush()
    {
        var method = StatusMethod();

        Assert.Contains(
            "TD.StatusSuccessBrush", method);

        Assert.Contains(
            "TryFindResource(", method);

        Assert.DoesNotContain(
            "\"#86EFAC\"", method);
    }

    [Fact]
    public void ErrorStatusResolvesSemanticErrorBrush()
    {
        var method = StatusMethod();

        Assert.Contains(
            "TD.StatusErrorBrush", method);

        Assert.Contains(
            "TryFindResource(", method);

        Assert.DoesNotContain(
            "\"#FCA5A5\"", method);
    }

    [Fact]
    public void ExistingStatusMessagesAndCallersArePreserved()
    {
        var code = Read(
            "Controls/Topology/TopologyCanvas.axaml.cs");

        var method = StatusMethod();

        Assert.Equal(
            14,
            Regex.Matches(
                code, @"\bSetExportStatus\s*\(").Count);

        Assert.Contains(
            "string message", method);

        Assert.Contains(
            "bool isError = false", method);

        Assert.Contains(
            "TopologyExportStatusText.Text", method);

        Assert.Contains(
            "message;", method);

        Assert.Contains(
            "TopologyExportStatusText.Foreground", method);

        Assert.Contains(
            "isError", method);

        Assert.Equal(
            Primary,
            (string?)ExportLabel().Attribute("Foreground"));
    }
}
