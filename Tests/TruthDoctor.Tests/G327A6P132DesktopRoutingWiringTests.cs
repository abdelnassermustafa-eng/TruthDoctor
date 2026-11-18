using System;
using System.IO;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P132DesktopRoutingWiringTests
{
    private static string DesktopSource()
    {
        const string relative =
            "Views/UniversalWorkbench/"
            + "UniversalWorkbenchWindow.axaml.cs";

        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                relative);

            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "TruthDoctor desktop source unavailable "
            + "for source-contract acceptance.");
    }

    [Fact]
    public void LegacyHttpRemainsTheDefaultDesktopBranch()
    {
        string source = DesktopSource();

        Assert.Contains(
            "DesktopDiscoveryRoute.LegacyHttp",
            source);

        Assert.Contains(
            "succeeded = await _discovery.DiscoverAsync(region);",
            source);

        Assert.Equal(
            DesktopDiscoveryRoute.LegacyHttp,
            DesktopDiscoveryRouteSelector.Select(
                false, false, false, false));
    }

    [Fact]
    public void DesktopSelectionUsesAllFourModeInputs()
    {
        string source = DesktopSource();

        Assert.Contains(
            "DesktopDiscoveryRouteSelector.Select(",
            source);

        Assert.Contains(
            "localDemo: _localDemo is not null",
            source);

        Assert.Contains(
            "syntheticSecurityGroup: _syntheticSgDemo is not null",
            source);

        Assert.Contains(
            "awsStandalone: _awsSession is not null",
            source);

        Assert.Contains(
            "inProcess: _inProcessDiscoveryRequested",
            source);
    }

    [Fact]
    public void InProcessOptInWithoutSourceFailsClosed()
    {
        string source = DesktopSource();

        Assert.Contains(
            "\"TRUTHDOCTOR_INPROCESS_DISCOVERY\"",
            source);

        Assert.Contains(
            "selectedRoute == DesktopDiscoveryRoute.InProcess",
            source);

        Assert.Contains(
            "In-process discovery is not configured.",
            source);

        Assert.Equal(
            DesktopDiscoveryRoute.InProcess,
            DesktopDiscoveryRouteSelector.Select(
                false, false, false, true));
    }

    [Fact]
    public void ExistingStandaloneSessionCallsRemainPresent()
    {
        string source = DesktopSource();

        Assert.Contains(
            "await _syntheticSgDemo.DiscoverAsync(",
            source);

        Assert.Contains(
            "await _awsSession.DiscoverAsync(",
            source);

        Assert.Contains(
            "await _localDemo!.DiscoverAsync(",
            source);

        Assert.Contains(
            "catch (InvalidOperationException exception)",
            source);

        Assert.Contains(
            "ApplyFailure(exception.Message);",
            source);
    }
}
