using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration composing an existing
/// separation-of-duties requirement with an optional policy override
/// or escalation request. The underlying requirement remains in force.
/// An override or escalation request is not an approved exception.
/// This declaration does not authenticate or select approvers,
/// evaluate eligibility or effective authorization, grant exceptions,
/// permit requester self-approval, approve or deny requests, schedule
/// work, or execute operations. Enforcement is a separate responsibility.
/// </summary>
public sealed class OperationFlexibleSeparationOfDutiesPolicy
{
    public OperationSeparationOfDutiesRequirement Requirement { get; }

    public OperationPolicyOverrideEscalation? OverrideEscalation { get; }

    public OperationFlexibleSeparationOfDutiesPolicy(
        OperationSeparationOfDutiesRequirement requirement,
        OperationPolicyOverrideEscalation? overrideEscalation = null)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        Requirement = requirement;
        OverrideEscalation = overrideEscalation;
    }
}
