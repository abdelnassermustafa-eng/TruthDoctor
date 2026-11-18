using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A235ProviderConnectionContractTests
{
    [Fact]
    public void ConnectionRequiresCancellableAsyncConnect()
    {
        MethodInfo? method =
            typeof(IProviderConnectionManager).GetMethod(
                "ConnectAsync",
                new[]
                {
                    typeof(string),
                    typeof(CancellationToken)
                });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.True(method.IsAbstract);
    }

    [Fact]
    public void ConnectionRequiresCancellableAsyncDisconnect()
    {
        MethodInfo? method =
            typeof(IProviderConnectionManager).GetMethod(
                "DisconnectAsync",
                new[]
                {
                    typeof(string),
                    typeof(CancellationToken)
                });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        Assert.True(method.IsAbstract);
    }

    [Fact]
    public void ConnectionRequiresProviderStateLookup()
    {
        MethodInfo? method =
            typeof(IProviderConnectionManager).GetMethod(
                "GetState",
                new[] { typeof(string) });

        Assert.NotNull(method);
        Assert.Equal(
            "TruthDoctor.Services.Standalone.ProviderConnectionState",
            method.ReturnType.FullName);
        Assert.True(method.IsAbstract);
    }

    [Fact]
    public void ConnectionStatesMustRemainDistinct()
    {
        Type? type = typeof(IProviderConnectionManager)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.ProviderConnectionState");

        Assert.NotNull(type);
        Assert.True(type.IsEnum);

        string[] requiredStates =
        {
            "Available",
            "Installed",
            "Configured",
            "Authenticated",
            "Connected",
            "Expired",
            "Failed"
        };

        string[] actualStates = Enum.GetNames(type);

        foreach (string state in requiredStates)
        {
            Assert.Contains(state, actualStates);
        }
    }
}
