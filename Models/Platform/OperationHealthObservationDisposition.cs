namespace TruthDoctor.Models.Platform;

/// <summary>
/// Availability of caller-supplied resource-health information.
/// No disposition authenticates observation provenance.
/// </summary>
public enum OperationHealthObservationDisposition
{
    Unknown = 0,
    Observed = 1,
    Unavailable = 2,
    NotAssessed = 3
}
