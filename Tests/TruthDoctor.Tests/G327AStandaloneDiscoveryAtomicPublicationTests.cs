using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryAtomicPublicationTests
{
    private sealed class MockAdapter : IStandaloneProviderAdapter
    {
        public string ProviderId => "mock-provider";

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
            IReadOnlyList<ProviderResourceRecord> resources =
            [
                new ProviderResourceRecord(
                    "mock-provider",
                    "resource-001",
                    "virtual-machine",
                    "Sample Resource")
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
    public async Task DiscoveryNotifiesOnlyAfterStateAndGraphAreConsistent()
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

        coordinator.RegisterAdapter(new MockAdapter());

        var orchestrator = new StandaloneDiscoveryOrchestrator(
            runtime.ConnectionCatalog,
            coordinator,
            runtime.ConnectionLifecycle);

        var workbench = new WorkbenchState();

        int notifications = 0;
        int inconsistentNotifications = 0;

        workbench.Changed += (_, _) =>
        {
            notifications++;

            bool platformPresent =
                workbench.PlatformState is not null;

            bool graphPresent =
                workbench.InfrastructureGraph is not null;

            if (platformPresent != graphPresent)
            {
                inconsistentNotifications++;
            }
        };

        await orchestrator.DiscoverIntoWorkbenchAsync(
            "connection-001",
            "account-001",
            "location-001",
            "compute",
            workbench);

        Assert.NotNull(workbench.PlatformState);
        Assert.NotNull(workbench.InfrastructureGraph);
        Assert.Single(workbench.PlatformState.Resources);

        Assert.True(
            workbench.InfrastructureGraph.Nodes.ContainsKey(
                "resource-001"));

        Assert.True(
            inconsistentNotifications == 0,
            "RED_EXPECTED_ATOMIC_WORKBENCH_PUBLICATION");

        Assert.Equal(1, notifications);
    }
}
