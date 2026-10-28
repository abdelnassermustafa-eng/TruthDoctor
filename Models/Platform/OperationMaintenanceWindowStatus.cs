namespace TruthDoctor.Models.Platform;

/// <summary>
/// Declarative, provider-neutral maintenance-window lifecycle status.
///
/// Values describe an explicitly reported window status.
/// They do not establish approval, authorization, scheduling,
/// operation completion, recovery outcomes, or execution rights.
///
/// No state transitions, clock evaluation, automatic expiration,
/// provider operations, or runtime enforcement are implemented here.
/// </summary>
public enum OperationMaintenanceWindowStatus
{
    /// <summary>No established lifecycle status.</summary>
    Unknown = 0,

    /// <summary>Window declared; approval is not implied.</summary>
    Planned = 1,

    /// <summary>Window reported active; execution is not implied.</summary>
    Active = 2,

    /// <summary>Window reported closed before its declared end.</summary>
    ClosedEarly = 3,

    /// <summary>Window administratively reported closed.</summary>
    Closed = 4,

    /// <summary>Window reported expired; no execution outcome implied.</summary>
    Expired = 5,

    /// <summary>Window reported cancelled.</summary>
    Cancelled = 6
}
