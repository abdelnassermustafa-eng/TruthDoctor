using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Maintains independent in-memory connection lifecycle states.
/// This component does not perform provider authentication or I/O.
/// </summary>
public sealed class ProviderConnectionLifecycle
{
    private readonly ProviderConnectionCatalog _catalog;
    private readonly object _gate = new();

    private readonly Dictionary<string, (long Generation, ProviderConnectionState State)>
        _states = new(StringComparer.Ordinal);

    public ProviderConnectionLifecycle(
        ProviderConnectionCatalog catalog)
    {
        _catalog = catalog ??
            throw new ArgumentNullException(nameof(catalog));
    }

    public ProviderConnectionState GetState(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        ProviderConnectionState result =
            ProviderConnectionState.Disconnected;

        // Catalog registration is protected for the entire state read.
        // Lock order: catalog -> lifecycle.
        _catalog.ExecuteForRegistration(connectionId, generation =>
        {
            lock (_gate)
            {
                result = _states.TryGetValue(
                    connectionId, out var entry)
                    && entry.Generation == generation
                    ? entry.State
                    : ProviderConnectionState.Disconnected;
            }
        });

        return result;
    }

    public void Transition(
        string connectionId,
        ProviderConnectionState nextState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        // Registration cannot be removed or replaced before the
        // state transition is validated and committed.
        // Lock order: catalog -> lifecycle.
        _catalog.ExecuteForRegistration(connectionId, generation =>
        {
            lock (_gate)
            {
                var current = _states.TryGetValue(
                    connectionId, out var entry)
                    && entry.Generation == generation
                    ? entry.State
                    : ProviderConnectionState.Disconnected;

                if (!IsAllowed(current, nextState))
                {
                    throw new InvalidOperationException(
                        $"Invalid connection lifecycle transition: " +
                        $"{current} -> {nextState}.");
                }

                _states[connectionId] = (generation, nextState);
            }
        });
    }

    private static bool IsAllowed(
        ProviderConnectionState current,
        ProviderConnectionState next)
    {
        return (current, next) switch
        {
            (ProviderConnectionState.Disconnected,
                ProviderConnectionState.Configured) => true,

            (ProviderConnectionState.Configured,
                ProviderConnectionState.Authenticating) => true,

            (ProviderConnectionState.Authenticating,
                ProviderConnectionState.Authenticated) => true,

            (ProviderConnectionState.Authenticating,
                ProviderConnectionState.Failed) => true,

            (ProviderConnectionState.Authenticated,
                ProviderConnectionState.Connecting) => true,

            (ProviderConnectionState.Connecting,
                ProviderConnectionState.Connected) => true,

            (ProviderConnectionState.Connecting,
                ProviderConnectionState.Failed) => true,

            (ProviderConnectionState.Connected,
                ProviderConnectionState.Disconnected) => true,

            (ProviderConnectionState.Connected,
                ProviderConnectionState.Expired) => true,

            (ProviderConnectionState.Expired,
                ProviderConnectionState.Disconnected) => true,

            (ProviderConnectionState.Failed,
                ProviderConnectionState.Disconnected) => true,

            (ProviderConnectionState.Connected,
                ProviderConnectionState.Failed) => true,

            _ => false
        };
    }
}
