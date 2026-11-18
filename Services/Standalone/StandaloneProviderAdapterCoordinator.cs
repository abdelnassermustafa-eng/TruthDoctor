using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Coordinates provider-neutral authentication, disconnection, and
/// resource discovery through the existing connection lifecycle.
/// It does not acquire, store, or own credentials.
/// </summary>
public sealed class StandaloneProviderAdapterCoordinator
{
    private readonly object _adapterGate = new();

    // Separate from lifecycle state: a provider cleanup task may
    // remain active after logical disconnection has completed.
    private readonly object _cleanupGate = new();

    private readonly HashSet<string> _pendingProviderCleanup =
        new(StringComparer.Ordinal);

    // Owns the original provider session while cleanup is active
    // or unresolved. Protected by _cleanupGate.
    private readonly Dictionary<string, RecoverySessionIdentity>
        _cleanupOwnership = new(StringComparer.Ordinal);

    // Tracks cleanup callbacks that have not settled yet.
    // This differs from unresolved cleanup ownership.
    // Both structures are protected by _cleanupGate.
    private readonly HashSet<string> _activeProviderCleanup =
        new(StringComparer.Ordinal);

    private readonly ProviderConnectionCatalog _catalog;
    private readonly ProviderConnectionLifecycle _lifecycle;
    private readonly ProviderConnectionManager _manager;

    private readonly Dictionary<string, IStandaloneProviderAdapter>
        _adapters = new(StringComparer.Ordinal);

    // Internal composition seam for provider-specific recovery verifiers.
    // Registration does not authorize cleanup ownership release.
    private readonly Dictionary<string, IProviderRecoveryVerifier>
        _recoveryVerifiers = new(StringComparer.Ordinal);

    public StandaloneProviderAdapterCoordinator(
        ProviderConnectionCatalog catalog,
        ProviderConnectionLifecycle lifecycle)
    {
        _catalog = catalog ??
            throw new ArgumentNullException(nameof(catalog));

        _lifecycle = lifecycle ??
            throw new ArgumentNullException(nameof(lifecycle));

        _manager = new ProviderConnectionManager(
            _catalog,
            _lifecycle,
            AuthenticateAdapterAsync);
    }

    public void RegisterAdapter(IStandaloneProviderAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        if (string.IsNullOrWhiteSpace(adapter.ProviderId))
        {
            throw new ArgumentException(
                "The provider adapter must declare a provider ID.",
                nameof(adapter));
        }

        lock (_adapterGate)
        {
            if (!_adapters.TryAdd(adapter.ProviderId, adapter))
            {
                throw new InvalidOperationException(
                    "A provider adapter is already registered.");
            }
        }
    }

    public IStandaloneProviderAdapter ResolveAdapter(
        string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        lock (_adapterGate)
        {
            if (_adapters.TryGetValue(providerId, out var adapter))
            {
                return adapter;
            }
        }

        throw new KeyNotFoundException(
            "No standalone adapter is registered for provider: "
            + providerId);
    }

    private IStandaloneProviderAdapter ResolveForConnection(
        string connectionId)
    {
        // Resolve provider identity from the catalog itself.
        // Never infer the provider from an adapter fallback.
        string providerId = _catalog.Get(connectionId).ProviderId;
        return ResolveAdapter(providerId);
    }

    private Task AuthenticateAdapterAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IStandaloneProviderAdapter adapter =
            ResolveForConnection(connectionId);

