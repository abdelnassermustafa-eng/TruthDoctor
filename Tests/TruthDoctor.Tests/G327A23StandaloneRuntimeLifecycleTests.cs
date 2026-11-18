using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A23StandaloneRuntimeLifecycleTests
{
    [Fact]
    public void RuntimeRequiresCancellableAsynchronousStart()
    {
        MethodInfo? method =
            typeof(IStandaloneBackendRuntime).GetMethod(
                "StartAsync",
                new[] { typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.True(method.IsAbstract);
    }

    [Fact]
    public void RuntimeRequiresCancellableAsynchronousStop()
    {
        MethodInfo? method =
            typeof(IStandaloneBackendRuntime).GetMethod(
                "StopAsync",
                new[] { typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.True(method.IsAbstract);
    }
}
