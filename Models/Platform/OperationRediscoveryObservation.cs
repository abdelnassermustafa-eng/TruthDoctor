using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable declaration of a caller-supplied rediscovery observation
/// for an identified operation and resource target.
///
/// Does not perform discovery, authenticate the observation source,
/// establish that discovery occurred after execution, compare expected
/// and observed states, or establish verified operation success.
/// </summary>
public sealed class OperationRediscoveryObservation
{
    public OperationCorrelationId Operation { get; }

    public OperationTarget Target { get; }

    public DateTimeOffset ObservedAt { get; }

    public OperationRediscoveryDisposition Disposition { get; }

    public string? ObservedState { get; }

    public OperationRediscoveryObservation(
        OperationCorrelationId operation,
        OperationTarget target,
        DateTimeOffset observedAt,
        OperationRediscoveryDisposition disposition,
        string? observedState = null)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(target);

        if (observedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAt),
                "A nondefault observation timestamp is required.");
        }

        if (disposition != OperationRediscoveryDisposition.Unknown &&
            disposition != OperationRediscoveryDisposition.Observed &&
            disposition != OperationRediscoveryDisposition.Absent &&
            disposition != OperationRediscoveryDisposition.Unavailable)
        {
            throw new ArgumentOutOfRangeException(
                nameof(disposition),
                disposition,
                "Unknown rediscovery disposition.");
        }

        string? normalizedState;

        if (disposition == OperationRediscoveryDisposition.Observed)
        {
            normalizedState = NormalizeObservedState(observedState);
        }
        else
        {
            if (observedState is not null)
            {
                throw new ArgumentException(
                    "Only observed resources may report a state.",
                    nameof(observedState));
            }

            normalizedState = null;
        }

        Operation = operation;
        Target = target;
        ObservedAt = observedAt;
        Disposition = disposition;
        ObservedState = normalizedState;
    }

    private static string NormalizeObservedState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "An observed resource requires a nonblank state.",
                nameof(value));
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Observed state cannot contain control characters.",
                    nameof(value));
            }
        }

        return normalized;
    }
}
