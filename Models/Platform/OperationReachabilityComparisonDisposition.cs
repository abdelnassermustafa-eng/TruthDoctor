namespace TruthDoctor.Models.Platform;

/// <summary>
/// Local reachability comparison classifications.
/// Matched is not proof of successful operation execution.
/// </summary>
public enum OperationReachabilityComparisonDisposition
{
    Unknown = 0,
    Matched = 1,
    Mismatched = 2,
    SourceMismatch = 3,
    SpecificationMismatch = 4,
    UnsupportedCondition = 5,
    UnsupportedProtocol = 6,
    UnsupportedCapability = 7,
    NoObservation = 8,
    Unavailable = 9,
    NotAssessed = 10
}
