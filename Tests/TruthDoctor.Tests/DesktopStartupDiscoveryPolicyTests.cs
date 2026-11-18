using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class DesktopStartupDiscoveryPolicyTests
{
    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, false, false, false, true)]
    [InlineData(false, true, false, false, true)]
    [InlineData(false, false, true, false, true)]
    [InlineData(false, false, false, true, true)]
    [InlineData(true, false, false, true, true)]
    public void StartupDiscoveryRequiresExplicitRoute(
        bool localDemo,
        bool syntheticSecurityGroup,
        bool awsStandalone,
        bool inProcess,
        bool expected)
    {
        Assert.Equal(
            expected,
            DesktopStartupDiscoveryPolicy.ShouldAutoDiscover(
                localDemo,
                syntheticSecurityGroup,
                awsStandalone,
                inProcess));
    }
}
