namespace TruthDoctor.Models.Platform;

/// <summary>
/// Qualitative estimated magnitude of a planned operation's potential effects.
/// Does not classify risk or authorize execution.
/// </summary>
public enum OperationImpactSeverity
{
    Unknown = 0,
    None = 1,
    Low = 2,
    Moderate = 3,
    High = 4
}
