using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A314CancellationLifetimeInterleavingTests
{
    private const string ConnectionId = "aws-primary";
    private static readonly TimeSpan Timeout =
        TimeSpan.FromSeconds(10);

    private static TaskCompletionSource<bool> Signal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static StandaloneBackendRuntime CreateRuntime()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(ConnectionId, "aws"));

        return runtime;
    }

    private static ProviderConnectionManager CreateManager(
        StandaloneBackendRuntime runtime,
        Func<string, CancellationToken, Task> callback) =>
        new(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle,
            callback);

    private static void ReplaceRegistration(
        StandaloneBackendRuntime runtime)
    {
        Assert.True(
            runtime.ConnectionCatalog.Unregister(ConnectionId));

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(ConnectionId, "aws"));
    }

    private static async Task Observe(Task task)
    {
        try
        {
            await task.WaitAsync(Timeout);
        }
        catch (Exception)
        {
            // Preserve the original assertion during cleanup.
        }
    }

    [Fact]
    public async Task ReplacementDisconnectSettlesStaleOperation()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();

        var manager = CreateManager(runtime, async (_, _) =>
        {
            entered.TrySetResult(true);
            await release.Task;
        });

        Task stale = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            ReplaceRegistration(runtime);

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            Assert.True(
                stale.IsCompleted,
                "Replacement disconnect must settle the stale " +
                "authentication caller without requiring the old " +
                "callback to return.");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => stale.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await Observe(stale);
        }
    }

    [Fact]
    public async Task ReplacementDisconnectSignalsStaleCallbackCancellation()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var cancellationObserved = Signal();
        var release = Signal();

        var manager = CreateManager(runtime, async (_, token) =>
        {
            using var registration = token.Register(
                () => cancellationObserved.TrySetResult(true));

            entered.TrySetResult(true);
            await release.Task;
        });

        Task stale = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            ReplaceRegistration(runtime);

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            Assert.True(
                cancellationObserved.Task.IsCompleted,
                "Replacement disconnect must request cancellation " +
                "of the stale provider callback, even when the " +
                "replacement registration is already disconnected.");

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await Observe(stale);
        }
    }

    [Fact]
    public async Task ReplacementConnectInvalidatesOldOperation()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        int calls = 0;

        var manager = CreateManager(runtime, async (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.TrySetResult(true);
                await release.Task;
            }
        });

        Task old = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);
            ReplaceRegistration(runtime);

            await manager.ConnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            Assert.True(old.IsCompleted);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => old.WaitAsync(Timeout));

            Assert.Equal(2, calls);
            Assert.Equal(
                ProviderConnectionState.Connected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await Observe(old);
        }
    }

    [Fact]
    public async Task LateCanceledCallbackCannotOverwriteSuccessfulRetry()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        int calls = 0;

        var manager = CreateManager(runtime, async (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.TrySetResult(true);
                await release.Task;
            }
        });

        Task old = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => old.WaitAsync(Timeout));

            await manager.ConnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            release.TrySetResult(true);

            await Observe(old);

            Assert.Equal(2, calls);
            Assert.Equal(
                ProviderConnectionState.Connected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await Observe(old);
        }
    }

    [Fact]
    public async Task DisconnectAfterSuccessfulCompletionStaysDisconnected()
    {
        var runtime = CreateRuntime();

        var manager = CreateManager(
            runtime, (_, _) => Task.CompletedTask);

        await manager.ConnectAsync(
            ConnectionId, CancellationToken.None)
            .WaitAsync(Timeout);

        Assert.Equal(
            ProviderConnectionState.Connected,
            manager.GetState(ConnectionId));

        await manager.DisconnectAsync(
            ConnectionId, CancellationToken.None)
            .WaitAsync(Timeout);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            manager.GetState(ConnectionId));
    }
}
