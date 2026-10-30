using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable caller-supplied service observation.
/// Timestamp and response status are declarations, not
/// authenticated evidence of execution or application health.
/// </summary>
public sealed class OperationServiceObservation
{
    public OperationCorrelationId Operation { get; }

    public OperationTarget Source { get; }

    public OperationServiceCheckSpecification Specification { get; }

    public DateTimeOffset ObservedAt { get; }

    public OperationServiceObservationDisposition Disposition { get; }

    public int? HttpStatusCode { get; }

    public OperationServiceObservation(
        OperationCorrelationId operation,
        OperationTarget source,
        OperationServiceCheckSpecification specification,
        DateTimeOffset observedAt,
        OperationServiceObservationDisposition disposition,
        int? httpStatusCode = null)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "Initialized operation correlation ID is required.",
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);

        if (observedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAt),
                "Nondefault observation timestamp is required.");
        }

        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(
                nameof(disposition),
                disposition,
                "Undefined service observation state.");
        }

        if (httpStatusCode.HasValue &&
            (httpStatusCode.Value < 100 ||
             httpStatusCode.Value > 599))
        {
            throw new ArgumentOutOfRangeException(
                nameof(httpStatusCode),
                httpStatusCode,
                "Observed HTTP status must be between 100 and 599.");
        }

        if (disposition == OperationServiceObservationDisposition.Responded)
        {
            if (!httpStatusCode.HasValue)
            {
                throw new ArgumentException(
                    "Responded requires an observed HTTP status.",
                    nameof(httpStatusCode));
            }
        }
        else if (httpStatusCode.HasValue)
        {
            throw new ArgumentException(
                "A non-response state cannot contain an HTTP status.",
                nameof(httpStatusCode));
        }

        Operation = operation;
        Source = source;
        Specification = specification;
        ObservedAt = observedAt;
        Disposition = disposition;
        HttpStatusCode = httpStatusCode;
    }
}
