using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Architectural boundary for provider infrastructure
/// resource discovery owned by TruthDoctor.
/// </summary>
/// <remarks>
/// Declarative contract only. Discovery methods, provider
/// capabilities, resource models, and failure handling
/// require separate design and verification.
/// </remarks>
public interface IProviderResourceDiscovery
{
    Task<PlatformState> DiscoverAsync(
        string connectionId,
        CancellationToken cancellationToken);
}
