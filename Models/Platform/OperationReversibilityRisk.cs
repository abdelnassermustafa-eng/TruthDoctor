using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral reversibility risk declaration.
/// Reversibility support does not guarantee restoration.
/// Does not infer risk, aggregate, authorize, or execute rollback.
/// </summary>
public sealed class OperationReversibilityRisk
{
    public CapabilityReversibilityStatus Status { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationReversibilityRisk(
        CapabilityReversibilityStatus status =
            CapabilityReversibilityStatus.Unknown,
        OperationRiskLevel? declaredRiskLevel = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Undefined capability reversibility status.");
        }

        if (declaredRiskLevel.HasValue &&
            !Enum.IsDefined(declaredRiskLevel.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(declaredRiskLevel),
                declaredRiskLevel,
                "Undefined operation risk level.");
        }

        Status = status;
        DeclaredRiskLevel = declaredRiskLevel;
    }
}
