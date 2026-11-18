using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryRegistrationGenerationTests
{
    private sealed class BlockingAdapter : IStandaloneProviderAdapter
    {
        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<IReadOnlyList<ProviderResourceRecord>>
            _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string ProviderId => "mock-provider";

        public int Calls { get; private set; }

        public Task Started => _started.Task;

        public void Complete()
        {
            IReadOnlyList<ProviderResourceRecord> resources =
            [
                new ProviderResourceRecord(
                    "mock-provider",
                    "stale-resource",
                    "virtual-machine",
                    "Stale Resource")
            ];
            _release.TrySetResult(resources);
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
            Calls++;
            _started.TrySetResult(true);
            return _release.Task;
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
    public async Task ReRegisteredConnectionCannotPublishOldDiscovery()
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

        var adapter = new BlockingAdapter();
        coordinator.RegisterAdapter(adapter);

        var orchestrator = new StandaloneDiscoveryOrchestrator(
            runtime.ConnectionCatalog,
            coordinator,
            runtime.ConnectionLifecycle);

        var workbench = new WorkbenchState();

        long originalGeneration =
            runtime.ConnectionCatalog.GetRegistrationGeneration(
                "connection-001");

        Task<TruthDoctor.Models.Platform.PlatformState> pending =
            orchestrator.DiscoverIntoWorkbenchAsync(
                "connection-001",
                "account-001",
                "location-001",
                "compute",
                workbench);

        await adapter.Started.WaitAsync(TimeSpan.FromSeconds(10));

        try
        {
            Assert.True(
                runtime.ConnectionCatalog.Unregister(
                    "connection-001"));

            runtime.ConnectionCatalog.Register(
                new ProviderConnectionIdentity(
                    "connection-001",
                    "mock-provider"));

            long replacementGeneration =
                runtime.ConnectionCatalog.GetRegistrationGeneration(
                    "connection-001");

            Assert.NotEqual(
                originalGeneration,
                replacementGeneration);

            Connect(runtime.ConnectionLifecycle);
        }
        finally
        {
            // Always unblock the old provider operation.
            adapter.Complete();
        }

        bool rejected = false;

        try
        {
            await pending.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Assert.True(
            rejected,
            "RED_EXPECTED_STALE_REGISTRATION_REJECTION");

        Assert.Equal(1, adapter.Calls);
        Assert.Null(workbench.PlatformState);
        Assert.Null(workbench.InfrastructureGraph);
        Assert.Empty(workbench.VisibleResources);
    }
}
