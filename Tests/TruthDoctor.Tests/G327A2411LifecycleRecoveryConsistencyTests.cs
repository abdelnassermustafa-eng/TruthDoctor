using System;
using System.Collections.Generic;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A2411LifecycleRecoveryConsistencyTests
{
    [Fact]
    public void ReRegisteredConnectionMustNotInheritPreviousState()
    {
        var catalog = new ProviderConnectionCatalog();

        var identity = new ProviderConnectionIdentity(
            "aws-primary", "aws");

        catalog.Register(identity);

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        lifecycle.Transition(
            identity.ConnectionId,
            ProviderConnectionState.Configured);

        Assert.Equal(
            ProviderConnectionState.Configured,
            lifecycle.GetState(identity.ConnectionId));

        Assert.True(catalog.Unregister(identity.ConnectionId));

        // Deliberately reuse the exact same identity object.
        // Equality or reference checks alone cannot detect
        // that this is a new registration.
        catalog.Register(identity);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState(identity.ConnectionId));
    }

    [Fact]
    public void FailedConnectionMayReturnToDisconnected()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Configured);

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Authenticating);

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Failed);

        Assert.Equal(
            ProviderConnectionState.Failed,
            lifecycle.GetState("aws-primary"));

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Disconnected);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));
    }

    [Fact]
    public void ExpiredConnectionMayDisconnectAndReconfigure()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        ProviderConnectionState[] initialSequence =
        {
            ProviderConnectionState.Configured,
            ProviderConnectionState.Authenticating,
            ProviderConnectionState.Authenticated,
            ProviderConnectionState.Connecting,
            ProviderConnectionState.Connected
        };

        foreach (var state in initialSequence)
        {
            lifecycle.Transition("aws-primary", state);
        }

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Expired);

        Assert.Equal(
            ProviderConnectionState.Expired,
            lifecycle.GetState("aws-primary"));

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Disconnected);

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Configured);

        Assert.Equal(
            ProviderConnectionState.Configured,
            lifecycle.GetState("aws-primary"));
    }

    [Fact]
    public void UnregisteredConnectionRemainsInaccessible()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));

        Assert.True(catalog.Unregister("aws-primary"));

        Assert.Throws<KeyNotFoundException>(
            () => lifecycle.GetState("aws-primary"));

        Assert.Throws<KeyNotFoundException>(
            () => lifecycle.Transition(
                "aws-primary",
                ProviderConnectionState.Configured));
    }

    [Fact]
    public void FailureOfOneConnectionPreservesOtherConnectionState()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        catalog.Register(new ProviderConnectionIdentity(
            "aws-secondary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Configured);

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Authenticating);

        lifecycle.Transition(
            "aws-primary", ProviderConnectionState.Failed);

        lifecycle.Transition(
            "aws-secondary", ProviderConnectionState.Configured);

        Assert.Equal(
            ProviderConnectionState.Failed,
            lifecycle.GetState("aws-primary"));

        Assert.Equal(
            ProviderConnectionState.Configured,
            lifecycle.GetState("aws-secondary"));
    }
}
