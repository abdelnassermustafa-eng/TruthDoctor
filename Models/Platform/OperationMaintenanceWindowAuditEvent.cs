using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of a reported
/// maintenance-window event associated with a lifecycle snapshot.
///
/// The event identifier, kind, reason, reporter, and timestamps
/// are explicitly supplied. They are not independently verified.
///
/// Occurrence and recording times remain separate declarations.
/// Their order is neither inferred nor enforced by this type.
///
/// Constructing an event does not authenticate its reporter,
/// prove historical truth, approve a lifecycle transition,
/// change an effective maintenance window, grant authorization,
/// persist evidence, schedule work, or execute operations.
///
/// Generalized operational history, evidence verification,
/// audit retention, and runtime enforcement are separate concerns.
/// </summary>
public sealed class OperationMaintenanceWindowAuditEvent
{
    public string EventId { get; }

    public OperationCorrelationId Operation { get; }

    public OperationActorIdentity Reporter { get; }

    public OperationMaintenanceWindowLifecycle Lifecycle { get; }

    public string EventKind { get; }

    public DateTimeOffset OccurredAt { get; }

    public DateTimeOffset RecordedAt { get; }

    public string Reason { get; }

    public OperationMaintenanceWindowAuditEvent(
        string eventId,
        OperationCorrelationId operation,
        OperationActorIdentity reporter,
        OperationMaintenanceWindowLifecycle lifecycle,
        string eventKind,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt,
        string reason)
    {
        var normalizedEventId = NormalizeRequired(
            eventId, nameof(eventId));

        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        if (string.IsNullOrEmpty(reporter.Value) ||
            !Enum.IsDefined(reporter.Kind))
        {
            throw new ArgumentException(
                "An initialized and defined reporter identity is required.",
                nameof(reporter));
        }

        ArgumentNullException.ThrowIfNull(lifecycle);

        if (operation != lifecycle.Operation)
        {
            throw new ArgumentException(
                "The event operation must match the lifecycle operation.",
                nameof(lifecycle));
        }

        var normalizedEventKind = NormalizeRequired(
            eventKind, nameof(eventKind));

        var normalizedReason = NormalizeRequired(
            reason, nameof(reason));

        EventId = normalizedEventId;
        Operation = operation;
        Reporter = reporter;
        Lifecycle = lifecycle;
        EventKind = normalizedEventKind;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        Reason = normalizedReason;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank maintenance-window audit-event value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Maintenance-window audit-event text cannot " +
                    "contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
