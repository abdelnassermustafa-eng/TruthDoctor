using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P174DesktopRunnerContractTests
{
    private static async Task<DesktopInProcessExecutionDecision>
        ExecuteRunnerAsync(
            WorkbenchState workbench,
            bool inProcessRequested,
            bool localDemoRequested,
            Func<CancellationToken, Task> discovery,
            CancellationToken cancellationToken = default)
    {
        Type? runnerType = typeof(WorkbenchState)
            .Assembly.GetType(
                "TruthDoctor.Controllers.Workbench."
                + "DesktopInProcessDiscoveryRunner");

        if (runnerType is null)
        {
            Assert.Fail(
                "P174 RED: DesktopInProcessDiscoveryRunner "
                + "has not been implemented.");
        }

        MethodInfo? method = runnerType!.GetMethod(
            "ExecuteAsync",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(WorkbenchState),
                typeof(bool),
                typeof(bool),
                typeof(Func<CancellationToken, Task>),
                typeof(CancellationToken)
            ],
            modifiers: null);

        Assert.NotNull(method);

        object? result = method!.Invoke(
            null,
            [
                workbench,
                inProcessRequested,
                localDemoRequested,
                discovery,
                cancellationToken
            ]);

        Task<DesktopInProcessExecutionDecision> operation =
            Assert.IsType<
                Task<DesktopInProcessExecutionDecision>>(
                result);

        return await operation;
    }

    [Fact]
    public async Task AuthorizedSuccessExecutesOnceAndBalancesState()
    {
        var state = new WorkbenchState();

        List<bool> observedLoading = [];
        int dispatches = 0;

        state.Changed += (_, _) =>
            observedLoading.Add(state.IsDiscovering);

        DesktopInProcessExecutionDecision decision =
            await ExecuteRunnerAsync(
                state,
                true,
                true,
                _ =>
                {
                    dispatches++;
                    Assert.True(state.IsDiscovering);
                    return Task.CompletedTask;
                });

        Assert.Equal(
            DesktopInProcessExecutionDecision.OfflineLocalDemo,
            decision);

        Assert.Equal(1, dispatches);
        Assert.Equal(
            new[] { true, false },
            observedLoading);
        Assert.False(state.IsDiscovering);
    }

    [Fact]
    public async Task OrdinaryFailurePropagatesAndClearsState()
    {
        var state = new WorkbenchState();

        List<bool> observedLoading = [];
        int dispatches = 0;

        var expected = new InvalidOperationException(
            "synthetic runner discovery failure");

        state.Changed += (_, _) =>
            observedLoading.Add(state.IsDiscovering);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () => ExecuteRunnerAsync(
                    state,
                    true,
                    true,
                    _ =>
                    {
                        dispatches++;

                        return Task.FromException(
                            expected);
                    }));

        Assert.Same(expected, actual);
        Assert.Equal(1, dispatches);

        Assert.Equal(
            new[] { true, false },
            observedLoading);

        Assert.False(state.IsDiscovering);

        // The desktop is the ordinary-failure owner.
        // The runner must not independently call
        // SetFailure or create duplicate notifications.
        Assert.True(
            string.IsNullOrEmpty(state.LastError));
    }

    [Fact]
    public async Task CancellationPropagatesWithoutGenericFailure()
    {
        var state = new WorkbenchState();

        List<bool> observedLoading = [];
        int dispatches = 0;

        using var source =
            new CancellationTokenSource();

        state.Changed += (_, _) =>
            observedLoading.Add(state.IsDiscovering);

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () => ExecuteRunnerAsync(
                state,
                true,
                true,
                token =>
                {
                    dispatches++;
                    source.Cancel();

                    return Task.FromCanceled(token);
                },
                source.Token));

        Assert.Equal(1, dispatches);

        Assert.Equal(
            new[] { true, false },
            observedLoading);

        Assert.False(state.IsDiscovering);

        Assert.True(
            string.IsNullOrEmpty(state.LastError));
    }

    [Fact]
    public async Task PreCancelledRequestNeverDispatches()
    {
        var state = new WorkbenchState();

        int notifications = 0;
        int dispatches = 0;

        using var source =
            new CancellationTokenSource();

        source.Cancel();

        state.Changed += (_, _) =>
            notifications++;

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () => ExecuteRunnerAsync(
                state,
                true,
                true,
                _ =>
                {
                    dispatches++;
                    return Task.CompletedTask;
                },
                source.Token));

        Assert.Equal(0, dispatches);
        Assert.Equal(0, notifications);
        Assert.False(state.IsDiscovering);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task MissingAuthorizationNeverDispatches(
        bool inProcessRequested,
        bool localDemoRequested)
    {
        var state = new WorkbenchState();

        int dispatches = 0;
        int notifications = 0;

        state.Changed += (_, _) =>
            notifications++;

        await ExecuteRunnerAsync(
            state,
            inProcessRequested,
            localDemoRequested,
            _ =>
            {
                dispatches++;
                return Task.CompletedTask;
            });

        Assert.Equal(0, dispatches);
        Assert.Equal(0, notifications);
        Assert.False(state.IsDiscovering);
    }
}
