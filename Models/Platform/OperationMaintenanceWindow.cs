using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of a bounded
/// maintenance interval.
///
/// Start is inclusive and End is exclusive when the interval
/// is interpreted by a future consumer.
///
/// The boundaries represent explicit instants. They do not
/// establish authorization, approval, scheduling, execution
/// readiness, or runtime enforcement.
///
/// Extensions, shortening, rescheduling, lifecycle changes,
/// emergency handling, and operational verification are
/// separate responsibilities.
/// </summary>
public sealed class OperationMaintenanceWindow
{
    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public OperationMaintenanceWindow(
        DateTimeOffset start,
        DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new ArgumentException(
                "The maintenance window end must be strictly later " +
                "than its start instant.",
                nameof(end));
        }

        Start = start;
        End = end;
    }
}
