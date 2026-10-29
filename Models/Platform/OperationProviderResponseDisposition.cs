namespace TruthDoctor.Models.Platform;

/// <summary>
/// Normalized classification of a caller-supplied provider response.
/// Neither acknowledgement nor rejection proves the final resource state.
/// </summary>
public enum OperationProviderResponseDisposition
{
    Unknown = 0,
    Acknowledged = 1,
    Rejected = 2
}
