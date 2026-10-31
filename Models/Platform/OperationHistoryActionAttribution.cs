using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of a history event with
/// a caller-declared action description.
///
/// Action text is descriptive, not an executable command. This
/// declaration does not establish initiation, attempted execution,
/// completed execution, success, approval, authorization, or
/// independently verified historical truth.
///
/// Constructing this declaration does not invoke providers,
/// dispatch commands, mutate resources, or persist evidence.
/// </summary>
public sealed class OperationHistoryActionAttribution
{
    public OperationHistoryEvent Event { get; }

    public string Action { get; }

    public OperationHistoryActionAttribution(
        OperationHistoryEvent historyEvent,
        string action)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(
                "A nonblank history action is required.",
                nameof(action));
        }

        var normalizedAction = action.Trim();

        foreach (var character in normalizedAction)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "History action text cannot contain control characters.",
                    nameof(action));
            }
        }

        Event = historyEvent;
        Action = normalizedAction;
    }
}
