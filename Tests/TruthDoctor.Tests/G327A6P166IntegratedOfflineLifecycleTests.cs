using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6P166IntegratedOfflineLifecycleTests
{
    private static Task<DesktopInProcessExecutionDecision> RunAsync(
        Func<CancellationToken, Task> discovery,
        Action<bool> loading,
        Action<Exception> failure,
        CancellationToken cancellationToken = default)
    {
        return DesktopOfflineExecutionCoordinator.ExecuteAsync(
            true,
            true,
            token => DesktopOfflineDiscoveryLifecycle.ExecuteAsync(
                discovery,
                loading,
                failure,
                token),
            cancellationToken);
    }

    [Fact]
    public async Task IntegratedSuccessExecutesOnceAndBalancesLoading()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int dispatchCount = 0;

        DesktopInProcessExecutionDecision result = await RunAsync(
            _ =>
            {
                dispatchCount++;
                Assert.Equal(new[] { true }, loading);
                return Task.CompletedTask;
            },
            loading.Add,
            errors.Add);

        Assert.Equal(
            DesktopInProcessExecutionDecision.OfflineLocalDemo,
            result);
        Assert.Equal(1, dispatchCount);
        Assert.Equal(new[] { true, false }, loading);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task IntegratedFailureReportsOnceAndClearsLoading()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int dispatchCount = 0;
        InvalidOperationException expected =
            new("synthetic integration failure");

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => RunAsync(
                    _ =>
                    {
                        dispatchCount++;
                        return Task.FromException(expected);
                    },
                    loading.Add,
                    errors.Add));

        Assert.Same(expected, actual);
        Assert.Equal(1, dispatchCount);
        Assert.Equal(new[] { true, false }, loading);
        Assert.Single(errors);
        Assert.Same(expected, errors[0]);
    }

    [Fact]
    public async Task IntegratedCancellationCleansUpWithoutError()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int dispatchCount = 0;

        using CancellationTokenSource source = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RunAsync(
                token =>
                {
                    dispatchCount++;
                    source.Cancel();
                    return Task.FromCanceled(token);
                },
                loading.Add,
                errors.Add,
                source.Token));

        Assert.Equal(1, dispatchCount);
        Assert.Equal(new[] { true, false }, loading);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task IntegratedPreCancellationNeverDispatches()
    {
        List<bool> loading = [];
        List<Exception> errors = [];
        int dispatchCount = 0;

        using CancellationTokenSource source = new();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => RunAsync(
                _ =>
                {
                    dispatchCount++;
                    return Task.CompletedTask;
                },
                loading.Add,
                errors.Add,
                source.Token));

        Assert.Equal(0, dispatchCount);
        Assert.Empty(loading);
        Assert.Empty(errors);
    }

    [Fact]
    public void DesktopMustHandleCancellationBeforeGenericFailure()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

        string? source = null;

        while (directory is not null)
        {
            string path = Path.Combine(
                directory.FullName,
                "Views",
                "UniversalWorkbench",
                "UniversalWorkbenchWindow.axaml.cs");

            if (File.Exists(path))
            {
                source = File.ReadAllText(path);
                break;
            }

            directory = directory.Parent;
        }

        Assert.NotNull(source);

        int start = source.IndexOf(
            "private async Task LoadStateAsync(",
            StringComparison.Ordinal);

        int end = source.IndexOf(
            "private void ApplyState(",
            StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start);

        string method = source[start..end];

        int cancellationCatch = method.IndexOf(
            "catch (OperationCanceledException",
            StringComparison.Ordinal);

        int genericCatch = method.IndexOf(
            "catch (Exception exception)",
            StringComparison.Ordinal);

        if (cancellationCatch < 0
            || genericCatch < 0
            || cancellationCatch > genericCatch)
        {
            Assert.Fail(
                "RED: desktop cancellation is not handled "
                + "before ordinary discovery failures.");
        }
    }
}
