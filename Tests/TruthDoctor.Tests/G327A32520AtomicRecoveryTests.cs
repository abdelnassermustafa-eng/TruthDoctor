using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A32520AtomicRecoveryTests
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
    public async Task ActiveCleanupCannotBeVerifiedAway()
    {
        var runtime = Runtime();
        var entered = Signal();
        var release = Signal();

        var adapter = new MockAdapter
        {
            Disconnect = async (_, token) =>
            {
                entered.TrySetResult(true);
                await release.Task.WaitAsync(Timeout);
                token.ThrowIfCancellationRequested();

                throw new IOException(
                    "Controlled cleanup failure after active callback.");
            }
        };

        var coordinator = Coordinator(runtime, adapter);

        int calls = 0;
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

        Task cleanup = coordinator.DisconnectAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => coordinator.RecoverAsync(
                    ConnectionId, CancellationToken.None).WaitAsync(Timeout));

            Assert.True(
                Volatile.Read(ref calls) == 0,
                "VR-ACTIVE: Running provider cleanup must not invoke recovery verification.");
        }
        finally
        {
            release.TrySetResult(true);
        }

        await Assert.ThrowsAsync<IOException>(
            async () => await cleanup.WaitAsync(Timeout));

        await coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(1, Volatile.Read(ref calls));
        await Connect(coordinator);
    }

    [Fact]
    public async Task StaleProofCannotReleaseReplacement()
    {
        var runtime = Runtime();
        var original = new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Original provider cleanup failed."))
        };
        var replacement = new MockAdapter
        {
            ProviderId = "azure"
        };

        var coordinator = Coordinator(
            runtime, original, replacement);

        var verifierEntered = Signal();
        var releaseVerifier = Signal();

        RecoverySessionIdentity observed = default;

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = async (session, token) =>
                {
                    observed = session;
                    verifierEntered.TrySetResult(true);
                    await releaseVerifier.Task.WaitAsync(Timeout);
                    token.ThrowIfCancellationRequested();
                    return RecoveryVerificationVerdict.VerifiedSafe;
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        long originalGeneration =
            runtime.ConnectionCatalog.GetRegistrationGeneration(
                ConnectionId);

        Task recovery = coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await verifierEntered.Task.WaitAsync(Timeout);

            Assert.Equal(ConnectionId, observed.ConnectionId);
            Assert.Equal("aws", observed.ProviderId);
            Assert.Equal(
                originalGeneration,
                observed.RegistrationGeneration);
            Assert.NotEqual(Guid.Empty, observed.CleanupOwnershipId);

            Assert.True(
                runtime.ConnectionCatalog.Unregister(ConnectionId));

            runtime.ConnectionCatalog.Register(
                new ProviderConnectionIdentity(
                    ConnectionId, "azure"));

            Assert.NotEqual(
                originalGeneration,
                runtime.ConnectionCatalog.GetRegistrationGeneration(
                    ConnectionId));
        }
        finally
        {
            releaseVerifier.TrySetResult(true);
        }

        await Assert.ThrowsAnyAsync<Exception>(
            async () => await recovery.WaitAsync(Timeout));

        bool blocked = false;
        try
        {
            await Connect(coordinator);
        }
        catch (InvalidOperationException)
        {
            blocked = true;
        }

        Assert.True(
            blocked,
            "VR-03: Stale proof must not authorize replacement reconnection.");
    }

    [Fact]
    public async Task RejectedOrInconclusiveProofCannotRelease()
    {
        foreach (var verdict in new[]
        {
            RecoveryVerificationVerdict.Rejected,
            RecoveryVerificationVerdict.Inconclusive
        })
        {
            var runtime = Runtime();

            var coordinator = Coordinator(runtime, new MockAdapter
            {
                Disconnect = (_, _) => Task.FromException(
                    new IOException("Cleanup remains unresolved."))
            });

            coordinator.RegisterTrustedRecoveryVerifier(
                new ControlledVerifier
                {
                    Verify = (_, _) => Task.FromResult(verdict)
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
    }

    [Fact]
    public async Task VerifierFailureCannotRelease()
    {
        var runtime = Runtime();
        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Cleanup failed."))
        });

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = (_, _) =>
                    Task.FromException<RecoveryVerificationVerdict>(
                        new IOException("Verifier failed."))
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
    }

    [Fact]
    public async Task CancellationCannotRelease()
    {
        var runtime = Runtime();
        var entered = Signal();
        var release = Signal();

        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Cleanup failed."))
        });

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = async (_, _) =>
                {
                    entered.TrySetResult(true);
                    await release.Task.WaitAsync(Timeout);
                    return RecoveryVerificationVerdict.VerifiedSafe;
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        using var cancellation = new CancellationTokenSource();

        Task recovery = coordinator.RecoverAsync(
            ConnectionId, cancellation.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout);
            cancellation.Cancel();
        }
        finally
        {
            release.TrySetResult(true);
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await recovery.WaitAsync(Timeout));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Connect(coordinator));
    }

    [Fact]
    public async Task ConcurrentRecoveryCannotDoubleRelease()
    {
        var runtime = Runtime();
        var entered = Signal();
        var release = Signal();

        int calls = 0;

        var coordinator = Coordinator(runtime, new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException("Cleanup failed."))
        });

        coordinator.RegisterTrustedRecoveryVerifier(
            new ControlledVerifier
            {
                Verify = async (_, _) =>
                {
                    if (Interlocked.Increment(ref calls) == 2)
                    {
                        entered.TrySetResult(true);
                    }

                    await release.Task.WaitAsync(Timeout);
                    return RecoveryVerificationVerdict.VerifiedSafe;
                }
            });

        await Connect(coordinator);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout));

        Task first = coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None);

        Task second = coordinator.RecoverAsync(
            ConnectionId, CancellationToken.None);

        try
        {
            await entered.Task.WaitAsync(Timeout);
        }
        finally
        {
            release.TrySetResult(true);
        }

        Exception? firstError = await Record.ExceptionAsync(
            async () => await first.WaitAsync(Timeout));

        Exception? secondError = await Record.ExceptionAsync(
            async () => await second.WaitAsync(Timeout));

        Assert.Equal(
            1,
            (firstError is null ? 1 : 0)
                + (secondError is null ? 1 : 0));

        Assert.True(
            firstError is null
                ? secondError is InvalidOperationException
                : firstError is InvalidOperationException,
            "VR-CONCURRENT: Only one verification may release the original ownership.");

        await Connect(coordinator);
    }
}
