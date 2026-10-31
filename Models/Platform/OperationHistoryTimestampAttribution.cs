using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of a history event with
/// caller-declared occurrence and recording timestamps.
///
/// Both DateTimeOffset values are preserved, including their offsets.
/// Either may precede the other and explicit defaults are accepted.
///
/// This declaration does not independently prove historical truth,
/// read the system clock, authenticate timestamp sources, execute
/// operations, authorize work, or persist audit evidence.
/// </summary>
public sealed class OperationHistoryTimestampAttribution
{
    public OperationHistoryEvent Event { get; }

    public DateTimeOffset OccurredAt { get; }

    public DateTimeOffset RecordedAt { get; }

    public OperationHistoryTimestampAttribution(
        OperationHistoryEvent historyEvent,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);

        Event = historyEvent;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
    }
}
