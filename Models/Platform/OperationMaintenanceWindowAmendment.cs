using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration requesting a change to
/// an existing maintenance-window interval.
///
/// The original and proposed intervals are preserved as separate
/// declarations. Creating an amendment does not approve the request,
/// modify an effective window, establish authorization, reschedule
/// work, dispatch operations, or enforce runtime policy.
///
/// Extension, shortening, and rescheduling are descriptions of
/// proposed boundary differences, not effective lifecycle changes.
/// </summary>
public sealed class OperationMaintenanceWindowAmendment
{
    public OperationCorrelationId Operation { get; }

    public OperationActorIdentity Requester { get; }

    public OperationMaintenanceWindow OriginalWindow { get; }

    public OperationMaintenanceWindow ProposedWindow { get; }

    public string Reason { get; }

    public OperationMaintenanceWindowAmendment(
        OperationCorrelationId operation,
        OperationActorIdentity requester,
        OperationMaintenanceWindow originalWindow,
        OperationMaintenanceWindow proposedWindow,
        string reason)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        if (string.IsNullOrEmpty(requester.Value) ||
            !Enum.IsDefined(requester.Kind))
        {
            throw new ArgumentException(
                "An initialized requester identity is required.",
                nameof(requester));
        }

        ArgumentNullException.ThrowIfNull(originalWindow);
        ArgumentNullException.ThrowIfNull(proposedWindow);

        if (originalWindow.Start == proposedWindow.Start &&
            originalWindow.End == proposedWindow.End)
        {
            throw new ArgumentException(
                "A proposed amendment must change at least one " +
                "maintenance-window boundary instant.",
                nameof(proposedWindow));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A nonblank amendment reason is required.",
                nameof(reason));
        }

        var normalizedReason = reason.Trim();

        foreach (var character in normalizedReason)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Amendment reasons cannot contain control characters.",
                    nameof(reason));
            }
        }

        Operation = operation;
        Requester = requester;
        OriginalWindow = originalWindow;
        ProposedWindow = proposedWindow;
        Reason = normalizedReason;
    }
}
