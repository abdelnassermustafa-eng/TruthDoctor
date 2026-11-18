using System;
using System.Reflection;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P148DualOptInPolicyContractTests
{
    [Theory]
    [InlineData(false, false, "LegacyHttp")]
    [InlineData(false, true, "LegacyHttp")]
    [InlineData(true, false, "FailClosed")]
    [InlineData(true, true, "OfflineLocalDemo")]
    public void ResolvesExplicitDualOptInPolicy(
        bool inProcessRequested,
        bool offlineLocalDemoAuthorized,
        string expectedDecision)
    {
        Assembly assembly =
            typeof(DesktopDiscoveryRouteSelector).Assembly;

        Type? policy = assembly.GetType(
            "TruthDoctor.Controllers.Workbench."
            + "DesktopInProcessExecutionPolicy",
            throwOnError: false);

        if (policy is null)
        {
            Assert.Fail(
                "RED: desktop dual-opt-in execution policy "
                + "is not implemented.");
        }

        MethodInfo? resolve = policy.GetMethod(
            "Resolve",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(bool),
                typeof(bool)
            ],
            modifiers: null);

        Assert.NotNull(resolve);

        Assert.True(
            resolve.ReturnType.IsEnum,
            "Policy decision must use a typed enum.");

        object? result = resolve.Invoke(
            null,
            [
                inProcessRequested,
                offlineLocalDemoAuthorized
            ]);

        Assert.NotNull(result);

        Assert.Equal(
            expectedDecision,
            Enum.GetName(resolve.ReturnType, result));
    }
}
