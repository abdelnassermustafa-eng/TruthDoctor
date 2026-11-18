using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;
using TruthDoctor.Services.Standalone;
using TruthDoctor.State;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AStandaloneDiscoveryLifecycleAuthorizationTests
{
    private sealed class MockAdapter : IStandaloneProviderAdapter
    {
        public string ProviderId => "mock-provider";

        public int DiscoveryCalls { get; private set; }

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

    private static (
        StandaloneBackendRuntime Runtime,
        StandaloneProviderAdapterCoordinator Coordinator,
        MockAdapter Adapter) Setup()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                "connection-001",
                "mock-provider"));

        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        var adapter = new MockAdapter();
        coordinator.RegisterAdapter(adapter);

        return (runtime, coordinator, adapter);
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

    private static async Task<PlatformState> DiscoverAsync(
        StandaloneBackendRuntime runtime,
        StandaloneProviderAdapterCoordinator coordinator,
        WorkbenchState state)
    {
        Type type = typeof(StandaloneBackendRuntime)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.StandaloneDiscoveryOrchestrator",
                throwOnError: true)!;

        Type[] threeArguments =
        [
            typeof(ProviderConnectionCatalog),
            typeof(StandaloneProviderAdapterCoordinator),
            typeof(ProviderConnectionLifecycle)
        ];

        ConstructorInfo? constructor =
            type.GetConstructor(threeArguments);

        object instance;

        if (constructor is not null)
        {
            instance = constructor.Invoke(
            [
                runtime.ConnectionCatalog,
                coordinator,
                runtime.ConnectionLifecycle
            ]);
        }
        else
        {
            constructor = type.GetConstructor(
            [
                typeof(ProviderConnectionCatalog),
                typeof(StandaloneProviderAdapterCoordinator)
            ]);

            Assert.NotNull(constructor);

            instance = constructor.Invoke(
            [
                runtime.ConnectionCatalog,
                coordinator
            ]);
        }

        MethodInfo? method = type.GetMethod(
            "DiscoverIntoWorkbenchAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        object? invocation = method.Invoke(
            instance,
            [
                "connection-001",
                "account-001",
                "location-001",
                "compute",
                state,
                CancellationToken.None
            ]);

        var task = Assert.IsAssignableFrom<Task<PlatformState>>(
            invocation);

        return await task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task DisconnectedConnectionCannotDiscover()
    {
        var setup = Setup();
        var state = new WorkbenchState();

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            setup.Runtime.ConnectionLifecycle.GetState(
                "connection-001"));

        try
        {
            await DiscoverAsync(
                setup.Runtime, setup.Coordinator, state);

            Assert.Fail(
                "RED_EXPECTED_DISCONNECTED_DISCOVERY_REJECTION");
        }
        catch (InvalidOperationException)
        {
            // Only a lifecycle authorization rejection is valid.
        }

        Assert.Equal(0, setup.Adapter.DiscoveryCalls);
        Assert.Null(state.PlatformState);
        Assert.Null(state.InfrastructureGraph);
    }

    [Fact]
    public async Task ConfiguredConnectionCannotDiscover()
    {
        var setup = Setup();
        var state = new WorkbenchState();

        setup.Runtime.ConnectionLifecycle.Transition(
            "connection-001",
            ProviderConnectionState.Configured);

        try
        {
            await DiscoverAsync(
                setup.Runtime, setup.Coordinator, state);

            Assert.Fail(
                "RED_EXPECTED_CONFIGURED_DISCOVERY_REJECTION");
        }
        catch (InvalidOperationException)
        {
            // A configured connection is not connected.
        }

        Assert.Equal(0, setup.Adapter.DiscoveryCalls);
        Assert.Null(state.PlatformState);
        Assert.Null(state.InfrastructureGraph);
    }

    [Fact]
    public async Task ConnectedConnectionCanDiscover()
    {
        var setup = Setup();
        AdvanceToConnected(
            setup.Runtime.ConnectionLifecycle);

        var state = new WorkbenchState();

        PlatformState platform = await DiscoverAsync(
            setup.Runtime, setup.Coordinator, state);

        Assert.Equal(1, setup.Adapter.DiscoveryCalls);
        Assert.Same(platform, state.PlatformState);
        Assert.Single(platform.Resources);
        Assert.NotNull(state.InfrastructureGraph);

        Assert.True(
            state.InfrastructureGraph.Nodes.ContainsKey(
                "resource-001"));
    }
}
