using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral composition of operation plan declarations
/// for review. Does not validate plan-wide consistency, approve, authorize,
/// schedule, execute, or verify an operation.
/// </summary>
public sealed class OperationPlanReview
{
    public string Summary { get; }

    public OperationIntent Intent { get; }

    public OperationTarget Target { get; }

    public OperationDesiredOutcome DesiredOutcome { get; }

    public OperationPlannedSteps PlannedSteps { get; }

    public OperationPreconditions? Preconditions { get; }

    public OperationStepDependencies? Dependencies { get; }

    public OperationRecoveryPlan? RecoveryPlan { get; }

    public OperationEstimatedImpact? EstimatedImpact { get; }

    public OperationEvidenceRequirements? EvidenceRequirements { get; }

    public OperationPlanReview(
        string summary,
        OperationIntent intent,
        OperationTarget target,
        OperationDesiredOutcome desiredOutcome,
        OperationPlannedSteps plannedSteps,
        OperationPreconditions? preconditions = null,
        OperationStepDependencies? dependencies = null,
        OperationRecoveryPlan? recoveryPlan = null,
        OperationEstimatedImpact? estimatedImpact = null,
        OperationEvidenceRequirements? evidenceRequirements = null)
    {
        Summary = NormalizeSummary(summary);

        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(desiredOutcome);
        ArgumentNullException.ThrowIfNull(plannedSteps);

        Intent = intent;
        Target = target;
        DesiredOutcome = desiredOutcome;
        PlannedSteps = plannedSteps;

        Preconditions = preconditions;
        Dependencies = dependencies;
        RecoveryPlan = recoveryPlan;
        EstimatedImpact = estimatedImpact;
        EvidenceRequirements = evidenceRequirements;
    }

    private static string NormalizeSummary(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank plan review summary is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Plan review summary cannot contain control characters.",
                    nameof(value));
            }
        }

        return normalized;
    }
}
