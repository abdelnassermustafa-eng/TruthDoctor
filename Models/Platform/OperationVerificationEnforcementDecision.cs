namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable classification derived by the enforcement evaluator.
/// Does not prove operation execution, authorization, provenance,
/// freshness, or the current state of a provider resource.
/// </summary>
public sealed class OperationVerificationEnforcementDecision
{
    public OperationVerificationEnforcementDisposition Disposition
    {
        get;
    }

    private OperationVerificationEnforcementDecision(
        OperationVerificationEnforcementDisposition disposition)
    {
        Disposition = disposition;
    }

    internal static OperationVerificationEnforcementDecision Create(
        OperationVerificationEnforcementDisposition disposition)
    {
        return new OperationVerificationEnforcementDecision(disposition);
    }
}
