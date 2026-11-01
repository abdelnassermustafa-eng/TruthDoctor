using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral association of a history event with
/// caller-declared operation intent.
///
/// Both supplied objects are preserved by reference. OperationIntent
/// owns capability identity, required description, and optional reason
/// validation; an omitted reason remains valid.
///
/// This association does not independently verify motive, establish
/// approval or authorization, demonstrate execution or success,
/// or establish any observed resource outcome. It does not plan,
/// invoke providers, execute operations, or persist evidence.
/// </summary>
public sealed class OperationHistoryIntentAttribution
{
    public OperationHistoryEvent Event { get; }

    public OperationIntent Intent { get; }

    public OperationHistoryIntentAttribution(
        OperationHistoryEvent historyEvent,
        OperationIntent intent)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        ArgumentNullException.ThrowIfNull(intent);

        Event = historyEvent;
        Intent = intent;
    }
}
