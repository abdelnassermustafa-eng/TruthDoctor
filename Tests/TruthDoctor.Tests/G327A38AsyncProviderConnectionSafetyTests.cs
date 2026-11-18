using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A38AsyncProviderConnectionSafetyTests
{
    private static readonly TimeSpan Timeout =
        TimeSpan.FromSeconds(10);

    private const string ConnectionId = "aws-primary";

    private static TaskCompletionSource<bool> Signal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static ProviderConnectionManager Manager(
        StandaloneBackendRuntime runtime,
        Func<string, CancellationToken, Task> authenticate)
    {
        return new ProviderConnectionManager(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle,
            authenticate);
    }

    private static void Register(StandaloneBackendRuntime runtime)
    {
        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(ConnectionId, "aws"));
    }

    [Fact]
    public async Task ReplacedRegistrationRejectsStaleAuthentication()
    {
        var runtime = new StandaloneBackendRuntime();
        Register(runtime);

        long originalGeneration = runtime.ConnectionCatalog
            .GetRegistrationGeneration(ConnectionId);

        var entered = Signal();
        var release = Signal();

        var manager = Manager(runtime, async (_, _) =>
        {
            entered.TrySetResult(true);
            await release.Task;
        });

        Task operation = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            Assert.True(
                runtime.ConnectionCatalog.Unregister(ConnectionId));

            Register(runtime);

            long replacementGeneration = runtime.ConnectionCatalog
                .GetRegistrationGeneration(ConnectionId);

            Assert.NotEqual(
                originalGeneration,
                replacementGeneration);

            release.TrySetResult(true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await operation.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            try { await operation.WaitAsync(Timeout); }
            catch (Exception) { }
        }
    }

    [Fact]
    public async Task ConcurrentConnectsShareOneAuthenticationOperation()
    {
        var runtime = new StandaloneBackendRuntime();
        Register(runtime);

        var entered = Signal();
        var release = Signal();
        int calls = 0;

        var manager = Manager(runtime, async (_, _) =>
        {
            Interlocked.Increment(ref calls);
            entered.TrySetResult(true);
            await release.Task;
        });

        Task first = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        Task? second = null;

        try
        {
            await entered.Task.WaitAsync(Timeout);

            second = manager.ConnectAsync(
                ConnectionId, CancellationToken.None);

            release.TrySetResult(true);

            await Task.WhenAll(first, second).WaitAsync(Timeout);

            Assert.Equal(1, calls);
            Assert.Equal(
                ProviderConnectionState.Connected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            try { await first.WaitAsync(Timeout); }
            catch (Exception) { }

            if (second is not null)
            {
                try { await second.WaitAsync(Timeout); }
                catch (Exception) { }
            }
        }
    }

    [Fact]
    public async Task CancellationDuringAuthenticationIsRetryable()
    {
        var runtime = new StandaloneBackendRuntime();
        Register(runtime);

        var entered = Signal();
        var release = Signal();

        var manager = Manager(runtime, async (_, _) =>
        {
            entered.TrySetResult(true);
            await release.Task;
        });

        using var cancellation = new CancellationTokenSource();

        Task operation = manager.ConnectAsync(
            ConnectionId, cancellation.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            cancellation.Cancel();
            release.TrySetResult(true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await operation.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            try { await operation.WaitAsync(Timeout); }
            catch (Exception) { }
        }
    }

    [Fact]
    public async Task DisconnectDuringAuthenticationPreventsLateConnection()
    {
        var runtime = new StandaloneBackendRuntime();
        Register(runtime);

        var entered = Signal();
        var release = Signal();

        var manager = Manager(runtime, async (_, _) =>
        {
            entered.TrySetResult(true);
            await release.Task;
        });

        Task operation = manager.ConnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            Task disconnect = manager.DisconnectAsync(
                ConnectionId, CancellationToken.None);

            await disconnect.WaitAsync(Timeout);

            release.TrySetResult(true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await operation.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                manager.GetState(ConnectionId));

            Assert.True(
                runtime.ConnectionCatalog.TryGet(
                    ConnectionId, out _));
        }
        finally
        {
            release.TrySetResult(true);
            try { await operation.WaitAsync(Timeout); }
            catch (Exception) { }
        }
    }

    [Fact]
    public async Task FailedAuthenticationCanRecoverOnRetry()
    {
        var runtime = new StandaloneBackendRuntime();
        Register(runtime);

        int calls = 0;

        var manager = Manager(runtime, (_, _) =>
        {
            int attempt = Interlocked.Increment(ref calls);

            if (attempt == 1)
            {
                throw new InvalidOperationException(
                    "Controlled first-attempt authentication failure.");
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
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(
            ProviderConnectionState.Connected,
            manager.GetState(ConnectionId));

        Assert.Equal(2, calls);

        Assert.True(
            runtime.ConnectionCatalog.TryGet(
                ConnectionId, out _));
    }
}
