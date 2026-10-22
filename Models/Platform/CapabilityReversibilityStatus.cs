namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declared reversal support for a universal capability.
/// Support does not guarantee successful rollback.
/// </summary>
public enum CapabilityReversibilityStatus
{
    Unknown = 0,
    Unsupported = 1,
    Supported = 2
}
