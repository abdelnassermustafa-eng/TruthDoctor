using System;
using System.Reflection;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P127DesktopDiscoveryRouteSelectionTests
{
    [Fact]
    public void ExplicitInProcessModeMustNotReplaceLegacyDefault()
    {
        // P127 establishes a test-first routing contract.
        //
        // No desktop, AWS, provider, HTTP or discovery
        // operation is executed by this test.
        //
        // Proposed provider-neutral static selector:
        //
        // DesktopDiscoveryRouteSelector.Select(
        //     bool localDemo,
        //     bool syntheticSecurityGroup,
        //     bool awsStandalone,
        //     bool inProcess)
        //
        // Required baseline:
        //   all false  -> LegacyHttp
        //   inProcess  -> InProcess
        //
        // No default routing change is authorized here.

        Assembly assembly =
            typeof(WorkbenchDiscoveryController).Assembly;

        Type? selector = assembly.GetType(
            "TruthDoctor.Controllers.Workbench."
            + "DesktopDiscoveryRouteSelector");

        Assert.True(
            selector is not null,
            "RED: explicit desktop discovery route selector "
            + "is not implemented.");

        MethodInfo? select = selector!.GetMethod(
            "Select",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[]
            {
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool)
            },
            modifiers: null);

        Assert.NotNull(select);

        object? legacy = select!.Invoke(
            null,
            new object[] { false, false, false, false });

        object? inProcess = select.Invoke(
            null,
            new object[] { false, false, false, true });

        Assert.Equal("LegacyHttp", legacy?.ToString());
        Assert.Equal("InProcess", inProcess?.ToString());
    }
}
