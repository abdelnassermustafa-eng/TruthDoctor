using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Exact, provider-neutral filtering over caller-supplied history events.
/// Matching uses existing event and correlation identities, not Workbench
/// free-text search semantics. Results preserve references, order, and
/// duplicate entries in a read-only collection snapshot.
///
/// This contract does not discover or authenticate history, query
/// providers, read or write persistence, authorize or execute operations,
/// or infer historical truth, verification, or operational success.
/// </summary>
public static class OperationHistoryFilter
{
    public static IReadOnlyList<OperationHistoryEvent> Filter(
        IEnumerable<OperationHistoryEvent> events,
        string? eventId = null,
        OperationCorrelationId? operation = null,
        string? eventKind = null)
    {
        ArgumentNullException.ThrowIfNull(events);

        ValidateTextCriterion(eventId, nameof(eventId));
        ValidateTextCriterion(eventKind, nameof(eventKind));

        if (operation.HasValue &&
            string.IsNullOrWhiteSpace(operation.Value.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        var results = new List<OperationHistoryEvent>();

        foreach (var historyEvent in events)
        {
            if (historyEvent is null)
            {
                throw new ArgumentException(
                    "History collections cannot contain null entries.",
                    nameof(events));
            }

            if (eventId is not null &&
                !string.Equals(
                    historyEvent.EventId,
                    eventId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (operation.HasValue &&
                historyEvent.Operation != operation.Value)
            {
                continue;
            }

            if (eventKind is not null &&
                !string.Equals(
                    historyEvent.EventKind,
                    eventKind,
                    StringComparison.Ordinal))
            {
                continue;
            }

            results.Add(historyEvent);
        }

        return results.AsReadOnly();
    }

    private static void ValidateTextCriterion(
        string? value,
        string parameterName)
    {
        if (value is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A supplied history-filter criterion cannot be blank.",
                parameterName);
        }

        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "History-filter criteria cannot contain control characters.",
                    parameterName);
            }
        }
    }
}
