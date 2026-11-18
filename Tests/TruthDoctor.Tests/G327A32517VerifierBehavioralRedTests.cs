using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A32517VerifierBehavioralRedTests
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

    [Fact]
    public void RegisteredMockVerifierIsResolvableWithoutInvocation()
    {
        var runtime = Runtime();
        var coordinator = Coordinator(runtime, new MockAdapter());
        var verifier = new MockRecoveryVerifier();

        coordinator.RegisterTrustedRecoveryVerifier(verifier);

        Assert.Same(
            verifier,
            coordinator.ResolveTrustedRecoveryVerifier("aws"));

        Assert.Equal(0, verifier.Calls);
    }

    [Fact]
    public async Task UnverifiedProofCannotReleaseCleanup()
    {
        var runtime = Runtime();

        int authenticationCalls = 0;

        var adapter = new MockAdapter
        {
            Authenticate = (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref authenticationCalls);
                return Task.CompletedTask;
            },
            Disconnect = (_, _) => Task.FromException(
                new IOException(
                    "Controlled unresolved provider cleanup."))
        };

        var coordinator = Coordinator(runtime, adapter);

        await coordinator.ConnectAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => coordinator.RecoverAsync(
                    ConnectionId, CancellationToken.None));

        Assert.Equal(
            "Trusted provider recovery is not configured.",
            exception.Message);

        bool reconnectRejected = false;

        try
        {
            await coordinator.ConnectAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout);
        }
        catch (InvalidOperationException)
        {
            reconnectRejected = true;
        }

        Assert.True(
            reconnectRejected,
            "VR-01: Unresolved provider cleanup must block reconnection.");

        Assert.Equal(
            1, Volatile.Read(ref authenticationCalls));
    }

    [Fact]
    public async Task CorrectSessionVerifiedSafeAllowsReconnect()
    {
        var runtime = Runtime();

        var adapter = new MockAdapter
        {
            Disconnect = (_, _) => Task.FromException(
                new IOException(
                    "Controlled unresolved cleanup for verifier test."))
        };

        var coordinator = Coordinator(runtime, adapter);
        var verifier = new MockRecoveryVerifier();

        coordinator.RegisterTrustedRecoveryVerifier(verifier);

        Assert.Same(
            verifier,
            coordinator.ResolveTrustedRecoveryVerifier("aws"));

        await coordinator.ConnectAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        await Assert.ThrowsAsync<IOException>(
            () => coordinator.DisconnectAsync(
                ConnectionId, CancellationToken.None));

        bool recovered = false;

        try
        {
            await coordinator.RecoverAsync(
                ConnectionId, CancellationToken.None).WaitAsync(Timeout);
            recovered = true;
        }
        catch (InvalidOperationException)
        {
            // The RED implementation cannot yet release ownership.
        }

        Assert.Equal(1, verifier.Calls);

        Assert.True(
            recovered,
            "VR-02: Verified-safe original session must complete recovery.");

        await coordinator.ConnectAsync(
            ConnectionId, CancellationToken.None).WaitAsync(Timeout);

        Assert.Equal(
            ProviderConnectionState.Connected,
            runtime.ConnectionLifecycle.GetState(ConnectionId));
    }
}
