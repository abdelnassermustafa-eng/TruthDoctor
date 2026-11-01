using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of a history event with
/// a caller-declared operation target.
///
/// Both supplied objects are preserved by reference. The existing
/// OperationTarget owns its six-dimensional identity validation.
///
/// This association does not prove resource discovery or existence,
/// provider reachability, modification, execution, success, approval,
/// authorization, or independently verified historical truth.
/// Constructing it does not contact providers or persist evidence.
/// </summary>
public sealed class OperationHistoryTargetAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationTarget Target { get; }

    public OperationHistoryTargetAttribution(
        OperationHistoryEvent historyEvent,
        OperationTarget target)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        ArgumentNullException.ThrowIfNull(target);

        Event = historyEvent;
        Target = target;
    }
}
