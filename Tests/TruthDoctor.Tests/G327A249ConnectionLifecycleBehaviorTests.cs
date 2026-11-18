using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A249ConnectionLifecycleBehaviorTests
{
    private static object NewLifecycle(
        ProviderConnectionCatalog catalog)
    {
        Type? type = typeof(ProviderConnectionCatalog)
            .Assembly.GetType(
                "TruthDoctor.Services.Standalone.ProviderConnectionLifecycle");

        Assert.NotNull(type);

        object? instance = Activator.CreateInstance(type, catalog);

        Assert.NotNull(instance);

        return instance;
    }

    private static ProviderConnectionState GetState(
        object lifecycle,
        string connectionId)
    {
        MethodInfo? method = lifecycle.GetType().GetMethod(
            "GetState", new[] { typeof(string) });

        Assert.NotNull(method);

        object? value = method.Invoke(
            lifecycle, new object[] { connectionId });

        return Assert.IsType<ProviderConnectionState>(value);
    }

    private static void Transition(
        object lifecycle,
        string connectionId,
        ProviderConnectionState nextState)
    {
        MethodInfo? method = lifecycle.GetType().GetMethod(
            "Transition",
            new[]
            {
                typeof(string),
                typeof(ProviderConnectionState)
            });

        Assert.NotNull(method);

        method.Invoke(
            lifecycle,
            new object[] { connectionId, nextState });
    }

    [Fact]
    public void RegisteredConnectionStartsDisconnected()
    {
        var catalog = new ProviderConnectionCatalog();
        catalog.Register(
            new ProviderConnectionIdentity("aws-one", "aws"));

        object lifecycle = NewLifecycle(catalog);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            GetState(lifecycle, "aws-one"));
    }

    [Fact]
    public void ValidLifecycleTransitionsAreAccepted()
    {
        var catalog = new ProviderConnectionCatalog();
        catalog.Register(
            new ProviderConnectionIdentity("aws-one", "aws"));

        object lifecycle = NewLifecycle(catalog);

        ProviderConnectionState[] steps =
        {
            ProviderConnectionState.Configured,
            ProviderConnectionState.Authenticating,
            ProviderConnectionState.Authenticated,
            ProviderConnectionState.Connecting,
            ProviderConnectionState.Connected,
            ProviderConnectionState.Disconnected
        };

        foreach (ProviderConnectionState state in steps)
        {
            Transition(lifecycle, "aws-one", state);
            Assert.Equal(state, GetState(lifecycle, "aws-one"));
        }
    }

    [Fact]
    public void UnknownConnectionIsRejected()
    {
        var catalog = new ProviderConnectionCatalog();
        object lifecycle = NewLifecycle(catalog);

        var getError = Assert.Throws<TargetInvocationException>(
            () => GetState(lifecycle, "unknown"));

        Assert.IsType<KeyNotFoundException>(
            getError.InnerException);

        var changeError = Assert.Throws<TargetInvocationException>(
            () => Transition(
                lifecycle,
                "unknown",
                ProviderConnectionState.Configured));

        Assert.IsType<KeyNotFoundException>(
            changeError.InnerException);
    }

    [Fact]
    public void InvalidDirectConnectionTransitionIsRejected()
    {
        var catalog = new ProviderConnectionCatalog();
        catalog.Register(
            new ProviderConnectionIdentity("aws-one", "aws"));

        object lifecycle = NewLifecycle(catalog);

        var error = Assert.Throws<TargetInvocationException>(
            () => Transition(
                lifecycle,
                "aws-one",
                ProviderConnectionState.Connected));

        Assert.IsType<InvalidOperationException>(
            error.InnerException);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            GetState(lifecycle, "aws-one"));
    }

    [Fact]
    public void FailedConnectionDoesNotChangeAnotherConnection()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(
            new ProviderConnectionIdentity("aws-one", "aws"));

        catalog.Register(
            new ProviderConnectionIdentity("aws-two", "aws"));

        object lifecycle = NewLifecycle(catalog);

        Transition(
            lifecycle, "aws-one",
            ProviderConnectionState.Configured);

        Transition(
            lifecycle, "aws-one",
            ProviderConnectionState.Authenticating);

        Transition(
            lifecycle, "aws-one",
            ProviderConnectionState.Failed);

        Transition(
            lifecycle, "aws-two",
            ProviderConnectionState.Configured);

        Assert.Equal(
            ProviderConnectionState.Failed,
            GetState(lifecycle, "aws-one"));

        Assert.Equal(
            ProviderConnectionState.Configured,
            GetState(lifecycle, "aws-two"));
    }
}
