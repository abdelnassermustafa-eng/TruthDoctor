using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral explanation of a declared operation risk.
/// Explanation and risk level are independently supplied and optional.
/// Does not infer evidence, calculate risk, aggregate, authorize, or execute.
/// </summary>
public sealed class OperationRiskExplanation
{
    public string? Explanation { get; }

    public OperationRiskLevel? DeclaredRiskLevel { get; }

    public OperationRiskExplanation(
        string? explanation = null,
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

        Explanation = NormalizeOptionalExplanation(explanation);
        DeclaredRiskLevel = declaredRiskLevel;
    }

    private static string? NormalizeOptionalExplanation(
        string? explanation)
    {
        if (string.IsNullOrWhiteSpace(explanation))
        {
            return null;
        }

        var normalized = explanation.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Risk explanation cannot contain control characters.",
                    nameof(explanation));
            }
        }

        return normalized;
    }
}
