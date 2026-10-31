using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral attribution of a reported history event
/// to a declared human or service reporter.
///
/// The event and reporter are supplied by the caller. Attribution does
/// not authenticate the reporter, establish who initiated or executed
/// an action, grant authorization, or verify historical truth.
///
/// Constructing this declaration does not perform an operation,
/// infer success, persist audit evidence, or alter the history event.
/// </summary>
public sealed class OperationHistoryActorAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationActorIdentity Reporter { get; }

    public OperationHistoryActorAttribution(
        OperationHistoryEvent historyEvent,
        OperationActorIdentity reporter)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);

        if (string.IsNullOrWhiteSpace(reporter.Value) ||
            !Enum.IsDefined(reporter.Kind))
        {
            throw new ArgumentException(
                "An initialized and defined reporter identity is required.",
                nameof(reporter));
        }

        Event = historyEvent;
        Reporter = reporter;
    }
}
