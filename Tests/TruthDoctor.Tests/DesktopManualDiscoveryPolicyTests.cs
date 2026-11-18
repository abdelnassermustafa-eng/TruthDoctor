using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class DesktopManualDiscoveryPolicyTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    public void LegacyHttpRequiresRouteAndExplicitAuthorization(
        bool legacyRoute, bool authorized, bool expected)
    {
        Assert.Equal(expected,
            DesktopManualDiscoveryPolicy.MayUseLegacyHttp(legacyRoute, authorized));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void OfflineDemoRequiresUserSelection(bool selected, bool expected)
    {
        Assert.Equal(expected,
            DesktopManualDiscoveryPolicy.MayUseOfflineDemo(selected));
    }
}
