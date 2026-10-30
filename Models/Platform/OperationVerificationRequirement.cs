using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable declaration of one required local verification check.
/// A check ID is not an authorization, identity, or evidence substitute.
/// </summary>
public sealed class OperationVerificationRequirement
{
    public OperationVerificationFamily Family { get; }
    public string CheckId { get; }
    public OperationOutcomeCondition Condition { get; }
    public OperationResourceScope Scope { get; }

    internal object? Detail { get; }
    internal OperationTarget? Dependency { get; }
    internal string? Relationship { get; }

    private OperationVerificationRequirement(
        OperationVerificationFamily family,
        string checkId,
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        object? detail = null,
        OperationTarget? dependency = null,
        string? relationship = null)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(scope);

        if (string.IsNullOrWhiteSpace(checkId))
            throw new ArgumentException(
                "A nonblank check ID is required.", nameof(checkId));

        var normalized = checkId.Trim();
        foreach (var character in normalized)
            if (char.IsControl(character))
                throw new ArgumentException(
                    "Check IDs cannot contain control characters.",
                    nameof(checkId));

        Family = family;
        CheckId = normalized;
        Condition = condition;
        Scope = scope;
        Detail = detail;
        Dependency = dependency;
        Relationship = relationship;
    }

    public static OperationVerificationRequirement ForResourceState(
        string checkId,
        OperationOutcomeCondition condition,
        OperationResourceScope scope)
        => new(OperationVerificationFamily.ResourceState,
            checkId, condition, scope);

    public static OperationVerificationRequirement ForHealth(
        string checkId,
        OperationOutcomeCondition condition,
        OperationResourceScope scope)
        => new(OperationVerificationFamily.Health,
            checkId, condition, scope);

    public static OperationVerificationRequirement ForDependency(
        string checkId,
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        OperationTarget dependency,
        string relationship)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException(
                "Relationship is required.", nameof(relationship));

        var normalized = relationship.Trim();
        foreach (var character in normalized)
            if (char.IsControl(character))
                throw new ArgumentException(
                    "Relationship cannot contain control characters.",
                    nameof(relationship));

        return new(OperationVerificationFamily.Dependency,
            checkId, condition, scope,
            dependency: dependency, relationship: normalized);
    }

    public static OperationVerificationRequirement ForReachability(
        string checkId,
        OperationOutcomeCondition condition,
        OperationResourceScope sourceScope,
        OperationReachabilityCheckSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return new(OperationVerificationFamily.Reachability,
            checkId, condition, sourceScope, specification);
    }

    public static OperationVerificationRequirement ForService(
        string checkId,
        OperationOutcomeCondition condition,
        OperationResourceScope sourceScope,
        OperationServiceCheckSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return new(OperationVerificationFamily.Service,
            checkId, condition, sourceScope, specification);
    }

    internal bool CorrespondsTo(OperationVerificationResult result)
    {
        if (Family != result.Family ||
            !string.Equals(CheckId, result.CheckId,
                StringComparison.Ordinal) ||
            !string.Equals(Condition.Key,
                result.Condition.Key, StringComparison.Ordinal) ||
            !string.Equals(Condition.ExpectedValue,
                result.Condition.ExpectedValue, StringComparison.Ordinal) ||
            !Scope.Matches(result.Scope.Target))
            return false;

        return Family switch
        {
            OperationVerificationFamily.ResourceState => true,
            OperationVerificationFamily.Health => true,
            OperationVerificationFamily.Dependency =>
                Dependency is not null &&
                result.Dependency is not null &&
                new OperationResourceScope(Dependency)
                    .Matches(result.Dependency) &&
                string.Equals(Relationship, result.Relationship,
                    StringComparison.Ordinal),
            OperationVerificationFamily.Reachability =>
                Detail is OperationReachabilityCheckSpecification expected &&
                result.Detail is OperationReachabilityCheckSpecification actual &&
                ReachabilityMatches(expected, actual),
            OperationVerificationFamily.Service =>
                Detail is OperationServiceCheckSpecification serviceExpected &&
                result.Detail is OperationServiceCheckSpecification serviceActual &&
                ServiceMatches(serviceExpected, serviceActual),
            _ => false
        };
    }

    private static bool ReachabilityMatches(
        OperationReachabilityCheckSpecification a,
        OperationReachabilityCheckSpecification b)
        => string.Equals(a.Destination, b.Destination,
                StringComparison.Ordinal) &&
           string.Equals(a.Protocol.Identifier, b.Protocol.Identifier,
                StringComparison.Ordinal) &&
           a.DestinationPort == b.DestinationPort &&
           string.Equals(a.Context.InterfaceId, b.Context.InterfaceId,
                StringComparison.Ordinal) &&
           string.Equals(a.Context.Vrf, b.Context.Vrf,
                StringComparison.Ordinal) &&
           a.Context.VlanId == b.Context.VlanId &&
           string.Equals(a.Context.AddressFamily, b.Context.AddressFamily,
                StringComparison.Ordinal);

    private static bool ServiceMatches(
        OperationServiceCheckSpecification a,
        OperationServiceCheckSpecification b)
        => string.Equals(a.Protocol.Identifier, b.Protocol.Identifier,
                StringComparison.Ordinal) &&
           string.Equals(a.Endpoint.Uri, b.Endpoint.Uri,
                StringComparison.Ordinal) &&
           string.Equals(a.Method, b.Method,
                StringComparison.Ordinal) &&
           a.Criteria.ExpectedHttpStatusCode ==
               b.Criteria.ExpectedHttpStatusCode;
}
