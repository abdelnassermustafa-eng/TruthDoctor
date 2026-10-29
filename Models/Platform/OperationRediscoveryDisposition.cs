namespace TruthDoctor.Models.Platform;

/// <summary>
/// Classification of a caller-supplied rediscovery observation.
/// These values do not establish verified operation success.
/// </summary>
public enum OperationRediscoveryDisposition
{
    Unknown = 0,
    Observed = 1,
    Absent = 2,
    Unavailable = 3
}
