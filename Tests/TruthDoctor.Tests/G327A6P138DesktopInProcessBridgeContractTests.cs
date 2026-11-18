using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P138DesktopInProcessBridgeContractTests
{
    [Fact]
    public void StandaloneSessionBridgeHasExplicitTypedContract()
    {
        const string fullName =
            "TruthDoctor.Controllers.Workbench."
            + "DesktopInProcessDiscoveryBridge";

        Assembly assembly = typeof(WorkbenchState).Assembly;

        Type? bridgeType = assembly.GetType(
            fullName,
            throwOnError: false);

        if (bridgeType is null)
        {
            Assert.Fail(
                "RED: desktop in-process discovery bridge "
                + "is not implemented.");
        }

        Type sessionDelegate = typeof(
            Func<
                WorkbenchState,
                string,
                CancellationToken,
                Task<PlatformState>>);

        MethodInfo? method = bridgeType.GetMethod(
            "ExecuteAsync",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(WorkbenchState),
                sessionDelegate,
                typeof(string),
                typeof(CancellationToken)
            ],
            modifiers: null);

        Assert.NotNull(method);

        Assert.Equal(
            typeof(Task<PlatformState>),
            method.ReturnType);
    }
}
