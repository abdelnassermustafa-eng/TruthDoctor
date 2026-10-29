using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Caller-supplied observation of a directed relationship between two
/// exactly identified resources, associated with a logical operation.
/// Observed means reported present; Absent means explicitly reported absent.
/// No graph query, provenance authentication, post-execution timing proof,
/// provider interaction or verified operation success is established.
/// </summary>
public sealed class OperationDependencyObservation
{
    public OperationCorrelationId Operation { get; }

    public OperationTarget Subject { get; }

    public OperationTarget Dependency { get; }

    public string Relationship { get; }

    public DateTimeOffset ObservedAt { get; }

    public OperationDependencyObservationDisposition Disposition { get; }

    public OperationDependencyObservation(
        OperationCorrelationId operation,
        OperationTarget subject,
        OperationTarget dependency,
        string relationship,
        DateTimeOffset observedAt,
        OperationDependencyObservationDisposition disposition)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(dependency);

        Relationship = NormalizeRelationship(relationship);

        if (observedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAt),
                "A nondefault observation timestamp is required.");
        }

        if (disposition != OperationDependencyObservationDisposition.Unknown &&
            disposition != OperationDependencyObservationDisposition.Observed &&
            disposition != OperationDependencyObservationDisposition.Absent &&
            disposition != OperationDependencyObservationDisposition.Unavailable &&
            disposition != OperationDependencyObservationDisposition.NotAssessed)
        {
            throw new ArgumentOutOfRangeException(
                nameof(disposition),
                disposition,
                "Undefined dependency observation disposition.");
        }

        Operation = operation;
        Subject = subject;
        Dependency = dependency;
        ObservedAt = observedAt;
        Disposition = disposition;
    }

    private static string NormalizeRelationship(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank directed relationship is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Relationship cannot contain control characters.",
                    nameof(value));
            }
        }

        return normalized;
    }
}
