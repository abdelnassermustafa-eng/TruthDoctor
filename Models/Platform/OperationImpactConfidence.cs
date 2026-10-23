namespace TruthDoctor.Models.Platform;

/// <summary>
/// Qualitative confidence in a declarative impact estimate.
/// Independent of severity and operational authorization.
/// </summary>
public enum OperationImpactConfidence
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
