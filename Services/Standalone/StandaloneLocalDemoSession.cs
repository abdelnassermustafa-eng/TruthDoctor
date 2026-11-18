using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.State;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Explicitly local, synthetic discovery for desktop integration.
/// No credentials, HTTP, cloud SDK, or external provider calls.
/// </summary>
public sealed class StandaloneLocalDemoSession
{
    private const string ConnectionId = "truthdoctor-local-demo";
    private const string ProviderIdValue = "local-demo";

    private readonly StandaloneDiscoveryOrchestrator _orchestrator;

    public StandaloneLocalDemoSession()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                ConnectionId, ProviderIdValue));

        var lifecycle = runtime.ConnectionLifecycle;

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

        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog, lifecycle);

        coordinator.RegisterAdapter(new DemoAdapter());

        _orchestrator = new StandaloneDiscoveryOrchestrator(
            runtime.ConnectionCatalog, coordinator, lifecycle);
    }

    public Task<PlatformState> DiscoverAsync(
        WorkbenchState workbench,
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        string requestedLocation =
            string.IsNullOrWhiteSpace(location)
                ? "local-demo"
                : location;

        return _orchestrator.DiscoverIntoWorkbenchAsync(
            ConnectionId,
            "local-demo-account",
            requestedLocation,
            "compute",
            workbench,
            cancellationToken);
    }

    private sealed class DemoAdapter : IStandaloneProviderAdapter
    {
        public string ProviderId => ProviderIdValue;

        public Task AuthenticateAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DisconnectAsync(
            string connectionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ProviderResourceRecord>>
            DiscoverAsync(
                string connectionId,
                CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<ProviderResourceRecord> resources =
            [
                new ProviderResourceRecord(
                    ProviderIdValue,
                    "demo-vm-001",
                    "virtual-machine",
                    "Demo Application Server"),
                new ProviderResourceRecord(
                    ProviderIdValue,
                    "demo-network-001",
                    "virtual-network",
                    "Demo Network"),
                new ProviderResourceRecord(
                    ProviderIdValue,
                    "demo-storage-001",
                    "storage-volume",
                    "Demo Data Volume")
            ];

            return Task.FromResult(resources);
        }
    }
}
