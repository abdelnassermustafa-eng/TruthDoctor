using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P171HeadlessWorkbenchStateTests
{
    private static Task<DesktopInProcessExecutionDecision> ExecuteAsync(
        Func<CancellationToken, Task> discovery,
        WorkbenchState state,
        Action<Exception> reportFailure,
        CancellationToken cancellationToken = default)
    {
        return DesktopOfflineExecutionCoordinator.ExecuteAsync(
            true,
            true,
            token => DesktopOfflineDiscoveryLifecycle.ExecuteAsync(
                discovery,
                isLoading =>
                {
                    state.IsDiscovering = isLoading;
                    state.NotifyChanged();
                },
                reportFailure,
                token),
            cancellationToken);
    }

    [Fact]
    public async Task SuccessNotifiesActualWorkbenchStateExactlyTwice()
    {
        var state = new WorkbenchState();
        List<bool> observations = [];
        List<Exception> failures = [];
        int dispatches = 0;

        state.Changed += (_, _) =>
            observations.Add(state.IsDiscovering);

        DesktopInProcessExecutionDecision result =
            await ExecuteAsync(
                _ =>
                {
                    dispatches++;
                    Assert.True(state.IsDiscovering);
                    Assert.Equal(new[] { true }, observations);
                    return Task.CompletedTask;
                },
                state,
                failures.Add);

        Assert.Equal(
            DesktopInProcessExecutionDecision.OfflineLocalDemo,
            result);

        Assert.Equal(1, dispatches);
        Assert.Equal(new[] { true, false }, observations);
        Assert.False(state.IsDiscovering);
        Assert.Empty(failures);
    }

    [Fact]
    public async Task FailureClearsStateAndPreservesOneError()
    {
        var state = new WorkbenchState();
        List<bool> observations = [];
        List<Exception> failures = [];
        int dispatches = 0;

        InvalidOperationException expected =
            new("synthetic state-level discovery failure");

        state.Changed += (_, _) =>
            observations.Add(state.IsDiscovering);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ExecuteAsync(
                    _ =>
                    {
                        dispatches++;
                        return Task.FromException(expected);
                    },
                    state,
                    failures.Add));

        Assert.Same(expected, actual);
        Assert.Equal(1, dispatches);
        Assert.Equal(new[] { true, false }, observations);
        Assert.False(state.IsDiscovering);
        Assert.Single(failures);
        Assert.Same(expected, failures[0]);

        // The real desktop catches an ordinary exception
        // and records its message. Exercise the real
        // WorkbenchState method without guessing whether
        // SetFailure emits an additional notification.
        state.SetFailure(actual.Message);

        Assert.Equal(expected.Message, state.LastError);
        Assert.False(state.IsDiscovering);
    }

    [Fact]
    public async Task CancellationClearsStateWithoutOrdinaryFailure()
    {
        var state = new WorkbenchState();
        List<bool> observations = [];
        List<Exception> failures = [];
        int dispatches = 0;

        using CancellationTokenSource source = new();

        state.Changed += (_, _) =>
            observations.Add(state.IsDiscovering);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(
                token =>
                {
                    dispatches++;
                    source.Cancel();
                    return Task.FromCanceled(token);
                },
                state,
                failures.Add,
                source.Token));

        Assert.Equal(1, dispatches);
        Assert.Equal(new[] { true, false }, observations);
        Assert.False(state.IsDiscovering);
        Assert.Empty(failures);
        Assert.True(string.IsNullOrEmpty(state.LastError));
    }

    [Fact]
    public async Task PreCancellationDoesNotNotifyOrDispatch()
    {
        var state = new WorkbenchState();
        List<bool> observations = [];
        List<Exception> failures = [];
        int dispatches = 0;

        using CancellationTokenSource source = new();
        source.Cancel();

        state.Changed += (_, _) =>
            observations.Add(state.IsDiscovering);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(
                _ =>
                {
                    dispatches++;
                    return Task.CompletedTask;
                },
                state,
                failures.Add,
                source.Token));

        Assert.Equal(0, dispatches);
        Assert.Empty(observations);
        Assert.False(state.IsDiscovering);
        Assert.Empty(failures);
        Assert.True(string.IsNullOrEmpty(state.LastError));
    }
}
