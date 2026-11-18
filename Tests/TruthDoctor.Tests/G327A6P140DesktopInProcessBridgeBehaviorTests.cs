using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P140DesktopInProcessBridgeBehaviorTests
{
    [Fact]
    public async Task DelegatesExactlyOnceAndForwardsInputs()
    {
        var workbench = new WorkbenchState();
        var session = new StandaloneLocalDemoSession();

        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;

        int calls = 0;
        int notifications = 0;

        workbench.Changed += (_, _) => notifications++;

        async Task<PlatformState> Discover(
            WorkbenchState receivedWorkbench,
            string? receivedLocation,
            CancellationToken receivedToken)
        {
            calls++;

            Assert.Same(workbench, receivedWorkbench);
            Assert.Equal("p140-offline", receivedLocation);
            Assert.Equal(token, receivedToken);

            return await session.DiscoverAsync(
                receivedWorkbench,
                receivedLocation,
                receivedToken);
        }

        PlatformState result =
            await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                workbench,
                Discover,
                "p140-offline",
                token);

        Assert.Equal(1, calls);
        Assert.Equal(1, notifications);
        Assert.Same(result, workbench.PlatformState);
        Assert.NotNull(workbench.InfrastructureGraph);
        Assert.NotNull(workbench.InfrastructureGraphIndex);
    }

    [Fact]
    public async Task BridgeDoesNotRepublishReturnedPlatform()
    {
        var workbench = new WorkbenchState();
        var session = new StandaloneLocalDemoSession();

        int notifications = 0;
        int calls = 0;

        workbench.Changed += (_, _) => notifications++;

        PlatformState committed =
            await session.DiscoverAsync(workbench);

        Assert.Equal(1, notifications);

        PlatformState returned =
            await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                workbench,
                (state, location, token) =>
                {
                    calls++;
                    Assert.Same(workbench, state);
                    return Task.FromResult(committed);
                },
                null,
                CancellationToken.None);

        Assert.Equal(1, calls);
        Assert.Equal(1, notifications);
        Assert.Same(committed, returned);
        Assert.Same(committed, workbench.PlatformState);
    }

    [Fact]
    public async Task PreCancelledRequestNeverInvokesDelegate()
    {
        var workbench = new WorkbenchState();
        using var cancellation = new CancellationTokenSource();

        cancellation.Cancel();

        int calls = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
                await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                    workbench,
                    (state, location, token) =>
                    {
                        calls++;
                        throw new InvalidOperationException(
                            "Delegate must not execute.");
                    },
                    "offline",
                    cancellation.Token));

        Assert.Equal(0, calls);
        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
    }

    [Fact]
    public async Task NullWorkbenchIsRejectedBeforeDelegation()
    {
        int calls = 0;

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () =>
                await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                    null!,
                    (state, location, token) =>
                    {
                        calls++;
                        throw new InvalidOperationException();
                    },
                    null,
                    CancellationToken.None));

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task NullDelegateIsRejected()
    {
        var workbench = new WorkbenchState();

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () =>
                await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                    workbench,
                    null!,
                    null,
                    CancellationToken.None));

        Assert.Null(workbench.PlatformState);
    }

    [Fact]
    public async Task SessionFailurePropagatesWithoutPublication()
    {
        var workbench = new WorkbenchState();

        int calls = 0;
        int notifications = 0;

        workbench.Changed += (_, _) => notifications++;

        var expected =
            new InvalidOperationException("Synthetic failure.");

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                        workbench,
                        (state, location, token) =>
                        {
                            calls++;
                            return Task.FromException<PlatformState>(
                                expected);
                        },
                        null,
                        CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(1, calls);
        Assert.Equal(0, notifications);
        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
    }

    [Fact]
    public async Task NullDiscoveryTaskIsRejected()
    {
        var workbench = new WorkbenchState();

        int calls = 0;

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                        workbench,
                        (state, location, token) =>
                        {
                            calls++;
                            return null!;
                        },
                        null,
                        CancellationToken.None));

        Assert.Equal(
            "In-process discovery returned no task.",
            exception.Message);

        Assert.Equal(1, calls);
        Assert.Null(workbench.PlatformState);
    }

    [Fact]
    public async Task NullPlatformResultIsRejected()
    {
        var workbench = new WorkbenchState();

        int calls = 0;

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await DesktopInProcessDiscoveryBridge.ExecuteAsync(
                        workbench,
                        (state, location, token) =>
                        {
                            calls++;

                            return Task.FromResult<PlatformState>(
                                null!);
                        },
                        null,
                        CancellationToken.None));

        Assert.Equal(
            "In-process discovery returned no platform state.",
            exception.Message);

        Assert.Equal(1, calls);
        Assert.Null(workbench.PlatformState);
    }
}
