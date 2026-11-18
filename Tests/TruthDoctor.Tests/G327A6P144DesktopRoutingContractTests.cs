using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P144DesktopRoutingContractTests
{
    private static string ReadSource(string relativePath)
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                relativePath);

            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TruthDoctor source unavailable: "
            + relativePath);
    }

    private static string WindowSource() =>
        ReadSource(
            "Views/UniversalWorkbench/"
            + "UniversalWorkbenchWindow.axaml.cs");

    [Fact]
    public void OfflineDemoRequiresSecondaryOptIn()
    {
        string source = WindowSource();

        Assert.Contains(
            "TRUTHDOCTOR_INPROCESS_DISCOVERY",
            source);

        Assert.Contains(
            "TRUTHDOCTOR_INPROCESS_LOCAL_DEMO",
            source);

        Assert.Contains(
            "_inProcessLocalDemoRequested",
            source);

        Assert.Matches(
            @"if\s*\(\s*!_inProcessLocalDemoRequested\s*\)"
            + @"\s*\{\s*ApplyFailure\s*\(",
            source);

        Assert.Contains(
            "In-process discovery is not configured.",
            source);
    }

    [Fact]
    public void InProcessRouteUsesBridgeAndOfflineDemoOnly()
    {
        string source = WindowSource();

        int start = source.IndexOf(
            "private async Task LoadStateAsync(",
            StringComparison.Ordinal);

        int end = source.IndexOf(
            "private void ApplyState(",
            StringComparison.Ordinal);

        Assert.True(start >= 0);
        Assert.True(end > start);

        string method = source[start..end];

        Assert.Contains(
            "DesktopDiscoveryRoute.InProcess",
            method);

        Assert.Contains(
            "DesktopInProcessDiscoveryBridge.ExecuteAsync(",
            method);

        Assert.Contains(
            "new StandaloneLocalDemoSession().DiscoverAsync(",
            method);

        Assert.Contains(
            "else if (_syntheticSgDemo is not null)",
            method);

        Assert.Equal(
            1,
            Regex.Matches(
                method,
                @"DesktopInProcessDiscoveryBridge"
                + @"\.ExecuteAsync\s*\(").Count);
    }

    [Fact]
    public void LegacyHttpAndExistingRoutesRemainPresent()
    {
        string source = WindowSource();

        Assert.Contains(
            "DesktopDiscoveryRoute.LegacyHttp",
            source);

        Assert.Contains(
            "_discovery.DiscoverAsync(region)",
            source);

        Assert.Contains(
            "_syntheticSgDemo.DiscoverAsync(",
            source);

        Assert.Contains(
            "_awsSession.DiscoverAsync(",
            source);

        Assert.Contains(
            "_localDemo!.DiscoverAsync(",
            source);

        Assert.Contains(
            "ApplyState(_state)",
            source);
    }

    [Fact]
    public void SelectorRejectsConflictsAndBridgeDoesNotPublish()
    {
        string selector = ReadSource(
            "Controllers/Workbench/"
            + "DesktopDiscoveryRouteSelector.cs");

        string bridge = ReadSource(
            "Controllers/Workbench/"
            + "DesktopInProcessDiscoveryBridge.cs");

        Assert.Contains("if (selected > 1)", selector);

        Assert.Contains(
            "DesktopDiscoveryRoute.LegacyHttp",
            selector);

        Assert.Contains(
            "DesktopDiscoveryRoute.InProcess",
            selector);

        Assert.Contains(
            "sessionDiscovery(",
            bridge);

        Assert.DoesNotContain(
            "SetPlatformState(",
            bridge);

        Assert.DoesNotContain(
            "SetInfrastructureGraph(",
            bridge);

        Assert.DoesNotContain(
            "CommitPreparedDiscoverySnapshot(",
            bridge);

        Assert.DoesNotContain(
            "NotifyCommittedDiscoverySnapshot(",
            bridge);
    }
}
