namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declared progress-reporting support for a universal capability.
/// Support does not guarantee percentage or live progress updates.
/// </summary>
public enum CapabilityProgressStatus
{
    Unknown = 0,
    Unsupported = 1,
    Supported = 2
}
