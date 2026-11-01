using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of an operation history event
/// with caller-supplied estimated impact context.
///
/// Both supplied objects are preserved by reference. The existing
/// OperationEstimatedImpact owns per-step impact estimates, declaration
/// order, duplicate-step validation, and immutable collection semantics.
///
/// This association does not establish that an operation was executed,
/// that an estimated effect occurred, that resource state changed, or
/// that any observed result was caused by the operation. It does not
/// compare before and after state, query providers, or persist evidence.
/// </summary>
public sealed class OperationHistoryImpactAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationEstimatedImpact EstimatedImpact { get; }

    public OperationHistoryImpactAttribution(
        OperationHistoryEvent historyEvent,
        OperationEstimatedImpact estimatedImpact)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        ArgumentNullException.ThrowIfNull(estimatedImpact);

        Event = historyEvent;
        EstimatedImpact = estimatedImpact;
    }
}
