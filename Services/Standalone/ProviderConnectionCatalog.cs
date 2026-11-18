using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Stores provider connection identities in memory.
/// This catalog does not authenticate or connect providers.
/// </summary>
public sealed class ProviderConnectionCatalog
{
    private readonly object _gate = new();

    private readonly Dictionary<string, ProviderConnectionIdentity>
        _connections = new(StringComparer.Ordinal);

    private readonly Dictionary<string, long> _generations =
        new(StringComparer.Ordinal);

    private long _nextRegistrationGeneration;

    public void Register(ProviderConnectionIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        lock (_gate)
        {
            if (!_connections.TryAdd(identity.ConnectionId, identity))
            {
                throw new ArgumentException(
                    "The connection identifier is already registered.",
                    nameof(identity));
            }

            long generation = checked(++_nextRegistrationGeneration);
            _generations.Add(identity.ConnectionId, generation);
        }
    }

    /// <summary>
    /// Looks up a registered connection without throwing when absent.
    /// </summary>
    public bool TryGet(
        string connectionId,
        out ProviderConnectionIdentity? identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            return _connections.TryGetValue(connectionId, out identity);
        }
    }

    /// <summary>
    /// Removes one connection without affecting other registrations.
    /// </summary>
    public bool Unregister(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            if (!_connections.Remove(connectionId))
            {
                return false;
            }

            _generations.Remove(connectionId);
            return true;
        }
    }

    /// <summary>
    /// Returns a detached snapshot for one provider.
    /// </summary>
    public IReadOnlyList<ProviderConnectionIdentity> GetByProvider(
        string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        lock (_gate)
        {
            var matches = new List<ProviderConnectionIdentity>();

            foreach (var identity in _connections.Values)
            {
                if (StringComparer.Ordinal.Equals(
                    identity.ProviderId, providerId))
                {
                    matches.Add(identity);
                }
            }

            matches.Sort((left, right) =>
                StringComparer.Ordinal.Compare(
                    left.ConnectionId, right.ConnectionId));

            return matches.ToArray();
        }
    }

    /// <summary>
    /// Returns the generation of the current registration instance.
    /// Removal and re-registration create a different generation,
    /// even if the same identity object is reused.
    /// </summary>
    /// <summary>
    /// Executes a short synchronous operation while retaining the
    /// catalog lock for the current registration generation.
    /// The callback must not perform blocking external I/O.
    /// </summary>
    public void ExecuteForRegistration(
        string connectionId,
        Action<long> operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(operation);

        lock (_gate)
        {
            if (!_generations.TryGetValue(
                connectionId, out var generation))
            {
                throw new KeyNotFoundException(
                    "The connection identifier is not registered.");
            }

            operation(generation);
        }
    }

    public long GetRegistrationGeneration(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            if (!_generations.TryGetValue(connectionId, out var generation))
            {
                throw new KeyNotFoundException(
                    "The connection identifier is not registered.");
            }

            return generation;
        }
    }

    public ProviderConnectionIdentity Get(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            if (!_connections.TryGetValue(connectionId, out var identity))
            {
                throw new KeyNotFoundException(
                    "The connection identifier is not registered.");
            }

            return identity;
        }
    }
}
