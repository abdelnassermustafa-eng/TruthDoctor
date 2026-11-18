using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P160OfflineDiscoveryLifecycleTests
{
    private static async Task ExecuteAsync(
        Func<CancellationToken, Task> discovery,
        Action<bool> setLoading,
        Action<Exception> reportFailure,
        CancellationToken cancellationToken = default)
    {
        Assembly assembly =
            typeof(DesktopOfflineExecutionCoordinator).Assembly;

        Type? lifecycle = assembly.GetType(
            "TruthDoctor.Controllers.Workbench."
            + "DesktopOfflineDiscoveryLifecycle",
            throwOnError: false);

        if (lifecycle is null)
        {
            Assert.Fail(
                "RED: offline discovery lifecycle "
                + "is not implemented.");
        }

        MethodInfo? execute = lifecycle.GetMethod(
            "ExecuteAsync",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(Func<CancellationToken, Task>),
                typeof(Action<bool>),
                typeof(Action<Exception>),
                typeof(CancellationToken)
            ],
            modifiers: null);

        Assert.NotNull(execute);

        object? result = execute.Invoke(
            null,
            [
                discovery,
                setLoading,
                reportFailure,
                cancellationToken
            ]);

        Task task = Assert.IsAssignableFrom<Task>(result);
        await task;
    }

    [Fact]
    public async Task SuccessBalancesLoadingAndRunsOnce()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int calls = 0;

        await ExecuteAsync(
            _ =>
            {
                calls++;
                Assert.Equal(new[] { true }, loading);
                return Task.CompletedTask;
            },
            loading.Add,
            errors.Add);

        Assert.Equal(1, calls);
        Assert.Equal(new[] { true, false }, loading);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task FailureReportsErrorAndClearsLoading()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int calls = 0;

        InvalidOperationException failure =
            new("synthetic offline failure");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ExecuteAsync(
                _ =>
                {
                    calls++;
                    return Task.FromException(failure);
                },
                loading.Add,
                errors.Add));

        Assert.Equal(1, calls);
        Assert.Equal(new[] { true, false }, loading);
        Assert.Single(errors);
        Assert.Same(failure, errors[0]);
    }

    [Fact]
    public async Task CancellationClearsLoadingAndPropagates()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int calls = 0;

        using CancellationTokenSource source = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(
                token =>
                {
                    calls++;
                    source.Cancel();
                    return Task.FromCanceled(token);
                },
                loading.Add,
                errors.Add,
                source.Token));

        Assert.Equal(1, calls);
        Assert.Equal(new[] { true, false }, loading);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task PreCancelledExecutionNeverDispatches()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int calls = 0;

        using CancellationTokenSource source = new();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(
                _ =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                loading.Add,
                errors.Add,
                source.Token));

        Assert.Equal(0, calls);
        Assert.Empty(loading);
        Assert.Empty(errors);
    }
}
