using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral record describing caller-declared
/// supporting evidence for an already evaluated verification result.
///
/// Identity, family, scope, and observation time are derived from the
/// original typed comparison, never supplied independently.
///
/// Does not authenticate a source, establish freshness, collect evidence,
/// contact a provider, or verify successful operation execution.
/// </summary>
public sealed class OperationVerificationEvidenceRecord
{
    public string EvidenceId { get; }

    public string SourceId { get; }

    public DateTimeOffset RecordedAt { get; }

    public DateTimeOffset ObservedAt { get; }

    public OperationCorrelationId Operation { get; }

    public OperationResourceScope Scope { get; }

    public OperationVerificationFamily Family { get; }

    public string CheckId { get; }

    public OperationVerificationResult Result { get; }

    public OperationVerificationEvidenceRecord(
        string evidenceId,
        string sourceId,
        DateTimeOffset recordedAt,
        OperationVerificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        EvidenceId = NormalizeRequired(evidenceId, nameof(evidenceId));
        SourceId = NormalizeRequired(sourceId, nameof(sourceId));

        if (recordedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordedAt),
                "A nondefault evidence recording time is required.");
        }

        var observedAt = result.OriginalComparison switch
        {
            OperationResourceStateComparison comparison
                when result.Family == OperationVerificationFamily.ResourceState
                => comparison.Observation.ObservedAt,

            OperationHealthComparison comparison
                when result.Family == OperationVerificationFamily.Health
                => comparison.Observation.ObservedAt,

            OperationDependencyComparison comparison
                when result.Family == OperationVerificationFamily.Dependency
                => comparison.Observation.ObservedAt,

            OperationReachabilityComparison comparison
                when result.Family == OperationVerificationFamily.Reachability
                => comparison.Observation.ObservedAt,

            OperationServiceComparison comparison
                when result.Family == OperationVerificationFamily.Service
                => comparison.Observation.ObservedAt,

            _ => throw new ArgumentException(
                "Verification family must correspond to a supported " +
                "original typed comparison.",
                nameof(result))
        };

        if (string.IsNullOrWhiteSpace(result.Operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation is required.",
                nameof(result));
        }

        ArgumentNullException.ThrowIfNull(result.Scope);

        if (string.IsNullOrWhiteSpace(result.CheckId))
        {
            throw new ArgumentException(
                "A verification check ID is required.",
                nameof(result));
        }

        RecordedAt = recordedAt;
        ObservedAt = observedAt;
        Operation = result.Operation;
        Scope = result.Scope;
        Family = result.Family;
        CheckId = result.CheckId;
        Result = result;
    }

    private static string NormalizeRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A nonblank evidence identifier is required.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Evidence identifiers cannot contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}
