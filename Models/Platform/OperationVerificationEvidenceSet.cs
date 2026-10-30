using System;
using System.Collections.Generic;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, declaration-ordered set of caller-declared verification
/// evidence for one operation and one exactly identified resource scope.
///
/// Validates local identity and collection consistency only.
/// Does not authenticate evidence, establish freshness, enforce verified
/// success, persist records, or collect observations.
/// </summary>
public sealed class OperationVerificationEvidenceSet
{
    public OperationCorrelationId Operation { get; }

    public OperationResourceScope Scope { get; }

    public IReadOnlyList<OperationVerificationEvidenceRecord> Records
    {
        get;
    }

    public OperationVerificationEvidenceSet(
        OperationCorrelationId operation,
        OperationResourceScope scope,
        IEnumerable<OperationVerificationEvidenceRecord> records)
    {
        if (string.IsNullOrWhiteSpace(operation.Value))
        {
            throw new ArgumentException(
                "An initialized operation correlation ID is required.",
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(records);

        var snapshot = new List<OperationVerificationEvidenceRecord>();
        var seenEvidenceIds = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (record is null)
            {
                throw new ArgumentException(
                    "Evidence records cannot contain null entries.",
                    nameof(records));
            }

            if (!string.Equals(
                    record.Operation.Value,
                    operation.Value,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Evidence belongs to a different operation.",
                    nameof(records));
            }

            if (!scope.Matches(record.Scope.Target))
            {
                throw new ArgumentException(
                    "Evidence belongs to a different resource scope.",
                    nameof(records));
            }

            if (!seenEvidenceIds.Add(record.EvidenceId))
            {
                throw new ArgumentException(
                    "Duplicate evidence IDs are not allowed.",
                    nameof(records));
            }

            snapshot.Add(record);
        }

        Operation = operation;
        Scope = scope;
        Records = snapshot.AsReadOnly();
    }
}
