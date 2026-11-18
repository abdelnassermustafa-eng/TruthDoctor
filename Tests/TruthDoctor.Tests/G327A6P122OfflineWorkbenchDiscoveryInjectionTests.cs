using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P122OfflineWorkbenchDiscoveryInjectionTests
{
    [Fact]
    public void WorkbenchDiscoveryControllerMustAcceptOfflineDiscoverySource()
    {
        // This test requires a new, explicitly injected
        // offline discovery constructor. The existing
        // HTTP-backed constructor must remain available.
        //
        // It does not execute HTTP, a provider or AWS.
        //
        // Proposed constructor:
        //   WorkbenchDiscoveryController(
        //       WorkbenchState state,
        //       Func<string?, CancellationToken,
        //            Task<PlatformState>> offlineDiscovery)
        //
        // Nullable reference annotations do not change
        // the runtime reflection signature.

        Type callback = typeof(
            Func<string, CancellationToken, Task<PlatformState>>);

        ConstructorInfo? constructor =
            typeof(WorkbenchDiscoveryController).GetConstructor(
                new[]
                {
                    typeof(WorkbenchState),
                    callback
                });

        Assert.True(
            constructor is not null,
            "RED: provider-neutral offline discovery injection "
            + "constructor not implemented.");

        Assert.True(constructor!.IsPublic);
    }
}
