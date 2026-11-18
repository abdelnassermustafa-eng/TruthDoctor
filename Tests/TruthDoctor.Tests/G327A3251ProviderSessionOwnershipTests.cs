using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A3251ProviderSessionOwnershipTests
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

        public Func<
            string,
            CancellationToken,
            Task<IReadOnlyList<ProviderResourceRecord>>> Discover
            { get; init; } = (_, _) =>
                Task.FromResult<IReadOnlyList<ProviderResourceRecord>>(
                    Array.Empty<ProviderResourceRecord>());

        public Task AuthenticateAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Authenticate(connectionId, cancellationToken);

        public Task DisconnectAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Disconnect(connectionId, cancellationToken);

        public Task<IReadOnlyList<ProviderResourceRecord>> DiscoverAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Discover(connectionId, cancellationToken);
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
        params IStandaloneProviderAdapter[] adapters)
    {
        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        foreach (var adapter in adapters)
        {
            coordinator.RegisterAdapter(adapter);
        }

        return coordinator;
    }

    private static Task Connect(
        StandaloneProviderAdapterCoordinator coordinator) =>
        coordinator.ConnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

    [Fact]
    public async Task PendingProviderCleanupRejectsReconnect()
    {
        var runtime = CreateRuntime();
        var cleanupEntered = Signal();
        var releaseCleanup = Signal();

        int authenticationCalls = 0;

        var adapter = new MockAdapter
        {
            Authenticate = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref authenticationCalls);
                return Task.CompletedTask;
            },
            Disconnect = async (_, token) =>
            {
                cleanupEntered.TrySetResult(true);

                await releaseCleanup.Task;

                token.ThrowIfCancellationRequested();
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        Task physicalDisconnect = coordinator.DisconnectAsync(
            ConnectionId,
            CancellationToken.None);

        try
        {
            await cleanupEntered.Task.WaitAsync(Timeout);

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
                "Reconnect accepted while prior provider cleanup is pending");

            Assert.Equal(
                1,
                Volatile.Read(ref authenticationCalls));
        }
        finally
        {
            releaseCleanup.TrySetResult(true);
            await physicalDisconnect.WaitAsync(Timeout);
        }
    }

    [Fact]
    public async Task DisconnectInvokesOnlyRegisteredProvider()
    {
        var runtime = CreateRuntime();

        int awsDisconnects = 0;
        int azureDisconnects = 0;

        var aws = new MockAdapter
        {
            Disconnect = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref awsDisconnects);
                return Task.CompletedTask;
            }
        };

        var azure = new MockAdapter
        {
            ProviderId = "azure",
            Disconnect = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref azureDisconnects);
                return Task.CompletedTask;
            }
        };

        var coordinator = CreateCoordinator(runtime, aws, azure);

        await Connect(coordinator);

        await coordinator.DisconnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(1, awsDisconnects);
        Assert.Equal(0, azureDisconnects);
        Assert.Equal(
            ProviderConnectionState.Disconnected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }

    [Fact]
    public async Task CompletedProviderCleanupAllowsReconnect()
    {
        var runtime = CreateRuntime();
        int authenticateCalls = 0;
        int disconnectCalls = 0;

        var adapter = new MockAdapter
        {
            Authenticate = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref authenticateCalls);
                return Task.CompletedTask;
            },
            Disconnect = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref disconnectCalls);
                return Task.CompletedTask;
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);

        await Connect(coordinator);

        await coordinator.DisconnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

        await Connect(coordinator);

        Assert.Equal(2, authenticateCalls);
        Assert.Equal(1, disconnectCalls);

        Assert.Equal(
            ProviderConnectionState.Connected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }

    [Fact]
    public async Task CancellationPropagatesDuringProviderCleanup()
    {
        var runtime = CreateRuntime();
        var cleanupEntered = Signal();
        var releaseCleanup = Signal();
        var cancellationObserved = Signal();

        var adapter = new MockAdapter
        {
            Disconnect = async (_, token) =>
            {
                using var registration = token.Register(
                    () => cancellationObserved.TrySetResult(true));

                cleanupEntered.TrySetResult(true);

                await releaseCleanup.Task;

                token.ThrowIfCancellationRequested();
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        using var owner = new CancellationTokenSource();

        Task disconnect = coordinator.DisconnectAsync(
            ConnectionId,
            owner.Token);

        try
        {
            await cleanupEntered.Task.WaitAsync(Timeout);

            owner.Cancel();

            await cancellationObserved.Task.WaitAsync(Timeout);

            releaseCleanup.TrySetResult(true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => disconnect.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                runtime.ConnectionLifecycle.GetState(ConnectionId));
        }
        finally
        {
            releaseCleanup.TrySetResult(true);
        }
    }
}
