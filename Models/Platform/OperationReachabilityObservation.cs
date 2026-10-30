using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable caller-supplied check observation associated with
/// an operation, source, specification, and declared timestamp.
/// Does not authenticate provenance, verify post-execution timing,
/// perform a network probe, or establish operation success.
/// </summary>
public sealed class OperationReachabilityObservation
{
    public OperationCorrelationId Operation { get; }

    public OperationTarget Source { get; }

    public OperationReachabilityCheckSpecification Specification
    {
        get;
    }

    public DateTimeOffset ObservedAt { get; }

    public OperationReachabilityObservationDisposition Disposition
    {
        get;
    }

    public OperationReachabilityObservation(
        OperationCorrelationId operation,
        OperationTarget source,
        OperationReachabilityCheckSpecification specification,
        DateTimeOffset observedAt,
        OperationReachabilityObservationDisposition disposition)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);

        if (observedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAt),
                "A nondefault observation timestamp is required.");
        }

        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(
                nameof(disposition),
                disposition,
                "Undefined reachability observation disposition.");
        }

        Operation = operation;
        Source = source;
        Specification = specification;
        ObservedAt = observedAt;
        Disposition = disposition;
    }
}
