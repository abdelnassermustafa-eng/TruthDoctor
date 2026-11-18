using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryRefreshConsistencyTests
{
    private sealed class RefreshAdapter : IStandaloneProviderAdapter
    {
        private int _calls;

        public string ProviderId => "mock-provider";

        public int Calls => Volatile.Read(ref _calls);

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

            if (call > 2)
            {
                throw new InvalidOperationException(
                    "Unexpected additional refresh.");
            }

            string id = call == 1
                ? "resource-first"
                : "resource-second";

            IReadOnlyList<ProviderResourceRecord> resources =
            [
                new ProviderResourceRecord(
                    "mock-provider",
                    id,
                    "virtual-machine",
                    id)
            ];

            return Task.FromResult(resources);
        }
    }

    private static void Connect(
        ProviderConnectionLifecycle lifecycle)
    {
        const string id = "connection-001";

        lifecycle.Transition(
            id, ProviderConnectionState.Configured);
        lifecycle.Transition(
            id, ProviderConnectionState.Authenticating);
        lifecycle.Transition(
            id, ProviderConnectionState.Authenticated);
        lifecycle.Transition(
            id, ProviderConnectionState.Connecting);
        lifecycle.Transition(
            id, ProviderConnectionState.Connected);
    }

    [Fact]
    public async Task RefreshNotifiesOnlyWithMatchingCompletedSnapshots()
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

        var adapter = new RefreshAdapter();
        coordinator.RegisterAdapter(adapter);

        var orchestrator = new StandaloneDiscoveryOrchestrator(
            runtime.ConnectionCatalog,
            coordinator,
            runtime.ConnectionLifecycle);

        var state = new WorkbenchState();

        var observed = new List<string>();

        state.Changed += (_, _) =>
        {
            Assert.NotNull(state.PlatformState);
            Assert.NotNull(state.InfrastructureGraph);

            string resourceId = Assert.Single(
                state.PlatformState.Resources).ResourceId;

            Assert.True(
                state.InfrastructureGraph.Nodes.ContainsKey(
                    resourceId));

            observed.Add(resourceId);
        };

        await orchestrator.DiscoverIntoWorkbenchAsync(
            "connection-001",
            "account-001",
            "location-001",
            "compute",
            state);

        Assert.Equal(
            ["resource-first"],
            observed);

        await orchestrator.DiscoverIntoWorkbenchAsync(
            "connection-001",
            "account-001",
            "location-001",
            "compute",
            state);

        Assert.Equal(
            ["resource-first", "resource-second"],
            observed);

        Assert.Equal(2, adapter.Calls);

        Assert.Equal(
            "resource-second",
            Assert.Single(state.PlatformState!.Resources).ResourceId);

        Assert.True(
            state.InfrastructureGraph!.Nodes.ContainsKey(
                "resource-second"));

        Assert.False(
            state.InfrastructureGraph.Nodes.ContainsKey(
                "resource-first"));
    }
}
