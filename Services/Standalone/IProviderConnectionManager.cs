using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Architectural boundary for TruthDoctor-owned provider
/// connection and authentication coordination.
/// </summary>
/// <remarks>
/// Declarative contract only. Connection methods, credential
/// handling, and authorization rules require separate tests
/// before implementation.
/// </remarks>
public interface IProviderConnectionManager
{
    Task ConnectAsync(string connectionId, CancellationToken cancellationToken);

    Task DisconnectAsync(string connectionId, CancellationToken cancellationToken);

    ProviderConnectionState GetState(string connectionId);
}
