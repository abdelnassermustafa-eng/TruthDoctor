namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declarative status of recovery planning for one operation step.
/// Does not establish recovery eligibility or execution support.
/// </summary>
public enum OperationRecoveryStatus
{
    Unknown = 0,
    Unsupported = 1,
    Planned = 2
}
