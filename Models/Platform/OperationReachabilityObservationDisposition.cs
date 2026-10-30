namespace TruthDoctor.Models.Platform;

/// <summary>
/// Caller-reported check disposition. Satisfied and Unsatisfied
/// do not establish authenticated execution or network causality.
/// </summary>
public enum OperationReachabilityObservationDisposition
{
    Unknown = 0,
    Satisfied = 1,
    Unsatisfied = 2,
    Unavailable = 3,
    NotAssessed = 4,
    UnsupportedCapability = 5
}
