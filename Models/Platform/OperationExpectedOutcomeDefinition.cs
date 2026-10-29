using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable declaration associating an existing operation plan with
/// declared verification capability metadata.
///
/// This definition does not observe or verify resource state, collect
/// evidence, approve, authorize, schedule, or execute operations.
/// Verification support does not establish successful verification.
/// </summary>
public sealed class OperationExpectedOutcomeDefinition
{
    public OperationPlanReview Plan { get; }

    public CapabilityVerificationMetadata Verification { get; }

    public OperationExpectedOutcomeDefinition(
        OperationPlanReview plan,
        CapabilityVerificationMetadata verification)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(verification);

        if (plan.Intent.Capability != verification.Capability)
        {
            throw new ArgumentException(
                "Verification metadata must describe the " +
                "operation plan's intended capability.",
                nameof(verification));
        }

        Plan = plan;
        Verification = verification;
    }
}
