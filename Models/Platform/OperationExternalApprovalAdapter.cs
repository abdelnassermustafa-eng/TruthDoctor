using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// An explicitly reported status from an external approval system.
///
/// These values are descriptive, caller-supplied declarations.
/// They are not independently verified approval decisions or
/// grants of execution authorization.
/// </summary>
public enum OperationExternalApprovalStatus
{
    Unknown = 0,
    Pending = 1,
    ReportedApproved = 2,
    ReportedDenied = 3,
    Unavailable = 4
}

/// <summary>
/// Immutable, provider-neutral declaration associating an existing
/// approval chain with a caller-supplied external approval reference
/// and its explicitly reported status.
///
/// This declaration does not contact external systems, generate
/// external requests, authenticate actors, verify approval decisions,
/// inspect credentials, evaluate authorization, or execute operations.
///
/// In particular, ReportedApproved does not establish an effective
/// approval or grant permission to execute. Unavailable and Unknown
/// cannot be treated as approval.
///
/// The supplied observation timestamp and its offset are preserved.
/// No current-clock evaluation, polling, scheduling, expiry, or
/// revocation processing takes place.
/// </summary>
public sealed class OperationExternalApprovalAdapter
{
    public string AdapterId { get; }

    public OperationApprovalChain Chain { get; }

    public string ExternalSystem { get; }

    public string ExternalRequestId { get; }

    public OperationExternalApprovalStatus Status { get; }

    public DateTimeOffset ObservedAt { get; }

    public string Reason { get; }

    public OperationExternalApprovalAdapter(
        string adapterId,
        OperationApprovalChain chain,
        string externalSystem,
        string externalRequestId,
        OperationExternalApprovalStatus status,
        DateTimeOffset observedAt,
        string reason)
    {
        var normalizedAdapterId = NormalizeRequired(
            adapterId, nameof(adapterId));

        ArgumentNullException.ThrowIfNull(chain);

        if (string.IsNullOrEmpty(chain.Request.Operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(chain));
        }

        var normalizedExternalSystem = NormalizeRequired(
            externalSystem, nameof(externalSystem));

        var normalizedExternalRequestId = NormalizeRequired(
            externalRequestId, nameof(externalRequestId));

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException(
                "A defined external approval status is required.",
                nameof(status));
        }

        var normalizedReason = NormalizeRequired(
            reason, nameof(reason));

        AdapterId = normalizedAdapterId;
        Chain = chain;
        ExternalSystem = normalizedExternalSystem;
        ExternalRequestId = normalizedExternalRequestId;
        Status = status;
        ObservedAt = observedAt;
        Reason = normalizedReason;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank external approval adapter value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "External approval adapter text cannot contain " +
                    "control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
