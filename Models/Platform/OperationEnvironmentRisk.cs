using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of environment information
/// and an optional, explicitly supplied R0-R5 risk classification.
/// Does not infer, calculate, authorize, approve, or execute operations.
/// </summary>
public sealed class OperationEnvironmentRisk
{
    public string? EnvironmentName { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationEnvironmentRisk(
        string? environmentName = null,
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

        EnvironmentName = NormalizeOptionalEnvironmentName(
            environmentName);
        DeclaredRiskLevel = declaredRiskLevel;
    }

    private static string? NormalizeOptionalEnvironmentName(
        string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return null;
        }

        var normalized = environmentName.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Environment name cannot contain control characters.",
                    nameof(environmentName));
            }
        }

        return normalized;
    }
}
