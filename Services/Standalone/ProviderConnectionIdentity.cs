using System;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Identifies a configured provider connection instance.
/// Multiple connection IDs may reference the same provider ID.
/// </summary>
/// <remarks>
/// Identity only. No credentials, connection status, or
/// authenticated session data is stored by this model.
/// </remarks>
public sealed record ProviderConnectionIdentity
{
    public ProviderConnectionIdentity(
        string connectionId,
        string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionId, nameof(connectionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(
            providerId, nameof(providerId));

        ConnectionId = connectionId;
        ProviderId = providerId;
    }

    public string ConnectionId { get; }

    public string ProviderId { get; }

    public void Deconstruct(
        out string connectionId,
        out string providerId)
    {
        connectionId = ConnectionId;
        providerId = ProviderId;
    }
}
