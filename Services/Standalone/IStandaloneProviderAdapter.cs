using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Defines the provider-facing boundary for a standalone connection.
/// Credential acquisition, storage, and provider implementation remain
/// outside the responsibilities of this interface.
/// </summary>
public interface IStandaloneProviderAdapter
{
    string ProviderId { get; }

    Task AuthenticateAsync(
        string connectionId,
        CancellationToken cancellationToken);

    Task DisconnectAsync(
        string connectionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProviderResourceRecord>> DiscoverAsync(
        string connectionId,
        CancellationToken cancellationToken);
}
