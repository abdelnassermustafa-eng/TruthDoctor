using System;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A317LifetimeEdgeCaseContractTests
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

    private static async Task Observe(Task task)
    {
        try
        {
            await task.WaitAsync(Timeout);
        }
        catch (Exception)
        {
            // Cleanup never substitutes for test assertions.
        }
    }

    [Fact]
    public async Task OwnerCancellationPrecedesPhysicalCallbackExit()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        var exited = Signal();

        var manager = CreateManager(runtime, async (_, _) =>
        {
            entered.TrySetResult(true);
            try
            {
                await release.Task;
            }
            finally
            {
                exited.TrySetResult(true);
            }
        });

        using var cancellation = new CancellationTokenSource();

        Task connecting = manager.ConnectAsync(
            ConnectionId, cancellation.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            cancellation.Cancel();

            Assert.True(
                connecting.IsCompleted,
                "Owner logical cancellation must settle promptly.");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => connecting.WaitAsync(Timeout));

            Assert.False(
                exited.Task.IsCompleted,
                "Logical cancellation must not be mistaken for " +
                "physical authentication callback termination.");

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await exited.Task.WaitAsync(Timeout);
            await Observe(connecting);
        }

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            manager.GetState(ConnectionId));
    }

    [Fact]
    public async Task DisconnectSignalsCallbackBeforePhysicalExit()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var canceled = Signal();
        var release = Signal();
        var exited = Signal();

        var manager = CreateManager(runtime, async (_, token) =>
        {
            using var registration = token.Register(
                () => canceled.TrySetResult(true));

            entered.TrySetResult(true);
            try
            {
                await release.Task;
            }
            finally
            {
                exited.TrySetResult(true);
            }
        });

        Task connecting = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            await canceled.Task.WaitAsync(Timeout);

            Assert.True(
                connecting.IsCompleted,
                "Disconnect must settle the logical caller.");

            Assert.False(
                exited.Task.IsCompleted,
                "Provider callback must remain physically active " +
                "until externally released.");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => connecting.WaitAsync(Timeout));
        }
        finally
        {
            release.TrySetResult(true);
            await exited.Task.WaitAsync(Timeout);
            await Observe(connecting);
        }

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            manager.GetState(ConnectionId));
    }

    [Fact]
    public async Task ThrowingCancellationCallbackDoesNotCorruptCleanup()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        var exited = Signal();

        var manager = CreateManager(runtime, async (_, token) =>
        {
            using var registration = token.Register(() =>
                throw new InvalidOperationException(
                    "Controlled callback cancellation exception."));

            entered.TrySetResult(true);

            try
            {
                await release.Task;
            }
            finally
            {
                exited.TrySetResult(true);
            }
        });

        Task connecting = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => connecting.WaitAsync(Timeout));
        }
        finally
        {
            release.TrySetResult(true);
            await exited.Task.WaitAsync(Timeout);
            await Observe(connecting);
        }

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            manager.GetState(ConnectionId));
    }

    [Fact]
    public async Task ReplacedRegistrationSurvivesLatePhysicalCompletion()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        var oldExited = Signal();
        int calls = 0;

        var manager = CreateManager(runtime, async (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.TrySetResult(true);

                try
                {
                    await release.Task;
                }
                finally
                {
                    oldExited.TrySetResult(true);
                }
            }
        });

        Task predecessor = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            Assert.True(
                runtime.ConnectionCatalog.Unregister(ConnectionId));

            runtime.ConnectionCatalog.Register(
                new ProviderConnectionIdentity(ConnectionId, "aws"));

            await manager.ConnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => predecessor.WaitAsync(Timeout));

            Assert.False(oldExited.Task.IsCompleted);
            Assert.Equal(2, calls);

            release.TrySetResult(true);
            await oldExited.Task.WaitAsync(Timeout);

            Assert.Equal(
                ProviderConnectionState.Connected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await oldExited.Task.WaitAsync(Timeout);
            await Observe(predecessor);
        }
    }

    [Fact]
    public async Task CompletionBeforeDisconnectHasStableFinalState()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        var exited = Signal();

        var manager = CreateManager(runtime, async (_, _) =>
        {
            entered.TrySetResult(true);

            try
            {
                await release.Task;
            }
            finally
            {
                exited.TrySetResult(true);
            }
        });

        Task connecting = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);
            release.TrySetResult(true);

            await connecting.WaitAsync(Timeout);
            await exited.Task.WaitAsync(Timeout);

            Assert.Equal(
                ProviderConnectionState.Connected,
                manager.GetState(ConnectionId));

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));

            await manager.DisconnectAsync(
                ConnectionId, CancellationToken.None)
                .WaitAsync(Timeout);

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await Observe(connecting);
        }
    }
}
