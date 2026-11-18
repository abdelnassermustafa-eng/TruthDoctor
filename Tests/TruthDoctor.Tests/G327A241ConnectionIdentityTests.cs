using System;
using System.Reflection;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A241ConnectionIdentityTests
{
    [Fact]
    public void ConnectionIdentityMustDistinguishInstanceAndProvider()
    {
        Type? identity = typeof(IProviderConnectionManager)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.ProviderConnectionIdentity");

        Assert.NotNull(identity);

        PropertyInfo? connectionId =
            identity.GetProperty("ConnectionId");

        PropertyInfo? providerId =
            identity.GetProperty("ProviderId");

        Assert.NotNull(connectionId);
        Assert.NotNull(providerId);
        Assert.Equal(typeof(string), connectionId.PropertyType);
        Assert.Equal(typeof(string), providerId.PropertyType);
    }

    [Fact]
    public void ConnectMustUseConnectionInstanceIdentity()
    {
        MethodInfo? method =
            typeof(IProviderConnectionManager)
                .GetMethod("ConnectAsync");

        Assert.NotNull(method);

        ParameterInfo[] parameters = method.GetParameters();

        Assert.Equal("connectionId", parameters[0].Name);
    }

    [Fact]
    public void DisconnectMustUseConnectionInstanceIdentity()
    {
        MethodInfo? method =
            typeof(IProviderConnectionManager)
                .GetMethod("DisconnectAsync");

        Assert.NotNull(method);

        ParameterInfo[] parameters = method.GetParameters();

        Assert.Equal("connectionId", parameters[0].Name);
    }

    [Fact]
    public void GetStateMustUseConnectionInstanceIdentity()
    {
        MethodInfo? method =
            typeof(IProviderConnectionManager)
                .GetMethod("GetState");

        Assert.NotNull(method);

        ParameterInfo[] parameters = method.GetParameters();

        Assert.Equal("connectionId", parameters[0].Name);
    }
}
