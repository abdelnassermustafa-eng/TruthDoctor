using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A3254CleanupIsolationContractTests
{
    private const string ConnectionId = "aws-primary";
    private static readonly TimeSpan Timeout =
        TimeSpan.FromSeconds(10);

    private static TaskCompletionSource<bool> Signal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class MockAdapter : IStandaloneProviderAdapter
    {
        public string ProviderId { get; init; } = "aws";

        public Func<string, CancellationToken, Task> Authenticate
            { get; init; } = (_, _) => Task.CompletedTask;

        public Func<string, CancellationToken, Task> Disconnect
            { get; init; } = (_, _) => Task.CompletedTask;

        public Task AuthenticateAsync(
            string connectionId,
            CancellationToken token) =>
            Authenticate(connectionId, token);

        public Task DisconnectAsync(
            string connectionId,
            CancellationToken token) =>
            Disconnect(connectionId, token);

        public Task<IReadOnlyList<ProviderResourceRecord>> DiscoverAsync(
            string connectionId,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ProviderResourceRecord>>(
                Array.Empty<ProviderResourceRecord>());
        }
    }

    private static StandaloneBackendRuntime CreateRuntime()
    {
        var runtime = new StandaloneBackendRuntime();
        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(ConnectionId, "aws"));
        return runtime;
    }

    private static StandaloneProviderAdapterCoordinator CreateCoordinator(
        StandaloneBackendRuntime runtime,
        IStandaloneProviderAdapter adapter)
    {
        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        coordinator.RegisterAdapter(adapter);
        return coordinator;
    }

    private static Task Connect(
        StandaloneProviderAdapterCoordinator coordinator) =>
        coordinator.ConnectAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

    [Fact]
    public async Task CancellationDoesNotReleasePendingCleanupPrematurely()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        var cancellationSeen = Signal();

        var adapter = new MockAdapter
        {
            Disconnect = async (_, token) =>
            {
                using var registration = token.Register(
                    () => cancellationSeen.TrySetResult(true));

                entered.TrySetResult(true);
                await release.Task;
                token.ThrowIfCancellationRequested();
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        using var owner = new CancellationTokenSource();

        Task cleanup = coordinator.DisconnectAsync(
            ConnectionId, owner.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout);
            owner.Cancel();
            await cancellationSeen.Task.WaitAsync(Timeout);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await coordinator.ConnectAsync(
                    ConnectionId, CancellationToken.None));

            Assert.False(
                cleanup.IsCompleted,
                "Provider cleanup unexpectedly completed before release.");
        }
        finally
        {
            release.TrySetResult(true);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cleanup.WaitAsync(Timeout));
    }

    [Fact]
    public async Task ReplacementRegistrationCannotBypassPendingCleanup()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();

        int newAuthenticationCalls = 0;

        var original = new MockAdapter
        {
            Disconnect = async (_, _) =>
            {
                entered.TrySetResult(true);
                await release.Task;
            }
        };

        var coordinator = CreateCoordinator(runtime, original);

        await Connect(coordinator);

        Task cleanup = coordinator.DisconnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            Assert.True(
                runtime.ConnectionCatalog.Unregister(ConnectionId));

            runtime.ConnectionCatalog.Register(
                new ProviderConnectionIdentity(
                    ConnectionId, "azure"));

            coordinator.RegisterAdapter(new MockAdapter
            {
                ProviderId = "azure",
                Authenticate = (_, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    Interlocked.Increment(ref newAuthenticationCalls);
                    return Task.CompletedTask;
                }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await coordinator.ConnectAsync(
                    ConnectionId, CancellationToken.None));

            Assert.Equal(
                0, Volatile.Read(ref newAuthenticationCalls));
        }
        finally
        {
            release.TrySetResult(true);
            await cleanup.WaitAsync(Timeout);
        }
    }

    [Fact]
    public async Task ConcurrentDisconnectDoesNotDuplicateProviderCleanup()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();

        int cleanupCalls = 0;

        var adapter = new MockAdapter
        {
            Disconnect = async (_, _) =>
            {
                Interlocked.Increment(ref cleanupCalls);
                entered.TrySetResult(true);
                await release.Task;
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        Task first = coordinator.DisconnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await coordinator.DisconnectAsync(
                    ConnectionId, CancellationToken.None));

            Assert.Equal(
                1, Volatile.Read(ref cleanupCalls));
        }
        finally
        {
            release.TrySetResult(true);
            await first.WaitAsync(Timeout);
        }

        Assert.Equal(1, cleanupCalls);
    }

    [Fact]
    public async Task FailedProviderCleanupRequiresExplicitRecovery()
    {
        var runtime = CreateRuntime();

        int authenticationCalls = 0;
        int cleanupCalls = 0;

        var adapter = new MockAdapter
        {
            Authenticate = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref authenticationCalls);
                return Task.CompletedTask;
            },
            Disconnect = (_, _) =>
            {
                Interlocked.Increment(ref cleanupCalls);
                return Task.FromException(
                    new IOException(
                        "Controlled unresolved provider cleanup."));
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId,
                CancellationToken.None));

        Assert.Equal(1, cleanupCalls);
        Assert.Equal(
            ProviderConnectionState.Disconnected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));

        bool rejected = false;

        try
        {
            await Connect(coordinator);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Assert.True(
            rejected,
            "Unresolved provider cleanup failure must require explicit recovery");

        Assert.Equal(
            1, authenticationCalls);
    }
}
