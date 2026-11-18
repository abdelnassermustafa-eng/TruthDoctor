using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P146RuntimeRouteMatrixTests
{
    // Inputs:
    // localDemo, syntheticSecurityGroup,
    // awsStandalone, inProcess.
    //
    // Expected:
    // -1 = conflicting sources must throw.
    //  0 = LegacyHttp
    //  1 = LocalDemo
    //  2 = SyntheticSecurityGroup
    //  3 = AwsStandalone
    //  4 = InProcess
    //
    // This invokes only the selector.
    // No provider, desktop GUI, HTTP or AWS is executed.

    [Theory]
    [InlineData(false, false, false, false,  0)]
    [InlineData(false, false, false, true,   4)]
    [InlineData(false, false, true,  false,  3)]
    [InlineData(false, false, true,  true,  -1)]
    [InlineData(false, true,  false, false,  2)]
    [InlineData(false, true,  false, true,  -1)]
    [InlineData(false, true,  true,  false, -1)]
    [InlineData(false, true,  true,  true,  -1)]
    [InlineData(true,  false, false, false,  1)]
    [InlineData(true,  false, false, true,  -1)]
    [InlineData(true,  false, true,  false, -1)]
    [InlineData(true,  false, true,  true,  -1)]
    [InlineData(true,  true,  false, false, -1)]
    [InlineData(true,  true,  false, true,  -1)]
    [InlineData(true,  true,  true,  false, -1)]
    [InlineData(true,  true,  true,  true,  -1)]
    public void SelectImplementsFullRouteMatrix(
        bool localDemo,
        bool syntheticSecurityGroup,
        bool awsStandalone,
        bool inProcess,
        int expected)
    {
        if (expected == -1)
        {
            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => DesktopDiscoveryRouteSelector.Select(
                        localDemo,
                        syntheticSecurityGroup,
                        awsStandalone,
                        inProcess));

            Assert.Equal(
                "Conflicting desktop discovery modes "
                + "are not permitted.",
                exception.Message);

            return;
        }

        DesktopDiscoveryRoute actual =
            DesktopDiscoveryRouteSelector.Select(
                localDemo,
                syntheticSecurityGroup,
                awsStandalone,
                inProcess);

        Assert.Equal(
            (DesktopDiscoveryRoute)expected,
            actual);
    }
}
