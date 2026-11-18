using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Graph;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryOrchestrationTests
{
    private sealed class MockAdapter : IStandaloneProviderAdapter
    {
        public string ProviderId => "mock-provider";

        public int DiscoveryCalls { get; private set; }

        public Func<string, CancellationToken,
            Task<IReadOnlyList<ProviderResourceRecord>>> Discover
            { get; init; } = (_, _) =>
                Task.FromResult<IReadOnlyList<ProviderResourceRecord>>(
                [
                    new ProviderResourceRecord(
                        "mock-provider",
                        "resource-001",
                        "virtual-machine",
                        "Sample Resource")
                ]);

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
            DiscoveryCalls++;
            return Discover(connectionId, cancellationToken);
        }
    }


    private static void AdvanceToConnected(
        ProviderConnectionLifecycle lifecycle)
    {
        lifecycle.Transition(
            "connection-001",
            ProviderConnectionState.Configured);
        lifecycle.Transition(
            "connection-001",
            ProviderConnectionState.Authenticating);
        lifecycle.Transition(
            "connection-001",
            ProviderConnectionState.Authenticated);
        lifecycle.Transition(
            "connection-001",
            ProviderConnectionState.Connecting);
        lifecycle.Transition(
            "connection-001",
            ProviderConnectionState.Connected);
    }

    private static (
        ProviderConnectionCatalog Catalog,
        StandaloneProviderAdapterCoordinator Coordinator,
        ProviderConnectionLifecycle Lifecycle,
        MockAdapter Adapter) Setup()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                "connection-001",
                "mock-provider"));

        AdvanceToConnected(runtime.ConnectionLifecycle);
        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        var adapter = new MockAdapter();
        coordinator.RegisterAdapter(adapter);

        return (runtime.ConnectionCatalog, coordinator, runtime.ConnectionLifecycle, adapter);
    }

    private static async Task<PlatformState> ExecuteAsync(
        ProviderConnectionCatalog catalog,
        StandaloneProviderAdapterCoordinator coordinator,
        ProviderConnectionLifecycle lifecycle,
        WorkbenchState state,
        string connectionId = "connection-001",
        CancellationToken cancellationToken = default)
    {
        Type? type = typeof(ProviderResourceRecord)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.StandaloneDiscoveryOrchestrator",
                throwOnError: false);

        Assert.True(
            type is not null,
            "RED_EXPECTED_MISSING_STANDALONE_DISCOVERY_ORCHESTRATOR");

        ConstructorInfo? constructor = type.GetConstructor(
        [
            typeof(ProviderConnectionCatalog),
            typeof(StandaloneProviderAdapterCoordinator),
            typeof(ProviderConnectionLifecycle)
        ]);

        Assert.NotNull(constructor);

        object instance = constructor.Invoke([catalog, coordinator, lifecycle]);

        MethodInfo? method = type.GetMethod(
            "DiscoverIntoWorkbenchAsync",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);

        object? invocation = method.Invoke(
            instance,
            [
                connectionId,
                "account-001",
                "location-001",
                "compute",
                state,
                cancellationToken
            ]);

        Task<PlatformState> task =
            Assert.IsAssignableFrom<Task<PlatformState>>(invocation);

        return await task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task RegisteredAdapterPopulatesWorkbenchWithoutTruthApi()
    {
        var setup = Setup();
        var state = new WorkbenchState();

        PlatformState result = await ExecuteAsync(
            setup.Catalog, setup.Coordinator, setup.Lifecycle, state);

        Assert.Equal(1, setup.Adapter.DiscoveryCalls);
        Assert.Same(result, state.PlatformState);
        Assert.Single(result.Resources);

        var resource = result.Resources[0];
        Assert.Equal("mock-provider", resource.ProviderId);
        Assert.Equal("resource-001", resource.ResourceId);
        Assert.Equal("account-001", resource.AccountId);
        Assert.Equal("location-001", resource.Location);
        Assert.Equal("compute", resource.DomainId);

        Assert.NotNull(state.InfrastructureGraph);
        Assert.True(
            state.InfrastructureGraph.Nodes.ContainsKey(
                "resource-001"));
        Assert.Single(state.VisibleResources);
    }

    [Fact]
    public async Task UnknownConnectionCannotInvokeProvider()
    {
        var setup = Setup();
        var state = new WorkbenchState();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => ExecuteAsync(
                setup.Catalog,
                setup.Coordinator,
                setup.Lifecycle, state,
                connectionId: "unknown-connection"));

        Assert.Equal(0, setup.Adapter.DiscoveryCalls);
        Assert.Null(state.PlatformState);
        Assert.Null(state.InfrastructureGraph);
    }

    [Fact]
    public async Task MismatchedProviderResultCannotPublishState()
    {
        var runtime = new StandaloneBackendRuntime();
        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                "connection-001", "mock-provider"));

        AdvanceToConnected(runtime.ConnectionLifecycle);
        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog, runtime.ConnectionLifecycle);

        var adapter = new MockAdapter
        {
            Discover = (_, _) =>
                Task.FromResult<IReadOnlyList<ProviderResourceRecord>>(
                [
                    new ProviderResourceRecord(
                        "wrong-provider",
                        "resource-001",
                        "virtual-machine",
                        "Invalid Provider")
                ])
        };
        coordinator.RegisterAdapter(adapter);

        var state = new WorkbenchState();

        await Assert.ThrowsAsync<ArgumentException>(
            () => ExecuteAsync(
                runtime.ConnectionCatalog,
                coordinator,
                runtime.ConnectionLifecycle, state));

        Assert.Equal(1, adapter.DiscoveryCalls);
        Assert.Null(state.PlatformState);
        Assert.Null(state.InfrastructureGraph);
    }

    [Fact]
    public async Task CancelledRequestCannotPublishState()
    {
        var setup = Setup();
        var state = new WorkbenchState();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(
                setup.Catalog,
                setup.Coordinator,
                setup.Lifecycle, state,
                cancellationToken: cts.Token));

        Assert.Equal(0, setup.Adapter.DiscoveryCalls);
        Assert.Null(state.PlatformState);
        Assert.Null(state.InfrastructureGraph);
    }

    [Fact]
    public async Task ProviderFailurePreservesExistingWorkbenchState()
    {
        var runtime = new StandaloneBackendRuntime();
        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                "connection-001", "mock-provider"));

        AdvanceToConnected(runtime.ConnectionLifecycle);
        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog, runtime.ConnectionLifecycle);

        coordinator.RegisterAdapter(new MockAdapter
        {
            Discover = (_, _) =>
                Task.FromException<IReadOnlyList<ProviderResourceRecord>>(
                    new InvalidOperationException("Provider failure"))
        });

        var original = StandaloneDiscoveryProjector.Project(
        [
            new ProviderResourceRecord(
                "mock-provider",
                "existing-resource",
                "virtual-machine",
                "Existing")
        ], "account-001", "location-001", "compute");

        var graph = new InfrastructureGraphBuilder().Build(original);
        var state = new WorkbenchState();
        state.SetPlatformState(original);
        state.SetInfrastructureGraph(graph);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ExecuteAsync(
                runtime.ConnectionCatalog,
                coordinator,
                runtime.ConnectionLifecycle, state));

        Assert.Same(original, state.PlatformState);
        Assert.Same(graph, state.InfrastructureGraph);
        Assert.Single(state.VisibleResources);
    }
}
