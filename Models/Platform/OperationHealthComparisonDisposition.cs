namespace TruthDoctor.Models.Platform;

/// <summary>
/// Classification of a local expected-versus-reported health comparison.
/// Matched does not establish verified operation success.
/// </summary>
public enum OperationHealthComparisonDisposition
{
    Unknown = 0,
    Matched = 1,
    Mismatched = 2,
    TargetMismatch = 3,
    UnsupportedCondition = 4,
    Unavailable = 5,
    NoObservation = 6,
    NotAssessed = 7
}
