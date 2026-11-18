using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A32521RecoveryHardeningTests
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

    private sealed class MockRecoveryVerifier : IProviderRecoveryVerifier
    {
        private int _calls;

        public string ProviderId => "aws";

        public int Calls => Volatile.Read(ref _calls);

        public Task<RecoveryVerificationVerdict> VerifyAsync(
            RecoverySessionIdentity session,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _calls);

            return Task.FromResult(
                RecoveryVerificationVerdict.VerifiedSafe);
        }
    }

    private sealed class ControlledVerifier : IProviderRecoveryVerifier
    {
        public string ProviderId => "aws";

        public Func<RecoverySessionIdentity, CancellationToken,
            Task<RecoveryVerificationVerdict>> Verify
            { get; init; } = (_, _) => Task.FromResult(
                RecoveryVerificationVerdict.VerifiedSafe);

        public Task<RecoveryVerificationVerdict> VerifyAsync(
            RecoverySessionIdentity session,
            CancellationToken cancellationToken) =>
            Verify(session, cancellationToken);
    }

    [Fact]
    public async Task CompletedRecoveryCannotBeReplayed()
    {
        var runtime = Runtime();
        int calls = 0;

        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Controlled cleanup failure."))
        });

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = (_, _) =>
                {
                    Interlocked.Increment(ref calls);
                    return Task.FromResult(
                        RecoveryVerificationVerdict.VerifiedSafe);
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        await coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(1, Volatile.Read(ref calls));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RecoverAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        Assert.Equal(
            1,
            Volatile.Read(ref calls));

        await Connect(coordinator);
    }

    [Fact]
    public async Task NewCleanupCycleRequiresFreshVerification()
    {
        var runtime = Runtime();
        int calls = 0;

        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Each cleanup requires recovery."))
        });

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = (_, _) =>
                {
                    Interlocked.Increment(ref calls);

                    return Task.FromResult(
                        RecoveryVerificationVerdict.VerifiedSafe);
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        await coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(1, Volatile.Read(ref calls));

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Connect(coordinator));

        Assert.Equal(1, Volatile.Read(ref calls));

        await coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(2, Volatile.Read(ref calls));

        await Connect(coordinator);
    }

    [Fact]
    public async Task MissingTrustedVerifierCannotReleaseOwnership()
    {
        var runtime = Runtime();

        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Controlled unresolved cleanup."))
        });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RecoverAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Connect(coordinator));
    }

    [Fact]
    public async Task VerifierFailureThenFreshSafeVerdictCanRecover()
    {
        var runtime = Runtime();
        int calls = 0;

        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Controlled unresolved cleanup."))
        });

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = (_, _) =>
                {
                    if (Interlocked.Increment(ref calls) == 1)
                    {
                        return Task.FromException<RecoveryVerificationVerdict>(
                            new IOException("Transient verifier failure."));
                    }

                    return Task.FromResult(
                        RecoveryVerificationVerdict.VerifiedSafe);
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.RecoverAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Connect(coordinator));

        Assert.Equal(1, Volatile.Read(ref calls));

        await coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(2, Volatile.Read(ref calls));

        await Connect(coordinator);
    }

    [Fact]
    public async Task PreCanceledRecoveryDoesNotInvokeVerifier()
    {
        var runtime = Runtime();
        int calls = 0;

        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Controlled unresolved cleanup."))
        });

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = (_, _) =>
                {
                    Interlocked.Increment(ref calls);
                    return Task.FromResult(
                        RecoveryVerificationVerdict.VerifiedSafe);
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => coordinator.RecoverAsync(
                ConnectionId, cancellation.Token).WaitAsync(Timeout));

        Assert.Equal(0, Volatile.Read(ref calls));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Connect(coordinator));
    }

    [Fact]
    public async Task RecoveryWithoutCleanupDoesNotInvokeVerifier()
    {
        var runtime = Runtime();
        int calls = 0;

        var coordinator = Coordinator(runtime, new MockAdapter());

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = (_, _) =>
                {
                    Interlocked.Increment(ref calls);
                    return Task.FromResult(
                        RecoveryVerificationVerdict.VerifiedSafe);
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RecoverAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        Assert.Equal(0, Volatile.Read(ref calls));
    }
}
