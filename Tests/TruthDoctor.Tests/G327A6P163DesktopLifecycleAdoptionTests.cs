using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P163DesktopLifecycleAdoptionTests
{
    private static string LoadStateSource()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string path = Path.Combine(
                directory.FullName,
                "Views",
                "UniversalWorkbench",
                "UniversalWorkbenchWindow.axaml.cs");

            if (File.Exists(path))
            {
                string source = File.ReadAllText(path);

                int start = source.IndexOf(
                    "private async Task LoadStateAsync(",
                    StringComparison.Ordinal);

                int end = source.IndexOf(
                    "private void ApplyState(",
                    StringComparison.Ordinal);

                Assert.True(start >= 0 && end > start);

                return source[start..end];
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TruthDoctor desktop source unavailable.");
    }

    [Fact]
    public void DesktopMustInvokeOfflineLifecycleAdapter()
    {
        string source = LoadStateSource();

        if (!P176VerifyTransitiveAdoption(source))
        {
            Assert.Fail(
                "RED: desktop does not invoke "
                + "the tested offline lifecycle adapter.");
        }
    }

    [Fact]
    public void OuterLoadingMustExcludeOfflineRoute()
    {
        string source = LoadStateSource();

        // Source-level adoption guard only:
        // the existing desktop loading assignments must
        // be conditional for NON-InProcess routes.
        // Behavioral acceptance remains a later checkpoint.
        string condition =
            @"if\s*\(\s*selectedRoute\s*!=\s*"
            + @"DesktopDiscoveryRoute\.InProcess\s*\)";

        MatchCollection matches = Regex.Matches(
            source,
            condition,
            RegexOptions.CultureInvariant);

        if (matches.Count < 2)
        {
            Assert.Fail(
                "RED: existing desktop loading transitions "
                + "are not isolated from the offline route.");
        }
    }

    private static bool P176VerifyTransitiveAdoption(
        string desktopSource)
    {
        var location =
            new global::System.IO.DirectoryInfo(
                global::System.AppContext.BaseDirectory);

        while (location is not null)
        {
            string runnerPath =
                global::System.IO.Path.Combine(
                    location.FullName,
                    "Controllers", "Workbench",
                    "DesktopInProcessDiscoveryRunner.cs");

            string coordinatorPath =
                global::System.IO.Path.Combine(
                    location.FullName,
                    "Controllers", "Workbench",
                    "DesktopOfflineExecutionCoordinator.cs");

            if (global::System.IO.File.Exists(runnerPath)
                && global::System.IO.File.Exists(coordinatorPath))
            {
                string runner =
                    global::System.IO.File.ReadAllText(runnerPath);

                string coordinator =
                    global::System.IO.File.ReadAllText(
                        coordinatorPath);

                return desktopSource.Contains("DesktopInProcessDiscoveryRunner.ExecuteAsync(", StringComparison.Ordinal)
            && desktopSource.Contains("DesktopInProcessDiscoveryBridge.ExecuteAsync(", StringComparison.Ordinal)
            && !desktopSource.Contains("DesktopOfflineExecutionCoordinator.ExecuteAsync(", StringComparison.Ordinal)
            && !desktopSource.Contains("DesktopOfflineDiscoveryLifecycle.ExecuteAsync(", StringComparison.Ordinal)
            && runner.Contains("DesktopOfflineExecutionCoordinator.ExecuteAsync(", StringComparison.Ordinal)
            && runner.Contains("DesktopOfflineDiscoveryLifecycle.ExecuteAsync(", StringComparison.Ordinal);
            }

            location = location.Parent;
        }

        return false;
    }

}
