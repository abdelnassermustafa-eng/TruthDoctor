using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral blast-radius risk declaration.
/// The affected-dependent count is supplied, not graph-calculated.
/// Does not infer risk thresholds, aggregate, authorize, or execute.
/// </summary>
public sealed class OperationBlastRadiusRisk
{
    public int? AffectedDependentResourceCount { get; }

    public OperationImpactConfidence Confidence { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationBlastRadiusRisk(
        int? affectedDependentResourceCount = null,
        OperationImpactConfidence confidence =
            OperationImpactConfidence.Unknown,
        OperationRiskLevel? declaredRiskLevel = null)
    {
        if (affectedDependentResourceCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(affectedDependentResourceCount),
                affectedDependentResourceCount,
                "Affected-dependent resource count cannot be negative.");
        }

        if (!Enum.IsDefined(confidence))
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                confidence,
                "Undefined operation impact confidence.");
        }

        if (declaredRiskLevel.HasValue &&
            !Enum.IsDefined(declaredRiskLevel.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(declaredRiskLevel),
                declaredRiskLevel,
                "Undefined operation risk level.");
        }

        AffectedDependentResourceCount = affectedDependentResourceCount;
        Confidence = confidence;
        DeclaredRiskLevel = declaredRiskLevel;
    }
}
