using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration that one planned operation step
/// depends on another. Does not authorize, schedule, or execute either step.
/// </summary>
public sealed class OperationStepDependency
{
    public string StepId { get; }

    public string PrerequisiteStepId { get; }

    public OperationStepDependency(
        string stepId,
        string prerequisiteStepId)
    {
        StepId = NormalizeRequired(stepId, nameof(stepId));
        PrerequisiteStepId = NormalizeRequired(
            prerequisiteStepId, nameof(prerequisiteStepId));

        if (string.Equals(
            StepId,
            PrerequisiteStepId,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "A planned step cannot depend on itself.",
                nameof(prerequisiteStepId));
        }
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank dependency step ID is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Dependency step IDs cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
