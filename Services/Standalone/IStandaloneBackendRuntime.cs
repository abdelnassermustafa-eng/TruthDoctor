using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Architectural boundary for the TruthDoctor-owned
/// standalone backend runtime.
/// </summary>
/// <remarks>
/// Declarative contract only. Runtime lifecycle operations
/// will be added after their behavior is specified and tested.
/// This interface does not start or expose an HTTP server.
/// </remarks>
public interface IStandaloneBackendRuntime
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
