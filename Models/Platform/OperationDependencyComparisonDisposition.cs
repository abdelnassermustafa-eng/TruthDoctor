namespace TruthDoctor.Models.Platform;

/// <summary>
/// Local classification of an expected directed dependency relationship
/// against a caller-supplied observation. Matched is not verified success.
/// Absent specifically means expected presence was explicitly reported absent.
/// </summary>
public enum OperationDependencyComparisonDisposition
{
    Unknown = 0,
    Matched = 1,
    Mismatched = 2,
    SubjectMismatch = 3,
    DependencyMismatch = 4,
    RelationshipMismatch = 5,
    UnsupportedCondition = 6,
    Absent = 7,
    Unavailable = 8,
    NoObservation = 9,
    NotAssessed = 10
}
