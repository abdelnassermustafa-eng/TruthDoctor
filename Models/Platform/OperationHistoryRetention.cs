using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Caller-declared history-retention mode.
///
/// Indefinite explicitly declares no finite retention deadline.
/// Until explicitly associates a supplied retention deadline.
/// Neither mode proves an applicable policy, legal compliance,
/// or permission to delete records.
/// </summary>
public enum OperationHistoryRetentionMode
{
    Indefinite = 0,
    Until = 1
}

/// <summary>
/// Immutable, provider-neutral association of one history event with
/// an explicitly caller-declared retention mode and optional deadline.
///
/// The event reference and supplied DateTimeOffset are preserved.
/// Occurrence or recording timestamps are not interpreted as retention
/// deadlines, and no system clock is consulted.
///
/// Construction does not enforce retention, establish expiration,
/// delete or archive records, contact providers, persist evidence,
/// authenticate a policy, implement legal holds, or establish
/// audit integrity or operational authorization.
/// </summary>
public sealed class OperationHistoryRetention
{
    public OperationHistoryEvent Event { get; }

    public OperationHistoryRetentionMode Mode { get; }

    public DateTimeOffset? RetainUntil { get; }

    public OperationHistoryRetention(
        OperationHistoryEvent historyEvent,
        OperationHistoryRetentionMode mode,
        DateTimeOffset? retainUntil)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentException(
                "A defined history-retention mode is required.",
                nameof(mode));
        }

        if (mode == OperationHistoryRetentionMode.Indefinite &&
            retainUntil.HasValue)
        {
            throw new ArgumentException(
                "Indefinite retention cannot specify a deadline.",
                nameof(retainUntil));
        }

        if (mode == OperationHistoryRetentionMode.Until &&
            !retainUntil.HasValue)
        {
            throw new ArgumentException(
                "Finite retention requires an explicit deadline.",
                nameof(retainUntil));
        }

        Event = historyEvent;
        Mode = mode;
        RetainUntil = retainUntil;
    }
}
