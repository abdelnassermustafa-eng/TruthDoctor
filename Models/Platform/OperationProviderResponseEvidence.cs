using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral declaration of a reported provider response
/// for one identified operation step.
///
/// Values are supplied by the caller; this contract does not establish
/// that a provider was contacted or that a response is authentic.
/// An acknowledged response is not verified resource-state success.
/// Does not execute, authorize, rediscover, verify, or collect evidence.
/// No raw provider response payload is retained.
/// </summary>
public sealed class OperationProviderResponseEvidence
{
    public OperationCorrelationId Operation { get; }

    public string StepId { get; }

    public string ProviderId { get; }

    public DateTimeOffset RecordedAt { get; }

    public OperationProviderResponseDisposition Disposition { get; }

    public OperationProviderResponseEvidence(
        OperationCorrelationId operation,
        string stepId,
        string providerId,
        DateTimeOffset recordedAt,
        OperationProviderResponseDisposition disposition)
    {
        if (string.IsNullOrEmpty(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        var normalizedStepId =
            NormalizeRequired(stepId, nameof(stepId));
        var normalizedProviderId =
            NormalizeRequired(providerId, nameof(providerId));

        if (recordedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordedAt),
                "A nondefault evidence recording time is required.");
        }

        if (disposition != OperationProviderResponseDisposition.Unknown &&
            disposition != OperationProviderResponseDisposition.Acknowledged &&
            disposition != OperationProviderResponseDisposition.Rejected)
        {
            throw new ArgumentOutOfRangeException(
                nameof(disposition), disposition,
                "Unknown provider response disposition.");
        }

        Operation = operation;
        StepId = normalizedStepId;
        ProviderId = normalizedProviderId;
        RecordedAt = recordedAt;
        Disposition = disposition;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank provider response identity is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Provider response identity cannot contain " +
                    "control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
