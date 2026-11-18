using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A32StandaloneRuntimeBehaviorTests
{
    private static readonly TimeSpan Timeout =
        TimeSpan.FromSeconds(10);

    private static IStandaloneBackendRuntime CreateRuntime()
    {
        Type? type = typeof(IStandaloneBackendRuntime)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.StandaloneBackendRuntime",
                throwOnError: false);

        Assert.True(
            type is not null,
            "Concrete StandaloneBackendRuntime is not implemented.");

        Assert.True(
            typeof(IStandaloneBackendRuntime).IsAssignableFrom(type),
            "Concrete runtime must implement IStandaloneBackendRuntime.");

        ConstructorInfo? constructor =
            type.GetConstructor(Type.EmptyTypes);

        Assert.NotNull(constructor);

        object? instance = constructor.Invoke(null);

        return Assert.IsAssignableFrom<IStandaloneBackendRuntime>(
            instance);
    }

    private static bool IsRunning(IStandaloneBackendRuntime runtime)
    {
        PropertyInfo? property = runtime.GetType().GetProperty(
            "IsRunning",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(property);

        Assert.Equal(typeof(bool), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);

        object? value = property.GetValue(runtime);
        return Assert.IsType<bool>(value);
    }

    private static async Task Start(IStandaloneBackendRuntime runtime)
    {
        await runtime.StartAsync(CancellationToken.None)
            .WaitAsync(Timeout);
    }

    private static async Task Stop(IStandaloneBackendRuntime runtime)
    {
        await runtime.StopAsync(CancellationToken.None)
            .WaitAsync(Timeout);
    }

    [Fact]
    public async Task StartupIsIdempotent()
    {
        var runtime = CreateRuntime();

        Assert.False(IsRunning(runtime));

        await Start(runtime);
        Assert.True(IsRunning(runtime));

        await Start(runtime);
        Assert.True(IsRunning(runtime));

        await Stop(runtime);
        Assert.False(IsRunning(runtime));
    }

    [Fact]
    public async Task ShutdownIsIdempotent()
    {
        var runtime = CreateRuntime();

        await Stop(runtime);
        Assert.False(IsRunning(runtime));

        await Start(runtime);
        Assert.True(IsRunning(runtime));

        await Stop(runtime);
        await Stop(runtime);

        Assert.False(IsRunning(runtime));
    }

    [Fact]
    public async Task RuntimeCanRestartAfterShutdown()
    {
        var runtime = CreateRuntime();

        await Start(runtime);
        Assert.True(IsRunning(runtime));

        await Stop(runtime);
        Assert.False(IsRunning(runtime));

        await Start(runtime);
        Assert.True(IsRunning(runtime));

        await Stop(runtime);
        Assert.False(IsRunning(runtime));
    }

    [Fact]
    public async Task CancellationPreservesRuntimeState()
    {
        var runtime = CreateRuntime();

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await runtime.StartAsync(cancellation.Token));

        Assert.False(IsRunning(runtime));

        await Start(runtime);
        Assert.True(IsRunning(runtime));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await runtime.StopAsync(cancellation.Token));

        Assert.True(IsRunning(runtime));

        await Stop(runtime);
        Assert.False(IsRunning(runtime));
    }

    [Fact]
    public async Task IndependentRuntimeInstancesRemainIsolated()
    {
        var first = CreateRuntime();
        var second = CreateRuntime();

        Assert.NotSame(first, second);

        Assert.False(IsRunning(first));
        Assert.False(IsRunning(second));

        await Start(first);

        Assert.True(IsRunning(first));
        Assert.False(IsRunning(second));

        await Start(second);
        await Stop(first);

        Assert.False(IsRunning(first));
        Assert.True(IsRunning(second));

        await Stop(second);

        Assert.False(IsRunning(second));
    }
}
