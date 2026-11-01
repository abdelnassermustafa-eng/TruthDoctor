using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of an operation history event
/// with caller-supplied reported approval lifecycle context.
///
/// Both supplied objects are preserved by reference. The existing
/// OperationApprovalLifecycle owns approval identity, approval chain,
/// reported status, observation time, expiry and revocation metadata,
/// and their established validation rules.
///
/// This association does not authenticate approval or event provenance,
/// establish completed approval or effective execution authorization,
/// validate operation correlation, evaluate clocks, calculate expiry,
/// process revocation, or convert external approval statuses.
/// It does not contact providers, execute operations, or persist evidence.
/// </summary>
public sealed class OperationHistoryApprovalAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationApprovalLifecycle Lifecycle { get; }

    public OperationHistoryApprovalAttribution(
        OperationHistoryEvent historyEvent,
        OperationApprovalLifecycle lifecycle)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        ArgumentNullException.ThrowIfNull(lifecycle);

        Event = historyEvent;
        Lifecycle = lifecycle;
    }
}
