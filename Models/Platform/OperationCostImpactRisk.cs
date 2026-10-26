using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral cost impact risk declaration.
/// The cost impact description is supplied, not calculated or inferred.
/// Does not estimate costs, aggregate risk, authorize, or execute.
/// </summary>
public sealed class OperationCostImpactRisk
{
    public string? CostImpactDescription { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationCostImpactRisk(
        string? costImpactDescription = null,
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

        CostImpactDescription = NormalizeOptionalDescription(
            costImpactDescription);
        DeclaredRiskLevel = declaredRiskLevel;
    }

    private static string? NormalizeOptionalDescription(
        string? costImpactDescription)
    {
        if (string.IsNullOrWhiteSpace(costImpactDescription))
        {
            return null;
        }

        var normalized = costImpactDescription.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Cost impact description cannot contain control characters.",
                    nameof(costImpactDescription));
            }
        }

        return normalized;
    }
}
