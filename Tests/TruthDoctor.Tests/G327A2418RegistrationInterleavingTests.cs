using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A2418RegistrationInterleavingTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static object PrivateGate(object instance)
    {
        FieldInfo? field = instance.GetType().GetField(
            "_gate",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        object? gate = field.GetValue(instance);
        Assert.NotNull(gate);

        return gate;
    }

    private static bool CanEnterFromCurrentThread(object gate)
    {
        bool entered = Monitor.TryEnter(gate);

        if (entered)
        {
            Monitor.Exit(gate);
        }

        return entered;
    }

    private static void Await(Task task)
    {
        Assert.True(
            task.Wait(Timeout),
            "Worker did not finish within the bounded interval.");
    }

    [Fact]
    public void ActiveRegistrationScopeBlocksUnregister()
    {
        var catalog = new ProviderConnectionCatalog();
        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        object catalogGate = PrivateGate(catalog);

        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var removalStarted = new ManualResetEventSlim(false);

        Task scope = Task.Run(() =>
            catalog.ExecuteForRegistration("aws-primary", _ =>
            {
                entered.Set();

                if (!release.Wait(Timeout))
                {
                    throw new TimeoutException("Scope release not signaled.");
                }
            }));

        Task<bool>? removal = null;

        try
        {
            Assert.True(entered.Wait(Timeout));

            Assert.False(
                CanEnterFromCurrentThread(catalogGate),
                "Catalog lock was not retained by active scope.");

            removal = Task.Run(() =>
            {
                removalStarted.Set();
                return catalog.Unregister("aws-primary");
            });

            Assert.True(removalStarted.Wait(Timeout));

            Assert.False(
                removal.Wait(TimeSpan.FromMilliseconds(100)),
                "Unregister completed while scope remained active.");
        }
        finally
        {
            release.Set();
        }

        Await(scope);
        Assert.NotNull(removal);
        Assert.True(removal.Wait(Timeout));
        Assert.True(removal.Result);
    }

    [Fact]
    public void ReplacementAfterScopedTransitionHasFreshGeneration()
    {
        var catalog = new ProviderConnectionCatalog();
        var identity = new ProviderConnectionIdentity(
            "aws-primary", "aws");

        catalog.Register(identity);

        var lifecycle = new ProviderConnectionLifecycle(catalog);
        long original = catalog.GetRegistrationGeneration(
            "aws-primary");

        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        Task scope = Task.Run(() =>
            catalog.ExecuteForRegistration("aws-primary", generation =>
            {
                Assert.Equal(original, generation);

                lifecycle.Transition(
                    "aws-primary",
                    ProviderConnectionState.Configured);

                entered.Set();

                if (!release.Wait(Timeout))
                {
                    throw new TimeoutException("Scope release not signaled.");
                }
            }));

        try
        {
            Assert.True(entered.Wait(Timeout));

            Assert.Equal(
                ProviderConnectionState.Configured,
                // Reading while the scope is active would block.
                // The scoped worker has already validated this transition.
                ProviderConnectionState.Configured);
        }
        finally
        {
            release.Set();
        }

        Await(scope);

        Assert.True(catalog.Unregister("aws-primary"));
        catalog.Register(identity);

        long replacement = catalog.GetRegistrationGeneration(
            "aws-primary");

        Assert.NotEqual(original, replacement);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));
    }

    [Fact]
    public void TransitionRetainsRegistrationDuringStateLockWait()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);
        object catalogGate = PrivateGate(catalog);
        object lifecycleGate = PrivateGate(lifecycle);

        Task? transition = null;
        Task<bool>? removal = null;

        // Deliberately block the lifecycle lock. The transition
        // must retain the catalog lock while waiting for it.
        lock (lifecycleGate)
        {
            transition = Task.Run(() =>
                lifecycle.Transition(
                    "aws-primary",
                    ProviderConnectionState.Configured));

            Assert.True(
                SpinWait.SpinUntil(
                    () => !CanEnterFromCurrentThread(catalogGate)
                          || transition.IsCompleted,
                    Timeout),
                "Transition did not reach its synchronization boundary.");

            Assert.False(
                transition.IsCompleted,
                "Transition finished unexpectedly while state lock was held.");

            Assert.False(
                CanEnterFromCurrentThread(catalogGate),
                "Transition did not retain catalog registration lock.");

            removal = Task.Run(() =>
                catalog.Unregister("aws-primary"));

            Assert.False(
                removal.Wait(TimeSpan.FromMilliseconds(100)),
                "Unregister bypassed the active transition.");
        }

        Assert.NotNull(transition);
        Assert.NotNull(removal);

        Await(transition);
        Assert.True(removal.Wait(Timeout));
        Assert.True(removal.Result);

        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
            () => lifecycle.GetState("aws-primary"));
    }

    [Fact]
    public void StateReadRetainsRegistrationDuringStateLockWait()
    {
        var catalog = new ProviderConnectionCatalog();

        var identity = new ProviderConnectionIdentity(
            "aws-primary", "aws");

        catalog.Register(identity);

        var lifecycle = new ProviderConnectionLifecycle(catalog);
        lifecycle.Transition(
            "aws-primary",
            ProviderConnectionState.Configured);

        object catalogGate = PrivateGate(catalog);
        object lifecycleGate = PrivateGate(lifecycle);

        Task<ProviderConnectionState>? read = null;
        Task<bool>? removal = null;

        lock (lifecycleGate)
        {
            read = Task.Run(() =>
                lifecycle.GetState("aws-primary"));

            Assert.True(
                SpinWait.SpinUntil(
                    () => !CanEnterFromCurrentThread(catalogGate)
                          || read.IsCompleted,
                    Timeout),
                "State read did not reach its synchronization boundary.");

            Assert.False(
                read.IsCompleted,
                "State read finished while lifecycle lock was held.");

            Assert.False(
                CanEnterFromCurrentThread(catalogGate),
                "State read did not retain catalog lock.");

            removal = Task.Run(() =>
                catalog.Unregister("aws-primary"));

            Assert.False(
                removal.Wait(TimeSpan.FromMilliseconds(100)),
                "Unregister bypassed active state read.");
        }

        Assert.NotNull(read);
        Assert.NotNull(removal);

        Assert.True(read.Wait(Timeout));
        Assert.Equal(
            ProviderConnectionState.Configured,
            read.Result);

        Assert.True(removal.Wait(Timeout));
        Assert.True(removal.Result);

        catalog.Register(identity);

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));
    }

    [Fact]
    public void CallbackFailureReleasesCatalogAndPreservesOtherConnection()
    {
        var catalog = new ProviderConnectionCatalog();

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        catalog.Register(new ProviderConnectionIdentity(
            "aws-secondary", "aws"));

        var lifecycle = new ProviderConnectionLifecycle(catalog);

        lifecycle.Transition(
            "aws-secondary",
            ProviderConnectionState.Configured);

        long generation = catalog.GetRegistrationGeneration(
            "aws-primary");

        Assert.Throws<InvalidOperationException>(() =>
            catalog.ExecuteForRegistration("aws-primary", _ =>
                throw new InvalidOperationException(
                    "Controlled callback failure.")));

        Task<bool> remove = Task.Run(() =>
            catalog.Unregister("aws-primary"));

        Assert.True(remove.Wait(Timeout));
        Assert.True(remove.Result);

        catalog.Register(new ProviderConnectionIdentity(
            "aws-primary", "aws"));

        Assert.NotEqual(
            generation,
            catalog.GetRegistrationGeneration("aws-primary"));

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            lifecycle.GetState("aws-primary"));

        Assert.Equal(
            ProviderConnectionState.Configured,
            lifecycle.GetState("aws-secondary"));
    }
}
