using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Owns the minimal in-process standalone runtime lifecycle.
/// </summary>
/// <remarks>
/// Does not start an HTTP server, launch TruthApi, access provider
/// credentials, or perform infrastructure discovery.
/// </remarks>
public sealed class StandaloneBackendRuntime
    : IStandaloneBackendRuntime
{
    private readonly object _gate = new();

    private bool _isRunning;

    private readonly ProviderConnectionCatalog
        _connectionCatalog = new();

    private readonly ProviderConnectionLifecycle
        _connectionLifecycle;

    public StandaloneBackendRuntime()
    {
        _connectionLifecycle =
            new ProviderConnectionLifecycle(_connectionCatalog);
    }

    /// <summary>
    /// Gets this runtime's owned connection registry.
    /// </summary>
    public ProviderConnectionCatalog ConnectionCatalog =>
        _connectionCatalog;

    /// <summary>
    /// Gets this runtime's generation-aware lifecycle coordinator.
    /// </summary>
    public ProviderConnectionLifecycle ConnectionLifecycle =>
        _connectionLifecycle;


    /// <summary>
    /// Gets the currently committed lifecycle state.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _isRunning;
            }
        }
    }

    /// <summary>
    /// Starts the in-process runtime.
    /// Repeated starts preserve the running state.
    /// </summary>
    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            // Reject pre-canceled operations before changing state.
            cancellationToken.ThrowIfCancellationRequested();

            _isRunning = true;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the in-process runtime.
    /// Repeated stops preserve the stopped state.
    /// </summary>
    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            // Reject pre-canceled operations before changing state.
            cancellationToken.ThrowIfCancellationRequested();

            _isRunning = false;
        }

        return Task.CompletedTask;
    }
}
