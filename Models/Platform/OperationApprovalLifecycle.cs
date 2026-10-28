using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Explicitly reported approval lifecycle state.
///
/// Values do not represent independently verified approval,
/// effective authorization, or permission to execute.
/// No clock evaluation or state transition is performed.
/// </summary>
public enum OperationApprovalLifecycleStatus
{
    Unknown = 0,
    ReportedActive = 1,
    Expired = 2,
    Revoked = 3
}

/// <summary>
/// Immutable, provider-neutral declaration of reported approval
/// expiration and revocation information for an existing approval chain.
///
/// The supplied status is descriptive. ReportedActive does not establish
/// that a completed approval exists or that execution is authorized.
///
/// Expiration is explicitly reported, not calculated from the current
/// clock or inferred from the observation timestamp.
///
/// Revocation metadata describes a reported event; constructing this
/// declaration does not revoke any permission or authenticate an actor.
///
/// This contract does not approve requests, validate role eligibility,
/// enforce separation of duties, interact with providers, execute
/// operations, or establish permission to execute.
/// </summary>
public sealed class OperationApprovalLifecycle
{
    public string ApprovalId { get; }

    public OperationApprovalChain Chain { get; }

    public OperationApprovalLifecycleStatus Status { get; }

    public DateTimeOffset ObservedAt { get; }

    public DateTimeOffset? ExpiresAt { get; }

    public DateTimeOffset? RevokedAt { get; }

    public OperationActorIdentity? RevokedBy { get; }

    public string Reason { get; }

    public OperationApprovalLifecycle(
        string approvalId,
        OperationApprovalChain chain,
        OperationApprovalLifecycleStatus status,
        DateTimeOffset observedAt,
        DateTimeOffset? expiresAt,
        DateTimeOffset? revokedAt,
        OperationActorIdentity? revokedBy,
        string reason)
    {
        var normalizedApprovalId = NormalizeRequired(
            approvalId, nameof(approvalId));

        ArgumentNullException.ThrowIfNull(chain);

        if (string.IsNullOrEmpty(chain.Request.Operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(chain));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException(
                "A defined approval lifecycle status is required.",
                nameof(status));
        }

        if (status == OperationApprovalLifecycleStatus.Expired &&
            !expiresAt.HasValue)
        {
            throw new ArgumentException(
                "An explicitly reported expiration timestamp is required.",
                nameof(expiresAt));
        }

        if (status == OperationApprovalLifecycleStatus.Revoked)
        {
            if (!revokedAt.HasValue)
            {
                throw new ArgumentException(
                    "An explicit revocation timestamp is required.",
                    nameof(revokedAt));
            }

            if (!revokedBy.HasValue)
            {
                throw new ArgumentException(
                    "An explicit revoking actor is required.",
                    nameof(revokedBy));
            }

            var actor = revokedBy.Value;

            if (string.IsNullOrEmpty(actor.Value) ||
                !Enum.IsDefined(actor.Kind))
            {
                throw new ArgumentException(
                    "An initialized revoking actor identity is required.",
                    nameof(revokedBy));
            }
        }
        else if (revokedAt.HasValue || revokedBy.HasValue)
        {
            throw new ArgumentException(
                "Revocation metadata requires a Revoked status.",
                nameof(status));
        }

        var normalizedReason = NormalizeRequired(
            reason, nameof(reason));

        ApprovalId = normalizedApprovalId;
        Chain = chain;
        Status = status;
        ObservedAt = observedAt;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        RevokedBy = revokedBy;
        Reason = normalizedReason;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank approval lifecycle value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Approval lifecycle text cannot contain " +
                    "control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
