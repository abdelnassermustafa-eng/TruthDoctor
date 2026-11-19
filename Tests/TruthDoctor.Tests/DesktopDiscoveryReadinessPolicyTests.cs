using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class DesktopDiscoveryReadinessPolicyTests
{
    // Route flags:
    // offlineDemo, offlineFixture, standaloneProvider,
    // inProcess, legacyHttp.
    //
    // Configuration and discovery authorization are
    // separate inputs. The policy never executes a route.

    [Theory]
    [InlineData(false, false, false, false, false,
        false, false, "ConfigurationRequired")]
    [InlineData(false, false, true, false, false,
        true, true, "Ready")]
    [InlineData(true, false, false, false, false,
        true, true, "Ready")]
    [InlineData(false, true, false, false, false,
        true, true, "Ready")]
    [InlineData(false, false, false, true, false,
        true, true, "Ready")]
    [InlineData(false, false, false, false, true,
        true, true, "Ready")]
    [InlineData(true, false, true, false, false,
        true, true, "Rejected")]
    [InlineData(false, false, true, false, true,
        true, true, "Rejected")]
    [InlineData(false, false, false, false, true,
        true, false, "Rejected")]
    [InlineData(false, false, true, false, false,
        true, false, "Rejected")]
    [InlineData(false, false, false, true, false,
        false, true, "ConfigurationRequired")]
    [InlineData(false, false, true, false, false,
        false, true, "ConfigurationRequired")]
    public void ReadinessDecisionMatchesApprovedMatrix(
        bool offlineDemo,
        bool offlineFixture,
        bool standaloneProvider,
        bool inProcess,
        bool legacyHttp,
        bool sourceConfigured,
        bool discoveryAuthorized,
        string expected)
    {
        var actual = DesktopDiscoveryReadinessPolicy.Evaluate(
            offlineDemo,
            offlineFixture,
            standaloneProvider,
            inProcess,
            legacyHttp,
            sourceConfigured,
            discoveryAuthorized);

        Assert.Equal(expected, actual.ToString());
    }

    [Fact]
    public void NoSelectionCannotBecomeReadyFromPermissions()
    {
        var actual = DesktopDiscoveryReadinessPolicy.Evaluate(
            offlineDemo: false,
            offlineFixture: false,
            standaloneProvider: false,
            inProcess: false,
            legacyHttp: false,
            sourceConfigured: true,
            discoveryAuthorized: true);

        Assert.Equal(
            "ConfigurationRequired",
            actual.ToString());
    }

    [Fact]
    public void AuthorizationDenialPrecedesMissingConfiguration()
    {
        var actual = DesktopDiscoveryReadinessPolicy.Evaluate(
            offlineDemo: false,
            offlineFixture: false,
            standaloneProvider: true,
            inProcess: false,
            legacyHttp: false,
            sourceConfigured: false,
            discoveryAuthorized: false);

        Assert.Equal("Rejected", actual.ToString());
    }
}
