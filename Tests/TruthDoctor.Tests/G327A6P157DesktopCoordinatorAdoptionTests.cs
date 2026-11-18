using System;
using System.IO;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P157DesktopCoordinatorAdoptionTests
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
    public void DesktopMustInvokeTestedOfflineCoordinator()
    {
        string source = LoadStateSource();

        if (!P176VerifyTransitiveAdoption(source))
        {
            Assert.Fail(
                "RED: desktop does not invoke the "
                + "tested offline execution coordinator.");
        }
    }

    [Fact]
    public void DesktopMustNotDuplicatePolicyEvaluation()
    {
        string source = LoadStateSource();

        if (source.Contains(
            "DesktopInProcessExecutionPolicy.Resolve(",
            StringComparison.Ordinal))
        {
            Assert.Fail(
                "RED: desktop still evaluates policy directly "
                + "instead of delegating to coordinator.");
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
            && runner.Contains("DesktopOfflineExecutionCoordinator.ExecuteAsync(", StringComparison.Ordinal);
            }

            location = location.Parent;
        }

        return false;
    }

}
