using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryPublicationConcurrencyTests
{
    private const string ConnectionId = "connection-001";

    private sealed class ControlledAdapter : IStandaloneProviderAdapter
    {
        private readonly bool _blocked;

        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<IReadOnlyList<ProviderResourceRecord>>
            _release =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ControlledAdapter(bool blocked)
        {
            _blocked = blocked;
        }

        public string ProviderId => "mock-provider";

        public Task Started => _started.Task;

        private static IReadOnlyList<ProviderResourceRecord> Resources =>
        [
            new ProviderResourceRecord(
                "mock-provider",
                "concurrency-resource",
                "virtual-machine",
                "Concurrency Resource")
        ];

        public void Complete()
        {
            _release.TrySetResult(Resources);
        }

        public Task AuthenticateAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DisconnectAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ProviderResourceRecord>> DiscoverAsync(
            string connectionId,
            CancellationToken cancellationToken)
        {
            _started.TrySetResult(true);

            return _blocked
                ? _release.Task
                : Task.FromResult(Resources);
        }
    }

    private static void Connect(ProviderConnectionLifecycle lifecycle)
    {
        lifecycle.Transition(
            ConnectionId, ProviderConnectionState.Configured);
        lifecycle.Transition(
            ConnectionId, ProviderConnectionState.Authenticating);
        lifecycle.Transition(
            ConnectionId, ProviderConnectionState.Authenticated);
        lifecycle.Transition(
            ConnectionId, ProviderConnectionState.Connecting);
        lifecycle.Transition(
            ConnectionId, ProviderConnectionState.Connected);
    }

    private static (
        StandaloneBackendRuntime Runtime,
        StandaloneDiscoveryOrchestrator Orchestrator,
        ControlledAdapter Adapter)
        Create(bool blocked)
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                ConnectionId, "mock-provider"));

        Connect(runtime.ConnectionLifecycle);

        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        var adapter = new ControlledAdapter(blocked);
        coordinator.RegisterAdapter(adapter);

        var orchestrator = new StandaloneDiscoveryOrchestrator(
            runtime.ConnectionCatalog,
            coordinator,
            runtime.ConnectionLifecycle);

        return (runtime, orchestrator, adapter);
    }

    private static Task<PlatformState> Discover(
        StandaloneDiscoveryOrchestrator orchestrator,
        WorkbenchState workbench) =>
        orchestrator.DiscoverIntoWorkbenchAsync(
            ConnectionId,
            "account-001",
            "location-001",
            "compute",
            workbench);

    [Fact]
    public async Task UnregisteredGenerationCannotPublishPendingDiscovery()
    {
        var (runtime, orchestrator, adapter) = Create(true);
        var workbench = new WorkbenchState();
        int notifications = 0;

        workbench.Changed += (_, _) =>
            Interlocked.Increment(ref notifications);

        Task<PlatformState> pending =
            Discover(orchestrator, workbench);

        await adapter.Started.WaitAsync(TimeSpan.FromSeconds(10));

        try
        {
            Assert.True(
                runtime.ConnectionCatalog.Unregister(ConnectionId));

            runtime.ConnectionCatalog.Register(
                new ProviderConnectionIdentity(
                    ConnectionId, "mock-provider"));

            Connect(runtime.ConnectionLifecycle);
        }
        finally
        {
            adapter.Complete();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pending.WaitAsync(
                TimeSpan.FromSeconds(10)));

        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
        Assert.Equal(0, notifications);
    }

    [Fact]
    public async Task DisconnectionBeforeCompletionPreventsPublication()
    {
        var (runtime, orchestrator, adapter) = Create(true);
        var workbench = new WorkbenchState();
        int notifications = 0;

        workbench.Changed += (_, _) =>
            Interlocked.Increment(ref notifications);

        Task<PlatformState> pending =
            Discover(orchestrator, workbench);

        await adapter.Started.WaitAsync(TimeSpan.FromSeconds(10));

        try
        {
            runtime.ConnectionLifecycle.Transition(
                ConnectionId,
                ProviderConnectionState.Disconnected);
        }
        finally
        {
            adapter.Complete();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pending.WaitAsync(
                TimeSpan.FromSeconds(10)));

        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
        Assert.Equal(0, notifications);
    }

    [Fact]
    public async Task NotificationAllowsConcurrentCatalogUnregistration()
    {
        var (runtime, orchestrator, _) = Create(false);
        var workbench = new WorkbenchState();

        int notifications = 0;
        bool unregistered = false;

        workbench.Changed += (_, _) =>
        {
            Interlocked.Increment(ref notifications);

            // Run on another thread so a catalog lock held by
            // the notifying thread cannot be re-entered.
            unregistered = Task.Run(() =>
                runtime.ConnectionCatalog.Unregister(
                    ConnectionId))
                .WaitAsync(TimeSpan.FromSeconds(10))
                .GetAwaiter()
                .GetResult();
        };

        await Discover(orchestrator, workbench)
            .WaitAsync(TimeSpan.FromSeconds(15));

        Assert.True(
            unregistered,
            "NOTIFICATION_MUST_RUN_OUTSIDE_CATALOG_LOCK");

        Assert.Equal(1, notifications);
        Assert.NotNull(workbench.PlatformState);
        Assert.NotNull(workbench.InfrastructureGraph);

        Assert.False(
            runtime.ConnectionCatalog.TryGet(
                ConnectionId, out _));
    }
}
