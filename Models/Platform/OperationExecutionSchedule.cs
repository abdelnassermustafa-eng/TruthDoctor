using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an intended
/// operation execution time within a supplied maintenance interval.
///
/// Start is inclusive and End is exclusive. Interval membership is
/// validated by instant, not by local wall-clock representation.
///
/// This declaration does not establish that its supplied window
/// is approved, effective, active, or authorized for execution.
///
/// Constructing a schedule does not approve, dispatch, execute,
/// revalidate, retry, or reschedule an operation. Runtime clock
/// checks, approval expiry/revocation, and enforcement are separate
/// responsibilities.
/// </summary>
public sealed class OperationExecutionSchedule
{
    public string ScheduleId { get; }

    public OperationCorrelationId Operation { get; }

    public OperationMaintenanceWindow Window { get; }

    public DateTimeOffset ScheduledAt { get; }

    public string Reason { get; }

    public OperationExecutionSchedule(
        string scheduleId,
        OperationCorrelationId operation,
        OperationMaintenanceWindow window,
        DateTimeOffset scheduledAt,
        string reason)
    {
        var normalizedScheduleId = NormalizeRequired(
            scheduleId, nameof(scheduleId));

        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(window);

        // DateTimeOffset comparisons use the represented UTC instant.
        // The inclusive-start/exclusive-end interval is declarative
        // and is not evidence of approval or execution readiness.
        if (scheduledAt < window.Start ||
            scheduledAt >= window.End)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scheduledAt),
                "Scheduled execution must fall within the supplied " +
                "maintenance window [Start, End).");
        }

        var normalizedReason = NormalizeRequired(
            reason, nameof(reason));

        ScheduleId = normalizedScheduleId;
        Operation = operation;
        Window = window;
        ScheduledAt = scheduledAt;
        Reason = normalizedReason;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank execution-schedule value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Execution-schedule text cannot contain " +
                    "control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
