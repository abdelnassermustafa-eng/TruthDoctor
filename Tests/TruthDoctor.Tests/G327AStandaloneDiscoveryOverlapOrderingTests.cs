using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryOverlapOrderingTests
{
    private sealed class OrderedAdapter : IStandaloneProviderAdapter
    {
        private readonly TaskCompletionSource<bool> _olderStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<IReadOnlyList<ProviderResourceRecord>>
            _olderRelease = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        private int _calls;

        public string ProviderId => "mock-provider";

        public int Calls => Volatile.Read(ref _calls);

        public Task OlderStarted => _olderStarted.Task;

        public void CompleteOlder()
        {
            IReadOnlyList<ProviderResourceRecord> records =
            [
                new ProviderResourceRecord(
                    "mock-provider",
                    "older-resource",
                    "virtual-machine",
                    "Older Discovery")
            ];

            _olderRelease.TrySetResult(records);
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
            int call = Interlocked.Increment(ref _calls);

            if (call == 1)
            {
                _olderStarted.TrySetResult(true);
                return _olderRelease.Task;
            }

            if (call == 2)
            {
                IReadOnlyList<ProviderResourceRecord> newer =
                [
                    new ProviderResourceRecord(
                        "mock-provider",
                        "newer-resource",
                        "virtual-machine",
                        "Newer Discovery")
                ];

                return Task.FromResult(newer);
            }

            throw new InvalidOperationException(
                "Unexpected additional discovery request.");
        }
    }

    private static void Connect(ProviderConnectionLifecycle lifecycle)
    {
        const string id = "connection-001";

        lifecycle.Transition(id, ProviderConnectionState.Configured);
        lifecycle.Transition(id, ProviderConnectionState.Authenticating);
        lifecycle.Transition(id, ProviderConnectionState.Authenticated);
        lifecycle.Transition(id, ProviderConnectionState.Connecting);
        lifecycle.Transition(id, ProviderConnectionState.Connected);
    }

    [Fact]
    public async Task OlderDiscoveryCannotOverwriteNewerPublishedResults()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                "connection-001",
                "mock-provider"));

        Connect(runtime.ConnectionLifecycle);

        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        var adapter = new OrderedAdapter();
        coordinator.RegisterAdapter(adapter);

        var orchestrator = new StandaloneDiscoveryOrchestrator(
            runtime.ConnectionCatalog,
            coordinator,
            runtime.ConnectionLifecycle);

        var workbench = new WorkbenchState();

        long registrationGeneration =
            runtime.ConnectionCatalog.GetRegistrationGeneration(
                "connection-001");

        Task<PlatformState> older =
            orchestrator.DiscoverIntoWorkbenchAsync(
                "connection-001",
                "account-001",
                "location-001",
                "compute",
                workbench);

        await adapter.OlderStarted.WaitAsync(
            TimeSpan.FromSeconds(10));

        Task<PlatformState>? newerTask = null;
        PlatformState? newer = null;

        try
        {
            newerTask = orchestrator.DiscoverIntoWorkbenchAsync(
                "connection-001",
                "account-001",
                "location-001",
                "compute",
                workbench);

            newer = await newerTask.WaitAsync(
                TimeSpan.FromSeconds(10));

            Assert.Equal(
                "newer-resource",
                Assert.Single(newer.Resources).ResourceId);

            Assert.Same(newer, workbench.PlatformState);

            Assert.NotNull(workbench.InfrastructureGraph);
            Assert.True(
                workbench.InfrastructureGraph.Nodes.ContainsKey(
                    "newer-resource"));
        }
        finally
        {
            // Always release the first provider invocation.
            adapter.CompleteOlder();
        }

        // A superseded operation may reject completion or return
        // its snapshot, but it must not publish stale state.
        try
        {
            await older.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (InvalidOperationException)
        {
            // Explicit supersession rejection is acceptable.
        }
        catch (OperationCanceledException)
        {
            // Explicit supersession cancellation is acceptable.
        }

        Assert.Equal(2, adapter.Calls);

        Assert.Equal(
            registrationGeneration,
            runtime.ConnectionCatalog.GetRegistrationGeneration(
                "connection-001"));

        Assert.Equal(
            ProviderConnectionState.Connected,
            runtime.ConnectionLifecycle.GetState(
                "connection-001"));

        Assert.True(
            ReferenceEquals(newer, workbench.PlatformState),
            "RED_EXPECTED_NEWEST_DISCOVERY_WINS");

        Assert.NotNull(workbench.InfrastructureGraph);

        Assert.True(
            workbench.InfrastructureGraph.Nodes.ContainsKey(
                "newer-resource"),
            "RED_EXPECTED_NEWER_GRAPH_PRESERVED");

        Assert.False(
            workbench.InfrastructureGraph.Nodes.ContainsKey(
                "older-resource"));

        Assert.Equal(
            "newer-resource",
            Assert.Single(workbench.VisibleResources).ResourceId);
    }
}
