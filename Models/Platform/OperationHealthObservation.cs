using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable caller-supplied health observation associated with an
/// operation, target, and declared observation timestamp.
///
/// Does not perform provider discovery, authenticate the observation
/// source, prove post-execution timing, or verify operation success.
/// </summary>
public sealed class OperationHealthObservation
{
    public OperationCorrelationId Operation { get; }

    public OperationTarget Target { get; }

    public DateTimeOffset ObservedAt { get; }

    public OperationHealthObservationDisposition Disposition { get; }

    public OperationHealthStatus? Health { get; }

    public OperationHealthObservation(
        OperationCorrelationId operation,
        OperationTarget target,
        DateTimeOffset observedAt,
        OperationHealthObservationDisposition disposition,
        OperationHealthStatus? health = null)
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

        if (disposition != OperationHealthObservationDisposition.Unknown &&
            disposition != OperationHealthObservationDisposition.Observed &&
            disposition != OperationHealthObservationDisposition.Unavailable &&
            disposition != OperationHealthObservationDisposition.NotAssessed)
        {
            throw new ArgumentOutOfRangeException(
                nameof(disposition),
                disposition,
                "Undefined health observation disposition.");
        }

        if (disposition == OperationHealthObservationDisposition.Observed)
        {
            if (!health.HasValue)
            {
                throw new ArgumentException(
                    "An observed resource requires a reported health value.",
                    nameof(health));
            }

            if (health.Value != OperationHealthStatus.Healthy &&
                health.Value != OperationHealthStatus.Degraded &&
                health.Value != OperationHealthStatus.Unhealthy)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(health),
                    health,
                    "An observed resource requires known reportable health.");
            }
        }
        else if (health.HasValue)
        {
            throw new ArgumentException(
                "Only observed health may include a reported health value.",
                nameof(health));
        }

        Operation = operation;
        Target = target;
        ObservedAt = observedAt;
        Disposition = disposition;
        Health = health;
    }
}
