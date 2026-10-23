using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral impact estimate for one operation step.
/// No graph traversal, runtime prediction, authorization, or execution.
/// </summary>
public sealed class OperationStepImpact
{
    public string StepId { get; }

    public OperationImpactSeverity Severity { get; }

    public OperationImpactConfidence Confidence { get; }

    public int? EstimatedAffectedResourceCount { get; }

    public bool? AvailabilityDisruptionExpected { get; }

    public string? Rationale { get; }

    public OperationStepImpact(
        string stepId,
        OperationImpactSeverity severity,
        OperationImpactConfidence confidence,
        int? estimatedAffectedResourceCount = null,
        bool? availabilityDisruptionExpected = null,
        string? rationale = null)
    {
        StepId = NormalizeRequired(stepId, nameof(stepId));

        if (severity != OperationImpactSeverity.Unknown &&
            severity != OperationImpactSeverity.None &&
            severity != OperationImpactSeverity.Low &&
            severity != OperationImpactSeverity.Moderate &&
            severity != OperationImpactSeverity.High)
        {
            throw new ArgumentOutOfRangeException(
                nameof(severity), severity,
                "Unknown operation impact severity.");
        }

        if (confidence != OperationImpactConfidence.Unknown &&
            confidence != OperationImpactConfidence.Low &&
            confidence != OperationImpactConfidence.Medium &&
            confidence != OperationImpactConfidence.High)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence), confidence,
                "Unknown operation impact confidence.");
        }

        if (estimatedAffectedResourceCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(estimatedAffectedResourceCount),
                estimatedAffectedResourceCount,
                "Estimated affected-resource count cannot be negative.");
        }

        Severity = severity;
        Confidence = confidence;
        EstimatedAffectedResourceCount = estimatedAffectedResourceCount;
        AvailabilityDisruptionExpected = availabilityDisruptionExpected;
        Rationale = NormalizeOptional(rationale, nameof(rationale));
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank impact step ID is required.",
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
                    "Impact text cannot contain control characters.",
                    parameterName);
            }
        }

        return value;
    }
}
