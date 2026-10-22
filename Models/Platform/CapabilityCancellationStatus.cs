namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declared cancellation support for a universal capability.
/// Support does not guarantee successful or immediate termination.
/// </summary>
public enum CapabilityCancellationStatus
{
    Unknown = 0,
    Unsupported = 1,
    Supported = 2
}
