using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of a history event with a
/// separately caller-declared operation correlation identifier.
///
/// The event reference and supplied correlation value are preserved.
/// The existing OperationCorrelationId type owns identifier validation,
/// normalization, and case-sensitive value equality.
///
/// This association does not require the event's operation and the
/// supplied correlation to match. It does not authenticate historical
/// correspondence, generate or propagate identifiers, verify execution,
/// authorize operations, search history, contact providers, or persist
/// or retain records.
/// </summary>
public sealed class OperationHistoryCorrelationAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationCorrelationId Correlation { get; }

    public OperationHistoryCorrelationAttribution(
        OperationHistoryEvent historyEvent,
        OperationCorrelationId correlation)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);

        if (string.IsNullOrWhiteSpace(correlation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(correlation));
        }

        Event = historyEvent;
        Correlation = correlation;
    }
}
