namespace TruthDoctor.Models.Platform;

/// <summary>
/// Classifies a declared recovery action.
/// Compensation does not guarantee restoration of original state.
/// </summary>
public enum OperationRecoveryKind
{
    None = 0,
    Rollback = 1,
    Compensation = 2
}
