using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral security impact risk declaration.
/// The security impact description is supplied, not inferred.
/// Does not scan, calculate risk, aggregate, authorize, or execute.
/// </summary>
public sealed class OperationSecurityImpactRisk
{
    public string? SecurityImpactDescription { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationSecurityImpactRisk(
        string? securityImpactDescription = null,
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

        SecurityImpactDescription = NormalizeOptionalDescription(
            securityImpactDescription);
        DeclaredRiskLevel = declaredRiskLevel;
    }

    private static string? NormalizeOptionalDescription(
        string? securityImpactDescription)
    {
        if (string.IsNullOrWhiteSpace(securityImpactDescription))
        {
            return null;
        }

        var normalized = securityImpactDescription.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Security impact description cannot contain control characters.",
                    nameof(securityImpactDescription));
            }
        }

        return normalized;
    }
}
