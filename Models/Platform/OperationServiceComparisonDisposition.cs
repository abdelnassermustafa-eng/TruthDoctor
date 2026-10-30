namespace TruthDoctor.Models.Platform;

/// <summary>
/// Local service comparison classifications.
/// Matched is not verified operation or application success.
/// </summary>
public enum OperationServiceComparisonDisposition
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
