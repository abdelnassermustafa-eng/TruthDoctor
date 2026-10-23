using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral evidence requirement for a planned step.
/// Describes expected evidence without collecting or validating it.
/// Does not authorize or execute an operation.
/// </summary>
public sealed class OperationEvidenceRequirement
{
    public string RequirementId { get; }

    public string StepId { get; }

    public string Category { get; }

    public string Description { get; }

    public bool IsMandatory { get; }

    public string? SourceHint { get; }

    public OperationEvidenceRequirement(
        string requirementId,
        string stepId,
        string category,
        string description,
        bool isMandatory = true,
        string? sourceHint = null)
    {
        RequirementId = NormalizeRequired(
            requirementId, nameof(requirementId));

        StepId = NormalizeRequired(
            stepId, nameof(stepId));

        Category = NormalizeRequired(
            category, nameof(category));

        Description = NormalizeRequired(
            description, nameof(description));

        IsMandatory = isMandatory;

        SourceHint = NormalizeOptional(
            sourceHint, nameof(sourceHint));
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank evidence requirement value is required.",
                parameterName);
        }

        return ValidateText(value.Trim(), parameterName);
    }

    private static string? NormalizeOptional(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ValidateText(value.Trim(), parameterName);
    }

    private static string ValidateText(
        string value,
        string parameterName)
    {
        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Evidence requirement text cannot contain control characters.",
                    parameterName);
            }
        }

        return value;
    }
}
