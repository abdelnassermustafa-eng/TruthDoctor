using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A323MockAdapterHardeningTests
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
        IStandaloneProviderAdapter adapter)
    {
        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        coordinator.RegisterAdapter(adapter);
        return coordinator;
    }

    private static async Task Connect(
        StandaloneProviderAdapterCoordinator coordinator)
    {
        await coordinator.ConnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);
    }

    [Fact]
    public async Task DisconnectFailureLeavesLifecycleDisconnected()
    {
        var runtime = CreateRuntime();
        int disconnectCalls = 0;

        var adapter = new MockAdapter
        {
            Disconnect = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref disconnectCalls);
                throw new IOException("Controlled disconnect failure.");
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId,
                CancellationToken.None));

        Assert.Equal(1, disconnectCalls);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }

    [Fact]
    public async Task DisconnectDuringDiscoveryRejectsLateResults()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();

        var adapter = new MockAdapter
        {
            Discover = async (_, token) =>
            {
                entered.TrySetResult(true);

                await release.Task;

                token.ThrowIfCancellationRequested();

                return new[]
                {
                    new ProviderResourceRecord(
                        "aws", "vpc-old", "vpc", "Old VPC")
                };
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        Task<IReadOnlyList<ProviderResourceRecord>> discovery =
            coordinator.DiscoverAsync(
                ConnectionId,
                CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            await coordinator.DisconnectAsync(
                ConnectionId,
                CancellationToken.None).WaitAsync(Timeout);

            release.TrySetResult(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => discovery.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                runtime.ConnectionLifecycle.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
        }
    }

    [Fact]
    public void DuplicateProviderRegistrationPreservesOriginalAdapter()
    {
        var runtime = CreateRuntime();
        var original = new MockAdapter();
        var replacement = new MockAdapter();

        var coordinator = CreateCoordinator(runtime, original);

        Assert.Throws<InvalidOperationException>(
            () => coordinator.RegisterAdapter(replacement));

        Assert.Same(
            original,
            coordinator.ResolveAdapter("aws"));
    }

    [Fact]
    public async Task DiscoveryCancellationPropagatesToProvider()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        var canceled = Signal();

        var adapter = new MockAdapter
        {
            Discover = async (_, token) =>
            {
                using var registration = token.Register(
                    () => canceled.TrySetResult(true));

                entered.TrySetResult(true);

                await release.Task;

                token.ThrowIfCancellationRequested();

                return Array.Empty<ProviderResourceRecord>();
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        using var owner = new CancellationTokenSource();

        Task<IReadOnlyList<ProviderResourceRecord>> discovery =
            coordinator.DiscoverAsync(
                ConnectionId,
                owner.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            owner.Cancel();

            await canceled.Task.WaitAsync(Timeout);

            release.TrySetResult(true);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => discovery.WaitAsync(Timeout));

            Assert.Equal(
                ProviderConnectionState.Connected,
                runtime.ConnectionLifecycle.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
        }
    }

    [Fact]
    public async Task ReplacementRegistrationRejectsStaleDiscovery()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        int discoveryCalls = 0;

        var adapter = new MockAdapter
        {
            Discover = async (_, token) =>
            {
                Interlocked.Increment(ref discoveryCalls);
                entered.TrySetResult(true);

                await release.Task;

                token.ThrowIfCancellationRequested();

                return new[]
                {
                    new ProviderResourceRecord(
                        "aws",
                        "vpc-old-generation",
                        "vpc",
                        "Stale Discovery Resource")
                };
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);
        await Connect(coordinator);

        Task<IReadOnlyList<ProviderResourceRecord>> oldDiscovery =
            coordinator.DiscoverAsync(
                ConnectionId,
                CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            Assert.True(
                runtime.ConnectionCatalog.Unregister(ConnectionId));

            runtime.ConnectionCatalog.Register(
                new ProviderConnectionIdentity(
                    ConnectionId, "aws"));

            await Connect(coordinator);

            Assert.Equal(
                ProviderConnectionState.Connected,
                runtime.ConnectionLifecycle.GetState(ConnectionId));

            release.TrySetResult(true);

            Exception? failure = await Record.ExceptionAsync(
                async () =>
                {
                    await oldDiscovery.WaitAsync(Timeout);
                });

            Assert.True(
                failure is InvalidOperationException
                    or OperationCanceledException,
                "Stale discovery must reject a result from an " +
                "earlier registration generation, even when the " +
                "replacement session is Connected. Actual outcome: " +
                (failure?.GetType().Name ?? "successful result"));

            Assert.Equal(1, discoveryCalls);

            Assert.Equal(
                ProviderConnectionState.Connected,
                runtime.ConnectionLifecycle.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
        }
    }
}
