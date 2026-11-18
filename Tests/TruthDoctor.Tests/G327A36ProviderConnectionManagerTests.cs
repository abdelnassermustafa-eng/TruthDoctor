using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A36ProviderConnectionManagerTests
{
    private static readonly TimeSpan Timeout =
        TimeSpan.FromSeconds(10);

    private const string MissingManager =
        "Concrete ProviderConnectionManager is not implemented.";

    private static IProviderConnectionManager CreateManager(
        StandaloneBackendRuntime runtime,
        Func<string, CancellationToken, Task> authenticate)
    {
        Type? implementation = typeof(IProviderConnectionManager)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.ProviderConnectionManager",
                throwOnError: false);

        Assert.True(implementation is not null, MissingManager);

        Assert.True(
            typeof(IProviderConnectionManager)
                .IsAssignableFrom(implementation),
            "Manager must implement IProviderConnectionManager.");

        ConstructorInfo? constructor = implementation.GetConstructor(
            new[]
            {
                typeof(ProviderConnectionCatalog),
                typeof(ProviderConnectionLifecycle),
                typeof(Func<string, CancellationToken, Task>)
            });

        Assert.NotNull(constructor);

        object? instance = constructor.Invoke(
            new object[]
            {
                runtime.ConnectionCatalog,
                runtime.ConnectionLifecycle,
                authenticate
            });

        return Assert.IsAssignableFrom<IProviderConnectionManager>(
            instance);
    }

    private static async Task Connect(
        IProviderConnectionManager manager,
        string id)
    {
        await manager.ConnectAsync(
            id, CancellationToken.None).WaitAsync(Timeout);
    }

    private static async Task Disconnect(
        IProviderConnectionManager manager,
        string id)
    {
        await manager.DisconnectAsync(
            id, CancellationToken.None).WaitAsync(Timeout);
    }

    [Fact]
    public void ManagerImplementsContractAndUsesRuntimeOwnership()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity("aws-primary", "aws"));

        var manager = CreateManager(
            runtime, (_, _) => Task.CompletedTask);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            manager.GetState("aws-primary"));

        Assert.Same(
            runtime.ConnectionCatalog,
            runtime.ConnectionCatalog);

        Assert.Same(
            runtime.ConnectionLifecycle,
            runtime.ConnectionLifecycle);
    }

    [Fact]
    public async Task UnknownConnectionIsRejectedBeforeAuthentication()
    {
        var runtime = new StandaloneBackendRuntime();
        int authenticationCalls = 0;

        var manager = CreateManager(runtime, (_, _) =>
        {
            Interlocked.Increment(ref authenticationCalls);
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => manager.ConnectAsync(
                "missing", CancellationToken.None));

        Assert.Equal(0, authenticationCalls);

        Assert.Throws<KeyNotFoundException>(
            () => manager.GetState("missing"));
    }

    [Fact]
    public async Task SeparateConnectionsAuthenticateIndependently()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity("aws-primary", "aws"));

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity("aws-secondary", "aws"));

        int authenticationCalls = 0;

        var manager = CreateManager(runtime, (_, _) =>
        {
            Interlocked.Increment(ref authenticationCalls);
            return Task.CompletedTask;
        });

        await Connect(manager, "aws-primary");

        Assert.Equal(
            ProviderConnectionState.Connected,
            manager.GetState("aws-primary"));

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            manager.GetState("aws-secondary"));

        await Connect(manager, "aws-secondary");

        Assert.Equal(
            ProviderConnectionState.Connected,
            manager.GetState("aws-secondary"));

        Assert.Equal(2, authenticationCalls);
    }

    [Fact]
    public async Task DisconnectRetainsRegistrationAndClearsState()
    {
        var runtime = new StandaloneBackendRuntime();

        var identity = new ProviderConnectionIdentity(
            "aws-primary", "aws");

        runtime.ConnectionCatalog.Register(identity);

        long generation = runtime.ConnectionCatalog
            .GetRegistrationGeneration("aws-primary");

        var manager = CreateManager(
            runtime, (_, _) => Task.CompletedTask);

        await Connect(manager, "aws-primary");
        await Disconnect(manager, "aws-primary");

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            manager.GetState("aws-primary"));

        Assert.Same(
            identity,
            runtime.ConnectionCatalog.Get("aws-primary"));

        Assert.Equal(
            generation,
            runtime.ConnectionCatalog
                .GetRegistrationGeneration("aws-primary"));
    }

    [Fact]
    public async Task FailedAuthenticationAndCancellationNeverConnect()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity("aws-primary", "aws"));

        int authenticationCalls = 0;

        var manager = CreateManager(runtime, (_, _) =>
        {
            Interlocked.Increment(ref authenticationCalls);
            throw new InvalidOperationException(
                "Controlled authentication failure.");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.ConnectAsync(
                "aws-primary", CancellationToken.None));

        Assert.NotEqual(
            ProviderConnectionState.Connected,
            manager.GetState("aws-primary"));

        Assert.NotEqual(
            ProviderConnectionState.Authenticated,
            manager.GetState("aws-primary"));

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => manager.ConnectAsync(
                "aws-primary", canceled.Token));

        Assert.Equal(1, authenticationCalls);

        Assert.NotEqual(
            ProviderConnectionState.Connected,
            manager.GetState("aws-primary"));
    }

    [Fact]
    public async Task ExistingRuntimeOwnershipAndLifecycleRemainIntact()
    {
        var runtime = new StandaloneBackendRuntime();

        var catalog = runtime.ConnectionCatalog;
        var lifecycle = runtime.ConnectionLifecycle;

        Assert.Same(catalog, runtime.ConnectionCatalog);
        Assert.Same(lifecycle, runtime.ConnectionLifecycle);

        catalog.Register(
            new ProviderConnectionIdentity("aws-primary", "aws"));

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Configured);

        Assert.Equal(
            ProviderConnectionState.Configured,
            lifecycle.GetState("aws-primary"));

        await runtime.StartAsync(CancellationToken.None);

        Assert.True(runtime.IsRunning);

        await runtime.StopAsync(CancellationToken.None);

        Assert.False(runtime.IsRunning);

        Assert.Same(catalog, runtime.ConnectionCatalog);
        Assert.Same(lifecycle, runtime.ConnectionLifecycle);
    }
}
