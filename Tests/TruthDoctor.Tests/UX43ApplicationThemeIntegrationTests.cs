using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class UX43ApplicationThemeIntegrationTests
{
    private static readonly string Root =
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(SourcePath())!,
            "..",
            ".."));

    private static string SourcePath(
        [CallerFilePath] string path = "") => path;

    [Fact]
    public void AppUsesProfessionalLightAsStartupVariant()
    {
        var document = XDocument.Load(
            Path.Combine(Root, "App.axaml"));

        var requested = document.Root?
            .Attribute("RequestedThemeVariant")?.Value;

        Assert.Equal("Light", requested);
    }

    [Fact]
    public void ApplicationStartupInstallsSemanticThemeResources()
    {
        var source = File.ReadAllText(
            Path.Combine(Root, "App.axaml.cs"));

        Assert.Contains(
            "TruthDoctorAvaloniaThemeAdapter.CreateResources(",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "Resources[",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationPreservesFluentAndDataGridThemes()
    {
        var document = XDocument.Load(
            Path.Combine(Root, "App.axaml"));

        var elements = document.Descendants().ToArray();

        Assert.Contains(
            elements,
            element => element.Name.LocalName == "FluentTheme");

        Assert.Contains(
            elements,
            element =>
                element.Name.LocalName == "StyleInclude"
                && (element.Attribute("Source")?.Value
                    ?? "").Contains(
                        "Avalonia.Controls.DataGrid",
                        StringComparison.Ordinal));
    }
}
