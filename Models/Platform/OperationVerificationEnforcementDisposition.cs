namespace TruthDoctor.Models.Platform;

/// <summary>
/// Conservative classification of whether a verification outcome
/// can support a claim about an operation.
/// None of these values establishes actual execution success.
/// </summary>
public enum OperationVerificationEnforcementDisposition
{
    NotEstablished = 0,
    Blocked = 1,
    LocallyConsistentOnly = 2
}
