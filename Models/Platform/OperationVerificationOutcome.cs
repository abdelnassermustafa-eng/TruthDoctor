using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable local summary of required verification comparisons.
/// Does not authenticate observations, establish temporal provenance,
/// contact providers, or assert successful operation execution.
/// </summary>
public sealed class OperationVerificationOutcome
{
    public OperationCorrelationId Operation { get; }
    public OperationResourceScope Scope { get; }
    public IReadOnlyList<OperationVerificationRequirement> Requirements
    {
        get;
    }
    public IReadOnlyList<OperationVerificationResult> Results { get; }
    public OperationVerificationOutcomeDisposition Disposition { get; }

    private OperationVerificationOutcome(
        OperationCorrelationId operation,
        OperationResourceScope scope,
        IReadOnlyList<OperationVerificationRequirement> requirements,
        IReadOnlyList<OperationVerificationResult> results,
        OperationVerificationOutcomeDisposition disposition)
    {
        Operation = operation;
        Scope = scope;
        Requirements = requirements;
        Results = results;
        Disposition = disposition;
    }

    public static OperationVerificationOutcome Evaluate(
        OperationCorrelationId operation,
        OperationResourceScope scope,
        IEnumerable<OperationVerificationRequirement> requirements,
        IEnumerable<OperationVerificationResult> results)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(results);

        if (string.IsNullOrWhiteSpace(operation.Value))
            throw new ArgumentException(
                "Operation correlation must be declared.",
                nameof(operation));

        var declared = requirements.ToArray();
        var observed = results.ToArray();

        if (declared.Any(x => x is null) ||
            observed.Any(x => x is null))
            throw new ArgumentException(
                "Verification collections cannot contain null elements.");

        var requirementSnapshot =
            new ReadOnlyCollection<OperationVerificationRequirement>(
                declared);
        var resultSnapshot =
            new ReadOnlyCollection<OperationVerificationResult>(
                observed);

        var disposition = Classify(
            operation, scope, declared, observed);

        return new OperationVerificationOutcome(
            operation, scope,
            requirementSnapshot, resultSnapshot, disposition);
    }

    private static OperationVerificationOutcomeDisposition Classify(
        OperationCorrelationId operation,
        OperationResourceScope scope,
        OperationVerificationRequirement[] requirements,
        OperationVerificationResult[] results)
    {
        if (requirements.Length == 0)
        {
            return results.Length == 0
                ? OperationVerificationOutcomeDisposition.NoRequirements
                : OperationVerificationOutcomeDisposition.Conflict;
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var requirement in requirements)
        {
            if (!scope.Matches(requirement.Scope.Target))
                return OperationVerificationOutcomeDisposition.Conflict;

            if (!keys.Add(requirement.CheckId))
                return OperationVerificationOutcomeDisposition.Conflict;
        }

        var byId = new Dictionary<string,
            OperationVerificationResult>(StringComparer.Ordinal);

        foreach (var result in results)
        {
            if (!byId.TryAdd(result.CheckId, result))
                return OperationVerificationOutcomeDisposition.Conflict;

            if (!string.Equals(
                    result.Operation.Value, operation.Value,
                    StringComparison.Ordinal) ||
                !scope.Matches(result.Scope.Target))
                return OperationVerificationOutcomeDisposition.Conflict;
        }

        foreach (var result in results)
        {
            var requirement = requirements.FirstOrDefault(
                x => string.Equals(
                    x.CheckId, result.CheckId,
                    StringComparison.Ordinal));

            if (requirement is null ||
                !requirement.CorrespondsTo(result))
                return OperationVerificationOutcomeDisposition.Conflict;
        }

        if (requirements.Any(
            x => !byId.ContainsKey(x.CheckId)))
            return OperationVerificationOutcomeDisposition.Incomplete;

        if (results.Any(x => IsInconsistent(x)))
            return OperationVerificationOutcomeDisposition.Inconsistent;

        if (results.Any(
            x => !string.Equals(
                x.DispositionName, "Matched",
                StringComparison.Ordinal)))
            return OperationVerificationOutcomeDisposition.Unresolved;

        return OperationVerificationOutcomeDisposition.LocallyConsistent;
    }

    private static bool IsInconsistent(OperationVerificationResult result)
    {
        if (string.Equals(
            result.DispositionName, "Mismatched",
            StringComparison.Ordinal))
            return true;

        return result.Family ==
            OperationVerificationFamily.Dependency &&
            string.Equals(
                result.DispositionName, "Absent",
                StringComparison.Ordinal);
    }
}
