using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A321MockProviderAdapterIntegrationContractTests
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

        public Task<IReadOnlyList<ProviderResourceRecord>>
            DiscoverAsync(
                string connectionId,
                CancellationToken cancellationToken) =>
            Discover(connectionId, cancellationToken);
    }

    private static StandaloneBackendRuntime CreateRuntime(
        string providerId = "aws")
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                ConnectionId, providerId));

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

    [Fact]
    public void RegisteredAdapterResolvesByProviderId()
    {
        var runtime = CreateRuntime();
        var adapter = new MockAdapter();

        var coordinator = CreateCoordinator(runtime, adapter);

        Assert.Same(
            adapter,
            coordinator.ResolveAdapter("aws"));

        Assert.Throws<KeyNotFoundException>(
            () => coordinator.ResolveAdapter("azure"));
    }

    [Fact]
    public async Task ConnectRoutesAuthenticationThroughProviderAdapter()
    {
        var runtime = CreateRuntime();
        string? authenticatedConnection = null;
        int calls = 0;

        var adapter = new MockAdapter
        {
            Authenticate = (connectionId, token) =>
            {
                token.ThrowIfCancellationRequested();
                authenticatedConnection = connectionId;
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);

        await coordinator.ConnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(ConnectionId, authenticatedConnection);
        Assert.Equal(1, calls);

        Assert.Equal(
            ProviderConnectionState.Connected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }

    [Fact]
    public async Task DisconnectInvokesAdapterAndUpdatesLifecycle()
    {
        var runtime = CreateRuntime();
        string? disconnectedConnection = null;
        int disconnectCalls = 0;

        var adapter = new MockAdapter
        {
            Disconnect = (connectionId, token) =>
            {
                token.ThrowIfCancellationRequested();
                disconnectedConnection = connectionId;
                Interlocked.Increment(ref disconnectCalls);
                return Task.CompletedTask;
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);

        await coordinator.ConnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

        await coordinator.DisconnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(ConnectionId, disconnectedConnection);
        Assert.Equal(1, disconnectCalls);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }

    [Fact]
    public async Task DiscoveryReturnsTypedResourcesFromAdapter()
    {
        var runtime = CreateRuntime();

        var resource = new ProviderResourceRecord(
            "aws",
            "vpc-mock-001",
            "vpc",
            "Mock Integration VPC");

        string? discoveryConnection = null;

        var adapter = new MockAdapter
        {
            Discover = (connectionId, token) =>
            {
                token.ThrowIfCancellationRequested();
                discoveryConnection = connectionId;

                IReadOnlyList<ProviderResourceRecord> records =
                    new[] { resource };

                return Task.FromResult(records);
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);

        await coordinator.ConnectAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

        var resources = await coordinator.DiscoverAsync(
            ConnectionId,
            CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(ConnectionId, discoveryConnection);

        var discovered = Assert.Single(resources);

        Assert.Equal("aws", discovered.ProviderId);
        Assert.Equal("vpc-mock-001", discovered.ResourceId);
        Assert.Equal("vpc", discovered.ResourceType);
        Assert.Equal(
            "Mock Integration VPC",
            discovered.DisplayName);
    }

    [Fact]
    public async Task UnsupportedProviderNeverUsesAnotherAdapter()
    {
        var runtime = CreateRuntime("aws");
        int wrongProviderCalls = 0;

        var azureAdapter = new MockAdapter
        {
            ProviderId = "azure",
            Authenticate = (_, _) =>
            {
                Interlocked.Increment(ref wrongProviderCalls);
                return Task.CompletedTask;
            }
        };

        var coordinator = CreateCoordinator(
            runtime,
            azureAdapter);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => coordinator.ConnectAsync(
                ConnectionId,
                CancellationToken.None));

        Assert.Equal(0, wrongProviderCalls);

        Assert.NotEqual(
            ProviderConnectionState.Connected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }

    [Fact]
    public async Task OwnerCancellationPreventsLateProviderConnection()
    {
        var runtime = CreateRuntime();
        var entered = Signal();
        var release = Signal();
        var callbackExited = Signal();
        var cancellationObserved = Signal();

        var adapter = new MockAdapter
        {
            Authenticate = async (_, token) =>
            {
                using var registration = token.Register(
                    () => cancellationObserved.TrySetResult(true));

                entered.TrySetResult(true);

                try
                {
                    await release.Task;
                }
                finally
                {
                    callbackExited.TrySetResult(true);
                }
            }
        };

        var coordinator = CreateCoordinator(runtime, adapter);

        using var owner = new CancellationTokenSource();

        Task connection = coordinator.ConnectAsync(
            ConnectionId,
            owner.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            owner.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => connection.WaitAsync(Timeout));

            await cancellationObserved.Task.WaitAsync(Timeout);

            Assert.False(
                callbackExited.Task.IsCompleted,
                "Logical cancellation must not be confused with " +
                "physical callback completion.");

            Assert.Equal(
                ProviderConnectionState.Disconnected,
                runtime.ConnectionLifecycle.GetState(ConnectionId));
        }
        finally
        {
            release.TrySetResult(true);
            await callbackExited.Task.WaitAsync(Timeout);
        }

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }
}
