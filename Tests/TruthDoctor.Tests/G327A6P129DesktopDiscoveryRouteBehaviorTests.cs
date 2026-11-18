using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P129DesktopDiscoveryRouteBehaviorTests
{
    [Theory]
    [InlineData(false, false, false, false,
        DesktopDiscoveryRoute.LegacyHttp)]
    [InlineData(true, false, false, false,
        DesktopDiscoveryRoute.LocalDemo)]
    [InlineData(false, true, false, false,
        DesktopDiscoveryRoute.SyntheticSecurityGroup)]
    [InlineData(false, false, true, false,
        DesktopDiscoveryRoute.AwsStandalone)]
    [InlineData(false, false, false, true,
        DesktopDiscoveryRoute.InProcess)]
    public void ExactlyOneRouteIsSelected(
        bool localDemo,
        bool syntheticSecurityGroup,
        bool awsStandalone,
        bool inProcess,
        DesktopDiscoveryRoute expected)
    {
        var actual = DesktopDiscoveryRouteSelector.Select(
            localDemo,
            syntheticSecurityGroup,
            awsStandalone,
            inProcess);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    public void ConflictingModesAreRejectedFailClosed(
        bool localDemo,
        bool syntheticSecurityGroup,
        bool awsStandalone,
        bool inProcess)
    {
        Assert.Throws<InvalidOperationException>(() =>
            DesktopDiscoveryRouteSelector.Select(
                localDemo,
                syntheticSecurityGroup,
                awsStandalone,
                inProcess));
    }
}
