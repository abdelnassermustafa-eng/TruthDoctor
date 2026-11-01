using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of a history event with a
/// caller-supplied rediscovery observation designated as before-state
/// context.
///
/// Both supplied objects are preserved by reference. The existing
/// OperationRediscoveryObservation owns operation identity, target,
/// timestamp, disposition, and nullable observed-state validation.
///
/// This association does not establish that observation preceded an
/// operation, independently verify provenance or resource state,
/// compare before and after values, or establish execution, success,
/// or a resource change. It does not query providers or persist evidence.
/// </summary>
public sealed class OperationHistoryBeforeStateAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationRediscoveryObservation Observation { get; }

    public OperationHistoryBeforeStateAttribution(
        OperationHistoryEvent historyEvent,
        OperationRediscoveryObservation observation)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        ArgumentNullException.ThrowIfNull(observation);

        Event = historyEvent;
        Observation = observation;
    }
}
