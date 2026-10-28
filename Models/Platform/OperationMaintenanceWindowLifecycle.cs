using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of one reported
/// maintenance-window lifecycle snapshot.
///
/// Status and observation time are supplied explicitly by a caller.
/// This type does not verify observation provenance, infer status
/// from time boundaries, or perform lifecycle transitions.
///
/// It does not approve work, establish authorization, schedule
/// operations, execute provider actions, or enforce runtime policy.
/// </summary>
public sealed class OperationMaintenanceWindowLifecycle
{
    public OperationCorrelationId Operation { get; }

    public OperationMaintenanceWindow Window { get; }

    public OperationMaintenanceWindowStatus Status { get; }

    public DateTimeOffset ObservedAt { get; }

    public OperationMaintenanceWindowLifecycle(
        OperationCorrelationId operation,
        OperationMaintenanceWindow window,
        OperationMaintenanceWindowStatus status,
        DateTimeOffset observedAt)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(window);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException(
                "A defined maintenance-window status is required.",
                nameof(status));
        }

        Operation = operation;
        Window = window;
        Status = status;
        ObservedAt = observedAt;
    }
}
