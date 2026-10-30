namespace TruthDoctor.Models.Platform;

/// <summary>
/// Caller-declared service observation state.
/// Responded does not authenticate a network request.
/// </summary>
public enum OperationServiceObservationDisposition
{
    Unknown = 0,
    Responded = 1,
    Unavailable = 2,
    NotAssessed = 3,
    UnsupportedCapability = 4
}
