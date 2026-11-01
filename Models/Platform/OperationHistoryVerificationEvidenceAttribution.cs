using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of an operation history event
/// with a caller-supplied verification evidence set.
///
/// Both supplied objects are preserved by reference. The existing
/// OperationVerificationEvidenceSet owns record identity, declaration
/// order, operation and resource-scope consistency, duplicate-ID
/// validation, and immutable evidence collection semantics.
///
/// This association does not authenticate either input or establish
/// historical truth, observation freshness, successful verification,
/// operation execution, or execution authorization. It does not enforce
/// new correlation between the event and evidence set, classify outcomes,
/// collect evidence, contact providers, or persist or retain records.
/// An empty evidence set remains empty and is not evidence of success.
/// </summary>
public sealed class OperationHistoryVerificationEvidenceAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationVerificationEvidenceSet EvidenceSet { get; }

    public OperationHistoryVerificationEvidenceAttribution(
        OperationHistoryEvent historyEvent,
        OperationVerificationEvidenceSet evidenceSet)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        ArgumentNullException.ThrowIfNull(evidenceSet);

        Event = historyEvent;
        EvidenceSet = evidenceSet;
    }
}
