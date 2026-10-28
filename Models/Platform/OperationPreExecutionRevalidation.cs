using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral record of an explicitly supplied
/// pre-execution revalidation assessment.
///
/// The referenced schedule, approval chain, and lifecycle snapshot
/// must describe the same logical operation. The lifecycle must
/// reference the same maintenance-window instance as the schedule.
///
/// Finding and Reason are descriptive, caller-supplied text.
/// They do not represent independently verified approval, effective
/// authorization, execution readiness, or permission to execute.
///
/// This declaration does not authenticate actors, evaluate policy,
/// validate completed approvals, check expiration or revocation,
/// inspect current infrastructure, read the system clock, dispatch,
/// schedule, or execute operations.
/// </summary>
public sealed class OperationPreExecutionRevalidation
{
    public string RevalidationId { get; }

    public OperationExecutionSchedule Schedule { get; }

    public OperationApprovalChain ApprovalChain { get; }

    public OperationMaintenanceWindowLifecycle Lifecycle { get; }

    public DateTimeOffset AssessedAt { get; }

    public string Finding { get; }

    public string Reason { get; }

    public OperationPreExecutionRevalidation(
        string revalidationId,
        OperationExecutionSchedule schedule,
        OperationApprovalChain approvalChain,
        OperationMaintenanceWindowLifecycle lifecycle,
        DateTimeOffset assessedAt,
        string finding,
        string reason)
    {
        var normalizedId = NormalizeRequired(
            revalidationId, nameof(revalidationId));

        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(approvalChain);
        ArgumentNullException.ThrowIfNull(lifecycle);

        if (schedule.Operation != approvalChain.Request.Operation)
        {
            throw new ArgumentException(
                "The approval chain must belong to the scheduled operation.",
                nameof(approvalChain));
        }

        if (schedule.Operation != lifecycle.Operation)
        {
            throw new ArgumentException(
                "The lifecycle must belong to the scheduled operation.",
                nameof(lifecycle));
        }

        if (!ReferenceEquals(schedule.Window, lifecycle.Window))
        {
            throw new ArgumentException(
                "The lifecycle must reference the schedule's exact " +
                "maintenance-window instance.",
                nameof(lifecycle));
        }

        var normalizedFinding = NormalizeRequired(
            finding, nameof(finding));

        var normalizedReason = NormalizeRequired(
            reason, nameof(reason));

        RevalidationId = normalizedId;
        Schedule = schedule;
        ApprovalChain = approvalChain;
        Lifecycle = lifecycle;
        AssessedAt = assessedAt;
        Finding = normalizedFinding;
        Reason = normalizedReason;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank pre-execution revalidation value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Pre-execution revalidation text cannot contain " +
                    "control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
