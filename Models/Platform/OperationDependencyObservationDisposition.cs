namespace TruthDoctor.Models.Platform;

/// <summary>
/// Availability of a caller-supplied, directed dependency observation.
/// Neither a reported presence nor absence authenticates provenance.
/// </summary>
public enum OperationDependencyObservationDisposition
{
    Unknown = 0,
    Observed = 1,
    Absent = 2,
    Unavailable = 3,
    NotAssessed = 4
}
