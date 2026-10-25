namespace TruthDoctor.Models.Platform;

/// <summary>
/// Provider-neutral, ordered R0-R5 operation risk classification.
/// Does not calculate risk, define thresholds, authorize, approve,
/// schedule, or execute operations.
/// </summary>
public enum OperationRiskLevel
{
    R0 = 0,
    R1 = 1,
    R2 = 2,
    R3 = 3,
    R4 = 4,
    R5 = 5
}
