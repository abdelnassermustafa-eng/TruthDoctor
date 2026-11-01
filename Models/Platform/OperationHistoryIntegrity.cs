using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Explicit caller-reported status for a history event.
///
/// NotAssessed does not represent a completed integrity assessment.
/// ReportedUnchanged and ReportedChanged preserve caller assertions.
/// These values are not independently authenticated or verified.
/// </summary>
public enum OperationHistoryIntegrityStatus
{
    NotAssessed = 0,
    ReportedUnchanged = 1,
    ReportedChanged = 2
}

/// <summary>
/// Immutable, provider-neutral association of an existing operation
/// history event with an explicitly caller-reported integrity status.
///
/// Preserves the original event reference and exact status value.
/// Does not infer integrity from identity, verification evidence,
/// retention, timestamps, or other history metadata.
///
/// Creating this declaration does not authenticate historical truth,
/// calculate or validate digests, check signatures or hash chains,
/// establish tamper-evident storage, enforce append-only history,
/// authorize actions, contact providers, persist or alter records,
/// evaluate compliance, or perform background work.
/// </summary>
public sealed class OperationHistoryIntegrity
{
    public OperationHistoryEvent Event { get; }

    public OperationHistoryIntegrityStatus Status { get; }

    public OperationHistoryIntegrity(
        OperationHistoryEvent historyEvent,
        OperationHistoryIntegrityStatus status)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException(
                "A defined reported-integrity status is required.",
                nameof(status));
        }

        Event = historyEvent;
        Status = status;
    }
}