        return adapter.AuthenticateAsync(
            connectionId, cancellationToken);
    }

    /// <summary>
    /// Fail-closed entry point for future trusted provider recovery.
    /// No cleanup ownership is released by this method.
    /// </summary>
    /// <summary>
    /// Registers a verifier for a previously registered provider.
    /// Restricted to the internal composition and test boundary.
    /// Registration does not execute or authorize recovery.
    /// </summary>
    internal void RegisterTrustedRecoveryVerifier(
        IProviderRecoveryVerifier verifier)
    {
        ArgumentNullException.ThrowIfNull(verifier);

        if (string.IsNullOrWhiteSpace(verifier.ProviderId))
        {
            throw new ArgumentException(
                "The recovery verifier must declare a provider ID.",
                nameof(verifier));
        }

        lock (_adapterGate)
        {
            if (!_adapters.ContainsKey(verifier.ProviderId))
            {
                throw new InvalidOperationException(
                    "A provider adapter must be registered before "
                    + "its recovery verifier.");
            }

            if (!_recoveryVerifiers.TryAdd(
                verifier.ProviderId, verifier))
            {
                throw new InvalidOperationException(
                    "A recovery verifier is already registered "
                    + "for this provider.");
            }
        }
    }

    /// <summary>
    /// Resolves only an internally registered verifier.
    /// This lookup does not perform provider verification.
    /// </summary>
    internal IProviderRecoveryVerifier ResolveTrustedRecoveryVerifier(
        string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        lock (_adapterGate)
        {
            if (_recoveryVerifiers.TryGetValue(
                providerId, out var verifier))
            {
                return verifier;
            }
        }

        throw new InvalidOperationException(
            "No trusted recovery verifier is registered "
            + "for this provider.");
    }

    public async Task RecoverAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException(
                "A connection ID is required.",
                nameof(connectionId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        RecoverySessionIdentity session;
        IProviderRecoveryVerifier verifier;

        // Snapshot the unresolved original session and resolve its
        // internally registered verifier under coordinator ordering.
        // Do not select a verifier from a replacement connection.
        lock (_cleanupGate)
        {
            if (!_cleanupOwnership.TryGetValue(
                connectionId, out session))
            {
                throw new InvalidOperationException(
                    "Trusted provider recovery is not configured.");
            }

            if (_activeProviderCleanup.Contains(connectionId))
            {
                throw new InvalidOperationException(
                    "Provider cleanup is still executing.");
            }

            try
            {
                verifier = ResolveTrustedRecoveryVerifier(
                    session.ProviderId);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "Trusted provider recovery is not configured.");
            }
        }

        // Never hold the cleanup or adapter lock across a provider
        // callback. The verifier receives the original session
        // identity, not the current replacement registration.
        RecoveryVerificationVerdict verdict =
            await verifier.VerifyAsync(
                session, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (verdict != RecoveryVerificationVerdict.VerifiedSafe)
        {
            throw new InvalidOperationException(
                "Provider recovery verification did not establish "
                + "a safe session.");
        }

        // The cleanup gate is the coordinator admission lock.
        // The catalog callback holds the registration lock.
        // No registration replacement can interleave with the
        // generation check and ownership release.
        lock (_cleanupGate)
        {
            if (!_cleanupOwnership.TryGetValue(
                    connectionId, out var current)
                || current != session)
            {
                throw new InvalidOperationException(
                    "Recovery ownership changed during verification.");
            }

            if (_activeProviderCleanup.Contains(connectionId))
            {
                throw new InvalidOperationException(
                    "Provider cleanup is still executing.");
            }

            if (!_pendingProviderCleanup.Contains(connectionId))
            {
                throw new InvalidOperationException(
                    "Recovery admission ownership is missing.");
            }

            _catalog.ExecuteForRegistration(
                connectionId,
                generation =>
                {
                    if (generation != session.RegistrationGeneration
                        || _catalog.Get(connectionId).ProviderId
                            != session.ProviderId)
                    {
                        throw new InvalidOperationException(
                            "The original provider registration has changed.");
                    }

                    // All identity checks and both removals execute
                    // under the cleanup and catalog locks.
                    _cleanupOwnership.Remove(connectionId);
                    _pendingProviderCleanup.Remove(connectionId);
                });
        }
    }

    public Task ConnectAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Make the pending-cleanup check and connection admission
        // atomic relative to new coordinator disconnect requests.
        lock (_cleanupGate)
        {
            if (_pendingProviderCleanup.Contains(connectionId))
            {
                throw new InvalidOperationException(
                    "Provider cleanup is still pending for this connection.");
            }

            // Preserve explicit registered-provider resolution.
            _ = ResolveForConnection(connectionId);

            // The existing manager remains the lifecycle authority.
            return _manager.ConnectAsync(
                connectionId, cancellationToken);
        }
    }

    public Task DisconnectAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cleanupGate)
        {
            // Resolve the adapter before acquiring the ownership
            // marker. Unknown providers cannot occupy the gate.
            IStandaloneProviderAdapter adapter =
                ResolveForConnection(connectionId);

            if (!_pendingProviderCleanup.Add(connectionId))
            {
                throw new InvalidOperationException(
                    "Provider cleanup is already pending for this connection.");
            }

            // Capture the exact original registration before
            // beginning any asynchronous cleanup callbacks.
            _cleanupOwnership.Add(
                connectionId,
                new RecoverySessionIdentity(
                    connectionId,
                    adapter.ProviderId,
                    _catalog.GetRegistrationGeneration(connectionId),
                    Guid.NewGuid()));

            _activeProviderCleanup.Add(connectionId);

            // Start the full disconnect operation while the gate
            // is held. Logical disconnect and admission are thus
            // ordered against ConnectAsync.
            return DisconnectWithProviderCleanupAsync(
                connectionId, adapter, cancellationToken);
        }
    }

    private async Task DisconnectWithProviderCleanupAsync(
        string connectionId,
        IStandaloneProviderAdapter adapter,
        CancellationToken cancellationToken)
    {
        bool providerCleanupCompleted = false;

        try
        {
            // The manager alone controls lifecycle transitions.
            await _manager.DisconnectAsync(
                connectionId, cancellationToken).ConfigureAwait(false);

            // The ownership marker remains active until this
            // provider callback task settles.
            await adapter.DisconnectAsync(
                connectionId, cancellationToken).ConfigureAwait(false);

            // Ownership may be released only after both cleanup
            // operations complete successfully.
            providerCleanupCompleted = true;
        }
        finally
        {
            lock (_cleanupGate)
            {
                _activeProviderCleanup.Remove(connectionId);

                if (providerCleanupCompleted)
                {
                    _pendingProviderCleanup.Remove(connectionId);
                    _cleanupOwnership.Remove(connectionId);
                }
            }

            // A failed or canceled cleanup is no longer active,
            // but its unresolved ownership and admission gate remain.
        }
    }

    public async Task<IReadOnlyList<ProviderResourceRecord>>
        DiscoverAsync(
            string connectionId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The connection ID may be reused following registration
        // removal. Capture its generation, not just its state.
        long registrationGeneration =
            _catalog.GetRegistrationGeneration(connectionId);

        IStandaloneProviderAdapter adapter =
            ResolveForConnection(connectionId);

        // Validate the starting session under catalog registration
        // coordination, including the selected provider.
        _catalog.ExecuteForRegistration(connectionId, generation =>
        {
            if (generation != registrationGeneration)
            {
                throw new InvalidOperationException(
                    "The registration changed before provider discovery.");
            }

            if (_catalog.Get(connectionId).ProviderId
                != adapter.ProviderId)
            {
                throw new InvalidOperationException(
                    "The provider changed before discovery.");
            }

            if (_lifecycle.GetState(connectionId)
                != ProviderConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    "Provider discovery requires a connected session.");
            }
        });

        IReadOnlyList<ProviderResourceRecord> resources =
            await adapter.DiscoverAsync(
                connectionId, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        // This is the discovery-result acceptance point. Verify
        // generation and state together under catalog coordination,
        // so a replacement cannot be mistaken for the original
        // connected registration.
        _catalog.ExecuteForRegistration(connectionId, generation =>
        {
            if (generation != registrationGeneration)
            {
                throw new InvalidOperationException(
                    "The registration changed during provider discovery.");
            }

            if (_lifecycle.GetState(connectionId)
                != ProviderConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    "The connection ended during provider discovery.");
            }
        });

        return resources ?? throw new InvalidOperationException(
            "The provider returned a null discovery result.");
    }
}
