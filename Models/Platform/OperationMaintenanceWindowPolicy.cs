using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of maintenance-window
/// governance requirements.
///
/// Conditions refer to existing authorization-condition declarations.
/// Boolean properties describe requirements explicitly supplied by
/// the caller; they do not evaluate or satisfy those requirements.
///
/// A false value does not grant permission or an exemption.
/// A true value does not establish an approved decision.
///
/// This contract does not approve amendments, authorize operations,
/// bypass separation of duties, permit overruns, grant emergency
/// exceptions, verify maintenance results, schedule work, or execute.
///
/// Policy evaluation, approval, audit evidence, and enforcement
/// are separate responsibilities.
/// </summary>
public sealed class OperationMaintenanceWindowPolicy
{
    public string PolicyId { get; }

    public OperationAuthorizationConditions Conditions { get; }

    public bool RequiresAmendmentApproval { get; }

    public bool RequiresEarlyClosureReview { get; }

    public bool RequiresCancellationReview { get; }

    public bool RequiresOverrunEscalation { get; }

    public bool RequiresEmergencyApproval { get; }

    public bool RequiresPostMaintenanceVerification { get; }

    public OperationMaintenanceWindowPolicy(
        string policyId,
        OperationAuthorizationConditions conditions,
        bool requiresAmendmentApproval,
        bool requiresEarlyClosureReview,
        bool requiresCancellationReview,
        bool requiresOverrunEscalation,
        bool requiresEmergencyApproval,
        bool requiresPostMaintenanceVerification)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            throw new ArgumentException(
                "A nonblank maintenance-window policy ID is required.",
                nameof(policyId));
        }

        var normalizedPolicyId = policyId.Trim();

        foreach (var character in normalizedPolicyId)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Maintenance-window policy IDs cannot contain " +
                    "control characters.",
                    nameof(policyId));
            }
        }

        ArgumentNullException.ThrowIfNull(conditions);

        PolicyId = normalizedPolicyId;
        Conditions = conditions;

        RequiresAmendmentApproval = requiresAmendmentApproval;
        RequiresEarlyClosureReview = requiresEarlyClosureReview;
        RequiresCancellationReview = requiresCancellationReview;
        RequiresOverrunEscalation = requiresOverrunEscalation;
        RequiresEmergencyApproval = requiresEmergencyApproval;
        RequiresPostMaintenanceVerification =
            requiresPostMaintenanceVerification;
    }
}
