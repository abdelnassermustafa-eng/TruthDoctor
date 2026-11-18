using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A3258FocusedRecoveryOwnershipTests
{
    private const string ConnectionId = "aws-primary";

    private static readonly TimeSpan Timeout =
        TimeSpan.FromSeconds(10);

    private static TaskCompletionSource<bool> Signal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class MockAdapter : IStandaloneProviderAdapter
    {
        public string ProviderId { get; init; } = "aws";

        public Func<string, CancellationToken, Task> Authenticate
            { get; init; } = (_, _) => Task.CompletedTask;

        public Func<string, CancellationToken, Task> Disconnect
            { get; init; } = (_, _) => Task.CompletedTask;

        public Task AuthenticateAsync(
            string connectionId,
            CancellationToken token) =>
            Authenticate(connectionId, token);

        public Task DisconnectAsync(
            string connectionId,
            CancellationToken token) =>
            Disconnect(connectionId, token);

        public Task<IReadOnlyList<ProviderResourceRecord>> DiscoverAsync(
            string connectionId,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return Task.FromResult<IReadOnlyList<ProviderResourceRecord>>(
                Array.Empty<ProviderResourceRecord>());
        }
    }

    private static StandaloneBackendRuntime Runtime()
    {
        var runtime = new StandaloneBackendRuntime();

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(ConnectionId, "aws"));

        return runtime;
    }

    private static StandaloneProviderAdapterCoordinator Coordinator(
        StandaloneBackendRuntime runtime,
        params IStandaloneProviderAdapter[] adapters)
    {
        var coordinator = new StandaloneProviderAdapterCoordinator(
            runtime.ConnectionCatalog,
            runtime.ConnectionLifecycle);

        foreach (var adapter in adapters)
        {
            coordinator.RegisterAdapter(adapter);
        }

        return coordinator;
    }

    private static Task Connect(
        StandaloneProviderAdapterCoordinator coordinator) =>
        coordinator.ConnectAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

    [Fact]
    public async Task CanceledCleanupRequiresResolutionBeforeReconnect()
    {
        var runtime = Runtime();
        var callbackEntered = Signal();
        var releaseCallback = Signal();

        int authenticationCalls = 0;

        var adapter = new MockAdapter
        {
            Authenticate = (_, token) =>
            {
                token.ThrowIfCancellationRequested();

                Interlocked.Increment(ref authenticationCalls);

                return Task.CompletedTask;
            },
            Disconnect = async (_, token) =>
            {
                callbackEntered.TrySetResult(true);

                await releaseCallback.Task;

                token.ThrowIfCancellationRequested();
            }
        };

        var coordinator = Coordinator(runtime, adapter);

        await Connect(coordinator);

        using var cancellation = new CancellationTokenSource();

        Task cleanup = coordinator.DisconnectAsync(
            ConnectionId, cancellation.Token);

        try
        {
            await callbackEntered.Task.WaitAsync(Timeout);

            cancellation.Cancel();
        }
        finally
        {
            releaseCallback.TrySetResult(true);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await cleanup.WaitAsync(Timeout));

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));

        bool reconnectRejected = false;

        try
        {
            await Connect(coordinator);
        }
        catch (InvalidOperationException)
        {
            reconnectRejected = true;
        }

        Assert.True(
            reconnectRejected,
            "RC-02: Canceled provider cleanup must require resolution");

        Assert.Equal(
            1, Volatile.Read(ref authenticationCalls));
    }

    [Fact]
    public async Task ReplacementCannotEraseUnresolvedCleanupFailure()
    {
        var runtime = Runtime();

        int originalAuthenticationCalls = 0;
        int replacementAuthenticationCalls = 0;

        var original = new MockAdapter
        {
            Authenticate = (_, token) =>
            {
                token.ThrowIfCancellationRequested();

                Interlocked.Increment(
                    ref originalAuthenticationCalls);

                return Task.CompletedTask;
            },
            Disconnect = (_, _) => Task.FromException(
                new IOException(
                    "Controlled unresolved cleanup for original session."))
        };

        var replacement = new MockAdapter
        {
            ProviderId = "azure",
            Authenticate = (_, token) =>
            {
                token.ThrowIfCancellationRequested();

                Interlocked.Increment(
                    ref replacementAuthenticationCalls);

                return Task.CompletedTask;
            }
        };

        var coordinator = Coordinator(
            runtime, original, replacement);

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            async () => await coordinator.DisconnectAsync(
                ConnectionId,
                CancellationToken.None).WaitAsync(Timeout));

        Assert.Equal(
            ProviderConnectionState.Disconnected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));

        long originalGeneration =
            runtime.ConnectionCatalog.GetRegistrationGeneration(
                ConnectionId);

        Assert.True(
            runtime.ConnectionCatalog.Unregister(ConnectionId));

        runtime.ConnectionCatalog.Register(
            new ProviderConnectionIdentity(
                ConnectionId, "azure"));

        long replacementGeneration =
            runtime.ConnectionCatalog.GetRegistrationGeneration(
                ConnectionId);

        Assert.NotEqual(
            originalGeneration, replacementGeneration);

        bool reconnectRejected = false;

        try
        {
            await Connect(coordinator);
        }
        catch (InvalidOperationException)
        {
            reconnectRejected = true;
        }

        Assert.True(
            reconnectRejected,
            "RC-03: Replacement registration must not erase unresolved cleanup");

        Assert.Equal(
            1, Volatile.Read(ref originalAuthenticationCalls));

        Assert.Equal(
            0, Volatile.Read(ref replacementAuthenticationCalls));
    }
}
