using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A34RuntimeOwnershipTests
{
    private const string MissingCatalog =
        "Runtime must expose its owned ConnectionCatalog.";

    private const string MissingLifecycle =
        "Runtime must expose its owned ConnectionLifecycle.";

    private static ProviderConnectionCatalog Catalog(
        StandaloneBackendRuntime runtime)
    {
        PropertyInfo? property = typeof(StandaloneBackendRuntime)
            .GetProperty(
                "ConnectionCatalog",
                BindingFlags.Instance | BindingFlags.Public);

        Assert.True(property is not null, MissingCatalog);
        Assert.Equal(
            typeof(ProviderConnectionCatalog),
            property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);

        object? value = property.GetValue(runtime);

        return Assert.IsType<ProviderConnectionCatalog>(value);
    }

    private static ProviderConnectionLifecycle Lifecycle(
        StandaloneBackendRuntime runtime)
    {
        PropertyInfo? property = typeof(StandaloneBackendRuntime)
            .GetProperty(
                "ConnectionLifecycle",
                BindingFlags.Instance | BindingFlags.Public);

        Assert.True(property is not null, MissingLifecycle);
        Assert.Equal(
            typeof(ProviderConnectionLifecycle),
            property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);

        object? value = property.GetValue(runtime);

        return Assert.IsType<ProviderConnectionLifecycle>(value);
    }

    [Fact]
    public void RuntimeExposesOwnedConnectionCatalog()
    {
        var runtime = new StandaloneBackendRuntime();

        var catalog = Catalog(runtime);

        Assert.Same(catalog, Catalog(runtime));
    }

    [Fact]
    public void RuntimeExposesOwnedConnectionLifecycle()
    {
        var runtime = new StandaloneBackendRuntime();

        var lifecycle = Lifecycle(runtime);

        Assert.Same(lifecycle, Lifecycle(runtime));
    }

    [Fact]
    public void OwnedLifecycleUsesRuntimeConnectionCatalog()
    {
        var runtime = new StandaloneBackendRuntime();

        var catalog = Catalog(runtime);
        var lifecycle = Lifecycle(runtime);

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        lifecycle.Transition(
            "aws-primary",
            ProviderConnectionState.Configured);

        Assert.Equal(
            ProviderConnectionState.Configured,
            lifecycle.GetState("aws-primary"));

        Assert.True(catalog.Unregister("aws-primary"));

        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
            () => lifecycle.GetState("aws-primary"));
    }

    [Fact]
    public void IndependentRuntimeConnectionsRemainIsolated()
    {
        var first = new StandaloneBackendRuntime();
        var second = new StandaloneBackendRuntime();

        var firstCatalog = Catalog(first);
        var secondCatalog = Catalog(second);

        var firstLifecycle = Lifecycle(first);
        var secondLifecycle = Lifecycle(second);

        Assert.NotSame(firstCatalog, secondCatalog);
        Assert.NotSame(firstLifecycle, secondLifecycle);

        firstCatalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        secondCatalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        firstLifecycle.Transition(
            "aws-primary",
            ProviderConnectionState.Configured);

        Assert.Equal(
            ProviderConnectionState.Configured,
            firstLifecycle.GetState("aws-primary"));

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            secondLifecycle.GetState("aws-primary"));
    }

    [Fact]
    public async Task ExistingRuntimeStartupShutdownRemainsFunctional()
    {
        var runtime = new StandaloneBackendRuntime();
        IStandaloneBackendRuntime contract = runtime;

        Assert.False(runtime.IsRunning);

        await contract.StartAsync(CancellationToken.None);

        Assert.True(runtime.IsRunning);

        await contract.StopAsync(CancellationToken.None);

        Assert.False(runtime.IsRunning);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => contract.StartAsync(canceled.Token));

        Assert.False(runtime.IsRunning);
    }
}
