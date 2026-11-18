using System;
using System.IO;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P142ExplicitOfflineDesktopWiringTests
{
    private static string ReadDesktopSource()
    {
        const string path =
            "Views/UniversalWorkbench/"
            + "UniversalWorkbenchWindow.axaml.cs";

        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                path);

            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TruthDoctor desktop source unavailable.");
    }

    [Fact]
    public void ExplicitOfflineDemoMustUseGuardedBridge()
    {
        string source = ReadDesktopSource();

        Assert.True(
            source.Contains(
                "TRUTHDOCTOR_INPROCESS_LOCAL_DEMO",
                StringComparison.Ordinal)
            && source.Contains(
                "DesktopInProcessDiscoveryBridge.ExecuteAsync(",
                StringComparison.Ordinal),
            "RED: explicit offline desktop session "
            + "bridge wiring is not implemented.");
    }
}
