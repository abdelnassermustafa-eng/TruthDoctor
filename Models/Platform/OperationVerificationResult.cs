using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable adapter for one already evaluated local comparison.
/// Keeps the original typed comparison and its disposition intact.
/// </summary>
public sealed class OperationVerificationResult
{
    public OperationVerificationFamily Family { get; }
    public string CheckId { get; }
    public string DispositionName { get; }
    public object OriginalComparison { get; }

    internal OperationCorrelationId Operation { get; }
    internal OperationResourceScope Scope { get; }
    internal OperationOutcomeCondition Condition { get; }
    internal object? Detail { get; }
    internal OperationTarget? Dependency { get; }
    internal string? Relationship { get; }

    private OperationVerificationResult(
        OperationVerificationFamily family,
        string checkId,
        string dispositionName,
        object comparison,
        OperationCorrelationId operation,
        OperationResourceScope scope,
        OperationOutcomeCondition condition,
        object? detail = null,
        OperationTarget? dependency = null,
        string? relationship = null)
    {
        if (string.IsNullOrWhiteSpace(checkId))
            throw new ArgumentException(
                "A nonblank check ID is required.", nameof(checkId));

        foreach (var character in checkId)
            if (char.IsControl(character))
                throw new ArgumentException(
                    "Check IDs cannot contain control characters.",
                    nameof(checkId));

        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(condition);

        Family = family;
        CheckId = checkId.Trim();
        DispositionName = dispositionName;
        OriginalComparison = comparison;
        Operation = operation;
        Scope = scope;
        Condition = condition;
        Detail = detail;
        Dependency = dependency;
        Relationship = relationship;
    }

    public static OperationVerificationResult FromResourceState(
        string checkId, OperationResourceStateComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return new(OperationVerificationFamily.ResourceState,
            checkId, comparison.Disposition.ToString(), comparison,
            comparison.Observation.Operation,
            comparison.Scope, comparison.Condition);
    }

    public static OperationVerificationResult FromHealth(
        string checkId, OperationHealthComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return new(OperationVerificationFamily.Health,
            checkId, comparison.Disposition.ToString(), comparison,
            comparison.Observation.Operation,
            comparison.Scope, comparison.Condition);
    }

    public static OperationVerificationResult FromDependency(
        string checkId, OperationDependencyComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return new(OperationVerificationFamily.Dependency,
            checkId, comparison.Disposition.ToString(), comparison,
            comparison.Observation.Operation,
            comparison.Scope, comparison.Condition,
            dependency: comparison.Dependency,
            relationship: comparison.Relationship);
    }

    public static OperationVerificationResult FromReachability(
        string checkId, OperationReachabilityComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return new(OperationVerificationFamily.Reachability,
            checkId, comparison.Disposition.ToString(), comparison,
            comparison.Observation.Operation,
            comparison.SourceScope, comparison.Condition,
            detail: comparison.Specification);
    }

    public static OperationVerificationResult FromService(
        string checkId, OperationServiceComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return new(OperationVerificationFamily.Service,
            checkId, comparison.Disposition.ToString(), comparison,
            comparison.Observation.Operation,
            comparison.SourceScope, comparison.Condition,
            detail: comparison.Specification);
    }
}
