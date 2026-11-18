using System;
using System.IO;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P151DesktopPolicyAdoptionContractTests
{
    private static string ReadDesktopSource()
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
                return File.ReadAllText(path);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TruthDoctor desktop source unavailable.");
    }

    private static string LoadStateSource()
    {
        string source = ReadDesktopSource();

        int start = source.IndexOf(
            "private async Task LoadStateAsync(",
            StringComparison.Ordinal);

        int end = source.IndexOf(
            "private void ApplyState(",
            StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start);

        return source[start..end];
    }

    [Fact]
    public void DesktopMustInvokeAcceptedPurePolicy()
    {
        string source = LoadStateSource();

        if (!P176VerifyTransitiveAdoption(source))
        {
            Assert.Fail(
                "RED: desktop has not adopted the "
                + "tested dual-opt-in policy.");
        }
    }

    [Fact]
    public void DesktopMustHandleTypedFailClosedDecision()
    {
        string source = LoadStateSource();

        if (!source.Contains(
            "DesktopInProcessExecutionDecision.FailClosed",
            StringComparison.Ordinal))
        {
            Assert.Fail(
                "RED: desktop does not handle the "
                + "typed FailClosed policy decision.");
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
            && coordinator.Contains("DesktopInProcessExecutionPolicy.Resolve(", StringComparison.Ordinal);
            }

            location = location.Parent;
        }

        return false;
    }

}
