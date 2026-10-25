using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral service criticality risk declaration.
/// Criticality is a supplied business label, not an inferred severity.
/// Does not calculate risk, aggregate, authorize, or execute operations.
/// </summary>
public sealed class OperationServiceCriticalityRisk
{
    public string? CriticalityLabel { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationServiceCriticalityRisk(
        string? criticalityLabel = null,
        OperationRiskLevel? declaredRiskLevel = null)
    {
        if (declaredRiskLevel.HasValue &&
            !Enum.IsDefined(declaredRiskLevel.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(declaredRiskLevel),
                declaredRiskLevel,
                "Undefined operation risk level.");
        }

        CriticalityLabel = NormalizeOptionalLabel(criticalityLabel);
        DeclaredRiskLevel = declaredRiskLevel;
    }

    private static string? NormalizeOptionalLabel(
        string? criticalityLabel)
    {
        if (string.IsNullOrWhiteSpace(criticalityLabel))
        {
            return null;
        }

        var normalized = criticalityLabel.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Criticality label cannot contain control characters.",
                    nameof(criticalityLabel));
            }
        }

        return normalized;
    }
}
