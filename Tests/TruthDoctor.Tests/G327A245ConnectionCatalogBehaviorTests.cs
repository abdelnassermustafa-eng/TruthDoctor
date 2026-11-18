using System;
using System.Reflection;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A245ConnectionCatalogBehaviorTests
{
    private static object NewCatalog()
    {
        Type? type = typeof(ProviderConnectionIdentity)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.ProviderConnectionCatalog");

        Assert.NotNull(type);

        object? instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        return instance;
    }

    private static void Register(
        object catalog,
        ProviderConnectionIdentity identity)
    {
        MethodInfo? method = catalog.GetType().GetMethod(
            "Register",
            new[] { typeof(ProviderConnectionIdentity) });

        Assert.NotNull(method);

        method.Invoke(catalog, new object[] { identity });
    }

    private static ProviderConnectionIdentity Get(
        object catalog,
        string connectionId)
    {
        MethodInfo? method = catalog.GetType().GetMethod(
            "Get",
            new[] { typeof(string) });

        Assert.NotNull(method);

        object? result = method.Invoke(
            catalog,
            new object[] { connectionId });

        return Assert.IsType<ProviderConnectionIdentity>(result);
    }

    [Fact]
    public void RegisteredConnectionCanBeRetrievedByInstanceId()
    {
        object catalog = NewCatalog();
        var identity = new ProviderConnectionIdentity(
            "aws-primary",
            "aws");

        Register(catalog, identity);

        Assert.Equal(identity, Get(catalog, "aws-primary"));
    }

    [Fact]
    public void DuplicateConnectionIdMustBeRejected()
    {
        object catalog = NewCatalog();

        var first = new ProviderConnectionIdentity(
            "aws-primary", "aws");
        var duplicate = new ProviderConnectionIdentity(
            "aws-primary", "other-provider");

        Register(catalog, first);

        var thrown = Assert.Throws<TargetInvocationException>(
            () => Register(catalog, duplicate));

        Assert.IsAssignableFrom<ArgumentException>(
            thrown.InnerException);

        Assert.Equal(first, Get(catalog, "aws-primary"));
    }

    [Fact]
    public void MultipleConnectionsMayShareSameProvider()
    {
        object catalog = NewCatalog();

        var first = new ProviderConnectionIdentity(
            "aws-primary", "aws");
        var second = new ProviderConnectionIdentity(
            "aws-secondary", "aws");

        Register(catalog, first);
        Register(catalog, second);

        Assert.Equal(first, Get(catalog, first.ConnectionId));
        Assert.Equal(second, Get(catalog, second.ConnectionId));
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void RejectedDuplicateMustNotCorruptOtherConnections()
    {
        object catalog = NewCatalog();

        var first = new ProviderConnectionIdentity(
            "aws-primary", "aws");
        var second = new ProviderConnectionIdentity(
            "aws-secondary", "aws");

        Register(catalog, first);
        Register(catalog, second);

        var duplicate = new ProviderConnectionIdentity(
            first.ConnectionId, "another-provider");

        var thrown = Assert.Throws<TargetInvocationException>(
            () => Register(catalog, duplicate));

        Assert.IsAssignableFrom<ArgumentException>(
            thrown.InnerException);

        Assert.Equal(first, Get(catalog, first.ConnectionId));
        Assert.Equal(second, Get(catalog, second.ConnectionId));
    }
}
