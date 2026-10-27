using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration that separation of duties
/// is required for an existing approval chain. The requester identified
/// by the chain's native approval request must not approve that request.
/// This requirement does not identify or authenticate an approver,
/// evaluate actor identity or role eligibility, determine effective
/// authorization, approve or deny requests, grant policy exceptions,
/// enforce an approval decision, schedule work, or execute operations.
/// Actual enforcement and flexible policy are separate responsibilities.
/// </summary>
public sealed class OperationSeparationOfDutiesRequirement
{
    public OperationApprovalChain Chain { get; }

    public OperationSeparationOfDutiesRequirement(
        OperationApprovalChain chain)
    {
        ArgumentNullException.ThrowIfNull(chain);

        Chain = chain;
    }
}
