using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A311CancellationOwnershipTests
{
    private const string ConnectionId = "aws-primary";

    private static readonly TimeSpan Timeout =
        TimeSpan.FromSeconds(10);

    private static TaskCompletionSource<bool> Signal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static ProviderConnectionManager Create(
        StandaloneBackendRuntime runtime,
        Func<string, CancellationToken, Task> authenticate)
    {
        return new ProviderConnectionManager(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle,
            authenticate);
    }

    private static StandaloneBackendRuntime RegisteredRuntime()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(ConnectionId, "aws"));

        return runtime;
    }

    private static async Task ObserveCompletion(Task operation)
    {
        try
        {
            await operation.WaitAsync(Timeout);
        }
        catch (Exception)
        {
            // Cleanup observes completion without hiding the
            // assertion outcome of the test.
        }
    }

    [Fact]
    public async Task DisconnectIsolatesCancellationCallbackExceptions()
    {
        var runtime = RegisteredRuntime();
        var entered = Signal();
        var release = Signal();

        var manager = Create(runtime, async (_, token) =>
        {
            using var registration = token.Register(() =>
                throw new InvalidOperationException(
                    "Controlled cancellation callback failure."));

            entered.TrySetResult(true);
            await release.Task;
        });

        Task operation = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            try
            {
                await manager.DisconnectAsync(
                    ConnectionId, CancellationToken.None)
                    .WaitAsync(Timeout);
            }
            catch (Exception exception)
            {
                Assert.True(
                    false,
                    "Disconnect must isolate cancellation callback " +
                    "exceptions: " + exception.GetType().Name);
            }

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveCompletion(operation);
        }
    }

    [Fact]
    public async Task DisconnectSettlesCallerBeforeIgnoredCallbackReturns()
    {
        var runtime = RegisteredRuntime();
        var entered = Signal();
        var release = Signal();

        var manager = Create(runtime, async (_, _) =>
        {
            entered.TrySetResult(true);

            // Deliberately ignore cancellation until externally released.
            await release.Task;
        });

        Task operation = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            Assert.True(
                operation.IsCompleted,
                "Disconnect must promptly settle the invalidated " +
                "connection caller without waiting for a callback " +
                "that ignores cancellation.");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => operation.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveCompletion(operation);
        }
    }

    [Fact]
    public async Task OwnerCancellationSettlesBeforeIgnoredCallbackReturns()
    {
        var runtime = RegisteredRuntime();
        var entered = Signal();
        var observedCancellation = Signal();
        var release = Signal();

        var manager = Create(runtime, async (_, token) =>
        {
            using var registration = token.Register(() =>
                observedCancellation.TrySetResult(true));

            entered.TrySetResult(true);

            // A provider callback may not promptly honor cancellation.
            await release.Task;
        });

        using var cancellation = new CancellationTokenSource();

        Task operation = manager.ConnectAsync(
            ConnectionId, cancellation.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            cancellation.Cancel();

            await observedCancellation.Task.WaitAsync(Timeout);

            Assert.True(
                operation.IsCompleted,
                "Owner cancellation must promptly settle the " +
                "connection caller even when authentication is pending.");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => operation.WaitAsync(Timeout));
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveCompletion(operation);
        }
    }

    [Fact]
    public async Task JoinedCallerCancellationDoesNotCancelOwner()
    {
        var runtime = RegisteredRuntime();
        var entered = Signal();
        var release = Signal();
        int calls = 0;

        var manager = Create(runtime, async (_, _) =>
        {
            Interlocked.Increment(ref calls);
            entered.TrySetResult(true);
            await release.Task;
        });

        Task owner = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            using var cancellation = new CancellationTokenSource();

            Task joined = manager.ConnectAsync(
                ConnectionId, cancellation.Token);

            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => joined.WaitAsync(Timeout));

            release.TrySetResult(true);

            await owner.WaitAsync(Timeout);

            Assert.Equal(1, calls);
            Assert.Equal(
                ProviderConnectionState.Connected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await ObserveCompletion(owner);
        }
    }

    [Fact]
    public async Task AuthenticationFailureRetryStillSucceeds()
    {
        var runtime = RegisteredRuntime();
        int calls = 0;

        var manager = Create(runtime, (_, _) =>
        {
            int attempt = Interlocked.Increment(ref calls);

            if (attempt == 1)
            {
                throw new InvalidOperationException(
                    "Controlled authentication failure.");
            }

            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.ConnectAsync(
                ConnectionId, CancellationToken.None));

        Assert.Equal(
            ProviderConnectionState.Failed,
            manager.GetState(ConnectionId));

        await manager.ConnectAsync(
            ConnectionId, CancellationToken.None)
            .WaitAsync(Timeout);

        Assert.Equal(2, calls);
        Assert.Equal(
            ProviderConnectionState.Connected,
            manager.GetState(ConnectionId));
    }
}
