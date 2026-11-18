using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P124OfflineWorkbenchDiscoveryBehaviorTests
{
    private static WorkbenchState NewState()
    {
        // Resolve the concrete state without making any
        // assumptions about a source-level constructor API.
        return (WorkbenchState)(
            Activator.CreateInstance(typeof(WorkbenchState))
            ?? throw new InvalidOperationException(
                "WorkbenchState cannot be constructed."));
    }

    [Fact]
    public void NullOfflineCallbackIsRejected()
    {
        var callback = (Func<
            string?,
            CancellationToken,
            Task<PlatformState>>)null!;

        Assert.Throws<ArgumentNullException>(() =>
            new WorkbenchDiscoveryController(
                NewState(),
                callback));
    }

    [Fact]
    public async Task OfflineCallbackRunsAndReportsFailure()
    {
        var state = NewState();
        int invocations = 0;
        int started = 0;
        int completed = 0;
        string? failure = null;

        var controller = new WorkbenchDiscoveryController(
            state,
            (string? location, CancellationToken token) =>
            {
                invocations++;
                return Task.FromException<PlatformState>(
                    new InvalidOperationException(
                        "offline-callback-sentinel"));
            });

        controller.DiscoveryStarted += (_, _) => started++;
        controller.DiscoveryCompleted += (_, _) => completed++;
        controller.DiscoveryFailed += (_, message) =>
            failure = message;

        bool succeeded =
            await controller.DiscoverAsync("offline-region");

        Assert.False(succeeded);
        Assert.Equal(1, invocations);
        Assert.Equal(1, started);
        Assert.Equal(0, completed);
        Assert.Equal("offline-callback-sentinel", failure);
    }

    [Fact]
    public async Task OfflineCallbackReceivesRequestedLocation()
    {
        var state = NewState();
        string? observedLocation = null;

        var controller = new WorkbenchDiscoveryController(
            state,
            (string? location, CancellationToken token) =>
            {
                observedLocation = location;
                return Task.FromException<PlatformState>(
                    new InvalidOperationException("offline-test"));
            });

        bool succeeded =
            await controller.DiscoverAsync("synthetic-location");

        Assert.False(succeeded);
        Assert.Equal(
            "synthetic-location",
            observedLocation);
    }

    [Fact]
    public async Task OfflineCallbackReceivesCallerCancellationToken()
    {
        var state = NewState();
        CancellationToken observedToken = default;
        using var source = new CancellationTokenSource();

        var controller = new WorkbenchDiscoveryController(
            state,
            (string? location, CancellationToken token) =>
            {
                observedToken = token;
                return Task.FromException<PlatformState>(
                    new InvalidOperationException("offline-test"));
            });

        bool succeeded = await controller.DiscoverAsync(
            "synthetic-location",
            source.Token);

        Assert.False(succeeded);
        Assert.Equal(source.Token, observedToken);
    }

    [Fact]
    public async Task ActiveDiscoveryRejectsAnotherOfflineInvocation()
    {
        var state = NewState();
        state.IsDiscovering = true;

        int invocations = 0;
        int started = 0;

        var controller = new WorkbenchDiscoveryController(
            state,
            (string? location, CancellationToken token) =>
            {
                invocations++;
                return Task.FromException<PlatformState>(
                    new InvalidOperationException("should-not-run"));
            });

        controller.DiscoveryStarted += (_, _) => started++;

        bool succeeded =
            await controller.DiscoverAsync("synthetic-location");

        Assert.False(succeeded);
        Assert.Equal(0, invocations);
        Assert.Equal(0, started);
    }
}
