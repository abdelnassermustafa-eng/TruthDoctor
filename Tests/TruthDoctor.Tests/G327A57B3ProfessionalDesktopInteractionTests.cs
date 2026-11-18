using System;
using System.IO;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A57B3ProfessionalDesktopInteractionTests
{
    private static string Source(string relative)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string path = Path.Combine(directory.FullName, relative);

            if (File.Exists(path))
                return File.ReadAllText(path);

            directory = directory.Parent;
        }

        throw new FileNotFoundException(relative);
    }

    [Fact]
    public void PrimaryDiscoveryHasExplicitFluentStateOverrides()
    {
        string app = Source("App.axaml");
        string window = Source(
            "Views/UniversalWorkbench/UniversalWorkbenchWindow.axaml");

        Assert.Contains("Button.td-primary:pointerover", app);
        // Fluent applies state colors to the ContentPresenter.
        // Button.Background alone does not control hover painting.
        Assert.Contains(
            "Button.td-primary:pointerover /template/ ContentPresenter#PART_ContentPresenter",
            app);
        Assert.Contains(
            "Button.td-primary:pressed /template/ ContentPresenter#PART_ContentPresenter",
            app);
        Assert.Contains(
            "Button.td-primary:disabled /template/ ContentPresenter#PART_ContentPresenter",
            app);
        Assert.Contains(
            "Button.td-primary:focus-visible /template/ ContentPresenter#PART_ContentPresenter",
            app);

        Assert.Contains("Button.td-primary:pressed", app);
        Assert.Contains("Button.td-primary:disabled", app);
        Assert.Contains("Button.td-primary:focus-visible", app);
        Assert.Contains("Classes=\"td-primary\"", window);
    }

    [Fact]
    public void ToolbarActionsUseSecondaryHoverAndFocusRoles()
    {
        string app = Source("App.axaml");
        string bar = Source(
            "Controls/Workbench/WorkbenchTopBar.axaml");

        Assert.Contains("Button.td-secondary:pointerover", app);
        Assert.Contains(
            "Button.td-secondary:pointerover /template/ ContentPresenter#PART_ContentPresenter",
            app);
        Assert.Contains(
            "Button.td-secondary:pressed /template/ ContentPresenter#PART_ContentPresenter",
            app);

        Assert.Contains("Button.td-secondary:pressed", app);
        Assert.Contains("Button.td-secondary:focus-visible", app);

        Assert.Equal(
            4,
            System.Text.RegularExpressions.Regex.Matches(
                bar, "Classes=\"td-secondary\"").Count);
    }

    [Fact]
    public void OfflineStatusIsSetAfterControllerContextUpdate()
    {
        string bar = Source(
            "Controls/Workbench/WorkbenchTopBar.axaml.cs");

        string window = Source(
            "Views/UniversalWorkbench/UniversalWorkbenchWindow.axaml.cs");

        Assert.Contains("void SetOfflineFixtureStatus()", bar);
        Assert.Contains("Offline demo ready", bar);

        int loaded = window.IndexOf(
            "_status.ApplyLoaded(",
            StringComparison.Ordinal);

        int offline = window.IndexOf(
            "TopBar.SetOfflineFixtureStatus();",
            StringComparison.Ordinal);

        Assert.True(loaded >= 0 && offline > loaded);
        Assert.Contains("Offline fixture - no cloud API", window);
    }

    [Fact]
    public void ActiveNavigationRetainsSelectedColorsOnHover()
    {
        string window = Source(
            "Views/UniversalWorkbench/UniversalWorkbenchWindow.axaml");

        Assert.Contains(
            "Button.nav-item.active:pointerover /template/ ContentPresenter#PART_ContentPresenter",
            window);

        Assert.Contains(
            "Button.nav-item.active:pressed /template/ ContentPresenter#PART_ContentPresenter",
            window);

        Assert.Contains(
            "Button.nav-item.active:focus-visible /template/ ContentPresenter#PART_ContentPresenter",
            window);

        Assert.Contains(
            "<Setter Property=\"Background\" Value=\"#1D4ED8\"/>",
            window);
    }
}
