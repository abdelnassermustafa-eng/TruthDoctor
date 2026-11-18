using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A2414RegistrationConcurrencyTests
{
    private static MethodInfo RequiredRegistrationScopeMethod()
    {
        MethodInfo? method = typeof(ProviderConnectionCatalog).GetMethod(
            "ExecuteForRegistration",
            new[] { typeof(string), typeof(Action<long>) });

        // RED contract: registration-scoped execution is not implemented yet.
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method.ReturnType);

        return method;
    }

    private static void ExecuteScoped(
        ProviderConnectionCatalog catalog,
        string connectionId,
        Action<long> operation)
    {
        MethodInfo method = RequiredRegistrationScopeMethod();

        method.Invoke(
            catalog,
            new object[] { connectionId, operation });
    }

    [Fact]
    public void ScopedOperationReceivesCurrentRegistrationGeneration()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        long expected = catalog.GetRegistrationGeneration("aws-primary");
        long observed = -1;
        int invoked = 0;

        ExecuteScoped(
            catalog,
            "aws-primary",
            generation =>
            {
                observed = generation;
                invoked++;
            });

        Assert.Equal(1, invoked);
        Assert.Equal(expected, observed);
    }

    [Fact]
    public void ScopedOperationHoldsCatalogRegistrationLock()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        FieldInfo? gateField = typeof(ProviderConnectionCatalog)
            .GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(gateField);

        object? gate = gateField.GetValue(catalog);
        Assert.NotNull(gate);

        // Resolve the new contract before starting any worker.
        MethodInfo method = RequiredRegistrationScopeMethod();

        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        Task worker = Task.Run(() =>
        {
            method.Invoke(
                catalog,
                new object[]
                {
                    "aws-primary",
                    (Action<long>)(_ =>
                    {
                        entered.Set();

                        if (!release.Wait(TimeSpan.FromSeconds(10)))
                        {
                            throw new TimeoutException(
                                "Registration-scope test release was not signaled.");
                        }
                    })
                });
        });

        try
        {
            Assert.True(
                entered.Wait(TimeSpan.FromSeconds(10)),
                "Registration-scoped callback did not begin.");

            // The callback is confirmed to be active in another thread.
            // Nonblocking Monitor.TryEnter checks the actual lock state,
            // not timing or whether a competing task happened to run.
            bool enteredGate = Monitor.TryEnter(gate);

            if (enteredGate)
            {
                Monitor.Exit(gate);
            }

            Assert.False(
                enteredGate,
                "Catalog lock was not held during scoped execution.");
        }
        finally
        {
            release.Set();
            Assert.True(
                worker.Wait(TimeSpan.FromSeconds(10)),
                "Registration-scope worker did not finish.");
        }
    }

    [Fact]
    public void ScopedOperationRejectsUnknownRegistration()
    {
        var catalog = new ProviderConnectionCatalog();
        int invoked = 0;

        MethodInfo method = RequiredRegistrationScopeMethod();

        var error = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(
                catalog,
                new object[]
                {
                    "missing",
                    (Action<long>)(_ => invoked++)
                }));

        Assert.IsType<KeyNotFoundException>(error.InnerException);
        Assert.Equal(0, invoked);
    }

    [Fact]
    public void ExistingConnectionStatesRemainIndependent()
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

    [Fact]
    public void InvalidDirectConnectionTransitionStillRejects()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        Assert.Throws<InvalidOperationException>(
            () => lifecycle.Transition(
                "aws-primary",
                ProviderConnectionState.Connected));

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));
    }
}
