using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable local comparison of a declared directed dependency condition
/// with one caller-supplied relationship observation.
/// Only dependency.relationship, depends-on, and present/absent are supported.
/// This does not query topology, contact providers, authenticate provenance,
/// establish post-execution timing, or verify operation success.
/// </summary>
public sealed class OperationDependencyComparison
{
    public OperationOutcomeCondition Condition { get; }

    public OperationResourceScope Scope { get; }

    public OperationTarget Dependency { get; }

    public string Relationship { get; }

    public OperationDependencyObservation Observation { get; }

    public OperationDependencyComparisonDisposition Disposition { get; }

    private OperationDependencyComparison(
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        OperationTarget dependency,
        string relationship,
        OperationDependencyObservation observation,
        OperationDependencyComparisonDisposition disposition)
    {
        Condition = condition;
        Scope = scope;
        Dependency = dependency;
        Relationship = relationship;
        Observation = observation;
        Disposition = disposition;
    }

    public static OperationDependencyComparison Evaluate(
        OperationOutcomeCondition condition,
        OperationResourceScope scope,
        OperationTarget dependency,
        string relationship,
        OperationDependencyObservation observation)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(observation);

        if (string.IsNullOrWhiteSpace(relationship))
        {
            throw new ArgumentException(
                "A nonblank directed relationship is required.",
                nameof(relationship));
        }

        var normalizedRelationship = relationship.Trim();

        foreach (var character in normalizedRelationship)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Relationship cannot contain control characters.",
                    nameof(relationship));
            }
        }

        OperationDependencyComparisonDisposition disposition;

        if (!scope.Matches(observation.Subject))
        {
            disposition =
                OperationDependencyComparisonDisposition.SubjectMismatch;
        }
        else if (!new OperationResourceScope(dependency)
            .Matches(observation.Dependency))
        {
            disposition =
                OperationDependencyComparisonDisposition.DependencyMismatch;
        }
        else if (!string.Equals(
            normalizedRelationship, "depends-on",
            StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                observation.Relationship, "depends-on",
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                normalizedRelationship,
                observation.Relationship,
                StringComparison.OrdinalIgnoreCase))
        {
            disposition =
                OperationDependencyComparisonDisposition.RelationshipMismatch;
        }
        else if (!string.Equals(
            condition.Key,
            "dependency.relationship",
            StringComparison.OrdinalIgnoreCase) ||
            !TryParseExpectedPresence(
                condition.ExpectedValue, out var expectedPresent))
        {
            disposition =
                OperationDependencyComparisonDisposition.UnsupportedCondition;
        }
        else
        {
            disposition = observation.Disposition switch
            {
                OperationDependencyObservationDisposition.Unknown =>
                    OperationDependencyComparisonDisposition.NoObservation,

                OperationDependencyObservationDisposition.Unavailable =>
                    OperationDependencyComparisonDisposition.Unavailable,

                OperationDependencyObservationDisposition.NotAssessed =>
                    OperationDependencyComparisonDisposition.NotAssessed,

                OperationDependencyObservationDisposition.Observed =>
                    expectedPresent
                        ? OperationDependencyComparisonDisposition.Matched
                        : OperationDependencyComparisonDisposition.Mismatched,

                OperationDependencyObservationDisposition.Absent =>
                    expectedPresent
                        ? OperationDependencyComparisonDisposition.Absent
                        : OperationDependencyComparisonDisposition.Matched,

                _ => OperationDependencyComparisonDisposition.Unknown
            };
        }

        return new OperationDependencyComparison(
            condition,
            scope,
            dependency,
            normalizedRelationship,
            observation,
            disposition);
    }

    private static bool TryParseExpectedPresence(
        string value,
        out bool expectedPresent)
    {
        if (string.Equals(
            value, "present", StringComparison.OrdinalIgnoreCase))
        {
            expectedPresent = true;
            return true;
        }

        if (string.Equals(
            value, "absent", StringComparison.OrdinalIgnoreCase))
        {
            expectedPresent = false;
            return true;
        }

        expectedPresent = false;
        return false;
    }
}
