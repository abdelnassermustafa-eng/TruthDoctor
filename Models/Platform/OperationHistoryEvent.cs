using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of an operation history event.
///
/// The event identifier, operation correlation, and event kind are
/// supplied by the caller. They are not independently authenticated.
///
/// Creating this declaration does not establish historical truth,
/// verify an operation's execution, imply operational success,
/// authorize or approve work, persist evidence, or execute actions.
///
/// Actor identity, timestamps, detailed actions, targets, intent,
/// state, effects, approval records, verification evidence,
/// retention, and audit integrity belong to separate history
/// contracts and roadmap submodules.
/// </summary>
public sealed class OperationHistoryEvent
{
    public string EventId { get; }

    public OperationCorrelationId Operation { get; }

    public string EventKind { get; }

    public OperationHistoryEvent(
        string eventId,
        OperationCorrelationId operation,
        string eventKind)
    {
        var normalizedEventId = NormalizeRequired(
            eventId, nameof(eventId));

        if (string.IsNullOrWhiteSpace(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        var normalizedEventKind = NormalizeRequired(
            eventKind, nameof(eventKind));

        EventId = normalizedEventId;
        Operation = operation;
        EventKind = normalizedEventKind;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank history-event value is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "History-event text cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
