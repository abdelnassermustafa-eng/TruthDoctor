using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A237ProviderResourceDiscoveryContractTests
{
    private static MethodInfo? FindDiscoveryMethod()
    {
        return typeof(IProviderResourceDiscovery).GetMethod(
            "DiscoverAsync",
            new[]
            {
                typeof(string),
                typeof(CancellationToken)
            });
    }

    [Fact]
    public void DiscoveryRequiresConnectionScopedCancellation()
    {
        MethodInfo? method = FindDiscoveryMethod();

        Assert.NotNull(method);
        Assert.True(method.IsAbstract);

        ParameterInfo[] parameters = method.GetParameters();

        Assert.Equal("connectionId", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void DiscoveryReturnsExistingPlatformStateModel()
    {
        MethodInfo? method = FindDiscoveryMethod();

        Assert.NotNull(method);
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(
            typeof(Task<>),
            method.ReturnType.GetGenericTypeDefinition());

        Type result = method.ReturnType.GenericTypeArguments[0];

        Assert.Equal("PlatformState", result.Name);
        Assert.Equal(
            typeof(IProviderResourceDiscovery).Assembly,
            result.Assembly);
    }
}
