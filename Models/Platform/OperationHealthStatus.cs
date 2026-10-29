namespace TruthDoctor.Models.Platform;

/// <summary>
/// Provider-neutral reported resource health.
/// Unknown does not represent a reportable healthy state.
/// This classification does not verify operation success.
/// </summary>
public enum OperationHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}
