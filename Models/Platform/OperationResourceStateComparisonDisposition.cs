namespace TruthDoctor.Models.Platform;

/// <summary>
/// Classification of a local comparison between a declared expected
/// resource state and a caller-supplied rediscovery observation.
/// A match does not establish verified operation success.
/// </summary>
public enum OperationResourceStateComparisonDisposition
{
    Unknown = 0,
    Matched = 1,
    Mismatched = 2,
    TargetMismatch = 3,
    UnsupportedCondition = 4,
    Absent = 5,
    Unavailable = 6,
    NoObservation = 7
}
