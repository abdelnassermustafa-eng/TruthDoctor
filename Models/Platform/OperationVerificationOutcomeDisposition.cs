namespace TruthDoctor.Models.Platform;

/// <summary>
/// Classification of local comparison completeness and consistency.
/// LocallyConsistent never means authenticated operation success.
/// </summary>
public enum OperationVerificationOutcomeDisposition
{
    NoRequirements = 0,
    LocallyConsistent = 1,
    Inconsistent = 2,
    Incomplete = 3,
    Unresolved = 4,
    Conflict = 5
}
