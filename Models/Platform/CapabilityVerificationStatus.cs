namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declared outcome-verification support for a universal capability.
/// Support does not establish successful verification.
/// </summary>
public enum CapabilityVerificationStatus
{
    Unknown = 0,
    Unsupported = 1,
    Supported = 2
}
