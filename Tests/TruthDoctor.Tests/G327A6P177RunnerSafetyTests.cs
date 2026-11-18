using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P177RunnerSafetyTests
{
    [Fact]
    public void NullWorkbenchRejectedBeforeDispatch()
    {
        int dispatches = 0;

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = DesktopInProcessDiscoveryRunner.ExecuteAsync(
                null!,
                true,
                true,
                _ =>
                {
                    dispatches++;
                    return Task.CompletedTask;
                });
        });

        Assert.Equal(0, dispatches);
    }

    [Fact]
    public void NullDiscoveryRejectedBeforeDispatch()
    {
        var state = new WorkbenchState();
        int notifications = 0;

        state.Changed += (_, _) => notifications++;

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = DesktopInProcessDiscoveryRunner.ExecuteAsync(
                state,
                true,
                true,
                null!);
        });

        Assert.Equal(0, notifications);
        Assert.False(state.IsDiscovering);
        Assert.Null(state.PlatformState);
    }

    [Fact]
    public async Task OriginalCancellationTokenForwardedOnce()
    {
        var state = new WorkbenchState();
        using var source = new CancellationTokenSource();

        int dispatches = 0;
        CancellationToken observed = default;
        List<bool> loading = [];

        state.Changed += (_, _) =>
            loading.Add(state.IsDiscovering);

        DesktopInProcessExecutionDecision decision =
            await DesktopInProcessDiscoveryRunner.ExecuteAsync(
                state,
                true,
                true,
                token =>
                {
                    dispatches++;
                    observed = token;
                    Assert.True(state.IsDiscovering);
                    return Task.CompletedTask;
                },
                source.Token);

        Assert.Equal(
            DesktopInProcessExecutionDecision.OfflineLocalDemo,
            decision);

        Assert.Equal(1, dispatches);
        Assert.Equal(source.Token, observed);
        Assert.Equal(new[] { true, false }, loading);
        Assert.False(state.IsDiscovering);
        Assert.Null(state.PlatformState);
        Assert.Null(state.InfrastructureGraph);
    }

    [Fact]
    public async Task FailureDoesNotPublishOrSetDuplicateError()
    {
        var state = new WorkbenchState();
        var expected = new InvalidOperationException(
            "synthetic runner failure");

        int dispatches = 0;
        List<bool> loading = [];

        state.Changed += (_, _) =>
            loading.Add(state.IsDiscovering);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () => DesktopInProcessDiscoveryRunner.ExecuteAsync(
                    state,
                    true,
                    true,
                    _ =>
                    {
                        dispatches++;
                        return Task.FromException(expected);
                    }));

        Assert.Same(expected, actual);
        Assert.Equal(1, dispatches);
        Assert.Equal(new[] { true, false }, loading);
        Assert.False(state.IsDiscovering);

        Assert.True(string.IsNullOrEmpty(state.LastError));
        Assert.Null(state.PlatformState);
        Assert.Null(state.InfrastructureGraph);
        Assert.Null(state.InfrastructureGraphIndex);
    }
}
